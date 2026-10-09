#nullable enable
using System;
namespace Application
{
    public class FontResource
    {
        public long Id { get; set; }

        public long InvitationThemeId { get; set; }

        public InvitationTheme Theme { get; set; } = null!;

        public string Key { get; set; } = string.Empty;

        public string FontFamily { get; set; } = string.Empty;

        public string? Url { get; set; }
    }
}
