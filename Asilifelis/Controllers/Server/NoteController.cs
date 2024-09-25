using Asilifelis.Data;
using Asilifelis.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Asilifelis.Controllers.Server;

[ApiController]
[Route("/api/[controller]")]
public class NoteController(ApplicationRepository repository, UriHelper uriHelper) : ControllerBase {
	private ApplicationRepository Repository { get; } = repository;
	private UriHelper UriHelper { get; } = uriHelper;

	[HttpGet("{id:guid}")]
	[Produces("application/ld+json", "application/activity+json")]
	public async ValueTask<IActionResult> GetNoteAsync(Guid id) {
		var note = await Repository.GetNoteByIdAsync(id);
		if (note is null) return NotFound();

		return Ok(new NoteView(UriHelper.GetBaseUri(Request), note.Author, note));
	}
}