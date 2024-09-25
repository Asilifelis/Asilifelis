using Asilifelis.Data;
using Asilifelis.Models;
using Asilifelis.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Asilifelis.Controllers.Server;

[ApiController]
[Route("/.well-known")]
public class WellKnownController(ApplicationRepository repository, UriHelper uriHelper) : ControllerBase {
	private ApplicationRepository Repository { get; } = repository;
	private UriHelper UriHelper { get; } = uriHelper;

	internal record Link(string Rel, string Type, string Href);

	internal record WebfingerActorView(string Subject, string[] Aliases, Link[] Links) {
		public WebfingerActorView(Uri baseUri, Actor actor) : this(
			$"acct:{actor.Username}@{baseUri.Host}",
			[
				new Uri(baseUri, $"/api/actor/{actor.Id}").AbsoluteUri,
				new Uri(baseUri, $"/api/actor/@{actor.Username}").AbsoluteUri
			],
			[new Link("self", "application/activity+json", new Uri(baseUri, $"/api/actor/{actor.Id}").AbsoluteUri)]) {}
	}

	[HttpGet("webfinger")]
	public async ValueTask<IActionResult> GetAsync(string? resource = null) {
		if (resource is not null && resource.Contains('@') && resource.StartsWith("acct:")) {
			string[] parts = resource.Split('@', 2);

			if (string.Equals(parts[1], Request.Host.Host, StringComparison.InvariantCultureIgnoreCase)) {
				string username = parts[0]["acct:".Length..];

				try {
					return Ok(new WebfingerActorView(UriHelper.GetBaseUri(Request), await Repository.GetActorAsync(username)));
				} catch (ActorNotFoundException) {
					return NotFound();
				}
			} else {
				return BadRequest("Unrecognized Host");
			}
		}
		return NoContent();
	}
}