using Asilifelis.Controllers.Server;

namespace Asilifelis.Models.View;

public record ActorView(Uri Id, ActorType Type, string Name, Uri Icon, string PreferredUsername, string Summary, Uri Inbox, Uri Outbox)
	: ActivityStreamsObject {
	public ActorView(Uri baseUri, Actor actor)
		: this(
			new Uri(baseUri, $"api/actor/{actor.Id}"),
			actor.Type,
			actor.DisplayName,
			new Uri(baseUri, "/media/default.jpg"),
			actor.Username,
			"Not implemented yet",
			new Uri(baseUri, $"api/actor/{actor.Id}/inbox"),
			new Uri(baseUri, $"api/actor/{actor.Id}/outbox")) { }
}