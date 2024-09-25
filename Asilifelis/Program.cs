using System.Net;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Asilifelis.Data;
using Asilifelis.Models;
using Asilifelis.Security;
using Fido2NetLib;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddControllers();
builder.Services.ConfigureHttpJsonOptions(options => {
	options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddDbContextFactory<ApplicationContext>(options => {
	options.UseSqlite("DataSource=asilifelis.db");
});
builder.Services.AddScoped<ApplicationRepository>();

#region Security

builder.Services.AddCors(options => {
	options.AddDefaultPolicy(policy => {
		policy.WithOrigins(builder.Configuration.GetSection("fido2:origins").Get<HashSet<string>>()?.ToArray() ?? throw new ApplicationException("Missing fido2:origins configuration."));
		policy.AllowAnyMethod();
		policy.AllowCredentials();
		policy.WithHeaders("Content-Type", "");
	});
});

builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options => {
	options.IdleTimeout = TimeSpan.FromMinutes(5);
	options.Cookie.HttpOnly = true;
	options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddFido2(options => {
	var config = builder.Configuration.GetSection("fido2");
	options.ServerDomain = config["serverDomain"];
	options.ServerName = "Asilifelis";
	options.Origins = config.GetSection("origins").Get<HashSet<string>>();
	options.TimestampDriftTolerance = config.GetValue<int>("timestampDriftTolerance");
	options.MDSCacheDirPath = config["MDSCacheDirPath"];
	options.BackupEligibleCredentialPolicy = config.GetValue<Fido2Configuration.CredentialBackupPolicy>("backupEligibleCredentialPolicy");
	options.BackedUpCredentialPolicy = config.GetValue<Fido2Configuration.CredentialBackupPolicy>("backedUpCredentialPolicy");
}).AddCachedMetadataService(config => {
	config.AddFidoMetadataRepository(httpClientBuilder => { });
});

builder.Services
	.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options => {
		options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
		options.SlidingExpiration = true;
		options.LoginPath = "";
		options.Cookie.SameSite = SameSiteMode.Strict;
		options.Events.OnRedirectToLogin = (context) => {
			context.Response.StatusCode = 401;
			return Task.CompletedTask;
		};
	});

#endregion

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseSession();

var api = app.MapGroup("/api");
api.MapGet("/actor", async Task<Results<Ok<Actor>, ForbidHttpResult, ProblemHttpResult>> (
		[FromServices] ApplicationRepository repository, 
		ClaimsPrincipal principal,
		CancellationToken cancellation) => {
	if (principal.Identity?.IsAuthenticated is not true)
		return TypedResults.Forbid();
	try {
		return TypedResults.Ok(await repository.GetInstanceActorAsync(cancellation));
	} catch (InvalidOperationException) {
		return TypedResults.Problem(
			"Failed to load instance actor due to an internal server error.", 
			statusCode:(int?)HttpStatusCode.InternalServerError);
	}
});

var sampleTodos = new Todo[] {
	new(1, "Walk the dog"),
	new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
	new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
	new(4, "Clean the bathroom"),
	new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
};

var todosApi = app.MapGroup("/todos");
todosApi.MapGet("/", () => sampleTodos);
todosApi.MapGet("/{id}", (int id) =>
	sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
		? Results.Ok(todo)
		: Results.NotFound());

await using (var scope = app.Services.CreateAsyncScope()) {
	await using var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
	context.Database.EnsureCreated();

	var repository = scope.ServiceProvider.GetRequiredService<ApplicationRepository>();
	await repository.InitializeAsync();
}
app.MapControllers();

app.Run();

public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext {

}