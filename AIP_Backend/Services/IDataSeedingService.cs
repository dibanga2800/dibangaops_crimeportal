namespace AIPBackend.Services
{
    public interface IDataSeedingService
    {
        Task SeedAsync();
        Task SeedRolesAsync();
        Task SeedPermissionsAsync();
        Task SeedAdminUserAsync();
        Task SeedCustomersAsync();
        Task SeedTestUsersAsync();
        Task SeedSampleUsersForTestingAsync();
        Task SeedTestEmployeesAsync();
        Task SeedLookupTablesAsync();
        Task MigrateUserRolesAsync();
        Task UpdatePositionsAsync();
        Task EnsureLookupCategoriesAsync();
    }
}
