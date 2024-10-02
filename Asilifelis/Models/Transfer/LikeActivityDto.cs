namespace Asilifelis.Models.Transfer;

public class LikeActivityDto {
	public required Uri Id { get; init; }
	public required string Type { get; init; }

	public required Uri Actor { get; init; }
	public required Uri Object { get; init; }

	public bool Validate() {
		// TODO check Context
		return string.Equals(Type, "like", StringComparison.InvariantCultureIgnoreCase);
	}
}