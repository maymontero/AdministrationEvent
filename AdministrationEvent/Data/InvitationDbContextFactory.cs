using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Design;
namespace AdministrationEvent.Data
{
    // Generar migraciones no necesita conectarse a una base de datos.
    // Aplicarlas sí requiere una conexión configurada explícitamente.
    public class InvitationDbContextFactory : IDesignTimeDbContextFactory<InvitationDbContext>
    {
        public InvitationDbContext CreateDbContext(string[] args)
        {
            var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<InvitationDbContextFactory>()
                .AddEnvironmentVariables().Build();
            var connection = configuration["ConnectionStrings:Invitations"] ?? "Server=localhost;Database=invitations;User=unconfigured";
            return new InvitationDbContext(new DbContextOptionsBuilder<InvitationDbContext>().UseMySQL(connection).Options);
        }
    }
}
