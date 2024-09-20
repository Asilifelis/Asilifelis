using System.ComponentModel.DataAnnotations;

namespace Asilifelis.Models;

public class Actor {
	public Guid Id { get; init; }

	public required string Username { get; init; }
	public required string DisplayName { get; init; }
}