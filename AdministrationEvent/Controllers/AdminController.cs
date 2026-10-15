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
        [HttpGet("denied")] public IActionResult Denied() { Response.StatusCode = 403; return View(); }
        [HttpGet("")] public IActionResult Index() => View(store.All());
        [AllowAnonymous, HttpGet("login")] public IActionResult Login() => View(new AccountLogin());
        [AllowAnonymous, HttpPost("login"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AccountLogin form, [FromServices] AdminAccounts accounts)
        {
            if (!ModelState.IsValid) return View(form);
            accounts.BootstrapOwner();
            var user = accounts.Authenticate(form.Username, form.Password);
            if (user == null) { ModelState.AddModelError("", "No pudimos iniciar sesión. Revisá tus datos o intentá más tarde."); return View(new AccountLogin { Username = form.Username }); }
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.DisplayName),
                new Claim(ClaimTypes.Role, user.IsOwner ? "Owner" : "Manager"), new Claim("stamp", user.SecurityStamp)
            }, CookieAuthenticationDefaults.AuthenticationScheme)));
            return RedirectToAction(nameof(Index));
        }
        [HttpPost("logout"), ValidateAntiForgeryToken] public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(); return RedirectToAction(nameof(Login)); }
        [HttpPost("save-date/{id:guid}"), ValidateAntiForgeryToken]
        public IActionResult SaveDate(Guid id) {
            var error = store.CreateSaveTheDate(id, out var invitation);
            if (error != null) { TempData["Error"] = error; return RedirectToAction(nameof(Index)); }
            return RedirectToAction(nameof(Edit), new { id = invitation.Id });
        }
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
