using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Asilifelis.Data;
using Asilifelis.Models;
using Microsoft.AspNetCore.Mvc;

namespace Asilifelis.Controllers;

public record NoteCreateDto(
	[Required(AllowEmptyStrings = false), MinLength(1), MaxLength(4096)] 
	[property:JsonPropertyName("content")]
	string Content);

[ApiController]
[Route("api/debug")]
public class DebugController(ApplicationRepository repository) : ControllerBase {
	private ApplicationRepository Repository { get; } = repository;

	[HttpPost("note")]
	public async ValueTask<IActionResult> PostNoteAsync(NoteCreateDto noteDto, CancellationToken cancellationToken) {
		try {
			if (User.Identity?.IsAuthenticated is not true) return Forbid();

			string? username = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (username is null) return Forbid();

			var actor = await Repository.GetActorAsync(username, cancellationToken);
			await Repository.PublishPostAsync(new Note {
				Author = actor,
				Content = noteDto.Content
			}, cancellationToken);

			return Ok();
		} catch (ActorNotFoundException) {
			return NotFound();
		} catch (Exception ex) {
			return Problem("Failed to store post: " + ex.Message);
		}

	}
}