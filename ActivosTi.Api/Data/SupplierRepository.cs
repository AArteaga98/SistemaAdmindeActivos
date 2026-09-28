using System.Data;
using ActivosTi.Api.Contracts;
using Microsoft.Data.SqlClient;

namespace ActivosTi.Api.Data;

public sealed class SupplierRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public SupplierRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<SupplierDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var suppliers = new List<SupplierDto>();

        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.usp_Suppliers_List",
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var emailOrdinal = reader.GetOrdinal("ContactEmail");

            suppliers.Add(new SupplierDto(
                Id: reader.GetInt64(reader.GetOrdinal("Id")),
                Name: reader.GetString(reader.GetOrdinal("Name")),
                ContactEmail: reader.IsDBNull(emailOrdinal)
                    ? null
                    : reader.GetString(emailOrdinal),
                IsActive: reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CreatedAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                Purchase: reader.GetBoolean(reader.GetOrdinal("Purchase")),
                Maintenance: reader.GetBoolean(reader.GetOrdinal("Maintenance")),
                Rental: reader.GetBoolean(reader.GetOrdinal("Rental"))
            ));
        }

        return suppliers;
    }

    public async Task<long> CreateAsync(
    CreateSupplierRequest request,
    CancellationToken cancellationToken = default)
    {
        await using var connection =
            _connectionFactory.Create();

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.usp_Suppliers_Create",
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(
            new SqlParameter(
                "@Name",
                SqlDbType.NVarChar,
                160)
            {
                Value = request.Name.Trim()
            });

        command.Parameters.Add(
            new SqlParameter(
                "@ContactEmail",
                SqlDbType.NVarChar,
                254)
            {
                Value = string.IsNullOrWhiteSpace(
                    request.ContactEmail)
                        ? DBNull.Value
                        : request.ContactEmail.Trim()
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Purchase",
                SqlDbType.Bit)
            {
                Value = request.Purchase
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Maintenance",
                SqlDbType.Bit)
            {
                Value = request.Maintenance
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Rental",
                SqlDbType.Bit)
            {
                Value = request.Rental
            });

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
}