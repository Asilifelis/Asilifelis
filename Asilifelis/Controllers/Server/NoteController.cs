using Asilifelis.Data;
using Asilifelis.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Threading;

namespace Asilifelis.Controllers.Server;

[ApiController]
[Route("/api/[controller]")]
public class NoteController(ApplicationRepository repository, UriHelper uriHelper) : ControllerBase {
	private ApplicationRepository Repository { get; } = repository;
	private UriHelper UriHelper { get; } = uriHelper;

	[HttpGet("{id:guid}")]
	[Produces("application/ld+json", "application/activity+json")]
	public async ValueTask<IActionResult> GetNoteAsync(Guid id, CancellationToken cancellationToken) {
		var note = await Repository.GetNoteByIdAsync(id, cancellationToken);
		if (note is null) return NotFound();

		var likes = await Repository.GetLikesAsync(note, cancellationToken);

		return Ok(new NoteView(UriHelper.GetBaseUri(Request), note.Author, note, 
			UriHelper.GetUriAbsolute(Request, "/api/note/" + note.Id + "/likes")));
	}
	
	[HttpGet("{id:guid}/likes")]
	[Produces("application/ld+json", "application/activity+json")]
	public async ValueTask<IActionResult> GetNoteLikesAsync(Guid id, CancellationToken cancellationToken) {
		var note = await Repository.GetNoteByIdAsync(id, cancellationToken);
		if (note is null) return NotFound();

		var likes = await Repository.GetLikesAsync(note, cancellationToken);

		return Ok(new OrderedCollection<LikeActivityView>("", likes.Count, likes.Select(actorLikes => {
			Uri? actorUri = null;

			if (Uri.IsWellFormedUriString(actorLikes.Actor.Uri, UriKind.Absolute))
				actorUri = new Uri(actorLikes.Actor.Uri);
			else return null;

			return new LikeActivityView(actorUri, UriHelper.GetUriAbsolute(Request, "/api/note/" + note.Id));
		}).Where(like => like != null).Select(like => like!)));
	}
}