namespace Asilifelis.Models;

public class ActorLikes {
	public int Id { get; init; }
	public required Actor Actor { get; init; }
	public required Note Note { get; init; }
}

public class Note {
	public Guid Id { get; init; }
	public required Actor Author { get; init; }
	public required string Content { get; init; }

	public DateTimeOffset PublishDate { get; init; } = DateTimeOffset.Now;

	public ICollection<Actor> Likes { get; } = [];
}