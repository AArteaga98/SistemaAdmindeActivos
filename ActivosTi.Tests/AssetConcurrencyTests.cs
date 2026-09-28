using System.Data;
using ActivosTi.Api.Contracts;
using ActivosTi.Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ActivosTi.Tests;

public sealed class AssetConcurrencyTests
{
    private readonly string _connectionString;

    public AssetConcurrencyTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<AssetConcurrencyTests>()
            .Build();

        _connectionString =
            configuration.GetConnectionString("ActivosTi")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión de pruebas.");
    }

    [Fact]
    public async Task AssignAsync_TwoConcurrentRequests_OnlyOneSucceeds()
    {
        var suffix = Guid.NewGuid()
            .ToString("N")[..10];

        long userId = 0;
        long firstEmployeeId = 0;
        long secondEmployeeId = 0;
        long assetId = 0;

        try
        {
            userId = await CreateUserAsync(
                $"test-user-{suffix}");

            firstEmployeeId = await CreateEmployeeAsync(
                $"TEST-EMP-A-{suffix}",
                $"employee-a-{suffix}@test.local");

            secondEmployeeId = await CreateEmployeeAsync(
                $"TEST-EMP-B-{suffix}",
                $"employee-b-{suffix}@test.local");

            assetId = await CreateAssetAsync(
                $"TEST-ASSET-{suffix}",
                userId);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:ActivosTi"] =
                            _connectionString
                    })
                .Build();

            var connectionFactory =
                new SqlConnectionFactory(configuration);

            var repository =
                new AssetOperationsRepository(
                    connectionFactory);

            var startGate =
                new TaskCompletionSource<bool>(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

            var firstRequest = TryAssignAsync(
                repository,
                startGate.Task,
                assetId,
                firstEmployeeId,
                userId);

            var secondRequest = TryAssignAsync(
                repository,
                startGate.Task,
                assetId,
                secondEmployeeId,
                userId);

            // Libera ambas operaciones prácticamente
            // al mismo tiempo.
            startGate.SetResult(true);

            var results = await Task.WhenAll(
                firstRequest,
                secondRequest);

            var successfulResults =
                results.Where(result => result.Success)
                    .ToList();

            var failedResults =
                results.Where(result => !result.Success)
                    .ToList();

            Assert.Single(successfulResults);
            Assert.Single(failedResults);

            Assert.Contains(
                failedResults[0].SqlErrorNumber,
                new int?[]
                {
                    51021,
                    51023,
                    2601,
                    2627
                });

            var activeAssignments =
                await CountActiveAssignmentsAsync(assetId);

            Assert.Equal(1, activeAssignments);

            var assetStatus =
                await GetAssetStatusAsync(assetId);

            Assert.Equal("Asignado", assetStatus);
        }
        finally
        {
            await CleanupAsync(
                assetId,
                firstEmployeeId,
                secondEmployeeId,
                userId);
        }
    }

    private async Task<AssignmentAttempt> TryAssignAsync(
        AssetOperationsRepository repository,
        Task startGate,
        long assetId,
        long employeeId,
        long userId)
    {
        await startGate;

        try
        {
            var request = new AssignAssetRequest
            {
                EmployeeId = employeeId,
                Observations =
                    "Prueba automatizada de concurrencia."
            };

            var assignmentId =
                await repository.AssignAsync(
                    assetId,
                    request,
                    userId);

            return new AssignmentAttempt(
                Success: true,
                AssignmentId: assignmentId,
                SqlErrorNumber: null);
        }
        catch (SqlException exception)
        {
            return new AssignmentAttempt(
                Success: false,
                AssignmentId: null,
                SqlErrorNumber: exception.Number);
        }
    }

    private async Task<long> CreateUserAsync(
        string userName)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command = new SqlCommand(
            "dbo.usp_Users_Create",
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(
            new SqlParameter(
                "@UserName",
                SqlDbType.NVarChar,
                100)
            {
                Value = userName
            });

        command.Parameters.Add(
            new SqlParameter(
                "@PasswordHash",
                SqlDbType.NVarChar,
                500)
            {
                Value = "TEST_HASH_NOT_USED_FOR_LOGIN"
            });

        command.Parameters.Add(
            new SqlParameter(
                "@RoleName",
                SqlDbType.VarChar,
                20)
            {
                Value = "Administrador"
            });

        var idParameter =
            new SqlParameter("@Id", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };

        command.Parameters.Add(idParameter);

        await command.ExecuteNonQueryAsync();

        return Convert.ToInt64(idParameter.Value);
    }

    private async Task<long> CreateEmployeeAsync(
        string employeeNumber,
        string email)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command = new SqlCommand(
            "dbo.usp_Employees_Create",
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(
            new SqlParameter(
                "@EmployeeNumber",
                SqlDbType.NVarChar,
                40)
            {
                Value = employeeNumber
            });

        command.Parameters.Add(
            new SqlParameter(
                "@FullName",
                SqlDbType.NVarChar,
                160)
            {
                Value = $"Empleado de prueba {employeeNumber}"
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Email",
                SqlDbType.NVarChar,
                254)
            {
                Value = email
            });

        var idParameter =
            new SqlParameter("@Id", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };

        command.Parameters.Add(idParameter);

        await command.ExecuteNonQueryAsync();

        return Convert.ToInt64(idParameter.Value);
    }

    private async Task<long> CreateAssetAsync(
        string assetCode,
        long userId)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command = new SqlCommand(
            "dbo.usp_Assets_Create",
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue(
            "@AssetCode",
            assetCode);

        command.Parameters.AddWithValue(
            "@SerialNumber",
            DBNull.Value);

        command.Parameters.AddWithValue(
            "@Category",
            "Laptop");

        command.Parameters.AddWithValue(
            "@Brand",
            "Marca de prueba");

        command.Parameters.AddWithValue(
            "@Model",
            "Modelo de prueba");

        command.Parameters.AddWithValue(
            "@OwnershipType",
            "Propio");

        command.Parameters.AddWithValue(
            "@SupplierId",
            DBNull.Value);

        command.Parameters.AddWithValue(
            "@CurrentLocation",
            "Laboratorio de pruebas");

        command.Parameters.AddWithValue(
            "@PurchaseDate",
            DateTime.UtcNow.Date);

        command.Parameters.AddWithValue(
            "@RentalEndDate",
            DBNull.Value);

        command.Parameters.AddWithValue(
            "@ActorUserId",
            userId);

        command.Parameters.AddWithValue(
            "@Observations",
            "Activo temporal para prueba.");

        var idParameter =
            new SqlParameter("@Id", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };

        command.Parameters.Add(idParameter);

        await command.ExecuteNonQueryAsync();

        return Convert.ToInt64(idParameter.Value);
    }

    private async Task<int> CountActiveAssignmentsAsync(
        long assetId)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command = new SqlCommand(
            """
            SELECT COUNT(*)
            FROM dbo.AssetAssignments
            WHERE AssetId = @AssetId
              AND ReturnedAt IS NULL;
            """,
            connection);

        command.Parameters.Add(
            new SqlParameter("@AssetId", SqlDbType.BigInt)
            {
                Value = assetId
            });

        return Convert.ToInt32(
            await command.ExecuteScalarAsync());
    }

    private async Task<string?> GetAssetStatusAsync(
        long assetId)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command = new SqlCommand(
            """
            SELECT Status
            FROM dbo.Assets
            WHERE Id = @AssetId;
            """,
            connection);

        command.Parameters.Add(
            new SqlParameter("@AssetId", SqlDbType.BigInt)
            {
                Value = assetId
            });

        return Convert.ToString(
            await command.ExecuteScalarAsync());
    }

    private async Task CleanupAsync(
        long assetId,
        long firstEmployeeId,
        long secondEmployeeId,
        long userId)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command = new SqlCommand(
            """
            DELETE FROM dbo.AssetMovements
            WHERE AssetId = @AssetId;

            DELETE FROM dbo.AssetAssignments
            WHERE AssetId = @AssetId;

            DELETE FROM dbo.Assets
            WHERE Id = @AssetId;

            DELETE FROM dbo.Employees
            WHERE Id IN (@FirstEmployeeId, @SecondEmployeeId);

            DELETE FROM dbo.AppUsers
            WHERE Id = @UserId;
            """,
            connection);

        command.Parameters.AddWithValue(
            "@AssetId",
            assetId);

        command.Parameters.AddWithValue(
            "@FirstEmployeeId",
            firstEmployeeId);

        command.Parameters.AddWithValue(
            "@SecondEmployeeId",
            secondEmployeeId);

        command.Parameters.AddWithValue(
            "@UserId",
            userId);

        await command.ExecuteNonQueryAsync();
    }

    private sealed record AssignmentAttempt(
        bool Success,
        long? AssignmentId,
        int? SqlErrorNumber);
}