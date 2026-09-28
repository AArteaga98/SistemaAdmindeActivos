using System.Data;
using ActivosTi.Api.Contracts;
using Microsoft.Data.SqlClient;

namespace ActivosTi.Api.Data;

public sealed class EmployeeRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public EmployeeRepository(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<EmployeeDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var employees = new List<EmployeeDto>();

        await using var connection =
            _connectionFactory.Create();

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.usp_Employees_List",
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            employees.Add(new EmployeeDto(
                Id: reader.GetInt64(
                    reader.GetOrdinal("Id")),

                EmployeeNumber: reader.GetString(
                    reader.GetOrdinal("EmployeeNumber")),

                FullName: reader.GetString(
                    reader.GetOrdinal("FullName")),

                Email: reader.GetString(
                    reader.GetOrdinal("Email")),

                IsActive: reader.GetBoolean(
                    reader.GetOrdinal("IsActive")),

                CreatedAt: reader.GetDateTime(
                    reader.GetOrdinal("CreatedAt"))
            ));
        }

        return employees;
    }

    public async Task<long> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            _connectionFactory.Create();

        await connection.OpenAsync(cancellationToken);

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
                Value = request.EmployeeNumber.Trim()
            });

        command.Parameters.Add(
            new SqlParameter(
                "@FullName",
                SqlDbType.NVarChar,
                160)
            {
                Value = request.FullName.Trim()
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Email",
                SqlDbType.NVarChar,
                254)
            {
                Value = request.Email.Trim()
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