using System.Security.Claims;
using System.Text;
using System.Threading;
using Asilifelis.Data;
using Asilifelis.Models;
using Asilifelis.Security;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Asilifelis.Controllers;

[Route("/api/[controller]")]
[ApiController]
public class AuthenticateController(ApplicationRepository repository, IFido2 fido2) : ControllerBase {
	private ApplicationRepository Repository { get; } = repository;
	private IFido2 Fido2 { get; } = fido2;

	[HttpPost("attestation/options")]
	public async ValueTask<Results<Ok<CredentialCreateOptions>, ProblemHttpResult>> PostCredentialOptions(
			[FromForm] string username,
			[FromForm] string? displayName,
			[FromForm] AttestationConveyancePreference attType,
			CancellationToken cancellationToken) {
		Fido2User user;
		List<PublicKeyCredentialDescriptor> existingCredentials = [];
		try {
			var actor = await Repository.GetActorAsync(username, cancellationToken);
			// an actor without an identity cannot be logged into, this might be because it's a system 
			// account or for other reasons it's method of logging in has been removed
			if (actor.Identity is null)
				return TypedResults.Problem("Username not valid");
			user = new Fido2User {
				Id = actor.Identity.Id,
				Name = actor.Username,
				DisplayName = actor.DisplayName
			};
			existingCredentials = actor.Identity.Credentials.Select(c => c.Descriptor).ToList();
		} catch (ActorNotFoundException) {
			user = new Fido2User {
				Id = Encoding.UTF8.GetBytes(username),
				Name = username,
				DisplayName = displayName ?? username
			};
		}
		
		var options = Fido2.RequestNewCredential(user, 
			existingCredentials, 
			AuthenticatorSelection.Default, 
			attType);

		HttpContext.Session.SetString("fido2.attestationOptions", options.ToJson());

		return TypedResults.Ok(options);
	}

	[HttpPost("attestation/make")]
	public async ValueTask<Results<Ok<MakeNewCredentialResult>, BadRequest<string>>> PostCredentialMake(
			AuthenticatorAttestationRawResponse attestationResponse, CancellationToken cancellationToken) {
		string? jsonOptions = HttpContext.Session.GetString("fido2.attestationOptions");
		if (jsonOptions is null)
			return TypedResults.BadRequest("No attestation options found, please first call " + 
				Url.Action(nameof(PostCredentialOptions)) + ".");
		HttpContext.Session.Remove("fido2.attestationOptions");

		var options = CredentialCreateOptions.FromJson(jsonOptions);

		MakeNewCredentialResult make;
		try {
			make = await Fido2.MakeNewCredentialAsync(attestationResponse, options, async (@params, token) => {
				string username = Encoding.UTF8.GetString(@params.User.Id);
				return (await Repository.IsUsernameTaken(username, token)) is false;
			}, cancellationToken);
		} catch (Fido2VerificationException ex) {
			return TypedResults.BadRequest(ex.Message);
		}
		
		string username = Encoding.UTF8.GetString(options.User.Id);
		Actor actor;
		try {
			actor = await Repository.GetActorAsync(username, cancellationToken);
		} catch (ActorNotFoundException) {
			actor = await Repository.CreateActorAsync(username, options.User.DisplayName,
				new UserIdentity { Id = options.User.Id, Credentials = [] }, cancellationToken);
		}
		actor.Identity!.Credentials.Add(new Credential {
			PublicKey = make.Result!.PublicKey,
			UserHandle = make.Result.User.Id,
			Descriptor = new PublicKeyCredentialDescriptor(make.Result.Id)
		});
		await Repository.UpdateActorAsync(actor, cancellationToken);

		return TypedResults.Ok(make);
	}

	[HttpPost("assertion/options")]
	public async ValueTask<Results<Ok<AssertionOptions>, NotFound<string>, ProblemHttpResult>> PostAssertionOptions(
			[FromForm] string username, 
			CancellationToken cancellationToken) {
		List<PublicKeyCredentialDescriptor> existingCredentials;
		try {
			var identity = await Repository.GetIdentityAsync(username, cancellationToken);
			existingCredentials = identity.Credentials.Select(c => c.Descriptor).ToList();
		} catch (ActorNotFoundException) {
			return TypedResults.NotFound("Username not found");
		}

		var options = Fido2.GetAssertionOptions(
			existingCredentials,
			UserVerificationRequirement.Preferred
		);

		HttpContext.Session.SetString("fido2.assertionOptions", options.ToJson());
		return TypedResults.Ok(options);
	}

	[HttpPost("assertion/make")]
	public async ValueTask<Results<SignInHttpResult, NotFound, BadRequest<string>>> PostAssertionMake(
			[FromBody] AuthenticatorAssertionRawResponse clientResponse, 
			CancellationToken cancellationToken) {
		string? jsonOptions = HttpContext.Session.GetString("fido2.assertionOptions");
		if (jsonOptions is null)
			return TypedResults.BadRequest("No attestation options found, please first call " + 
				Url.Action(nameof(PostAssertionOptions)) + ".");
		HttpContext.Session.Remove("fido2.assertionOptions");
		var options = AssertionOptions.FromJson(jsonOptions);

		Actor actor;
		try {
			actor = await Repository.GetActorByCredentialIdAsync(clientResponse.Id, cancellationToken);
		} catch (ActorNotFoundException) {
			return TypedResults.NotFound();
		}

		var identity = actor.Identity!;
		var credential = identity.Credentials.First(c => c.Descriptor.Id.SequenceEqual(clientResponse.Id)); 

		uint counter = identity.Counter;
		IsUserHandleOwnerOfCredentialIdAsync callback = async (@params, token) => {
			var c = await Repository.GetCredentialByUserHandleAsync(@params.UserHandle, token);
			return c is not null && c.Descriptor.Id.SequenceEqual(@params.CredentialId);
		};
		var result = await Fido2.MakeAssertionAsync(clientResponse, options, 
			credential.PublicKey, identity.Credentials.Select(c => c.PublicKey).ToList(), 
			counter, callback, cancellationToken: cancellationToken);

		// TODO increase counter
		if (result.Status is not "ok") return TypedResults.BadRequest(result.ErrorMessage);

		var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
			new Claim(ClaimTypes.Name, actor.DisplayName),
			new Claim(ClaimTypes.NameIdentifier, actor.Username)
		], CookieAuthenticationDefaults.AuthenticationScheme));

		return TypedResults.SignIn(claimsPrincipal, new AuthenticationProperties() {
			AllowRefresh = true,
		},  CookieAuthenticationDefaults.AuthenticationScheme);
	}

	[HttpGet("me")]
	public async ValueTask<Results<Ok<Actor>, BadRequest<string>, UnauthorizedHttpResult>> GetMeAsync() {
		if (User.Identity?.IsAuthenticated is not true) return TypedResults.Unauthorized();
		string? username = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (username is null) return TypedResults.BadRequest("Invalid Identity format, try signing in again.");

		try {
			var actor = await Repository.GetActorAsync(username);
			return TypedResults.Ok(new Actor {
				DisplayName = actor.DisplayName,
				Username = actor.Username,
				Identity = null
			});
		} catch (ActorNotFoundException) {
			return TypedResults.BadRequest("Invalid Identity format, try signing in again.");
		}
	}
}
