using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonLD.Core;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Asilifelis.Utilities;

public class JsonLdInputFormatter : TextInputFormatter {
	private JsonSerializerOptions Options { get; }

	public JsonLdInputFormatter(JsonSerializerOptions options) {
		Options = options;
		SupportedMediaTypes.Add("application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"");
		SupportedMediaTypes.Add("application/activity+json");
		
		SupportedEncodings.Add(Encoding.UTF8);
		SupportedEncodings.Add(Encoding.Unicode);
	}

	public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding) {
		var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JsonLdInputFormatter>>();
		try {
			using var reader = new StreamReader(context.HttpContext.Request.Body, encoding);
			var json = await JObject.LoadAsync(new JsonTextReader(reader));
			// logger.LogDebug("Parsing {jsonLd}", json.ToString());
			// var expandedDocument = JsonLdProcessor.Flatten(json, new JsonLdOptions());
			// if (expandedDocument is null) return await InputFormatterResult.FailureAsync();
			return await InputFormatterResult.SuccessAsync(json);
		} catch (Exception ex) {
			logger.LogError(ex, "Failed to parse json-ld.");
			return await InputFormatterResult.FailureAsync();
		}
	}
}

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
		await context.HttpContext.Response.WriteAsJsonAsync(context.Object, Options, 
			"application/ld+json; profile=\"https://www.w3.org/ns/activitystreams");
	}
}