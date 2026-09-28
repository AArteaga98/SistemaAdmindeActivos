using System.Data;
using ActivosTi.Api.Authentication;
using Microsoft.Data.SqlClient;

namespace ActivosTi.Api.Data;

public sealed class UserRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public UserRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AuthenticatedUser?> FindForLoginAsync( string userName,CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.usp_Users_GetCredentialsByUserName",
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(
            new SqlParameter("@UserName", SqlDbType.NVarChar, 100)
            {
                Value = userName
            });

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken)) // Si no hay filas, devuelve null
        {
            return null;
        }

        return new AuthenticatedUser(
            Id: reader.GetInt64(reader.GetOrdinal("Id")),
            UserName: reader.GetString(reader.GetOrdinal("UserName")),
            PasswordHash: reader.GetString(reader.GetOrdinal("PasswordHash")),
            RoleName: reader.GetString(reader.GetOrdinal("RoleName")),
            IsActive: reader.GetBoolean(reader.GetOrdinal("IsActive"))
        );
    }

    public async Task<long> CreateAsync(string userName, string passwordHash,string roleName,CancellationToken cancellationToken = default)

     {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.usp_Users_Create",
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(
            new SqlParameter("@UserName", SqlDbType.NVarChar, 100)
            {
                Value = userName
            });

        command.Parameters.Add(
            new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 500)
            {
                Value = passwordHash
            });

        command.Parameters.Add(
            new SqlParameter("@RoleName", SqlDbType.VarChar, 20)
            {
                Value = roleName
            });

        var idParameter = new SqlParameter("@Id", SqlDbType.BigInt)
        {
            Direction = ParameterDirection.Output
        };

        command.Parameters.Add(idParameter); //Agrega el parámetro de salida al comando

        await command.ExecuteNonQueryAsync(cancellationToken);

        return Convert.ToInt64(idParameter.Value); // Devuelve el valor del parámetro de salida como un numero
    }
}