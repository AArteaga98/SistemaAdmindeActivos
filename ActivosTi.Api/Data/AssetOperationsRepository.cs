using System.Data;
using ActivosTi.Api.Contracts;
using Microsoft.Data.SqlClient;

namespace ActivosTi.Api.Data;

public sealed class AssetOperationsRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public AssetOperationsRepository(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> AssignAsync( long assetId,AssignAssetRequest request, long actorUserId, CancellationToken cancellationToken = default)
    {
        await using var connection =
            _connectionFactory.Create();

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.usp_Assets_Assign",
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(
            new SqlParameter("@AssetId", SqlDbType.BigInt)
            {
                Value = assetId
            });

        command.Parameters.Add(
            new SqlParameter("@EmployeeId", SqlDbType.BigInt)
            {
                Value = request.EmployeeId
            });

        command.Parameters.Add(
            new SqlParameter("@ActorUserId", SqlDbType.BigInt)
            {
                Value = actorUserId
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Observations",
                SqlDbType.NVarChar,
                500)
            {
                Value = string.IsNullOrWhiteSpace(
                    request.Observations)
                        ? DBNull.Value
                        : request.Observations.Trim()
            });

        var assignmentIdParameter =
            new SqlParameter(
                "@AssignmentId",
                SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };

        command.Parameters.Add(assignmentIdParameter);

        await command.ExecuteNonQueryAsync(
            cancellationToken);

        return Convert.ToInt64(
            assignmentIdParameter.Value);
    }

    public async Task<long> ReturnAsync(  long assetId,ReturnAssetRequest request,long actorUserId,CancellationToken cancellationToken = default)
    {
        await using var connection =
            _connectionFactory.Create();

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.usp_Assets_Return",
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(
            new SqlParameter("@AssetId", SqlDbType.BigInt)
            {
                Value = assetId
            });

        command.Parameters.Add(
            new SqlParameter(
                "@ReturnCondition",
                SqlDbType.NVarChar,
                300)
            {
                Value = request.ReturnCondition.Trim()
            });

        command.Parameters.Add(
            new SqlParameter("@ActorUserId", SqlDbType.BigInt)
            {
                Value = actorUserId
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Observations",
                SqlDbType.NVarChar,
                500)
            {
                Value = string.IsNullOrWhiteSpace(
                    request.Observations)
                        ? DBNull.Value
                        : request.Observations.Trim()
            });

        var assignmentIdParameter =
            new SqlParameter(
                "@AssignmentId",
                SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };

        command.Parameters.Add(assignmentIdParameter);

        await command.ExecuteNonQueryAsync(
            cancellationToken);

        return Convert.ToInt64(
            assignmentIdParameter.Value);
    }

    public async Task<IReadOnlyList<AssetMovementDto>> HistoryAsync(long assetId,CancellationToken cancellationToken = default)
    {
        var movements =
            new List<AssetMovementDto>();

        await using var connection =
            _connectionFactory.Create();

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.usp_Assets_History",
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(
            new SqlParameter("@AssetId", SqlDbType.BigInt)
            {
                Value = assetId
            });

        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            movements.Add(new AssetMovementDto(
                Id: reader.GetInt64(
                    reader.GetOrdinal("Id")),

                AssetId: reader.GetInt64(
                    reader.GetOrdinal("AssetId")),

                AssignmentId: GetNullableInt64(
                    reader,
                    "AssignmentId"),

                MovementType: reader.GetString(
                    reader.GetOrdinal("MovementType")),

                PreviousStatus: GetNullableString(
                    reader,
                    "PreviousStatus"),

                NewStatus: GetNullableString(
                    reader,
                    "NewStatus"),

                PreviousLocation: GetNullableString(
                    reader,
                    "PreviousLocation"),

                NewLocation: GetNullableString(
                    reader,
                    "NewLocation"),

                PerformedByUserId: reader.GetInt64(
                    reader.GetOrdinal("PerformedByUserId")),

                UserName: reader.GetString(
                    reader.GetOrdinal("UserName")),

                OccurredAt: reader.GetDateTime(
                    reader.GetOrdinal("OccurredAt")),

                Observations: GetNullableString(
                    reader,
                    "Observations"),

                EmployeeId: GetNullableInt64(
                    reader,
                    "EmployeeId"),

                ReturnCondition: GetNullableString(
                    reader,
                    "ReturnCondition")
            ));
        }

        return movements;
    }

    private static long? GetNullableInt64(SqlDataReader reader, string columnName)
    {
        var ordinal =
            reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetInt64(ordinal);
    }

    private static string? GetNullableString(SqlDataReader reader,string columnName)
    {
        var ordinal =
            reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetString(ordinal);
    }
}