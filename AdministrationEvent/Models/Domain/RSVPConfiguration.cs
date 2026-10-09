#nullable enable
using System;
namespace Application
{
    public class RSVPConfiguration
    {
        public long Id { get; set; }

        public long InvitationId { get; set; }

        public Invitation Invitation { get; set; } = null!;

        public bool Enabled { get; set; } = true;

        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? GoogleFormUrl { get; set; }

        public DateTime? ConfirmationDeadline { get; set; }
    }
}
