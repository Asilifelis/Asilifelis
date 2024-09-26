using System.Reflection;
using System.Text.Json.Serialization;
using Asilifelis.Data;
using Asilifelis.Models;
using Asilifelis.Utilities;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;

namespace Asilifelis.Controllers;

public record NodeInfoSoftware(string Name, string Version);
public record NodeInfoMetadata(string NodeName, string NodeDescription);
public record NodeInfoUsageUsers(int Total, int ActiveMonth, int ActiveHalfYear);
public record NodeInfoServices(string[] Outbound, string[] Inbound);
public record NodeInfoUsage(int LocalPosts, NodeInfoUsageUsers Users);

public record NodeInfo( 
	bool OpenRegistrations,
	NodeInfoMetadata Metadata,
	NodeInfoSoftware Software,
	NodeInfoUsage Usage) {

#pragma warning disable CA1822
	[JsonPropertyName("version"), JsonPropertyOrder(-1), UsedImplicitly]
	public string Version => "2.0"; 
	[JsonPropertyName("protocols"), UsedImplicitly]
	public string[] Protocols => ["activitypub"];
	[JsonPropertyName("services"), UsedImplicitly]
	public NodeInfoServices Services => new([], []);
#pragma warning restore CA1822
}

[ApiController]
[Route("/.well-known")]
public class WellKnownController(ApplicationRepository repository, UriHelper uriHelper) : ControllerBase {
	private ApplicationRepository Repository { get; } = repository;
	private UriHelper UriHelper { get; } = uriHelper;

	internal record Link(string Rel, string Type, string Href);

	internal record WebfingerActorView(string Subject, string[] Aliases, Link[] Links) {
		public WebfingerActorView(Uri baseUri, Actor actor) : this(
			$"acct:{actor.Username}@{baseUri.Host}",
			[
				new Uri(baseUri, $"/api/actor/{actor.Id}").AbsoluteUri,
				new Uri(baseUri, $"/api/actor/@{actor.Username}").AbsoluteUri
			],
			[new Link("self", "application/activity+json", new Uri(baseUri, $"/api/actor/{actor.Id}").AbsoluteUri)]) { }
	}

	[HttpGet("webfinger")]
	[Produces("application/json")]
	public async ValueTask<IActionResult> GetAsync(string? resource = null) {
		if (resource is not null && resource.Contains('@') && resource.StartsWith("acct:")) {
			string[] parts = resource.Split('@', 2);

			if (string.Equals(parts[1], Request.Host.Host, StringComparison.InvariantCultureIgnoreCase)) {
				string username = parts[0]["acct:".Length..];

				try {
					return Ok(new WebfingerActorView(UriHelper.GetBaseUri(Request), await Repository.GetActorAsync(username)));
				} catch (ActorNotFoundException) {
					return NotFound();
				}
			} else {
				return BadRequest("Unrecognized Host");
			}
		}
		return NoContent();
	}
	
	[HttpGet("nodeinfo")]
	[Produces("application/json")]
	public IActionResult GetNodeInfo() {
		return Ok(new {
			Links = (object[])[
				new {
					Rel = "http://nodeinfo.diaspora.software/ns/schema/2.0", 
					Href = UriHelper.GetUriAbsolute(Request, "/.well-known/nodeinfo/2.0").AbsoluteUri
				}
			]
		});
	}

	[HttpGet("nodeinfo/2.0")]
	[Produces("application/json")]
	public async ValueTask<IActionResult> GetNodeInfo2() {
		string? humanReadableVersion = Assembly.GetEntryAssembly()?
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
			.InformationalVersion.Split("+", 2)[0];

		return Ok(new NodeInfo(
			true,
			new NodeInfoMetadata("Asilifelis", ""),
			new NodeInfoSoftware("Asilifelis", humanReadableVersion ?? "1.0"),
			new NodeInfoUsage(-1, new NodeInfoUsageUsers(1, 1, 1))));
	}
}