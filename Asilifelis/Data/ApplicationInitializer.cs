using Asilifelis.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Asilifelis.Data;

public static class ApplicationInitializer {
	public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration) {
		var optionsSection = configuration.GetSection(InstanceOptions.ConfigurationSectionKeyName);
		services.Configure<InstanceOptions>(optionsSection);
		var options = new InstanceOptions();
		optionsSection.Bind(options);

		services.AddDbContextFactory<ApplicationContext>(sqliteOptions => {
			sqliteOptions.UseSqlite(
				"DataSource=" + Path.Combine(options.SqliteLocation, "Asilifelis.db"));
		});
		services.AddScoped<ApplicationRepository>();

		return services;
	}
}