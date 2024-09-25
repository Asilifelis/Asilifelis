using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Asilifelis.Utilities;

public class JsonLdOutputFormatter : TextOutputFormatter {
	private JsonSerializerOptions Options { get; }

	public JsonLdOutputFormatter(JsonSerializerOptions jsonSerializerOptions) {
		Options = jsonSerializerOptions;
		SupportedMediaTypes.Add("application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"");
		SupportedMediaTypes.Add("application/activity+json");
		
		SupportedEncodings.Add(Encoding.UTF8);
		SupportedEncodings.Add(Encoding.Unicode);
	}

	protected override bool CanWriteType(Type? type) => true;

	public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding) {
		Console.WriteLine("TEST");
		await context.HttpContext.Response.WriteAsJsonAsync(context.Object, Options, 
			"application/ld+json; profile=\"https://www.w3.org/ns/activitystreams");
	}
}