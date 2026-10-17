using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using AdministrationEvent.Models;
using AdministrationEvent.Services;
namespace AdministrationEvent.Controllers
{
    [Authorize(Roles = "Owner"), Route("admin")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class AdminController : Controller
    {
        private readonly InvitationStore store;
        private readonly IConfiguration config;
        private readonly InvitationDbContext db;
        private readonly IWebHostEnvironment environment;
        public AdminController(InvitationStore store, IConfiguration config, InvitationDbContext db, IWebHostEnvironment environment) { this.store = store; this.config = config; this.db = db; this.environment = environment; }
        [HttpGet("responses/{id:guid}")]
        public IActionResult Responses(Guid id) {
            if (store.Find(id) == null) return NotFound();
            return RedirectToAction("Index", "Guests", new { id });
        }
        [AllowAnonymous, HttpGet("denied")] public IActionResult Denied() { Response.StatusCode = 403; return View(); }
        [HttpGet("")] public IActionResult Index() => View(store.All());
        [AllowAnonymous, HttpGet("login")] public IActionResult Login(string returnUrl = null) { ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : ""; return View(new AccountLogin()); }
        [AllowAnonymous, HttpPost("login"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AccountLogin form, string returnUrl, [FromServices] AdminAccounts accounts)
        {
            ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : "";
            if (!ModelState.IsValid) return View(form);
            accounts.BootstrapOwner();
            var user = accounts.Authenticate(form.Username, form.Password);
            if (user == null) { ModelState.AddModelError("", "No pudimos iniciar sesión. Revisá tus datos o intentá más tarde."); return View(new AccountLogin { Username = form.Username }); }
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.DisplayName),
                new Claim(ClaimTypes.Role, user.IsOwner ? "Owner" : "Viewer"), new Claim("stamp", user.SecurityStamp)
            }, CookieAuthenticationDefaults.AuthenticationScheme)));
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : Redirect(user.IsOwner ? "/admin" : "/mis-invitaciones");
        }
        [AllowAnonymous, HttpPost("logout"), ValidateAntiForgeryToken] public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(); return RedirectToAction(nameof(Login)); }
        [HttpGet("delete/{id:guid}")]
        public IActionResult Delete(Guid id) {
            var invitation = store.Find(id);
            if (invitation == null) return NotFound();
            return View(invitation);
        }
        [HttpPost("delete/{id:guid}"), ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(Guid id, string confirmation) {
            if (confirmation != "ELIMINAR") { ModelState.AddModelError("", "Escribí ELIMINAR para confirmar."); return Delete(id); }
            if (!store.Delete(id)) return NotFound();
            TempData["Success"] = "Invitación eliminada.";
            return RedirectToAction(nameof(Index));
        }
        [HttpGet("new-beach")]
        public IActionResult CreateBeach() {
            var invitation = BeachInvitation.Create();
            ViewData["New"] = true;
            return View("Edit", new InvitationEditor { Id = invitation.Id, ConfigurationJson = JsonSerializer.Serialize(invitation, InvitationStore.JsonOptions) });
        }
        [HttpGet("new")] public IActionResult Create() { var invitation = new InvitationConfiguration { AssetCollection = "invitacion-" + Guid.NewGuid().ToString("N") }; ViewData["New"] = true; return View("Edit", new InvitationEditor { Id = invitation.Id, ConfigurationJson = JsonSerializer.Serialize(invitation, InvitationStore.JsonOptions) }); }
        [HttpGet("edit/{id:guid}")]
        public IActionResult Edit(Guid id) { var item = store.Find(id); return item == null ? (IActionResult)NotFound() : View(new InvitationEditor { Id = id, ConfigurationJson = JsonSerializer.Serialize(item, InvitationStore.JsonOptions) }); }
        [HttpGet("assets")]
        public IActionResult Assets(Guid invitationId)
        {
            var invitation = store.Find(invitationId);
            var collection = AssetCollectionFor(invitation, invitationId);
            var webRoot = environment.WebRootPath;
            var imagesRoot = Path.Combine(webRoot, "images");
            var fontsRoot = Path.Combine(webRoot, "fonts");
            var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif", ".svg" };
            var fontExtensions = new[] { ".ttf", ".otf", ".woff", ".woff2" };
            var collections = new[] { "shared", collection }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            var images = collections.SelectMany(collection => {
                var root = Path.Combine(imagesRoot, collection);
                return Directory.Exists(root) ? Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories) : Enumerable.Empty<string>();
            }).Where(path => imageExtensions.Contains(Path.GetExtension(path).ToLowerInvariant())).OrderBy(path => path).Select(path => "/images/" + Path.GetRelativePath(imagesRoot, path).Replace(Path.DirectorySeparatorChar, '/')).ToList();
            var fonts = Directory.Exists(fontsRoot) ? Directory.EnumerateFiles(fontsRoot, "*", SearchOption.AllDirectories).Where(path => fontExtensions.Contains(Path.GetExtension(path).ToLowerInvariant())).OrderBy(path => path).Select(path => (object)new { name = Path.GetFileNameWithoutExtension(path).Replace('-', ' '), url = "/fonts/" + Path.GetRelativePath(fontsRoot, path).Replace(Path.DirectorySeparatorChar, '/') }).ToList() : new System.Collections.Generic.List<object>();
            return Json(new { images, fonts });
        }
        [HttpPost("assets/upload"), ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAsset(Guid invitationId, IFormFile file)
        {
            var invitation = store.Find(invitationId);
            var collection = AssetCollectionFor(invitation, invitationId);
            if (file == null || file.Length == 0 || file.Length > 10 * 1024 * 1024) return BadRequest(new { error = "Elegí una imagen de hasta 10 MB." });
            var extensions = new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!extensions.Contains(extension) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return BadRequest(new { error = "Solo se permiten imágenes PNG, JPG, WebP o GIF." });
            var folder = Path.Combine(environment.WebRootPath, "images", collection); Directory.CreateDirectory(folder);
            var fileName = Guid.NewGuid().ToString("N") + extension;
            await using (var stream = System.IO.File.Create(Path.Combine(folder, fileName))) await file.CopyToAsync(stream);
            return Json(new { url = "/images/" + collection + "/" + fileName });
        }

        private static string AssetCollectionFor(InvitationConfiguration invitation, Guid invitationId)
        {
            if (invitation != null) return invitation.AssetCollection;
            // Una invitación nueva ya recibe un ID y una colección única desde el editor.
            // Así puede elegir fuentes y subir imágenes antes de su primer guardado, sin ver
            // imágenes privadas de otras tarjetas.
            return "invitacion-" + invitationId.ToString("N");
        }
        [HttpPost("edit/{id:guid}"), ValidateAntiForgeryToken]
        public IActionResult Edit(Guid id, InvitationEditor editor)
        {
            editor.Id = id;
            if (store.Find(id) == null) ViewData["New"] = true;
            if (ModelState.IsValid)
            {
                try {
                    var item = JsonSerializer.Deserialize<InvitationConfiguration>(editor.ConfigurationJson, InvitationStore.JsonOptions);
                    if (item == null) ModelState.AddModelError("", "La configuración debe ser un objeto JSON.");
                    else { item.Id = id; var error = store.Save(item); if (error == null) { TempData["Success"] = "Invitación guardada."; return RedirectToAction(nameof(Edit), new { id }); } ModelState.AddModelError("", error); }
                } catch (JsonException) { ModelState.AddModelError("", "El JSON no es válido. Revisá las comas, comillas y tipos de datos."); }
            }
            return View(editor);
        }
    }
}
