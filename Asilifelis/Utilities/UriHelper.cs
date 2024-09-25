﻿using Microsoft.Extensions.Options;

namespace Asilifelis.Utilities;

public class InstanceOptions {
	public Uri? BaseUri { get; init; }
}

public class UriHelper(IOptionsMonitor<InstanceOptions> options) {
	private IOptionsMonitor<InstanceOptions> Options { get; } = options;

	public Uri GetBaseUri(HttpRequest request) {
		if (Options.CurrentValue.BaseUri is { IsAbsoluteUri: true } uri) {
			return uri;
		}

		return new Uri($"{request.Scheme}://{request.Host}{request.PathBase}");
	}
}