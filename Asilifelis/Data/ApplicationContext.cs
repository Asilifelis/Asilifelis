using Asilifelis.Models;
using Asilifelis.Security;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using NSec.Cryptography;

namespace Asilifelis.Data;

public class GuidV7Generator : GuidValueGenerator {
	/// <summary>
	///     Generates <see cref="Guid" /> values using <see cref="Guid.CreateVersion7()" />.
	///     The generated values are non-temporary, meaning they will be saved to the database.
	/// </summary>
	/// <remarks>
	///     See <see href="https://aka.ms/efcore-docs-value-generation">EF Core value generation</see> for more information and examples.
	/// </remarks>
	public override Guid Next(EntityEntry entry) {
		return Guid.CreateVersion7(DateTimeOffset.UtcNow);
	}
}

public class ApplicationContext : DbContext {
	public DbSet<Actor> Actors => Set<Actor>();
	public DbSet<Note> Notes => Set<Note>();

	protected ApplicationContext() {}
	public ApplicationContext(DbContextOptions options) : base(options) {}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
		base.OnConfiguring(optionsBuilder);

		optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
#if DEBUG
		optionsBuilder.EnableDetailedErrors();
		optionsBuilder.EnableSensitiveDataLogging();
#endif

		if (optionsBuilder.IsConfigured) return;

		// something something maybe some day
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
		base.ConfigureConventions(configurationBuilder);

		configurationBuilder.Properties<string>().AreUnicode().HaveMaxLength(256);
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Actor>(actor => {
			actor.HasKey(a => a.Id);
			actor.Property(a => a.Id).HasValueGenerator<GuidV7Generator>();

			actor.Property(a => a.Username).HasMaxLength(256).IsRequired().UseCollation("NOCASE"); // TODO use non-sqlite collation
			// We can limit the length on the Repository layer, thus making it customizable
			actor.Property(a => a.DisplayName).HasMaxLength(4096).IsRequired();

			actor.HasMany(a => a.Notes).WithOne(n => n.Author).OnDelete(DeleteBehavior.Cascade);
		});
		modelBuilder.Entity<Note>(note => {
			note.HasKey(n => n.Id);
			note.Property(n => n.Id).HasValueGenerator<GuidV7Generator>();

			note.Property(n => n.Content).HasMaxLength(4096).IsUnicode().IsRequired();
		});

		modelBuilder.Entity<UserIdentity>(identity => {
			identity.OwnsMany(i => i.Credentials).HasKey(c => c.Id);
		});
		modelBuilder.Entity<PublicKeyCredentialDescriptor>(credentialDescriptor => {
			credentialDescriptor.Property(cd => cd.Id).IsRequired();
			credentialDescriptor.Property(cd => cd.Type).IsRequired();
			credentialDescriptor.Property(cd => cd.Transports).IsRequired(false);
		});
	}
}