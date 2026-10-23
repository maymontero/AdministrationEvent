using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace AdministrationEvent.Models
{
    public class InvitationConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required, StringLength(120)] public string Title { get; set; } = "Nueva invitación";
        [Required, RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$"), StringLength(120)] public string Slug { get; set; } = "nuevo-evento";
        public bool IsPublished { get; set; }
        [Required, RegularExpression("^(public|link)$")] public string SharingMode { get; set; } = "public";
        [RegularExpression("^$|^[a-f0-9]{48}$")] public string ShareKey { get; set; } = "";
        [Required, StringLength(80)] public string EventType { get; set; } = "Casamiento";
        [StringLength(200)] public string Hosts { get; set; } = "Ana & Mateo";
        public DateTime? Date { get; set; }
        public bool HasConfirmedTime { get; set; } = true;
        public bool ShowCountdown { get; set; }
        public bool RsvpEnabled { get; set; }
        public bool SeatingPlanEnabled { get; set; }
        [Required, StringLength(100)] public string TimeZone { get; set; } = "America/Montevideo";
        [StringLength(2000)] public string Message { get; set; } = "Queremos compartir este día con vos.";
        public string CoverImageUrl { get; set; } = "";
        [Required, RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$"), StringLength(120)] public string AssetCollection { get; set; } = "shared";
        public InvitationTheme Theme { get; set; } = new InvitationTheme();
        public List<InvitationSection> Sections { get; set; } = new List<InvitationSection>();
    }
    public class InvitationTheme
    {
        [StringLength(2000), RegularExpression(@"^$|^https://open\.spotify\.com/(?:embed/)?(?:track|playlist)/[a-zA-Z0-9]{22}/?(?:\?[^\s]*)?$")]
        public string SpotifyTrackUrl { get; set; } = "";
        [Required, RegularExpression("^(classic|vineyard|vineyard-archive|relaxed|save-date|beach|beach-continuous|custom)$")] public string Layout { get; set; } = "custom";
        [Required, RegularExpression("^#[0-9a-fA-F]{6}$")] public string Secondary { get; set; } = "#76805e";
        [Required, RegularExpression("^#[0-9a-fA-F]{6}$")] public string Highlight { get; set; } = "#eee3bd";
        [Required, RegularExpression("^#[0-9a-fA-F]{6}$")] public string Background { get; set; } = "#f9f5ee";
        [Required, RegularExpression("^#[0-9a-fA-F]{6}$")] public string Text { get; set; } = "#343e37";
        [Required, RegularExpression("^#[0-9a-fA-F]{6}$")] public string Accent { get; set; } = "#65775c";
        [Required, RegularExpression("^[a-zA-Z0-9 ,'-]{1,120}$")] public string HeadingFont { get; set; } = "Georgia, serif";
        [Required, RegularExpression("^[a-zA-Z0-9 ,'-]{1,120}$")] public string BodyFont { get; set; } = "Arial, sans-serif";
        public string HeadingFontUrl { get; set; } = "";
        public string BodyFontUrl { get; set; } = "";
        [Range(0, 48)] public int CornerRadius { get; set; } = 20;
        [Range(14, 24)] public int FontSize { get; set; } = 18;
        [Range(600, 1400)] public int ContentWidth { get; set; } = 960;
    }
    public class InvitationSection
    {
        // The layout-specific renderer uses this presentation hint instead of
        // inferring a visual treatment from the wording of the title.
        [Required, RegularExpression("^(standard|art-above|backdrop)$")]
        public string DisplayMode { get; set; } = "standard";
        [Range(16, 80)] public int? TitleFontSize { get; set; }
        public bool Enabled { get; set; } = true;
        [Required, StringLength(120)] public string Title { get; set; } = "Celebración";
        [StringLength(5000)] public string Content { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        [StringLength(80)] public string LinkLabel { get; set; } = "Ver más";
        public string LinkUrl { get; set; } = "";
    }
    public class InvitationEditor
    {
        public Guid Id { get; set; }
        [Required] public string ConfigurationJson { get; set; }
    }
}
