using Asilifelis.Security;

namespace Asilifelis.Models;

public enum ActorType {
	Person
}

public class Actor {
	public Guid Id { get; init; }
	public required UserIdentity? Identity { get; init; }

	public ActorType Type { get; init; }
	public required string Username { get; init; }
	public required string DisplayName { get; init; }

	public ICollection<Note> Notes { get; } = [];
}