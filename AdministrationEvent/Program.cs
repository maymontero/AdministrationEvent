using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace AdministrationEvent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using (var host = CreateApplicationHostBuilder(args).Build())
            {
                if (args.Contains("--create-beach"))
                {
                    using (var scope = host.Services.CreateScope())
                    {
                        var store = scope.ServiceProvider.GetRequiredService<Services.InvitationStore>();
                        var existing = store.All().FirstOrDefault(x => x.Slug == "despedida-sorpresa");
                        if (existing != null) { Console.WriteLine("La despedida ya existe: " + existing.Id); return; }
                        var invitation = Services.BeachInvitation.Create();
                        var error = store.Save(invitation);
                        if (error != null) throw new InvalidOperationException(error);
                        Console.WriteLine("Despedida creada como borrador: " + invitation.Id);
                    }
                    return;
                }
                if (args.Contains("--create-save-date"))
                {
                    var index = Array.IndexOf(args, "--create-save-date");
                    if (index + 1 >= args.Length || !Guid.TryParse(args[index + 1], out var sourceId)) throw new ArgumentException("Indicá el ID de la invitación original.");
                    using (var scope = host.Services.CreateScope()) {
                        var store = scope.ServiceProvider.GetRequiredService<Services.InvitationStore>();
                        var error = store.CreateSaveTheDate(sourceId, out var invitation);
                        if (error != null) throw new InvalidOperationException(error);
                        Console.WriteLine("Save the date creado: " + invitation.Id);
                    }
                    return;
                }
                if (args.Contains("--import-invitations"))
                {
                    using (var scope = host.Services.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<InvitationDbContext>();
                        var store = scope.ServiceProvider.GetRequiredService<Services.InvitationStore>();
                        var folder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "App_Data", "Invitations");
                        if (!System.IO.Directory.Exists(folder)) { Console.WriteLine("No hay archivos JSON para importar."); return; }
                        var invitations = System.IO.Directory.GetFiles(folder, "*.json")
                            .Select(path => System.Text.Json.JsonSerializer.Deserialize<Models.InvitationConfiguration>(System.IO.File.ReadAllText(path), Services.InvitationStore.JsonOptions)).ToList();
                        using (var transaction = db.Database.BeginTransaction())
                        {
                            var count = 0;
                            foreach (var invitation in invitations)
                            {
                                if (invitation == null) throw new InvalidOperationException("Hay un archivo de invitación vacío.");
                                if (store.Find(invitation.Id) != null) continue;
                                var error = store.Save(invitation);
                                if (error != null) throw new InvalidOperationException("No se pudo importar la invitación: " + error);
                                count++;
                            }
                            transaction.Commit();
                            Console.WriteLine("Invitaciones importadas: " + count + ". Los archivos originales se conservan.");
                        }
                    }
                    return;
                }
                host.Run();
            }
        }

        // EF usa InvitationDbContextFactory para migraciones, sin iniciar la configuración web.
        public static IHostBuilder CreateApplicationHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
