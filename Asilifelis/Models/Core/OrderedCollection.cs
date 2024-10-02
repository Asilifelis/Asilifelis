using System.Text.Json.Serialization;
using Asilifelis.Controllers.Server;
using JetBrains.Annotations;

namespace Asilifelis.Models.Core;

public record OrderedCollection<T>(string Summary, int TotalItems, IEnumerable<T> OrderedItems) : ActivityStreamsObject {
	[JsonPropertyName("type"), UsedImplicitly]
	public string Type => "OrderedCollection";
}