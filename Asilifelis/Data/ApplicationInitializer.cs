using Asilifelis.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Asilifelis.Data;

public static class ApplicationInitializer {
	public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration) {
		var optionsSection = configuration.GetSection(InstanceOptions.ConfigurationSectionKeyName);
		services.Configure<InstanceOptions>(optionsSection);
		var options = new InstanceOptions();
		optionsSection.Bind(options);

		string? postgres = configuration.GetConnectionString("postgres");

		if (postgres is not null) {
			Console.WriteLine("postgres connection string detected.");
			services.AddDbContextFactory<ApplicationContext>(postgresOptions => {
				postgresOptions.UseNpgsql(postgres);
			});
		} else {
			Console.WriteLine("no postgres connection, falling back to sqlite.");
			services.AddDbContextFactory<ApplicationContext>(sqliteOptions => {
				sqliteOptions.UseSqlite(
					"DataSource=" + Path.Combine(options.DataLocation, "Asilifelis.db"));
			});
		}
		services.AddScoped<ApplicationRepository>();

		return services;
	}
}