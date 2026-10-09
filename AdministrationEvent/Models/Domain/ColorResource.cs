#nullable enable
using System;
namespace Application
{
    public class ColorResource
    {
        public long Id { get; set; }

        public long InvitationThemeId { get; set; }

        public InvitationTheme Theme { get; set; } = null!;

        public string Key { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }
}
