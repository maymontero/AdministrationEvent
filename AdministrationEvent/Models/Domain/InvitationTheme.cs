#nullable enable
using System;
using System.Collections.Generic;

namespace Application
{
    public class InvitationTheme
    {
        public long Id { get; set; }
        public string Layout { get; set; } = "custom";
        public int CornerRadius { get; set; } = 20;
        public int FontSize { get; set; } = 18;
        public int ContentWidth { get; set; } = 960;

        public long InvitationId { get; set; }

        public Invitation Invitation { get; set; } = null!;

        public ICollection<ColorResource> Colors { get; set; } = new List<ColorResource>();

        public ICollection<FontResource> Fonts { get; set; } = new List<FontResource>();

        public ICollection<ImageResource> Images { get; set; } = new List<ImageResource>();

        public ICollection<AudioResource> Audios { get; set; } = new List<AudioResource>();
    }
}
