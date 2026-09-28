using ActivosTi.Api.Contracts;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ActivosTi.Api.Data;

public sealed class AssetRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public AssetRepository(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> CreateAsync(CreateAssetRequest request,long actorUserId,CancellationToken cancellationToken = default)
    {
        await using var connection =
            _connectionFactory.Create();

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.usp_Assets_Create",
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        AddString(command, "@AssetCode", 50, request.AssetCode);
        AddNullableString(
            command,
            "@SerialNumber",
            100,
            request.SerialNumber);

        AddString(command, "@Category", 60, request.Category);
        AddString(command, "@Brand", 80, request.Brand);
        AddString(command, "@Model", 100, request.Model);

        command.Parameters.Add(
            new SqlParameter(
                "@OwnershipType",
                SqlDbType.VarChar,
                20)
            {
                Value = request.OwnershipType
            });

        AddNullableLong(
            command,
            "@SupplierId",
            request.SupplierId);

        AddString(
            command,
            "@CurrentLocation",
            160,
            request.CurrentLocation);

        AddNullableDate(
            command,
            "@PurchaseDate",
            request.PurchaseDate);

        AddNullableDate(
            command,
            "@RentalEndDate",
            request.RentalEndDate);

        command.Parameters.Add(
            new SqlParameter(
                "@ActorUserId",
                SqlDbType.BigInt)
            {
                Value = actorUserId
            });

        AddNullableString(
            command,
            "@Observations",
            500,
            request.Observations);

        var idParameter = new SqlParameter(
            "@Id",
            SqlDbType.BigInt)
        {
            Direction = ParameterDirection.Output
        };

        command.Parameters.Add(idParameter);

        await command.ExecuteNonQueryAsync(
            cancellationToken);

        return Convert.ToInt64(idParameter.Value);
    }

    public async Task<PagedResult<AssetDto>> ListAsync( AssetQuery query, CancellationToken cancellationToken = default)
    {
        var assets = new List<AssetDto>();

        await using var connection =
            _connectionFactory.Create();

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.usp_Assets_List",
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        AddNullableString(
            command,
            "@Search",
            100,
            query.Search);

        command.Parameters.Add(
            new SqlParameter(
                "@Status",
                SqlDbType.VarChar,
                20)
            {
                Value = string.IsNullOrWhiteSpace(query.Status)
                    ? DBNull.Value
                    : query.Status.Trim()
            });

        AddNullableString(
            command,
            "@Category",
            60,
            query.Category);

        command.Parameters.Add(
            new SqlParameter("@Page", SqlDbType.Int)
            {
                Value = query.Page
            });

        command.Parameters.Add(
            new SqlParameter("@PageSize", SqlDbType.Int)
            {
                Value = query.PageSize
            });

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            assets.Add(ReadAsset(reader));
        }

        long totalCount = 0;

        if (await reader.NextResultAsync(cancellationToken) &&
            await reader.ReadAsync(cancellationToken))
        {
            totalCount = reader.GetInt64(
                reader.GetOrdinal("TotalCount"));
        }

        return new PagedResult<AssetDto>(
            Items: assets,
            TotalCount: totalCount,
            Page: query.Page,
            PageSize: query.PageSize
        );
    }

    public async Task UpdateAsync(long assetId,UpdateAssetRequest request,long actorUserId,CancellationToken cancellationToken = default)
    {
        await using var connection =
            _connectionFactory.Create();

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.usp_Assets_Update",
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(
            new SqlParameter("@AssetId", SqlDbType.BigInt)
            {
                Value = assetId
            });

        AddString(command, "@Category", 60, request.Category);
        AddString(command, "@Brand", 80, request.Brand);
        AddString(command, "@Model", 100, request.Model);

        command.Parameters.Add(
            new SqlParameter(
                "@OwnershipType",
                SqlDbType.VarChar,
                20)
            {
                Value = request.OwnershipType
            });

        AddNullableLong(
            command,
            "@SupplierId",
            request.SupplierId);

        AddNullableDate(
            command,
            "@PurchaseDate",
            request.PurchaseDate);

        AddNullableDate(
            command,
            "@RentalEndDate",
            request.RentalEndDate);

        AddString(
            command,
            "@CurrentLocation",
            160,
            request.CurrentLocation);

        command.Parameters.Add(
            new SqlParameter(
                "@Status",
                SqlDbType.VarChar,
                20)
            {
                Value = request.Status
            });

        command.Parameters.Add(
            new SqlParameter(
                "@ActorUserId",
                SqlDbType.BigInt)
            {
                Value = actorUserId
            });

        AddNullableString(
            command,
            "@Observations",
            500,
            request.Observations);

        await command.ExecuteNonQueryAsync(
            cancellationToken);
    }

    private static AssetDto ReadAsset(SqlDataReader reader)
    {
        return new AssetDto(
            Id: reader.GetInt64(reader.GetOrdinal("Id")),
            AssetCode: reader.GetString(
                reader.GetOrdinal("AssetCode")),
            SerialNumber: GetNullableString(
                reader,
                "SerialNumber"),
            Category: reader.GetString(
                reader.GetOrdinal("Category")),
            Brand: reader.GetString(
                reader.GetOrdinal("Brand")),
            Model: reader.GetString(
                reader.GetOrdinal("Model")),
            OwnershipType: reader.GetString(
                reader.GetOrdinal("OwnershipType")),
            SupplierId: GetNullableInt64(
                reader,
                "SupplierId"),
            Status: reader.GetString(
                reader.GetOrdinal("Status")),
            CurrentLocation: reader.GetString(
                reader.GetOrdinal("CurrentLocation")),
            PurchaseDate: GetNullableDate(
                reader,
                "PurchaseDate"),
            RentalEndDate: GetNullableDate(
                reader,
                "RentalEndDate"),
            CreatedAt: reader.GetDateTime(
                reader.GetOrdinal("CreatedAt")),
            UpdatedAt: reader.GetDateTime(
                reader.GetOrdinal("UpdatedAt")),
            AssignedEmployeeId: GetNullableInt64(
                reader,
                "AssignedEmployeeId"),
            AssignedEmployeeName: GetNullableString(
                reader,
                "AssignedEmployeeName")
        );
    }


    //Funciones Auxiliares para agregar parámetros a SqlCommand y obtener valores de SqlDataReader
    //su funcion es simplificar el código y evitar repetición al trabajar con parámetros y resultados de la base de datos.
    private static void AddString( SqlCommand command,string name,int size,string value)
    {
        command.Parameters.Add(
            new SqlParameter(name, SqlDbType.NVarChar, size)
            {
                Value = value.Trim()
            });
    }

    private static void AddNullableString(SqlCommand command,string name,int size,string? value)
    {
        command.Parameters.Add(
            new SqlParameter(name, SqlDbType.NVarChar, size)
            {
                Value = string.IsNullOrWhiteSpace(value)
                    ? DBNull.Value
                    : value.Trim()
            });
    }

    private static void AddNullableLong(SqlCommand command,string name,long? value)
    {
        command.Parameters.Add(
            new SqlParameter(name, SqlDbType.BigInt)
            {
                Value = value.HasValue
                    ? value.Value
                    : DBNull.Value
            });
    }

    private static void AddNullableDate(SqlCommand command,string name,DateOnly? value)
    {
        command.Parameters.Add(
            new SqlParameter(name, SqlDbType.Date)
            {
                Value = value.HasValue
                    ? value.Value.ToDateTime(TimeOnly.MinValue)
                    : DBNull.Value
            });
    }

    private static string? GetNullableString(SqlDataReader reader,string column)
    {
        var ordinal = reader.GetOrdinal(column);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetString(ordinal);
    }

    private static long? GetNullableInt64(SqlDataReader reader,string column)
    {
        var ordinal = reader.GetOrdinal(column);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetInt64(ordinal);
    }

    private static DateOnly? GetNullableDate(SqlDataReader reader,string column)
    {
        var ordinal = reader.GetOrdinal(column);

        return reader.IsDBNull(ordinal)
            ? null
            : DateOnly.FromDateTime(
                reader.GetDateTime(ordinal));
    }
}