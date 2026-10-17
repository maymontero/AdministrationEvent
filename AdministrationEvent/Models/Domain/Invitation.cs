using System.Collections.Generic;
#nullable enable
using System;

namespace Application
{
    public class Invitation
    {
        public long Id { get; set; }
        public Guid PublicId { get; set; } = Guid.NewGuid();
        public string EventType { get; set; } = "Casamiento";
        public string? Hosts { get; set; }
        public DateTime? Date { get; set; }
        public bool HasConfirmedTime { get; set; } = true;
        public bool ShowCountdown { get; set; }
        public bool RsvpEnabled { get; set; }
        public string TimeZone { get; set; } = "America/Montevideo";
        public string? Message { get; set; }
        public string? CoverImageUrl { get; set; }
        public string AssetCollection { get; set; } = "shared";
        public ICollection<InvitationSection> Sections { get; set; } = new List<InvitationSection>();

        public string Title { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public bool IsPublished { get; set; }
        public string SharingMode { get; set; } = "public";
        public string? ShareKey { get; set; }

        public ICollection<Event> Events { get; set; } = new List<Event>();

        public InvitationTheme? Theme { get; set; }

        public RSVPConfiguration? RSVP { get; set; }
    }
}
