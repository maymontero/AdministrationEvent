#nullable enable
namespace Application
{
    public class InvitationSection
    {
        public int? TitleFontSize { get; set; }
        public long Id { get; set; }
        public long InvitationId { get; set; }
        public Invitation Invitation { get; set; } = null!;
        public int Order { get; set; }
        public bool Enabled { get; set; } = true;
        public string Title { get; set; } = "";
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public string? LinkLabel { get; set; }
        public string? LinkUrl { get; set; }
    }
}
