using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Asilifelis.Data;
using Asilifelis.Models;
using Asilifelis.Utilities;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic.CompilerServices;

namespace Asilifelis.Controllers.Server;

public record ActivityStreamsObject {
	[JsonPropertyName("@context"), JsonPropertyOrder(-1), UsedImplicitly]
	public string Context => "https://www.w3.org/ns/activitystreams";
}

public record OrderedCollection<T>(string Summary, int TotalItems, IEnumerable<T> OrderedItems) : ActivityStreamsObject {
	[JsonPropertyName("type"), UsedImplicitly]
	public string Type => "OrderedCollection";
}

public record ActorView(Uri Id, ActorType Type, string Name, string PreferredUsername, string Summary, Uri Inbox, Uri Outbox) 
	: ActivityStreamsObject {
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

public class ActorDto {
	public required Uri Id { get; init; }
	public required string Type { get; init; }
	public required string PreferredUsername { get; init; }
	public required string Name { get; init; }
}

public class LikeActivityDto  {
	public required Uri Id { get; init; }
	public required string Type { get; init; }
	
	public required Uri Actor { get; init; }
	public required Uri Object { get; init; }

	public bool Validate() {
		// TODO check Context
		return string.Equals(Type, "like", StringComparison.InvariantCultureIgnoreCase);
	}
}

public record LikeActivityView(Uri Actor, Uri Object, string Type = "like") {

}

public class NoteDto {

}

public record NoteView(Uri Id, Uri AttributedTo, Uri[] To, Uri? InReplyTo, string Content, SourceView Source, DateTimeOffset Published, bool Sensitive, Uri Likes) : ActivityStreamsObject {
	[JsonPropertyName("type"), UsedImplicitly]
	public string Type => "Note";

	public NoteView(Uri baseUri, Actor author, Note note, Uri likes) : this(
		new Uri(baseUri, $"api/note/{note.Id}"),
		new Uri(baseUri, $"api/actor/{author.Id}"),
		[
			new Uri("https://www.w3.org/ns/activitystreams#Public")
		],
		null,
		note.Content, new SourceView(note.Content, "text/plain"),
		note.PublishDate,
		false,
		likes){}
}

[ApiController]
[Route("/api/[controller]")]
[Produces("application/ld+json", "application/activity+json")]
public class ActorController(ApplicationRepository repository, UriHelper uriHelper, ILogger<ActorController> logger, IHttpClientFactory client) : ControllerBase {
	private ApplicationRepository Repository { get; } = repository;
	private UriHelper UriHelper { get; } = uriHelper;
	private ILogger<ActorController> Logger { get; } = logger;
	private IHttpClientFactory Client { get; } = client;

	[HttpGet("{id}")]
	[Produces("application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"", "application/activity+json")]
	public async ValueTask<IActionResult> GetProfileAsync(string id, CancellationToken cancellationToken) {
		try {
			var actor = await Repository.GetActorByIdentifierAsync(id, cancellationToken);
			return Ok(new ActorView(UriHelper.GetBaseUri(Request), actor));
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
				.Select(n => new NoteView(UriHelper.GetBaseUri(Request), actor, n, 
					UriHelper.GetUriAbsolute(Request, "/api/note/" + n.Id + "/likes")))));
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
	public async ValueTask<IActionResult> PostInboxAsync(Guid id, [FromBody] LikeActivityDto activity, CancellationToken cancellationToken) {
		if (!string.Equals(activity.Type, "like", StringComparison.InvariantCultureIgnoreCase))
			return StatusCode(StatusCodes.Status405MethodNotAllowed, $"Activities of type {activity.Type} are not supported (yet).");
		// 1. validate what we got was an (acceptable) activity
		if (!activity.Validate()) return BadRequest();

		if (UriHelper.IsSameHost(Request, activity.Actor)) {
			// Why would anyone send *us* a like *our* actor did 
			return BadRequest("Actor of like is on this instance, like Activity is therefore nonsensical.");
		}
		

		ActorDto actorDto;
		try {
			using var client = Client.CreateClient();
			var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, activity.Actor) {
				Headers = {
					Accept = {
						// ReSharper disable twice ArrangeObjectCreationWhenTypeNotEvident
						MediaTypeWithQualityHeaderValue.Parse("application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\""),
						new("application/activity+json")
					}
				}
			}, cancellationToken);
			response.EnsureSuccessStatusCode();
			actorDto = await response.Content.ReadFromJsonAsync<ActorDto>(cancellationToken)
						?? throw new ApplicationException("Could not parse actor.");
		} catch (Exception ex) {
			Logger.LogWarning(ex, "Failed to resolve actor of activity {ActivityId}.", activity.Id);
			return BadRequest("Could not resolve activity actor.");
		}
		
		if (!UriHelper.IsSameHost(Request, activity.Object)) {
			// this is not a like of one of our objects, so this inbox is definitely the wrong place for it
			return BadRequest("Target of like is not any object on this Instance.");
		}

		string? localId = activity.Object.AbsolutePath.Split('/').LastOrDefault();
		if (localId is null) {
			return BadRequest("Failed to extract local ID of object.");
		}
		if (!Guid.TryParse(localId, out var localGuid)) {
			return BadRequest("Failed to parse local ID of object.");
		}

		// 2. validate its origin

		// 3. Resolve Actor
		var actor = new Actor {
			Uri = actorDto.Id.AbsoluteUri,
			Username = actorDto.PreferredUsername,
			DisplayName = actorDto.Name,
			Identity = null
		};

		// 4. Resolve Note
		var note = await Repository.GetNoteByIdAsync(localGuid, cancellationToken);
		if (note is null) {
			return NotFound("Object not found (Looked for: Note).");
		}

		// 5. Process the like
		try {
			await Repository.LikeNoteAsync(actor, note, cancellationToken);
		} catch (Exception ex) {
			Logger.LogError(ex, "Failed to process like activity {ActivityId}", activity.Id);
			return Problem("Failed to save like due to an internal server error. sorry :(");
		}

		return Ok();
	}
}