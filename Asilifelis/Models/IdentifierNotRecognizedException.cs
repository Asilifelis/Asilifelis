namespace Asilifelis.Models;

public class IdentifierNotRecognizedException : ApplicationException {
	public IdentifierNotRecognizedException() {}
	public IdentifierNotRecognizedException(string? message) : base(message) {}
	public IdentifierNotRecognizedException(string? message, Exception? innerException) : base(message, innerException) {}
}