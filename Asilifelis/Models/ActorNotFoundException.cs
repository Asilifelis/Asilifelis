namespace Asilifelis.Models;

public class ActorNotFoundException : ApplicationException {
	public ActorNotFoundException() : base() { }
	public ActorNotFoundException(string message) : base(message) { }
	public ActorNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}