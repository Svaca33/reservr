using System.Net.Http.Json;
using Npgsql;

namespace Reservr.IntegrationTests;

[Collection(ReservrAppCollection.Name)]
public sealed class LanesTests(ReservrAppFixture fixture)
{
    private sealed record LaneDto(Guid Id, int Number);

    [Fact]
    public async Task Get_lanes_on_empty_database_returns_empty_json_array()
    {
        using var response = await fixture.Api.GetAsync("/lanes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_lanes_returns_lanes_stored_in_database_ordered_by_number()
    {
        var lane2 = Guid.CreateVersion7();
        var lane1 = Guid.CreateVersion7();
        await ExecuteSqlAsync(
            "INSERT INTO lanes (id, number) VALUES (@lane2, 2), (@lane1, 1)",
            ("lane2", lane2), ("lane1", lane1));

        try
        {
            using var response = await fixture.Api.GetAsync("/lanes");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var lanes = await response.Content.ReadFromJsonAsync<LaneDto[]>();
            Assert.NotNull(lanes);
            Assert.Equal([new LaneDto(lane1, 1), new LaneDto(lane2, 2)], lanes);
        }
        finally
        {
            // Leave the database empty for the other tests.
            await ExecuteSqlAsync("DELETE FROM lanes WHERE id IN (@lane1, @lane2)",
                ("lane1", lane1), ("lane2", lane2));
        }
    }

    [Fact]
    public async Task Api_startup_applies_initial_migration()
    {
        await using var connection = new NpgsqlConnection(fixture.DatabaseConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """SELECT "MigrationId" FROM "__EFMigrationsHistory" """, connection);

        var migrations = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            migrations.Add(reader.GetString(0));
        }

        Assert.Contains(migrations, m => m.EndsWith("_InitialCreate", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Database_rejects_non_positive_lane_number()
    {
        var ex = await Assert.ThrowsAsync<PostgresException>(() => ExecuteSqlAsync(
            "INSERT INTO lanes (id, number) VALUES (@id, 0)", ("id", Guid.CreateVersion7())));

        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
    }

    private async Task ExecuteSqlAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(fixture.DatabaseConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }
        await command.ExecuteNonQueryAsync();
    }
}
