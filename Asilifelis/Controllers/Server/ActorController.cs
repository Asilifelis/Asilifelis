using System.Text.Json.Serialization;
using System.Threading;
using Asilifelis.Data;
using Asilifelis.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Asilifelis.Controllers.Server;

public record ActivityStreamsObject {
	[JsonPropertyName("@context"), JsonPropertyOrder(-1), UsedImplicitly]
	public string Context => "https://www.w3.org/ns/activitystreams";
}

public record OrderedCollection<T>(string Summary, int TotalItems, IEnumerable<T> Items) : ActivityStreamsObject {
	[JsonPropertyName("type"), UsedImplicitly]
	public string Type => "OrderedCollection";
}

public record ActorView(Uri Id, ActorType Type, string Name, string PreferredUsername, string Summary, Uri Inbox, Uri Outbox) 
	: ActivityStreamsObject{
	public ActorView(Uri baseUri, Actor actor)
		: this(
			new Uri(baseUri, $"api/actor/{actor.Id}"),
			actor.Type,
			actor.DisplayName,
			actor.Username,
			"Not implemented yet",
			new Uri(baseUri, $"api/actor/{actor.Id}/inbox"),
			new Uri(baseUri, $"api/actor/{actor.Id}/outbox")) {}
}

public record SourceView(string Content, string MediaType);

public record NoteView(Uri Id, Uri AttributedTo, Uri[] To, Uri? InReplyTo, string Content, SourceView Source, DateTimeOffset Published, bool Sensitive) {
	[JsonPropertyName("type"), UsedImplicitly]
	public string Type => "Note";

	public NoteView(Uri baseUri, Actor author, Note note) : this(
		new Uri(baseUri, $"api/note/{note.Id}"),
		new Uri(baseUri, $"api/actor/{author.Id}"),
		[
			new Uri("https://www.w3.org/ns/activitystreams#Public")
		],
		null,
		note.Content, new SourceView(note.Content, "text/plain"),
		note.PublishDate, 
		false) {}
}

[ApiController]
[Route("/api/[controller]")]
[Produces("application/ld+json", "application/activity+json")]
public class ActorController(ApplicationRepository repository) : ControllerBase {
	private ApplicationRepository Repository { get; } = repository;

	[HttpGet("{id}")]
	[Produces("application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"", "application/activity+json")]
	public async ValueTask<IActionResult> GetProfileAsync(string id, CancellationToken cancellationToken) {
		try {
			var actor = await Repository.GetActorByIdentifierAsync(id, cancellationToken);
			return Ok(new ActorView(new Uri($"{Request.Scheme}://{Request.Host}{Request.PathBase}"), actor));
		} catch (ActorNotFoundException) {
			return NotFound("user not found.");
		} catch (IdentifierNotRecognizedException) {
			return BadRequest("ID format not recognized.");
		}
	}

	[HttpGet("{id}/outbox")]
	[Produces("application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"", "application/activity+json")]
	public async ValueTask<IActionResult> GetOutboxAsync(string id, CancellationToken cancellationToken) {
		try {
			var actor = await Repository.GetActorByIdentifierAsync(id, cancellationToken);
			(var notes, int total) = await Repository.GetNotesAsync(actor, cancellationToken: cancellationToken);
			return Ok(new OrderedCollection<NoteView>("", total, notes
				.Select(n => new NoteView(new Uri($"{Request.Scheme}://{Request.Host}{Request.PathBase}"), actor, n))));
		} catch (ActorNotFoundException) {
			return NotFound("user not found.");
		} catch (IdentifierNotRecognizedException) {
			return BadRequest("ID format not recognized.");
		}
	}
	[HttpGet("{id}/inbox")]
	[Produces("application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"", "application/activity+json")]
	public async ValueTask<IActionResult> GetInboxAsync(string id) {
		return Ok(new OrderedCollection<object>("Actually, this is not implemented", 0, []));
	}
	[HttpPost("{id}/inbox")]
	[Produces("application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"", "application/activity+json")]
	public async ValueTask<IActionResult> PostInboxAsync(string id) {
		// https://www.w3.org/TR/activitypub/#server-to-server-interactions 5.2
		return StatusCode(StatusCodes.Status405MethodNotAllowed);
	}
}