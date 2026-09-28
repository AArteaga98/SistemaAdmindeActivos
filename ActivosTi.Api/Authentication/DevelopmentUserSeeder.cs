using ActivosTi.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace ActivosTi.Api.Authentication;

public sealed class DevelopmentUserSeeder
{
    private readonly UserRepository _userRepository;
    private readonly IPasswordHasher<AuthenticatedUser> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public DevelopmentUserSeeder(
        UserRepository userRepository,
        IPasswordHasher<AuthenticatedUser> passwordHasher,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment() ||
            !_configuration.GetValue<bool>("SeedUsers:Enabled"))
        {
            return;
        }

        await CreateIfMissingAsync(
            "SeedUsers:Administrator",
            "Administrador",
            cancellationToken);

        await CreateIfMissingAsync(
            "SeedUsers:Operator",
            "Operador",
            cancellationToken);
    }

    private async Task CreateIfMissingAsync(
        string sectionName,
        string roleName,
        CancellationToken cancellationToken)
    {
        var section = _configuration.GetSection(sectionName);
        var userName = section["UserName"];
        var password = section["Password"];

        if (string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                $"Faltan las credenciales de {sectionName}.");
        }

        var existingUser =
            await _userRepository.FindForLoginAsync(
                userName,
                cancellationToken);

        if (existingUser is not null)
        {
            return;
        }

        var userToHash = new AuthenticatedUser(
            Id: 0,
            UserName: userName,
            PasswordHash: string.Empty,
            RoleName: roleName,
            IsActive: true
        );

        var hash = _passwordHasher.HashPassword(
            userToHash,
            password);

        try
        {
            await _userRepository.CreateAsync(
                userName,
                hash,
                roleName,
                cancellationToken);
        }
        catch (SqlException exception)
            when (exception.Number is 2601 or 2627)
        {
            throw new InvalidOperationException(
                $"El usuario '{userName}' ya existe en la base de datos.",
                exception);
        }
    }
}