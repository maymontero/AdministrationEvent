using System.Collections.Generic;
#nullable enable
using System;
namespace Application
{
    public class Event
    {
        public long Id { get; set; }

        public long InvitationId { get; set; }

        public Invitation Invitation { get; set; } = null!;

        public string? Title { get; set; }

        public DateTime? DateTime { get; set; }

        public string? DressCode { get; set; }

        public int Order { get; set; }

        // Lugar
        public long? LocationId { get; set; }

        public Location? Location { get; set; }

        // Evento padre
        public long? ParentEventId { get; set; }

        public Event? ParentEvent { get; set; }

        // Subeventos
        public ICollection<Event> Children { get; set; } = new List<Event>();
    }
}
