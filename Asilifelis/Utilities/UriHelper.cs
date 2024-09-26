using Microsoft.Extensions.Options;

namespace Asilifelis.Utilities;

public class UriHelper(IOptionsMonitor<InstanceOptions> options) {
	private IOptionsMonitor<InstanceOptions> Options { get; } = options;

	public bool IsSameHost(HttpRequest request, Uri uri) {
		var baseUri = GetBaseUri(request);

		return string.Equals(baseUri.Host, uri.Host);
	}

	public Uri GetBaseUri(HttpRequest request) {
		if (Options.CurrentValue.BaseUri is { IsAbsoluteUri: true } uri) {
			return uri;
		}

		return new Uri($"{request.Scheme}://{request.Host}{request.PathBase}");
	}

	public Uri GetUriAbsolute(HttpRequest request, string relative) {
		return new Uri(GetBaseUri(request), relative);
	}
}