namespace Asilifelis.Utilities;

public class InstanceOptions {
	public const string ConfigurationSectionKeyName = "Instance";

	public Uri? BaseUri { get; init; } = null;
	public string DataLocation { get; init; } = "/app/data";
}