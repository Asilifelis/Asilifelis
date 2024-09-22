using Microsoft.AspNetCore.Mvc;
using Asilifelis.Data;
using Asilifelis.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Asilifelis.Controllers;

public record ActorView(string DisplayName, string Username) {
	public ActorView(Actor actor) : this(actor.DisplayName, actor.Username) {}
}

public record NoteView(string Content, ActorView author) {
	public NoteView(Note note) : this(note.Content, new ActorView(note.Author)) {}
}

[ApiController]
[Route("/api/[controller]")]
public class NoteController(ApplicationRepository repository) : ControllerBase {
	private ApplicationRepository Repository { get; } = repository;


	[HttpGet("{id:guid}")]
	public async ValueTask<Results<Ok<NoteView>, NotFound>> GetNoteAsync(Guid id, CancellationToken cancellationToken) {
		var note = await Repository.GetNoteByIdAsync(id, cancellationToken);
		if (note is null) return TypedResults.NotFound();

		return TypedResults.Ok(new NoteView(note));
	}
}