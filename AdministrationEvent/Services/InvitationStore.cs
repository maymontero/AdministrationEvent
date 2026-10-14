using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using AdministrationEvent.Models;
using Domain = Application;
namespace AdministrationEvent.Services
{
    public class InvitationStore
    {
        private readonly InvitationDbContext db;
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true };
        public InvitationStore(InvitationDbContext db) { this.db = db; }
        private IQueryable<Domain.Invitation> Query() => db.Invitations
            .Include(x => x.Theme).ThenInclude(x => x.Colors)
            .Include(x => x.Theme).ThenInclude(x => x.Fonts)
            .Include(x => x.Sections);
        public List<InvitationConfiguration> All() => Query().AsNoTracking().OrderBy(x => x.Title).ToList().Select(ToConfiguration).ToList();
        public InvitationConfiguration Find(Guid id) { var item = Query().AsNoTracking().SingleOrDefault(x => x.PublicId == id); return item == null ? null : ToConfiguration(item); }
        public InvitationConfiguration FindPublished(string slug) { var item = Query().AsNoTracking().SingleOrDefault(x => x.Slug == slug && x.IsPublished); return item == null ? null : ToConfiguration(item); }
        public string Save(InvitationConfiguration item)
        {
            var errors = new List<ValidationResult>();
            if (item.Theme == null || item.Sections == null || item.Sections.Any(s => s == null)) return "El tema y las secciones son obligatorios.";
            foreach (var value in new object[] { item, item.Theme }.Concat(item.Sections.Cast<object>()))
                Validator.TryValidateObject(value, new ValidationContext(value), errors, true);
            var assets = new[] { item.CoverImageUrl, item.Theme.HeadingFontUrl, item.Theme.BodyFontUrl }.Concat(item.Sections.Select(s => s.ImageUrl));
            var links = item.Sections.Select(s => s.LinkUrl);
            if (assets.Concat(links).Any(url => url != null && url.Length > 2000)) return "Las URLs pueden tener hasta 2000 caracteres.";
            if (assets.Any(url => !IsSafeAsset(url)) || links.Any(url => !IsHttpsOrEmpty(url))) return "Usá imágenes/fuentes HTTPS o rutas locales seguras; los enlaces deben ser HTTPS.";
            try { TimeZoneInfo.FindSystemTimeZoneById(item.TimeZone ?? ""); } catch (Exception e) when (e is TimeZoneNotFoundException || e is InvalidTimeZoneException || e is ArgumentException) { return "La zona horaria no es válida."; }
            if (errors.Count > 0) return string.Join(" ", errors.Select(e => e.ErrorMessage));
            if (db.Invitations.Any(x => x.Slug == item.Slug && x.PublicId != item.Id)) return "Ese enlace ya pertenece a otra invitación.";
            var entity = Query().SingleOrDefault(x => x.PublicId == item.Id);
            if (entity == null) { entity = new Domain.Invitation { PublicId = item.Id }; db.Invitations.Add(entity); }
            entity.Title = item.Title; entity.Slug = item.Slug; entity.IsPublished = item.IsPublished;
            entity.HasConfirmedTime = item.HasConfirmedTime; entity.ShowCountdown = item.ShowCountdown; entity.RsvpEnabled = item.RsvpEnabled; entity.TimeZone = item.TimeZone;
            entity.EventType = item.EventType; entity.Hosts = item.Hosts; entity.Date = item.Date;
            entity.Message = item.Message; entity.CoverImageUrl = item.CoverImageUrl;
            if (entity.Theme == null) entity.Theme = new Domain.InvitationTheme();
            var theme = entity.Theme;
            theme.Layout = item.Theme.Layout;
            theme.CornerRadius = item.Theme.CornerRadius; theme.FontSize = item.Theme.FontSize; theme.ContentWidth = item.Theme.ContentWidth;
            SetColor(theme, "background", item.Theme.Background); SetColor(theme, "text", item.Theme.Text); SetColor(theme, "accent", item.Theme.Accent);
            SetColor(theme, "secondary", item.Theme.Secondary); SetColor(theme, "highlight", item.Theme.Highlight);
            SetFont(theme, "heading", item.Theme.HeadingFont, item.Theme.HeadingFontUrl);
            SetFont(theme, "body", item.Theme.BodyFont, item.Theme.BodyFontUrl);
            db.InvitationSections.RemoveRange(entity.Sections);
            entity.Sections = item.Sections.Select((s, index) => new Domain.InvitationSection { Order = index, Enabled = s.Enabled, Title = s.Title, TitleFontSize = s.TitleFontSize, Content = s.Content, ImageUrl = s.ImageUrl, LinkLabel = s.LinkLabel, LinkUrl = s.LinkUrl }).ToList();
            try { db.SaveChanges(); }
            catch (DbUpdateException error) when (error.InnerException is MySqlException mysql && mysql.Number == 1062)
            { return "Ese enlace ya pertenece a otra invitación."; }
            return null;
        }
        private static bool IsHttpsOrEmpty(string url) => string.IsNullOrEmpty(url) || (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https" && string.IsNullOrEmpty(uri.UserInfo));
        private static bool IsSafeAsset(string url) => IsHttpsOrEmpty(url) || (url != null && System.Text.RegularExpressions.Regex.IsMatch(url, @"^/(?:images|fonts)/[a-zA-Z0-9_/-]+\.[a-zA-Z0-9]+$") && !url.Contains("//") && !url.Split('/').Any(segment => segment == "." || segment == ".."));
        private static void SetColor(Domain.InvitationTheme theme, string key, string value)
        { var color = theme.Colors.SingleOrDefault(x => x.Key == key); if (color == null) { color = new Domain.ColorResource { Key = key }; theme.Colors.Add(color); } color.Value = value; }
        private static void SetFont(Domain.InvitationTheme theme, string key, string family, string url)
        { var font = theme.Fonts.SingleOrDefault(x => x.Key == key); if (font == null) { font = new Domain.FontResource { Key = key }; theme.Fonts.Add(font); } font.FontFamily = family; font.Url = url; }
        private static InvitationConfiguration ToConfiguration(Domain.Invitation entity)
        {
            var result = new InvitationConfiguration { Id = entity.PublicId, Title = entity.Title, Slug = entity.Slug, IsPublished = entity.IsPublished, EventType = entity.EventType, Hosts = entity.Hosts, Date = entity.Date, HasConfirmedTime = entity.HasConfirmedTime, ShowCountdown = entity.ShowCountdown, RsvpEnabled = entity.RsvpEnabled, TimeZone = entity.TimeZone, Message = entity.Message, CoverImageUrl = entity.CoverImageUrl };
            if (entity.Theme != null) {
                var theme = entity.Theme;
                result.Theme.Layout = theme.Layout;
                result.Theme.Secondary = theme.Colors.SingleOrDefault(x => x.Key == "secondary")?.Value ?? result.Theme.Secondary;
                result.Theme.Highlight = theme.Colors.SingleOrDefault(x => x.Key == "highlight")?.Value ?? result.Theme.Highlight;
                result.Theme.Background = theme.Colors.SingleOrDefault(x => x.Key == "background")?.Value ?? result.Theme.Background;
                result.Theme.Text = theme.Colors.SingleOrDefault(x => x.Key == "text")?.Value ?? result.Theme.Text;
                result.Theme.Accent = theme.Colors.SingleOrDefault(x => x.Key == "accent")?.Value ?? result.Theme.Accent;
                result.Theme.HeadingFont = theme.Fonts.SingleOrDefault(x => x.Key == "heading")?.FontFamily ?? result.Theme.HeadingFont;
                result.Theme.BodyFont = theme.Fonts.SingleOrDefault(x => x.Key == "body")?.FontFamily ?? result.Theme.BodyFont;
                result.Theme.HeadingFontUrl = theme.Fonts.SingleOrDefault(x => x.Key == "heading")?.Url;
                result.Theme.BodyFontUrl = theme.Fonts.SingleOrDefault(x => x.Key == "body")?.Url;
                result.Theme.CornerRadius = theme.CornerRadius; result.Theme.FontSize = theme.FontSize; result.Theme.ContentWidth = theme.ContentWidth;
            }
            result.Sections = entity.Sections.OrderBy(s => s.Order).Select(s => new InvitationSection { Enabled = s.Enabled, Title = s.Title, TitleFontSize = s.TitleFontSize, Content = s.Content, ImageUrl = s.ImageUrl, LinkLabel = s.LinkLabel, LinkUrl = s.LinkUrl }).ToList();
            return result;
        }
    }
}
