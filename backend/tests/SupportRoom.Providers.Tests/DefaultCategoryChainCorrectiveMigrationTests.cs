using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using SupportRoom.Providers.Data.Migrations;

namespace SupportRoom.Providers.Tests;

public sealed class DefaultCategoryChainCorrectiveMigrationTests
{
    [Fact]
    public void CorrectiveMigrationRestampsOnlyLeavesCreatedForExistingParents()
    {
        var migration = new CorrectDefaultCategoryChainLeafCreateDate();
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        InvokeMigrationMethod(migration, "Up", builder);

        var operation = Assert.Single(builder.Operations.OfType<SqlOperation>());
        Assert.Contains("SET \"CreateDate\" = now()", operation.Sql, StringComparison.Ordinal);
        Assert.Contains("'kbcat-company-admin-leaf-' || md5(leaf.\"CompanyId\")", operation.Sql, StringComparison.Ordinal);
        Assert.Contains("parent.\"Id\" <> 'kbcat-company-admin-parent-'", operation.Sql, StringComparison.Ordinal);
        Assert.DoesNotContain("INSERT", operation.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ALTER TABLE", operation.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CREATE TABLE", operation.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP TABLE", operation.Sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CorrectiveMigrationDownIsNoOpBecauseOriginalTimestampCannotBeRecovered()
    {
        var migration = new CorrectDefaultCategoryChainLeafCreateDate();
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        InvokeMigrationMethod(migration, "Down", builder);

        Assert.Empty(builder.Operations);
    }

    private static void InvokeMigrationMethod(Migration migration, string methodName, MigrationBuilder builder)
    {
        var method = typeof(Migration).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        method.Invoke(migration, [builder]);
    }
}
