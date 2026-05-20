using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace AIPBackend.Tests;

internal static class TestServiceCollectionExtensions
{
	public static void RemoveDbContext<TContext>(this IServiceCollection services)
		where TContext : DbContext
	{
		var contextType = typeof(TContext);
		var optionsType = typeof(DbContextOptions<TContext>);
		var optionsConfigurationType = typeof(IDbContextOptionsConfiguration<TContext>);

		var descriptors = services
			.Where(descriptor =>
				descriptor.ServiceType == contextType ||
				descriptor.ServiceType == optionsType ||
				descriptor.ServiceType == optionsConfigurationType ||
				descriptor.ServiceType == typeof(DbContextOptions))
			.ToList();

		foreach (var descriptor in descriptors)
		{
			services.Remove(descriptor);
		}
	}
}
