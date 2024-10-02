using System.Text.Json.Serialization;
using Asilifelis.Controllers.Server;
using JetBrains.Annotations;

namespace Asilifelis.Models.Transfer;

public record NoteView(Uri Id, Uri AttributedTo, Uri[] To, Uri? InReplyTo, string Content, NoteView.SourceView Source, DateTimeOffset Published, bool Sensitive, Uri Likes) : ActivityStreamsObject {
	public record SourceView(string Content, string MediaType);

	[JsonPropertyName("type"), UsedImplicitly]
	public string Type => "Note";

	public NoteView(Uri baseUri, Actor author, Note note, Uri likes) : this(
		new Uri(baseUri, $"api/note/{note.Id}"),
		new Uri(baseUri, $"api/actor/{author.Id}"),
		[
			new Uri("https://www.w3.org/ns/activitystreams#Public")
		],
		null,
		note.Content, new SourceView(note.Content, "text/plain"),
		note.PublishDate,
		false,
		likes) { }
}