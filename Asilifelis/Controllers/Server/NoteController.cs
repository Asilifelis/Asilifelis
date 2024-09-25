using Asilifelis.Data;
using Microsoft.AspNetCore.Mvc;

namespace Asilifelis.Controllers.Server;

[ApiController]
[Route("/api/[controller]")]
public class NoteController(ApplicationRepository repository) : ControllerBase {
	private ApplicationRepository Repository { get; } = repository;

	[HttpGet("{id:guid}")]
	[Produces("application/ld+json", "application/activity+json")]
	public async ValueTask<IActionResult> GetNoteAsync(Guid id) {
		var note = await Repository.GetNoteByIdAsync(id);
		if (note is null) return NotFound();

		return Ok(new NoteView(new Uri($"{Request.Scheme}://{Request.Host}{Request.PathBase}"), note.Author, note));
	}
}