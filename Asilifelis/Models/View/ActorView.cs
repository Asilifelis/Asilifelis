using Asilifelis.Controllers.Server;

namespace Asilifelis.Models.View;

public record ActorView(Uri Id, ActorType Type, string Name, string PreferredUsername, string Summary, Uri Inbox, Uri Outbox)
	: ActivityStreamsObject {
	public ActorView(Uri baseUri, Actor actor)
		: this(
			new Uri(baseUri, $"api/actor/{actor.Id}"),
			actor.Type,
			actor.DisplayName,
			actor.Username,
			"Not implemented yet",
			new Uri(baseUri, $"api/actor/{actor.Id}/inbox"),
			new Uri(baseUri, $"api/actor/{actor.Id}/outbox")) { }
}