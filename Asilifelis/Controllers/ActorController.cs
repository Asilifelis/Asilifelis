using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Asilifelis.Data;
using Asilifelis.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Asilifelis.Controllers;

public record NoteCreateDto(
	[Required(AllowEmptyStrings = false), MinLength(1), MaxLength(4096)] 
	[property:JsonPropertyName("content")]
	string Content);

[ApiController]
[Route("/api/[controller]")]
public class ActorController(ApplicationRepository repository) : ControllerBase {
	private ApplicationRepository Repository { get; } = repository;
	
	[HttpPost("note")]
	public async ValueTask<Results<Ok, UnauthorizedHttpResult, BadRequest<string>, NotFound<string>>> PostNoteAsync(
		NoteCreateDto noteCreate, 
		CancellationToken cancellationToken) {
		try {
			if (User.Identity?.IsAuthenticated is not true) return TypedResults.Unauthorized();
			string? username = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (username is null)
				return TypedResults.BadRequest("Failed to look up Actor information, try signing in again");

			var actor = await Repository.GetActorAsync(username, cancellationToken);

			var note = new Note() {
				Author = actor,
				Content = noteCreate.Content
			};

			await Repository.PublishPostAsync(note, cancellationToken);

			return TypedResults.Ok();
		} catch (ActorNotFoundException) {
			return TypedResults.NotFound("User not found");
		}
	}

	[HttpGet("note")]
	public async ValueTask<Results<Ok<ICollection<Note>>, NotFound<string>, UnauthorizedHttpResult, BadRequest<string>>> GetNoteAsync(CancellationToken cancellationToken) {
		try {
			if (User.Identity?.IsAuthenticated is not true) return TypedResults.Unauthorized();
			string? username = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (username is null)
				return TypedResults.BadRequest("Failed to look up Actor information, try signing in again");

			var actor = await Repository.GetActorAsync(username, cancellationToken);
			return TypedResults.Ok(await Repository.GetNotesAsync(actor, cancellationToken: cancellationToken));
		} catch (ActorNotFoundException) {
			return TypedResults.NotFound("User not found");
		}
	}
}