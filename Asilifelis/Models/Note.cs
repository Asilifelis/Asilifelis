namespace Asilifelis.Models;

public class Note {
	public Guid Id { get; init; }
	public required Actor Author { get; init; }
	public required string Content { get; init; }
}