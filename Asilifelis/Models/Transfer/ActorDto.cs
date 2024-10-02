namespace Asilifelis.Models.Transfer;

public class ActorDto {
	public required Uri Id { get; init; }
	public required string Type { get; init; }
	public required string PreferredUsername { get; init; }
	public required string Name { get; init; }
}