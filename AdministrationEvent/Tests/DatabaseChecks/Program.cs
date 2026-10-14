using System;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using AdministrationEvent.Controllers;
using AdministrationEvent.Models;
using AdministrationEvent.Services;
class DatabaseChecks
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Main(string[] args)
    {
        if (Array.IndexOf(args, "--mysql") >= 0) { CheckMySql(); return; }
        using (var connection = new SqliteConnection("Data Source=:memory:"))
        {
            connection.Open();
            var options = new DbContextOptionsBuilder<InvitationDbContext>().UseSqlite(connection).Options;
            using (var db = new InvitationDbContext(options)) db.Database.EnsureCreated();
            using (var accountDb = new InvitationDbContext(options)) {
                var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string> { ["Admin:Password"] = "Test-Owner-Password-2026" }).Build();
                var accounts = new AdminAccounts(accountDb, configuration);
                accounts.BootstrapOwner(); accounts.BootstrapOwner();
                Check(accountDb.AdminUsers.Count() == 1, "Administradora se crea una sola vez");
                var owner = accounts.Authenticate("ADMIN", "Test-Owner-Password-2026");
                Check(owner != null && owner.IsOwner && owner.PasswordHash != "Test-Owner-Password-2026", "Administradora conserva acceso con hash");
                Check(accounts.Create("chea", "Chea", "Chea-Test-Password-2026") == null, "Cuenta propia de Chea");
                Check(accounts.Create("CHEA", "Duplicado", "Chea-Test-Password-2026") != null, "Nombre único sin distinguir mayúsculas");
                var chea = accounts.Authenticate(" chea ", "Chea-Test-Password-2026");
                Check(chea != null && !chea.IsOwner, "Chea administra invitaciones sin gestionar cuentas");
                for (var attempt = 0; attempt < 5; attempt++) Check(accounts.Authenticate("chea", "incorrecta") == null, "Contraseña incorrecta rechazada");
                Check(accounts.Authenticate("chea", "Chea-Test-Password-2026") == null, "Bloqueo temporal tras cinco intentos");
                var oldStamp = chea.SecurityStamp;
                accounts.SetPassword(chea, "Chea-New-Password-2026");
                Check(chea.SecurityStamp != oldStamp && accounts.Authenticate("chea", "Chea-New-Password-2026") != null, "Restablecer contraseña revoca sesiones y desbloquea");
                chea.IsActive = false; accountDb.SaveChanges();
                Check(accounts.Authenticate("chea", "Chea-New-Password-2026") == null, "Cuenta desactivada no ingresa");
            }
            var item = new InvitationConfiguration { Slug = "prueba", Date = new DateTime(2027,3,20,18,0,0), Sections = new List<InvitationSection> {
                new InvitationSection { Title = "Primera", Content = "Ceremonia", TitleFontSize = 28 },
                new InvitationSection { Title = "Oculta", Enabled = false }
            }};
            item.Theme.HeadingFontUrl = "/fonts/test.woff2";
            item.Theme.SpotifyTrackUrl = "https://open.spotify.com/embed/track/2TD8Bhc5hFPKh52lFJ2Ec5?theme=0";
            item.Theme.Layout = "vineyard"; item.ShowCountdown = true; item.HasConfirmedTime = false; item.RsvpEnabled = true;
            item.CoverImageUrl = "/images/zenia-diego/vineyard-watercolor.png";
            using (var db = new InvitationDbContext(options)) Check(new InvitationStore(db).Save(item) == null, "Crear invitación");
            using (var db = new InvitationDbContext(options)) {
                var store = new InvitationStore(db); var restored = store.Find(item.Id);
                Check(restored.Sections[0].TitleFontSize == 28, "Tamaño de título por sección persistido");
                Check(restored.Date == item.Date && restored.Sections.Count == 2 && !restored.Sections[1].Enabled, "Fecha y secciones persistidas");
                Check(restored.Theme.HeadingFontUrl == item.Theme.HeadingFontUrl, "Fuente persistida");
                Check(restored.Theme.SpotifyTrackUrl == item.Theme.SpotifyTrackUrl, "Canción Spotify persistida");
                restored.Theme.SpotifyTrackUrl = "https://example.com/track/2TD8Bhc5hFPKh52lFJ2Ec5";
                Check(store.Save(restored) != null, "Rechazar enlaces ajenos a Spotify");
                restored.Theme.SpotifyTrackUrl = "";
                Check(store.Save(restored) == null && store.Find(item.Id).Theme.SpotifyTrackUrl == "", "Quitar música guardada");
                Check(restored.Theme.Layout == "vineyard" && restored.ShowCountdown && !restored.HasConfirmedTime && restored.RsvpEnabled && restored.TimeZone == "America/Montevideo", "Opciones de diseño persistidas");
                Check(store.FindPublished(item.Slug) == null, "Borrador privado");
                restored.IsPublished = true; restored.Theme.Accent = "#123456"; restored.Theme.Layout = "relaxed";
                restored.Sections.RemoveAt(0); restored.Sections[0].Title = "Nueva primera";
                Check(store.Save(restored) == null, "Actualizar invitación");
            }
            using (var db = new InvitationDbContext(options)) {
                var store = new InvitationStore(db); var restored = store.FindPublished(item.Slug);
                Check(restored != null && restored.Theme.Accent == "#123456" && restored.Theme.Layout == "relaxed", "Publicación y tema actualizado");
                Check(restored.Sections.Count == 1 && restored.Sections[0].Title == "Nueva primera", "Sin secciones obsoletas");
                CheckRsvp(db, store, restored);
                CheckGuests(db, store, restored);
                CheckOrganization(db, store, restored);
                Check(db.ColorResources.CountAsync().Result == 5 && db.FontResources.CountAsync().Result == 2, "Sin recursos duplicados");
                Check(store.Save(new InvitationConfiguration { Slug = item.Slug }) != null, "Slug duplicado rechazado");
                var invalid = new InvitationConfiguration { Slug = "invalida", CoverImageUrl = "javascript:alert(1)" };
                Check(store.Save(invalid) != null && store.Find(invalid.Id) == null, "URL inválida rechazada sin guardar");
                foreach (var url in new[] { "//evil.org/a.png", "/images/../a.png", "/images/a.png'", "http://example.org/a.png", "/images/%2e%2e/a.png" }) { invalid.CoverImageUrl = url; Check(store.Save(invalid) != null, "Ruta insegura rechazada: " + url); }
                invalid.CoverImageUrl = "/images/a.png"; invalid.TimeZone = "invalida"; Check(store.Save(invalid) != null, "Zona inválida rechazada");
                var entity = db.Invitations.SingleAsync().Result;
                db.GuestResponses.Add(new Application.GuestResponse { InvitationId = entity.Id, Name = "Prueba", Attending = true, GuestCount = 2, SubmittedAtUtc = DateTime.UtcNow }); db.SaveChanges();
                Check(db.GuestResponses.AnyAsync(x => x.Name == "Prueba" && x.GuestCount == 2).Result, "Asistencia persistida");
                Check(store.All().Count == 1, "Listado persistido");
            }
            using (var db = new InvitationDbContext(options)) {
                db.Invitations.Remove(db.Invitations.SingleAsync().Result); db.SaveChanges();
                Check(db.GuestResponses.CountAsync().Result == 0 && db.InvitationSections.CountAsync().Result == 0 && db.InvitationThemes.CountAsync().Result == 0 && db.ColorResources.CountAsync().Result == 0, "Borrado en cascada");
            }
        }
        Console.WriteLine("DatabaseChecks: todas las verificaciones pasaron (SQLite; MySQL requiere prueba con servidor real).");
    }
    static void CheckRsvp(InvitationDbContext db, InvitationStore store, InvitationConfiguration invitation)
    {
        var http = new DefaultHttpContext();
        var controller = new InvitationsController(store, db) { ControllerContext = new ControllerContext { HttpContext = http }, TempData = new TempDataDictionary(http, new MemoryTempDataProvider()) };
        var initial = db.GuestResponses.CountAsync().Result;
        Check(controller.Respond(invitation.Slug, new RsvpForm { Name = " Prueba asistencia ", Attending = true, GuestCount = 2, DietaryRestrictions = " Sin gluten ", SongRequest = " Brindo · Benja Torres " }) is RedirectResult, "Formulario: confirmación válida");
        Check(db.GuestResponses.AnyAsync(x => x.Name == "Prueba asistencia" && x.GuestCount == 2 && x.Attending && x.DietaryRestrictions == "Sin gluten" && x.SongRequest == "Brindo · Benja Torres").Result, "Formulario: respuesta guardada y normalizada");
        Check(controller.Respond(invitation.Slug, new RsvpForm { Name = "Prueba ausencia", Attending = false, GuestCount = 1 }) is RedirectResult, "Formulario: ausencia válida");
        Check(db.GuestResponses.AnyAsync(x => x.Name == "Prueba ausencia" && x.GuestCount == 0 && !x.Attending).Result, "Formulario: ausencia no suma personas");
        Check(controller.PreviewResponse(invitation.Id, new RsvpForm { Name = "Prueba sin guardar", Attending = true, GuestCount = 1 }) is RedirectResult && db.GuestResponses.CountAsync().Result == initial + 2, "Vista previa no guarda respuestas");
        invitation.IsPublished = false; Check(store.Save(invitation) == null, "Preparar borrador");
        Check(controller.Respond(invitation.Slug, new RsvpForm { Name = "No debe guardarse", Attending = true }) is NotFoundResult, "Borrador no admite respuestas públicas");
        invitation.IsPublished = true; invitation.RsvpEnabled = false; Check(store.Save(invitation) == null, "Desactivar formulario");
        Check(controller.Respond(invitation.Slug, new RsvpForm { Name = "No debe guardarse", Attending = true }) is NotFoundResult, "Formulario desactivado no admite respuestas");
        invitation.RsvpEnabled = true; Check(store.Save(invitation) == null, "Restaurar configuración de prueba");
    }
    static void CheckGuests(InvitationDbContext db, InvitationStore store, InvitationConfiguration invitation)
    {
        var http = new DefaultHttpContext();
        var controller = new GuestsController(db, store) { ControllerContext = new ControllerContext { HttpContext = http }, TempData = new TempDataDictionary(http, new MemoryTempDataProvider()) };
        Check(controller.Save(invitation.Id, new GuestEditor { Name = " María   Pérez ", InvitedCount = 2 }) is RedirectToActionResult, "Invitado: crear");
        var entityId = db.Invitations.Single(x => x.PublicId == invitation.Id).Id;
        var guest = db.Guests.Single(x => x.InvitationId == entityId && x.Name == "María   Pérez");
        Check(GuestMatching.FindUnique(db, entityId, "maria perez") == guest.Id, "Coincidencia normalizada única");
        var publicController = new InvitationsController(store, db) { ControllerContext = new ControllerContext { HttpContext = http }, TempData = new TempDataDictionary(http, new MemoryTempDataProvider()) };
        Check(publicController.Respond(invitation.Slug, new RsvpForm { Name = "maria perez", Attending = true, GuestCount = 2 }) is RedirectResult, "RSVP: asociar invitado");
        Check(db.GuestResponses.Any(x => x.GuestId == guest.Id && x.Attending && x.GuestCount == 2), "RSVP: vínculo persistido");
        Check(controller.Record(invitation.Id, guest.Id, new RsvpForm { Attending = false, GuestCount = 1 }) is RedirectToActionResult, "Administrador: registrar ausencia");
        Check(db.GuestResponses.Where(x => x.GuestId == guest.Id).OrderByDescending(x => x.SubmittedAtUtc).ThenByDescending(x => x.Id).First().GuestCount == 0, "Última respuesta no suma y conserva historial");
        var count = db.GuestResponses.Count();
        controller.Record(invitation.Id, guest.Id, new RsvpForm { Attending = true, GuestCount = 3 });
        Check(db.GuestResponses.Count() == count, "Rechazar cantidad superior al cupo desde administración");
        controller.Save(invitation.Id, new GuestEditor { Name = "Maria Perez", InvitedCount = 1 });
        Check(GuestMatching.FindUnique(db, entityId, "maria perez") == null, "Homónimos requieren asociación manual");
        var duplicate = db.Guests.Single(x => x.InvitationId == entityId && x.Name == "Maria Perez");
        controller.Archive(invitation.Id, duplicate.Id);
        Check(duplicate.IsArchived && GuestMatching.FindUnique(db, entityId, "maria perez") == guest.Id, "Archivados no se asocian automáticamente");
        controller.Archive(invitation.Id, duplicate.Id, true);
        Check(!duplicate.IsArchived, "Recuperar invitado");
        controller.Membership(invitation.Id, duplicate.Id, true);
        Check(duplicate.IsWaitlisted && GuestMatching.FindUnique(db, entityId, "maria perez") == guest.Id, "Lista de espera no se asocia automáticamente");
        var waitingCount = db.GuestResponses.Count();
        Check(controller.Record(invitation.Id, duplicate.Id, new RsvpForm { Attending = true, GuestCount = 1 }) is NotFoundResult && db.GuestResponses.Count() == waitingCount, "No confirmar invitados en espera desde administración");
        controller.Membership(invitation.Id, duplicate.Id, false);
        Check(!duplicate.IsWaitlisted, "Promover a lista fija");
        var validation = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var missingCountForm = new RsvpForm { Name = "Pepita", Attending = true };
        Check(!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(missingCountForm, new System.ComponentModel.DataAnnotations.ValidationContext(missingCountForm), validation, true), "Cantidad explícita obligatoria al confirmar");
        validation.Clear();
        var absentForm = new RsvpForm { Name = "Pepita", Attending = false };
        Check(System.ComponentModel.DataAnnotations.Validator.TryValidateObject(absentForm, new System.ComponentModel.DataAnnotations.ValidationContext(absentForm), validation, true), "Ausencia no requiere cantidad");
        Check(publicController.Respond(invitation.Slug, new RsvpForm { Name = "Tía Pepita y tío Pepito", Attending = true, GuestCount = 2 }) is RedirectResult && db.GuestResponses.Any(x => x.InvitationId == entityId && x.Name == "Tía Pepita y tío Pepito" && x.GuestCount == 2), "Dos nombres en una respuesta cuentan dos por cantidad");
        Check(publicController.Respond(invitation.Slug, new RsvpForm { Name = "Tía Pepita + uno", Attending = true, GuestCount = 2 }) is RedirectResult && db.GuestResponses.Any(x => x.InvitationId == entityId && x.Name == "Tía Pepita + uno" && x.GuestCount == 2), "Acompañante sin nombre cuenta en cantidad total");
        var other = new InvitationConfiguration { Slug = "otro-" + Guid.NewGuid().ToString("N") };
        Check(store.Save(other) == null, "Preparar otra invitación");
        var response = db.GuestResponses.First(x => x.GuestId == guest.Id);
        Check(controller.Link(other.Id, response.Id, guest.Id) is NotFoundResult, "No se modifican respuestas de otra invitación");
        Check(controller.Edit(other.Id, guest.Id) is NotFoundResult && controller.Archive(other.Id, guest.Id) is NotFoundResult, "No editar ni archivar invitados de otra invitación");
        Check(controller.Link(invitation.Id, response.Id, duplicate.Id) is RedirectToActionResult && response.GuestId == duplicate.Id, "Asociación manual");
        Check(controller.Link(invitation.Id, response.Id, null) is RedirectToActionResult && response.GuestId == null, "Desvincular respuesta");
        controller.Archive(invitation.Id, guest.Id);
        Check(db.GuestResponses.Any(x => x.GuestId == guest.Id), "Archivar conserva respuestas");
        db.Invitations.Remove(db.Invitations.Single(x => x.PublicId == other.Id)); db.SaveChanges();
    }
    class MemoryTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
    static void CheckOrganization(InvitationDbContext db, InvitationStore store, InvitationConfiguration invitation)
    {
        var http = new DefaultHttpContext();
        GuestsController Controller() => new GuestsController(db, store) { ControllerContext = new ControllerContext { HttpContext = http }, TempData = new TempDataDictionary(http, new MemoryTempDataProvider()) };
        Check(typeof(GuestsController).GetMethod("Save").GetParameters()[0].GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.FromRouteAttribute), false).Length == 1, "Guardar: ID de invitación se toma de la ruta, no del ID del invitado");
        var controller = Controller(); var entityId = db.Invitations.Single(x => x.PublicId == invitation.Id).Id;
        controller.SaveTag(invitation.Id, 0, "Familia", "#54745b"); controller.SaveTable(invitation.Id, 0, "Mesa 1", 8);
        var tag = db.GuestTags.Single(x => x.InvitationId == entityId); var table = db.SeatingTables.Single(x => x.InvitationId == entityId);
        var editor = new GuestEditor { Name = "Grupo de organización", InvitedCount = 2, TableId = table.Id, TagIds = new() { tag.Id } };
        Check(controller.Save(invitation.Id, editor) is RedirectToActionResult, "Organización: guardar grupo con mesa y etiqueta");
        var guest = db.Guests.Single(x => x.Name == editor.Name && x.InvitationId == entityId); editor.Id = guest.Id;
        Check(guest.TableId == table.Id && db.GuestTagAssignments.Any(x => x.GuestId == guest.Id && x.TagId == tag.Id), "Mesa y etiquetas persistidas");
        Check(controller.Save(invitation.Id, editor) is RedirectToActionResult, "Reeditar sin duplicar etiquetas");
        controller.SaveTable(invitation.Id, table.Id, "Mesa familia", 1);
        Check(table.Capacity == 1, "Capacidad editable incluso con exceso para revisar");
        var other = new InvitationConfiguration { Slug = "organizacion-" + Guid.NewGuid().ToString("N") }; Check(store.Save(other) == null, "Crear otra invitación");
        Check(controller.AssignTable(other.Id, guest.Id, table.Id) is NotFoundResult, "No asignar invitados de otra invitación");
        Check(controller.RemoveTag(other.Id, tag.Id) is NotFoundResult && controller.RemoveTable(other.Id, table.Id) is NotFoundResult, "No eliminar etiquetas ni mesas ajenas");
        var otherId = db.Invitations.Single(x => x.PublicId == other.Id).Id;
        var foreignTable = new Application.SeatingTable { InvitationId = otherId, Name = "Ajena", Capacity = 8 }; db.SeatingTables.Add(foreignTable); db.SaveChanges();
        Check(controller.AssignTable(invitation.Id, guest.Id, foreignTable.Id) is BadRequestResult, "Mesa ajena rechazada");
        var foreignTag = new Application.GuestTag { InvitationId = otherId, Name = "Ajena" }; db.GuestTags.Add(foreignTag); db.SaveChanges();
        editor.TagIds = new() { foreignTag.Id };
        Check(Controller().Save(invitation.Id, editor) is ViewResult && !db.GuestTagAssignments.Any(x => x.GuestId == guest.Id && x.TagId == foreignTag.Id), "Etiqueta ajena rechazada");
        controller.AssignTable(invitation.Id, guest.Id, null); Check(guest.TableId == null, "Quitar asignación");
        controller.AssignTable(invitation.Id, guest.Id, table.Id);
        controller.RemoveTable(invitation.Id, table.Id); Check(guest.TableId == null && db.Guests.Any(x => x.Id == guest.Id), "Eliminar mesa conserva invitados sin mesa");
        controller.RemoveTag(invitation.Id, tag.Id); Check(!db.GuestTagAssignments.Any(x => x.TagId == tag.Id), "Eliminar etiqueta limpia asociaciones");
        db.Invitations.Remove(db.Invitations.Single(x => x.PublicId == other.Id)); db.SaveChanges();
    }
    static void CheckMySql()
    {
        using (var db = new AdministrationEvent.Data.InvitationDbContextFactory().CreateDbContext(new string[0]))
        using (var transaction = db.Database.BeginTransaction())
        {
            var store = new InvitationStore(db);
            var item = new InvitationConfiguration { Title = "Verificación temporal", Slug = "verificacion-" + Guid.NewGuid().ToString("N"), Date = new DateTime(2027,3,20,18,0,0), Sections = new List<InvitationSection> {
                new InvitationSection { Title = "Ceremonia", Content = "Primera sección" },
                new InvitationSection { Title = "Oculta", Enabled = false }
            }};
            item.Theme.HeadingFontUrl = "/fonts/test.woff2";
            item.Theme.SpotifyTrackUrl = "https://open.spotify.com/embed/track/2TD8Bhc5hFPKh52lFJ2Ec5?theme=0";
            item.Theme.Layout = "vineyard"; item.ShowCountdown = true; item.HasConfirmedTime = false; item.RsvpEnabled = true;
            item.CoverImageUrl = "/images/zenia-diego/vineyard-watercolor.png";
            Check(store.Save(item) == null, "MySQL: crear invitación");
            var restored = store.Find(item.Id);
            Check(restored != null && restored.Date == item.Date && restored.Sections.Count == 2 && !restored.Sections[1].Enabled, "MySQL: leer fecha y secciones");
            Check(restored.Theme.HeadingFontUrl == item.Theme.HeadingFontUrl, "MySQL: fuente persistida");
            Check(store.FindPublished(item.Slug) == null, "MySQL: borrador privado");
            restored.IsPublished = true; restored.Theme.Accent = "#123456"; restored.Theme.Layout = "relaxed";
            restored.Sections.RemoveAt(0); restored.Sections[0].Title = "Nueva primera";
            Check(store.Save(restored) == null, "MySQL: actualizar");
            var published = store.FindPublished(item.Slug);
            Check(published.ShowCountdown && !published.HasConfirmedTime && published.Theme.Layout == "relaxed" && published.RsvpEnabled, "MySQL: opciones de diseño persistidas");
            CheckRsvp(db, store, published);
            CheckGuests(db, store, published);
            CheckOrganization(db, store, published);
            Check(published.Theme.Accent == "#123456" && published.Sections.Count == 1 && published.Sections[0].Title == "Nueva primera", "MySQL: tema y secciones actualizados");
            Check(store.Save(new InvitationConfiguration { Slug = item.Slug }) != null, "MySQL: enlace duplicado rechazado");
            db.Invitations.Remove(db.Invitations.SingleAsync(x => x.PublicId == item.Id).Result); db.SaveChanges();
            Check(store.Find(item.Id) == null, "MySQL: eliminación");
            transaction.Rollback();
        }
        Console.WriteLine("MySQL: creación, lectura, actualización, publicación y eliminación verificadas; transacción revertida, sin datos de prueba guardados.");
    }

}
