using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Asilifelis.Data;
using Asilifelis.Utilities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Asilifelis.Controllers.Mastodon;

[JsonConverter(typeof(JsonStringEnumConverter<MastodonVisibility>))]
public enum MastodonVisibility {
	Public, Unlisted, Private, Direct
}

public record MastodonAccount(
	string Id, 
	string Username, 
	string Acct,
	Uri Url,
	[property: JsonPropertyName("display_name")]
	string DisplayName,
	[property: JsonPropertyName("note")]
	string Summary,
	[property:JsonPropertyName("created_at")]
	DateTimeOffset CreatedAt,
	[property:JsonPropertyName("last_status_at")]
	DateTimeOffset LastNoteAt,
	[property:JsonPropertyName("statuses_count")]
	int NotesCount,
	[property:JsonPropertyName("followers_count")]
	int FollowersCount,
	[property:JsonPropertyName("following_count")]
	int FollowingCount,
	Uri Avatar,
	[property: JsonPropertyName("avatar_static")]
	Uri AvatarStatic,
	Uri Header,
	[property: JsonPropertyName("header_static")]
	Uri HeaderStatic,
	object[] Fields,
	object[] Emojis,
	bool Locked = false,
	bool Bot = false,
	bool Group = false, 
	bool Discoverable = false,
	[property: JsonPropertyName("no_index")]
	bool NoIndex = false
	);
public record MastodonApplication(
	string Name = "Asilifelis", 
	string Website = "https://github.com/Asilifelis/Asilifelis");
public record MastodonNoteView(
	Guid Id, 
	Uri Url, 
	[property:JsonPropertyName("created_at")]
	DateTimeOffset CreatedAt,
	[property:JsonPropertyName("edited_at")]
	DateTimeOffset? EditedAt,
	string Content,
	MastodonVisibility Visibility,
	bool Sensitive,
	[property: JsonPropertyName("spoiler_text")]
	string? SpoilerText,
	MastodonApplication Application,
	MastodonAccount Account,
	[property: JsonPropertyName("media_attachments")]
	object[] MediaAttachment,
	object[] Mentions,
	object[] Tags,
	object[] Emojis,
	[property: JsonPropertyName("reblogs_count")]
	int ReblogsCount,
	[property: JsonPropertyName("favorites_count")]
	int FavouritesCount,
	[property: JsonPropertyName("replies_count")]
	int RepliesCount,
	[property: JsonPropertyName("in_reply_to_id")]
	string? InReplyToId = null,
	[property: JsonPropertyName("in_reply_to_account_id")]
	string? InReplyToAccountId = null,
	string? Language = null
	);

[ApiController]
[Route("/api/v1/[controller]")]
public class TimelinesController(ApplicationRepository repository, UriHelper uriHelper) : ControllerBase {
	private ApplicationRepository Repository { get; } = repository;
	private UriHelper UriHelper { get; } = uriHelper;
	 
	[HttpGet("public")]
	public async Task<Results<Ok<IEnumerable<MastodonNoteView>>, NoContent>> GetPublicAsync(
		[FromQuery] bool local = false, 
		[FromQuery] bool remote = false,
		[FromQuery(Name = "only_media")] bool onlyMedia = false,
		[FromQuery(Name = "max_id")] string? maxId = null,
		[FromQuery(Name = "since_id")] string? sinceId = null,
		[FromQuery(Name = "min_id")] string? minId = null,
		[FromQuery(Name = "limit"), Range(1, 40)] int limit = 20,
		CancellationToken cancellationToken = default
		) {
		var notes = await Repository.GetPublicNotesAsync(limit, cancellationToken);
		if (notes.Count < 1) return TypedResults.NoContent();

		var img = UriHelper.GetUriAbsolute(Request, "/media/default.jpg");
		return TypedResults.Ok(notes.Select(n => new MastodonNoteView(
			n.Id, 
			UriHelper.GetUriAbsolute(Request, "/api/note/" + n.Id),
			n.PublishDate,
			null,
			n.Content,
			MastodonVisibility.Public,
			false,
			null,
			new MastodonApplication(),
			new MastodonAccount(
				n.Author.Id.ToString(),
				n.Author.Username,
				$"{n.Author.Username}@{n.Author.Host ?? UriHelper.GetBaseUri(Request).Host}",
				n.Author.Uri != null ? new Uri(n.Author.Uri) : UriHelper.GetUriAbsolute(Request, "/api/actor/" + n.Id),
				n.Author.DisplayName,
				"Not Implemented",
				DateTimeOffset.Now,
				DateTimeOffset.Now,
				0,
				0,
				0,
				img, img,
				img, img,
				[], []
				),
			[], [], [], [],
			0, n.Likes.Count, 0
			)));
	}
}