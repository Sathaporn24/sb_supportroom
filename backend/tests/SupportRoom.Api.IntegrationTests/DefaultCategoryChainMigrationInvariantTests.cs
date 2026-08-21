using Npgsql;
using SupportRoom.Domain.Configuration;

namespace SupportRoom.Api.IntegrationTests;

public sealed class DefaultCategoryChainMigrationInvariantTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task EveryCompanyHasExactlyOneSystemDefaultLeafAfterMigration()
    {
        LoadEnvironment();
        var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
        Assert.False(string.IsNullOrWhiteSpace(connectionString));

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT COUNT(*)
            FROM (
                SELECT company."Id"
                FROM "Company" company
                LEFT JOIN "KnowledgeCategory" leaf
                  ON leaf."CompanyId" = company."Id"
                 AND leaf."IsSystemDefault" = true
                 AND leaf."Level" = 2
                GROUP BY company."Id"
                HAVING COUNT(leaf."Id") <> 1
            ) violations;
            """, connection);

        var violationCount = Convert.ToInt32(await command.ExecuteScalarAsync());

        Assert.Equal(0, violationCount);
    }

    private static void LoadEnvironment()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SupportRoom.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        DotEnv.Load(Path.Combine(directory.FullName, "src", "SupportRoom.Api", ".env"));
    }
}
