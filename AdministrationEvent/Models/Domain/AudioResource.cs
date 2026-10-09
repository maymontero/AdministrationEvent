#nullable enable
using System;
namespace Application
{
    public class AudioResource
    {
        public long Id { get; set; }

        public long InvitationThemeId { get; set; }

        public InvitationTheme Theme { get; set; } = null!;

        public string Key { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public bool Loop { get; set; } = true;
    }
}
