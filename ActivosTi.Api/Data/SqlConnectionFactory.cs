using Microsoft.Data.SqlClient;

namespace ActivosTi.Api.Data;

public sealed class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ActivosTi")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión 'ActivosTi'.");
    }

    public SqlConnection Create()
    {
        return new SqlConnection(_connectionString);
    }
}