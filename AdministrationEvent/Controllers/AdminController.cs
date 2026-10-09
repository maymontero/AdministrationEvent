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
using AdministrationEvent.Models;
using AdministrationEvent.Services;
namespace AdministrationEvent.Controllers
{
    [Authorize, Route("admin")]
    public class AdminController : Controller
    {
        private readonly InvitationStore store;
        private readonly IConfiguration config;
        private readonly InvitationDbContext db;
        public AdminController(InvitationStore store, IConfiguration config, InvitationDbContext db) { this.store = store; this.config = config; this.db = db; }
        [HttpGet("responses/{id:guid}")]
        public IActionResult Responses(Guid id) {
            if (store.Find(id) == null) return NotFound();
            return RedirectToAction("Index", "Guests", new { id });
        }
        [HttpGet("")] public IActionResult Index() => View(store.All());
        [AllowAnonymous, HttpGet("login")] public IActionResult Login() { ViewData["Configured"] = !string.IsNullOrWhiteSpace(config["Admin:Password"]); return View(); }
        [AllowAnonymous, HttpPost("login"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string password)
        {
            var expected = config["Admin:Password"];
            if (string.IsNullOrWhiteSpace(expected) || password != expected) { ModelState.AddModelError("", "No se pudo iniciar sesión. Verificá la contraseña y la configuración del administrador."); return Login(); }
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "Administradora") }, CookieAuthenticationDefaults.AuthenticationScheme)));
            return RedirectToAction(nameof(Index));
        }
        [HttpPost("logout"), ValidateAntiForgeryToken] public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(); return RedirectToAction(nameof(Login)); }
        [HttpGet("new")] public IActionResult Create() { ViewData["New"] = true; return View("Edit", new InvitationEditor { Id = Guid.NewGuid(), ConfigurationJson = JsonSerializer.Serialize(new InvitationConfiguration(), InvitationStore.JsonOptions) }); }
        [HttpGet("edit/{id:guid}")]
        public IActionResult Edit(Guid id) { var item = store.Find(id); return item == null ? (IActionResult)NotFound() : View(new InvitationEditor { Id = id, ConfigurationJson = JsonSerializer.Serialize(item, InvitationStore.JsonOptions) }); }
        [HttpPost("edit/{id:guid}"), ValidateAntiForgeryToken]
        public IActionResult Edit(Guid id, InvitationEditor editor)
        {
            editor.Id = id;
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
