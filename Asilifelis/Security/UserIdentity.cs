using Fido2NetLib.Objects;
using Fido2NetLib;
using System.Text.Json.Serialization;

namespace Asilifelis.Security;

public class Credential {
	public int Id { get; init; }
	public required byte[] UserHandle { get; init; }
	public required byte[] PublicKey { get; init; }
	public required PublicKeyCredentialDescriptor Descriptor { get; init; }
}

public class UserIdentity {
	[JsonPropertyName("id")]
	[JsonConverter(typeof(Base64UrlConverter))]
	public required byte[] Id { get; set; }
	public required List<Credential> Credentials { get; init; } = [];

	public uint Counter { get; init; } = 0;
}