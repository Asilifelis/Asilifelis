using Asilifelis.Models;
using Asilifelis.Security;
using Fido2NetLib.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

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

			var username = actor.Property(a => a.Username).HasMaxLength(256).IsRequired();

			if (Database.IsSqlite()) {
				username.UseCollation("NOCASE");
			} else if (Database.IsNpgsql()) {
				// und = undefined (basically inherit the language of the database)
				// u = unicode
				// ks = https://www.postgresql.org/docs/current/collation.html#ICU-COLLATION-COMPARISON-LEVELS
				// https://www.postgresql.org/docs/current/collation.html#COLLATION-NONDETERMINISTIC
				modelBuilder.HasCollation(null, "default_case_insensitive", "und-u-ks-level1", "und-u-ks-level1", "icu", false);
				username.UseCollation("default_case_insensitive");
			}

			// We can limit the length on the Repository layer, thus making it customizable
			actor.Property(a => a.DisplayName).HasMaxLength(4096).IsRequired();

			actor.HasOne(a => a.Identity).WithMany().OnDelete(DeleteBehavior.Cascade);
			actor.HasMany(a => a.Notes).WithOne(n => n.Author).IsRequired()
				.HasForeignKey("AuthorId").HasPrincipalKey(nameof(Actor.Id))
				.OnDelete(DeleteBehavior.Cascade);
		});
		modelBuilder.Entity<Note>(note => {
			note.HasKey(n => n.Id);
			note.Property(n => n.Id).HasValueGenerator<GuidV7Generator>();

			note.Property(n => n.Content).HasMaxLength(4096).IsUnicode().IsRequired();
			note.HasMany(n => n.Likes).WithMany(a => a.Liked)
				.UsingEntity<ActorLikes>(
					actorLikes => actorLikes.HasOne(al => al.Actor).WithMany()
						.HasForeignKey("LikedActorId").HasPrincipalKey(nameof(Actor.Id)),
					actorLikes => actorLikes.HasOne(al => al.Note).WithMany()
						.HasForeignKey("LikesNoteId").HasPrincipalKey(nameof(Note.Id)),
					actorLikes => {
						actorLikes.HasKey(al => al.Id);
						actorLikes.Property<Guid>("LikedActorId");
						actorLikes.Property<Guid>("LikesNoteId");
					});
		});

		modelBuilder.Entity<UserIdentity>(identity => {
			identity.OwnsMany(i => i.Credentials).HasKey(c => c.Id);
		});
		modelBuilder.Entity<PublicKeyCredentialDescriptor>(credentialDescriptor => {
			credentialDescriptor.Property(cd => cd.Id).IsRequired();
			credentialDescriptor.Property(cd => cd.Type).IsRequired();
			var transports = credentialDescriptor.Property(cd => cd.Transports).IsRequired(false);
			if (Database.IsNpgsql()) transports.HasColumnType("jsonb");
		});
	}
}