﻿namespace Asilifelis.Utilities;

public class InstanceOptions {
	public const string ConfigurationSectionKeyName = "Instance";

	public Uri? BaseUri { get; init; } = null;
	public string SqliteLocation { get; init; } = "/app/data";
}