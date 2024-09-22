using Asilifelis.Models;
using Asilifelis.Security;
using Microsoft.EntityFrameworkCore;

namespace Asilifelis.Data;

public class ApplicationRepository(ApplicationContext context) {
	private ApplicationContext Context { get; } = context;
	private const string ReservedInstanceActorName = "@@";
	
	public async ValueTask InitializeAsync(CancellationToken cancellationToken = default) {
		if (await Context.Actors.FirstOrDefaultAsync(
				a => a.Username == ReservedInstanceActorName, cancellationToken) is null) {
			try {
				Actor instanceActor = new() {
					Username = ReservedInstanceActorName,
					DisplayName = "Instance Actor",
					Identity = null
				};
				await Context.AddAsync(instanceActor, cancellationToken);
				await Context.SaveChangesAsync(cancellationToken);
			} catch (Exception ex) {
				throw new InvalidOperationException("Failed creating instance actor.", ex);
			}
		}
	}

	public async ValueTask<Actor> GetInstanceActorAsync(CancellationToken cancellationToken = default) {
		try {
			return await GetActorAsync(ReservedInstanceActorName, cancellationToken);
		} catch (ActorNotFoundException ex) {
			// TODO try (re-)creating the actor?
			throw new InvalidOperationException(
				"Instance actor could not be retrieved. " +
				"This might be due to a connection error with the data store or because it is corrupted.",
				ex);
		}
	}

	public async ValueTask<bool> IsUsernameTaken(string username, CancellationToken cancellationToken = default) {
		return await Context.Actors.AnyAsync(a => a.Username == username, cancellationToken);
	}

	public async ValueTask UpdateActorAsync(Actor actor, CancellationToken cancellationToken = default) {
		Context.Actors.Update(actor);
		await Context.SaveChangesAsync(cancellationToken);
	}

	public async ValueTask<Actor> GetActorAsync(string username, CancellationToken cancellationToken = default) {
		var actor = await Context.Actors.FirstOrDefaultAsync(
			a => a.Username == username, cancellationToken);
		
		if (actor is null) throw new ActorNotFoundException();
		return actor;
	}

	public async ValueTask<UserIdentity> GetIdentityAsync(string username, CancellationToken cancellationToken = default) {
		var actor = await Context.Actors
			.Include(a => a.Identity)
			.ThenInclude(i => i!.Credentials)
			.ThenInclude(c => c.Descriptor)
			.FirstOrDefaultAsync(a => a.Identity != null && a.Username == username, cancellationToken);
		
		if (actor is null) throw new ActorNotFoundException();
		return actor.Identity!;
	}

	public async ValueTask<Actor> GetActorByCredentialIdAsync(byte[] credentialId, CancellationToken cancellationToken = default) {
		var actor = await Context.Actors
			.Include(a => a.Identity)
			.ThenInclude(i => i!.Credentials)
			.ThenInclude(c => c.Descriptor)
			.FirstOrDefaultAsync(a => 
				a.Identity != null && 
				a.Identity.Credentials.Any(c => c.Descriptor.Id == credentialId), 
				cancellationToken: cancellationToken);

		if (actor is null) throw new ActorNotFoundException();

		return actor;
	}

	public async ValueTask<Credential?> GetCredentialByUserHandleAsync(byte[] userHandle, CancellationToken cancellationToken = default) {
		var credential = await Context.Set<UserIdentity>()
			.SelectMany(u => u.Credentials)
			.Include(c => c.Descriptor)
			.FirstOrDefaultAsync(c => c.UserHandle.SequenceEqual(userHandle), cancellationToken);
		return credential;
	}

	public async ValueTask<Actor> CreateActorAsync(string username, string? displayName = null, UserIdentity? identity = null, CancellationToken cancellationToken = default) {
		if (username.Length < 1) 
			throw new InvalidOperationException("username too short, must be at least 1 character.");
		if (username.Length > 64) 
			throw new InvalidOperationException("username too long, must be not longer than 64 characters.");

		if (displayName is not null) {
			if (displayName.Length < 1)
				throw new InvalidOperationException("display name too short, must be at least 1 character.");
			if (displayName.Length > 64)
				throw new InvalidOperationException("display name too long, must be not longer than 64 characters");
		}
		
		var actor = new Actor {
			Username = username,
			DisplayName = displayName ?? username,
			Identity = identity
		};

		try {
			await Context.Actors.AddAsync(actor, cancellationToken);
			await Context.SaveChangesAsync(cancellationToken);
			return actor;
		} catch (Exception ex) {
			throw new InvalidOperationException("Failed to create Actor", ex);
		}
	}
}