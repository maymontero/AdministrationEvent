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
    static System.Security.Claims.ClaimsPrincipal Principal(long id) => new(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, id.ToString()) }, "test"));
    static void CheckVisualDesign(InvitationDbContext db, InvitationStore store, InvitationConfiguration invitation)
    {
        var owner=db.AdminUsers.First(x=>x.IsOwner);
        var http=new DefaultHttpContext { User=Principal(owner.Id) };
        var permissions=new InvitationPermissions(db);
        var controller=new DesignController(db,store,permissions,null,null) { ControllerContext=new ControllerContext { HttpContext=http } };
        var doc=DesignController.Starter(invitation);
        Check(doc.Validate()==null, "Canvas: diseño inicial válido");
        doc.Sections[0].Elements[0].Font="CanvasFonttest";
        doc.Sections[0].Elements[0].FontUrl="/fonts/library/Bright Darling Script.otf";
        Check(doc.Validate()==null, "Canvas: aceptar fuentes locales compartidas con espacios");
        doc.Sections[0].Elements[0].FontUrl="https://example.com/font.ttf";
        Check(doc.Validate()!=null, "Canvas: rechazar fuentes fuera de la biblioteca local");
        doc.Sections[0].Elements[0].FontUrl="/fonts/library/Bright Darling Script.otf";
        var input=new DesignController.SaveInput { Document=doc,Version=0 };
        Check(controller.Save(invitation.Id,input) is JsonResult, "Canvas: guardar borrador");
        var entityId=db.Invitations.Single(x=>x.PublicId==invitation.Id).Id;
        var design=db.VisualDesigns.Single(x=>x.InvitationId==entityId);
        Check(design.PublishedJson==null, "Canvas: guardar no altera diseño publicado");
        Check(design.DraftJson.Contains("Bright Darling Script.otf"), "Canvas: conservar fuente al guardar");
        Check(controller.Save(invitation.Id,input) is ConflictObjectResult, "Canvas: rechazar versión obsoleta");
        Check(controller.Activate(invitation.Id,true) is OkResult && design.PublishedJson==design.DraftJson, "Canvas: aplicar instantánea");
        var applied=design.PublishedJson;input.Version=design.Version;doc.Sections[0].Elements[0].Text="Nuevo borrador";
        Check(controller.Save(invitation.Id,input) is JsonResult && design.PublishedJson==applied, "Canvas: seguir editando conserva versión aplicada");
        doc.Sections[0].Elements[0].Link="javascript:alert(1)";
        Check(controller.Save(invitation.Id,input) is BadRequestObjectResult, "Canvas: rechazar enlaces ejecutables");
        var viewer=new Application.AdminUser { Username="canvas-test",NormalizedUsername="CANVAS-TEST",DisplayName="Diseñadora" };db.AdminUsers.Add(viewer);db.SaveChanges();
        http.User=Principal(viewer.Id);
        Check(controller.Index(invitation.Id) is NotFoundResult, "Canvas: sin permiso no se puede editar");
        var access=new Application.InvitationAccess { UserId=viewer.Id,InvitationId=entityId };db.InvitationAccesses.Add(access);db.SaveChanges();
        Check(controller.Index(invitation.Id) is NotFoundResult, "Canvas: permiso de lectura no permite editar");
        access.CanDesign=true;db.SaveChanges();
        Check(controller.Index(invitation.Id) is ViewResult, "Canvas: permiso explícito permite editar");
        access.AvailableFromUtc=DateTime.UtcNow.AddDays(1);db.SaveChanges();
        Check(controller.Index(invitation.Id) is NotFoundResult, "Canvas: respetar fecha de acceso");
        http.User=Principal(owner.Id);Check(controller.Activate(invitation.Id,false) is OkResult && design.PublishedJson==null, "Canvas: restaurar diseño anterior");
        db.AdminUsers.Remove(viewer);db.VisualDesigns.Remove(design);db.SaveChanges();
    }
    static void CheckSeatingPlan(InvitationDbContext db, InvitationStore store, InvitationConfiguration invitation)
    {
        var controller = new SeatingPlanController(db, store);
        var previouslyEnabled = invitation.SeatingPlanEnabled;
        invitation.SeatingPlanEnabled = false; Check(store.Save(invitation) == null, "Plano: deshabilitar por invitación");
        Check(controller.Index(invitation.Id) is NotFoundResult && controller.AddTable(invitation.Id, "No crear", 8) is NotFoundResult, "Plano: desactivado bloquea acceso y creación");
        invitation.SeatingPlanEnabled = true; Check(store.Save(invitation) == null && store.Find(invitation.Id).SeatingPlanEnabled, "Plano: habilitación persistida");
        var entityId = db.Invitations.Single(x => x.PublicId == invitation.Id).Id;
        Check(controller.AddTable(invitation.Id, "Plano prueba", 8) is JsonResult, "Plano: crear mesa vinculada a invitados");
        var table = db.SeatingTables.Single(x => x.InvitationId == entityId && x.Name == "Plano prueba");
        var input = new SeatingPlanController.PlanInput { Tables = new() { new() { TableId = table.Id, X = 30, Y = 40, Shape = "rectangle", Rotation = 45, Description = "Amigas" } } };
        Check(controller.Save(invitation.Id, input) is OkResult, "Plano: guardar posición y forma");
        Check(db.SeatingPlans.Single(x => x.InvitationId == entityId).LayoutJson.Contains("Amigas"), "Plano: distribución persistida");
        input.Tables[0].X = 100;
        Check(controller.Save(invitation.Id, input) is BadRequestObjectResult, "Plano: rechazar posiciones fuera del salón");
        input.Tables[0].X = 40; input.Tables[0].TableId = long.MaxValue;
        Check(controller.Save(invitation.Id, input) is BadRequestObjectResult, "Plano: rechazar mesas ajenas");
        input.Tables.Clear(); input.UseVenueImage = true;
        Check(controller.Save(invitation.Id, input) is BadRequestObjectResult, "Plano: no usar imagen inexistente");
        input.UseVenueImage = false;
        Check(controller.Save(invitation.Id, input) is OkResult && db.SeatingTables.Any(x => x.Id == table.Id), "Plano: quitar posición conserva mesa");
        using var stream = new System.IO.MemoryStream(new byte[] { 60,115,118,103,62 });
        var upload = new FormFile(stream, 0, stream.Length, "file", "fake.png");
        Check(controller.Upload(invitation.Id, upload).GetAwaiter().GetResult() is BadRequestObjectResult, "Plano: rechazar archivos que no son PNG/JPG");
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jH1sAAAAASUVORK5CYII=");
        using var pngStream = new System.IO.MemoryStream(png);
        Check(controller.Upload(invitation.Id, new FormFile(pngStream, 0, png.Length, "file", "plano.png")).GetAwaiter().GetResult() is OkResult, "Plano: subir PNG");
        input.UseVenueImage = true;
        Check(controller.Save(invitation.Id, input) is OkResult && controller.Image(invitation.Id) is FileContentResult image && image.FileContents.SequenceEqual(png), "Plano: usar y recuperar imagen privada");
        Check(controller.Image(Guid.NewGuid()) is NotFoundResult, "Plano: imagen limitada a su invitación");
        invitation.SeatingPlanEnabled = false; Check(store.Save(invitation) == null, "Plano: desactivar con distribución existente");
        Check(controller.Image(invitation.Id) is NotFoundResult && db.SeatingPlans.Any(x => x.InvitationId == entityId), "Plano: ocultar conserva datos y bloquea imagen");
        invitation.SeatingPlanEnabled = true; Check(store.Save(invitation) == null && controller.Image(invitation.Id) is FileContentResult, "Plano: reactivar recupera distribución");
        db.SeatingPlans.RemoveRange(db.SeatingPlans.Where(x => x.InvitationId == entityId)); db.SeatingTables.Remove(table); db.SaveChanges();
        invitation.SeatingPlanEnabled = previouslyEnabled; Check(store.Save(invitation) == null, "Plano: restaurar opción original");
    }
    static void CheckPermissions(InvitationDbContext db, InvitationStore store, InvitationConfiguration invitation)
    {
        var viewer = new Application.AdminUser { Username = "viewer-test", NormalizedUsername = "VIEWER-TEST", DisplayName = "Amiga" };
        db.AdminUsers.Add(viewer); db.SaveChanges();
        var principal = Principal(viewer.Id); var permissions = new InvitationPermissions(db); var now = DateTime.UtcNow;
        var http = new DefaultHttpContext { User = principal };
        var controller = new InvitationsController(store, db) { ControllerContext = new ControllerContext { HttpContext = http } };
        http.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());
        Check(controller.Show(invitation.Slug) is ViewResult, "Publicada se abre sin cuenta ni permiso");
        controller.TempData = new TempDataDictionary(http, new MemoryTempDataProvider());
        var responses = db.GuestResponses.Count();
        Check(controller.Respond(invitation.Slug, new RsvpForm { Name = "Público", Attending = true, GuestCount = 1 }) is RedirectResult && db.GuestResponses.Count() == responses + 1, "RSVP público no requiere cuenta");
        db.GuestResponses.Remove(db.GuestResponses.Single(x => x.Name == "Público")); db.SaveChanges();
        invitation.IsPublished = false; Check(store.Save(invitation) == null, "Preparar borrador para compartir");
        Check(controller.Show(invitation.Slug) is NotFoundResult, "Borrador no se abre por enlace público");
        Check(controller.Preview(invitation.Id) is ChallengeResult, "Vista previa requiere sesión");
        http.User = principal;
        Check(controller.Preview(invitation.Id) is NotFoundResult, "Sin permiso no se puede ver borrador");
        Check(controller.PreviewResponse(invitation.Id, new RsvpForm()) is NotFoundResult, "Sin permiso no se puede probar RSVP de borrador");
        var access = new Application.InvitationAccess { UserId = viewer.Id, InvitationId = db.Invitations.Single(x => x.PublicId == invitation.Id).Id, AvailableFromUtc = now.AddHours(1) };
        db.InvitationAccesses.Add(access); db.SaveChanges();
        Check(!permissions.CanView(principal, invitation.Id, now), "Permiso futuro no habilita antes de tiempo");
        Check(permissions.CanView(principal, invitation.Id, now.AddHours(1)), "Permiso se habilita en la fecha exacta");
        access.AvailableFromUtc = null; db.SaveChanges();
        Check(controller.Preview(invitation.Id) is ViewResult, "Permiso permite ver borrador");
        var other = new InvitationConfiguration { Slug = "privada-" + Guid.NewGuid().ToString("N"), IsPublished = false };
        Check(store.Save(other) == null && controller.Preview(other.Id) is NotFoundResult, "Permiso no habilita otros borradores");
        Check(permissions.CanView(Principal(db.AdminUsers.First(x => x.IsOwner).Id), other.Id, now), "Administradora puede ver todas");
        viewer.IsActive = false; db.SaveChanges();
        Check(!permissions.CanView(principal, invitation.Id, now), "Desactivar cuenta bloquea permiso existente");
        viewer.IsActive = true; db.InvitationAccesses.Remove(access); db.SaveChanges();
        Check(controller.Preview(invitation.Id) is NotFoundResult, "Revocar permiso bloquea vista previa");
        invitation.IsPublished = true; Check(store.Save(invitation) == null, "Restaurar publicada");
        Check(controller.Show(invitation.Slug) is ViewResult, "Publicada permanece pública sin permiso");
        invitation.SharingMode = "link"; invitation.ShareKey = "";
        Check(store.Save(invitation) == null, "Publicar con enlace privado");
        var privateLink = store.Find(invitation.Id);
        Check(privateLink.ShareKey?.Length == 48 && controller.Show(privateLink.Slug) is NotFoundResult && controller.Shared(privateLink.Slug, privateLink.ShareKey) is ViewResult, "Enlace privado solo abre con su clave");
        invitation.SharingMode = "public"; invitation.ShareKey = "";
        Check(store.Save(invitation) == null && controller.Show(invitation.Slug) is ViewResult, "Volver a acceso público elimina la clave privada");
        db.AdminUsers.Remove(viewer); db.Invitations.Remove(db.Invitations.Single(x => x.PublicId == other.Id)); db.SaveChanges();
        foreach (var type in new[] { typeof(AdminController), typeof(GuestsController) })
            Check(type.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true).Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Any(a => a.Roles == "Owner"), "Administración solo Owner");
    }
    static void CheckLifecycle(InvitationDbContext db, InvitationStore store, InvitationConfiguration source)
    {
        var initial = db.Invitations.Count();
        Check(store.CreateSaveTheDate(source.Id, out var draft, persist: false) == null, "Preparar copia sin guardarla");
        Check(db.Invitations.Count() == initial && store.Find(draft.Id) == null, "Abrir editor no crea una invitación");
        draft.Slug = source.Slug;
        Check(store.Save(draft) != null && db.Invitations.Count() == initial && store.Find(draft.Id) == null, "Error por enlace repetido no crea copia");
        draft.Slug = "lifecycle-" + Guid.NewGuid().ToString("N");
        Check(store.Save(draft) == null && store.Save(draft) == null && db.Invitations.Count() == initial + 1, "Repetir guardado del mismo formulario no duplica");
        var entityId = db.Invitations.Single(x => x.PublicId == draft.Id).Id;
        db.Guests.Add(new Application.Guest { InvitationId = entityId, Name = "Invitado temporal" });
        db.GuestResponses.Add(new Application.GuestResponse { InvitationId = entityId, Name = "Respuesta temporal", Attending = true, GuestCount = 1, SubmittedAtUtc = DateTime.UtcNow });
        db.InvitationAccesses.Add(new Application.InvitationAccess { InvitationId = entityId, UserId = db.AdminUsers.First(x => x.IsOwner).Id });
        db.SaveChanges();
        Check(store.Delete(draft.Id), "Eliminar invitación existente");
        Check(store.Find(draft.Id) == null && !db.Guests.Any(x => x.InvitationId == entityId) && !db.GuestResponses.Any(x => x.InvitationId == entityId) && !db.InvitationAccesses.Any(x => x.InvitationId == entityId), "Eliminación limpia invitados, respuestas y permisos");
        Check(store.Find(source.Id) != null && !store.Delete(draft.Id), "Eliminar no afecta otras invitaciones y no repite eliminación");
    }
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
            var item = new InvitationConfiguration { Slug = "prueba", AssetCollection = "prueba-privada", Date = new DateTime(2027,3,20,18,0,0), Sections = new List<InvitationSection> {
                new InvitationSection { Title = "Primera", Content = "Ceremonia", TitleFontSize = 28, DisplayMode = "art-above" },
                new InvitationSection { Title = "Oculta", Enabled = false }
            }};
            item.Theme.HeadingFontUrl = "/fonts/test.woff2";
            item.Theme.SpotifyTrackUrl = "https://open.spotify.com/embed/track/2TD8Bhc5hFPKh52lFJ2Ec5?theme=0";
            item.Theme.Layout = "vineyard"; item.ShowCountdown = true; item.HasConfirmedTime = false; item.RsvpEnabled = true;
            item.CoverImageUrl = "/images/zenia-diego/vineyard-watercolor.png";
            using (var db = new InvitationDbContext(options)) Check(new InvitationStore(db).Save(item) == null, "Crear invitación");
            using (var db = new InvitationDbContext(options)) {
                var store = new InvitationStore(db);
                Check(store.CreateSaveTheDate(item.Id, out var saveDate) == null, "Crear Save the date desde invitación");
                Check(saveDate.Id != item.Id && !saveDate.IsPublished && !saveDate.RsvpEnabled && !saveDate.HasConfirmedTime && saveDate.Sections.All(x => !x.Enabled), "Save the date independiente y en borrador");
                Check(saveDate.Date == item.Date && saveDate.Theme.SpotifyTrackUrl == item.Theme.SpotifyTrackUrl && store.Find(saveDate.Id).Theme.Layout == "save-date", "Fecha, música y diseño conservados");
                Check(store.Find(item.Id).Theme.Layout == "vineyard" && store.Find(item.Id).RsvpEnabled, "Invitación original conservada");
                Check(store.CreateSaveTheDate(item.Id, out var secondSaveDate) == null && secondSaveDate.Slug != saveDate.Slug, "Enlaces independientes al duplicar");
                db.Invitations.RemoveRange(db.Invitations.Where(x => x.PublicId == saveDate.Id || x.PublicId == secondSaveDate.Id)); db.SaveChanges();
            }
            using (var db = new InvitationDbContext(options)) {
                var store = new InvitationStore(db); var restored = store.Find(item.Id);
                Check(restored.Sections[0].TitleFontSize == 28, "Tamaño de título por sección persistido");
                Check(restored.Sections[0].DisplayMode == "art-above", "Tratamiento visual por sección persistido");
                Check(restored.AssetCollection == "prueba-privada", "Colección privada de imágenes persistida");
                Check(restored.Date == item.Date && restored.Sections.Count == 2 && !restored.Sections[1].Enabled, "Fecha y secciones persistidas");
                Check(restored.Theme.HeadingFontUrl == item.Theme.HeadingFontUrl, "Fuente persistida");
                Check(restored.Theme.SpotifyTrackUrl == item.Theme.SpotifyTrackUrl, "Canción Spotify persistida");
                restored.Theme.SpotifyTrackUrl = "https://open.spotify.com/playlist/37i9dQZF1DZ06evO3eIivx?si=test";
                Check(store.Save(restored) == null && store.Find(item.Id).Theme.SpotifyTrackUrl == restored.Theme.SpotifyTrackUrl, "Playlist Spotify persistida");
                restored.Theme.SpotifyTrackUrl = "https://open.spotify.com/embed/playlist/37i9dQZF1DZ06evO3eIivx";
                Check(store.Save(restored) == null, "Aceptar embed de playlist");
                restored.Theme.SpotifyTrackUrl = "https://example.com/track/2TD8Bhc5hFPKh52lFJ2Ec5";
                Check(store.Save(restored) != null, "Rechazar enlaces ajenos a Spotify");
                restored.Theme.SpotifyTrackUrl = "";
                Check(store.Save(restored) == null && store.Find(item.Id).Theme.SpotifyTrackUrl == "", "Quitar música guardada");
                restored.Theme.Layout = "custom";
                Check(store.Save(restored) == null && store.Find(item.Id).Theme.Layout == "custom", "Vista libre persistida");
                Check(restored.Theme.Layout == "custom" && restored.ShowCountdown && !restored.HasConfirmedTime && restored.RsvpEnabled && restored.TimeZone == "America/Montevideo", "Opciones de diseño persistidas");
                Check(store.FindPublished(item.Slug) == null, "Borrador privado");
                restored.IsPublished = true; restored.Theme.Accent = "#123456"; restored.Theme.Layout = "relaxed";
                restored.Sections.RemoveAt(0); restored.Sections[0].Title = "Nueva primera";
                Check(store.Save(restored) == null, "Actualizar invitación");
            }
            using (var db = new InvitationDbContext(options)) {
                var store = new InvitationStore(db); var restored = store.FindPublished(item.Slug);
                Check(restored != null && restored.Theme.Accent == "#123456" && restored.Theme.Layout == "relaxed", "Publicación y tema actualizado");
                Check(restored.Sections.Count == 1 && restored.Sections[0].Title == "Nueva primera", "Sin secciones obsoletas");
                CheckLifecycle(db, store, restored);
                CheckPermissions(db, store, restored);
                CheckRsvp(db, store, restored);
                CheckGuests(db, store, restored);
                CheckOrganization(db, store, restored);
                CheckSeatingPlan(db, store, restored);
                CheckVisualDesign(db, store, restored);
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
        http.User = Principal(db.AdminUsers.First(x => x.IsOwner).Id);
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
        http.User = Principal(db.AdminUsers.First(x => x.IsOwner).Id);
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
        http.User = Principal(db.AdminUsers.First(x => x.IsOwner).Id);
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
            var item = new InvitationConfiguration { Title = "Verificación temporal", Slug = "verificacion-" + Guid.NewGuid().ToString("N"), AssetCollection = "prueba-mysql", Date = new DateTime(2027,3,20,18,0,0), Sections = new List<InvitationSection> {
                new InvitationSection { Title = "Ceremonia", Content = "Primera sección", DisplayMode = "backdrop" },
                new InvitationSection { Title = "Oculta", Enabled = false }
            }};
            item.Theme.HeadingFontUrl = "/fonts/test.woff2";
            item.Theme.SpotifyTrackUrl = "https://open.spotify.com/embed/track/2TD8Bhc5hFPKh52lFJ2Ec5?theme=0";
            item.Theme.Layout = "vineyard"; item.ShowCountdown = true; item.HasConfirmedTime = false; item.RsvpEnabled = true;
            item.CoverImageUrl = "/images/zenia-diego/vineyard-watercolor.png";
            Check(store.Save(item) == null, "MySQL: crear invitación");
            var restored = store.Find(item.Id);
            Check(restored != null && restored.Date == item.Date && restored.Sections.Count == 2 && !restored.Sections[1].Enabled, "MySQL: leer fecha y secciones");
            Check(restored.Sections[0].DisplayMode == "backdrop", "MySQL: tratamiento visual por sección");
            Check(restored.AssetCollection == "prueba-mysql", "MySQL: colección privada de imágenes");
            Check(restored.Theme.HeadingFontUrl == item.Theme.HeadingFontUrl, "MySQL: fuente persistida");
            Check(store.FindPublished(item.Slug) == null, "MySQL: borrador privado");
            restored.IsPublished = true; restored.Theme.Accent = "#123456"; restored.Theme.Layout = "relaxed";
            restored.Sections.RemoveAt(0); restored.Sections[0].Title = "Nueva primera";
            Check(store.Save(restored) == null, "MySQL: actualizar");
            var published = store.FindPublished(item.Slug);
            Check(published.ShowCountdown && !published.HasConfirmedTime && published.Theme.Layout == "relaxed" && published.RsvpEnabled, "MySQL: opciones de diseño persistidas");
            CheckLifecycle(db, store, published);
            CheckPermissions(db, store, published);
                CheckRsvp(db, store, published);
            CheckGuests(db, store, published);
            CheckOrganization(db, store, published);
            CheckSeatingPlan(db, store, published);
            CheckVisualDesign(db, store, published);
            Check(published.Theme.Accent == "#123456" && published.Sections.Count == 1 && published.Sections[0].Title == "Nueva primera", "MySQL: tema y secciones actualizados");
            Check(store.Save(new InvitationConfiguration { Slug = item.Slug }) != null, "MySQL: enlace duplicado rechazado");
            db.Invitations.Remove(db.Invitations.SingleAsync(x => x.PublicId == item.Id).Result); db.SaveChanges();
            Check(store.Find(item.Id) == null, "MySQL: eliminación");
            transaction.Rollback();
        }
        Console.WriteLine("MySQL: creación, lectura, actualización, publicación y eliminación verificadas; transacción revertida, sin datos de prueba guardados.");
    }

}
