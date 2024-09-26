using Asilifelis.Data;
using Asilifelis.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Asilifelis.Tests.DataTests;

public class ApplicationRepositoryTests {
	private ApplicationRepository Repository { get; set; } = null!;

	[SetUp]
	public void Setup() {
		var connection = new SqliteConnection("DataSource=:memory:");
		connection.Open();
		var context = new ApplicationContext(
			new DbContextOptionsBuilder<ApplicationContext>()
				.UseSqlite(connection).Options);
		context.Database.EnsureCreated();
		Repository = new ApplicationRepository(context);
	}
	
	[Test]
	public void Get_ActorByUsername_Success() {
		const string username = "miawinter";
		Assert.DoesNotThrowAsync(async () => await Repository.CreateActorAsync(username));

		Actor actor = null!;
		Assert.DoesNotThrowAsync(async () => actor = await Repository.GetActorAsync(username));

		Assert.That(actor, Is.Not.Null);
		Assert.That(actor.Id, Is.Not.EqualTo(Guid.Empty));
		Assert.That(actor.Username, Is.Not.Null.And.Length.GreaterThan(0).And.Length.LessThan(256));
		Assert.That(actor.DisplayName, Is.Not.Null.And.Length.GreaterThan(0).And.Length.LessThan(4096));
	}

	[Test]
	public void Get_ActorByUsername_ThrowsNotExists() {
		const string username = "this user does not exist";

		Assert.ThrowsAsync<ActorNotFoundException>(async () => await Repository.GetActorAsync(username));
	}
}