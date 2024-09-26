namespace Asilifelis.Utilities;

public class UriResolverSingleton {
	public static UriResolverSingleton Instance { get; } = new();

	private HttpClient Client { get; } = new HttpClient();

	public async ValueTask<T?> Resolve<T>(Uri uri) {
		using var response = await Client.GetAsync(uri);
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadFromJsonAsync<T>();
	}
}