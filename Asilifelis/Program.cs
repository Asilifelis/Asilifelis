using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Asilifelis.Data;
using Asilifelis.Utilities;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddHttpClient();

builder.Services.AddControllers(options => {
	options.RespectBrowserAcceptHeader = true;
	var jsonOptions = new JsonSerializerOptions(JsonSerializerOptions.Web) {
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};
	jsonOptions.Converters.Add(new JsonStringEnumConverter());

	options.OutputFormatters.Insert(0, new JsonLdOutputFormatter(jsonOptions));
	options.InputFormatters.Insert(1, new JsonLdInputFormatter(jsonOptions));
}).AddJsonOptions(options => {
	options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter<PublicKeyCredentialType>(JsonNamingPolicy.KebabCaseLower));
	options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddSingleton<UriHelper>();

#region Security

builder.Services.AddCors(options => {
	options.AddDefaultPolicy(policy => {
		policy.WithOrigins(builder.Configuration.GetSection("fido2:origins").Get<HashSet<string>>()?.ToArray() 
							?? throw new ApplicationException("Missing fido2:origins configuration."));
		policy.WithMethods("GET", "POST", "PUT", "DELETE");
		policy.AllowCredentials();
		policy.WithHeaders("Content-Type", "Accept");
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
app.UseStaticFiles("/media");
app.UseAuthentication();
app.UseSession();

var api = app.MapGroup("/api");
api.MapGet("/version", Results<Ok<string>, NotFound> () => {
	string? humanReadableVersion = Assembly.GetEntryAssembly()?
		.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
		.InformationalVersion.Split("+", 2)[0];
	return humanReadableVersion is null ? TypedResults.NotFound() : TypedResults.Ok(humanReadableVersion);
});

await using (var scope = app.Services.CreateAsyncScope()) {
	await using var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
	await context.Database.MigrateAsync();

	var repository = scope.ServiceProvider.GetRequiredService<ApplicationRepository>();
	await repository.InitializeAsync();
}
app.MapControllers();

app.Run();
