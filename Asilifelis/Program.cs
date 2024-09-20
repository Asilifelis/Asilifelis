using System.Net;
using System.Text.Json.Serialization;
using Asilifelis.Data;
using Asilifelis.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options => {
	options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddDbContextFactory<ApplicationContext>(options => {
	options.UseSqlite("DataSource=asilifelis.db");
});
builder.Services.AddScoped<ApplicationRepository>();

var app = builder.Build();

var api = app.MapGroup("/api");
api.MapGet("/actor", async Task<Results<Ok<Actor>, ProblemHttpResult>> (
		[FromServices] ApplicationRepository repository, 
		CancellationToken cancellation) => {
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

app.Run();

public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext {

}