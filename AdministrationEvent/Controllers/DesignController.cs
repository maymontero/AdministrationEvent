using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AdministrationEvent.Models;
using AdministrationEvent.Services;
using Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace AdministrationEvent.Controllers
{
    [Authorize, Route("design/{id:guid}"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class DesignController : Controller
    {
        readonly InvitationDbContext db; readonly InvitationStore store; readonly InvitationPermissions permissions;
        readonly IWebHostEnvironment environment; readonly IConfiguration config;
        public static readonly JsonSerializerOptions CanvasJson = new(JsonSerializerDefaults.Web);
        public DesignController(InvitationDbContext db, InvitationStore store, InvitationPermissions permissions, IWebHostEnvironment environment, IConfiguration config) { this.db=db;this.store=store;this.permissions=permissions;this.environment=environment;this.config=config; }
        bool Allowed(Guid id) => permissions.CanDesign(User,id,DateTime.UtcNow);
        long EntityId(Guid id) => db.Invitations.Single(i=>i.PublicId==id).Id;
        public static CanvasDocument Starter(InvitationConfiguration invitation)
        {
            var doc=new CanvasDocument();
            var hero=new CanvasSection { Name="Portada",Background=invitation.Theme.Background };
            hero.Elements.Add(new CanvasElement { Type="title",Text=invitation.Hosts,FontSize=64,Y=60,Height=160 });
            hero.Elements.Add(new CanvasElement { Text=invitation.Message,Y=230,Height=120 });
            if(invitation.CoverImageUrl?.StartsWith("/images/") == true) hero.Elements.Add(new CanvasElement {Type="image",Image=invitation.CoverImageUrl,Y=380,Height=280});
            doc.Sections.Add(hero);
            foreach(var block in invitation.Sections.Where(s=>s.Enabled).Take(28)) {
                var section=new CanvasSection { Name=block.Title, Background=invitation.Theme.Background };
                section.Elements.Add(new CanvasElement {Type="title",Text=block.Title,FontSize=42,Y=40,Height=110});
                section.Elements.Add(new CanvasElement {Text=block.Content,Y=165,Height=180});
                if(block.ImageUrl?.StartsWith("/images/")==true) section.Elements.Add(new CanvasElement {Type="image",Image=block.ImageUrl,Y=370,Height=230});
                if(Uri.TryCreate(block.LinkUrl,UriKind.Absolute,out var url) && url.Scheme=="https") section.Elements.Add(new CanvasElement {Type="button",Text=block.LinkLabel,Link=block.LinkUrl,Y=615,Height=60});
                doc.Sections.Add(section);
            }
            return doc;
        }
        [HttpGet("")]
        public IActionResult Index(Guid id)
        {
            if(!Allowed(id))return NotFound(); var invitation=store.Find(id);
            var design=db.VisualDesigns.AsNoTracking().SingleOrDefault(d=>d.InvitationId==EntityId(id));
            ViewData["Document"]=design?.DraftJson ?? JsonSerializer.Serialize(Starter(invitation),CanvasJson); ViewData["Version"]=design?.Version ?? 0; ViewData["Active"]=design?.PublishedJson!=null;
            return View(invitation);
        }
        public class SaveInput { public CanvasDocument Document {get;set;} public int Version {get;set;} }
        [HttpPost(""), ValidateAntiForgeryToken, RequestSizeLimit(2*1024*1024)]
        public IActionResult Save(Guid id,[FromBody]SaveInput input)
        {
            if(!Allowed(id))return NotFound();
            if(!ModelState.IsValid || input?.Document==null)return BadRequest(new {error="El diseño no es válido."});
            var error=input.Document.Validate();if(error!=null)return BadRequest(new {error});
            var entityId=EntityId(id);var design=db.VisualDesigns.SingleOrDefault(d=>d.InvitationId==entityId);
            if((design?.Version ?? 0)!=input.Version)return Conflict(new {error="Otra persona guardó cambios. Recargá antes de continuar para no sobrescribirlos."});
            if(design==null){design=new VisualDesign {InvitationId=entityId};db.VisualDesigns.Add(design);}
            design.DraftJson=JsonSerializer.Serialize(input.Document,CanvasJson);design.Version++;
            try{db.SaveChanges();}catch(DbUpdateException){return Conflict(new {error="El diseño cambió en otra sesión. Recargá la página."});}
            return Json(new {version=design.Version});
        }
        [Authorize(Roles="Owner"), HttpPost("activate"),ValidateAntiForgeryToken]
        public IActionResult Activate(Guid id,bool enabled)
        {
            if(!Allowed(id))return NotFound();var design=db.VisualDesigns.SingleOrDefault(d=>d.InvitationId==EntityId(id));if(design==null)return BadRequest();
            design.PublishedJson=enabled?design.DraftJson:null;design.Version++;db.SaveChanges();return Ok();
        }
        [HttpGet("preview")]
        public IActionResult Preview(Guid id)
        {
            if(!Allowed(id))return NotFound();var invitation=store.Find(id);var design=db.VisualDesigns.AsNoTracking().SingleOrDefault(d=>d.InvitationId==EntityId(id));
            ViewData["Invitation"]=invitation;ViewData["CanvasPreview"]=true;ViewData["Preview"]=true;
            return View("Canvas",design==null?Starter(invitation):JsonSerializer.Deserialize<CanvasDocument>(design.DraftJson,CanvasJson));
        }
        [HttpGet("assets")]
        public IActionResult Assets(Guid id) => !Allowed(id)?NotFound():new AdminController(store,config,db,environment).Assets(id);
        [HttpPost("upload"),ValidateAntiForgeryToken,RequestSizeLimit(11*1024*1024)]
        public async Task<IActionResult> Upload(Guid id,IFormFile file) => !Allowed(id)?NotFound():await new AdminController(store,config,db,environment).UploadAsset(id,file);
    }
}
