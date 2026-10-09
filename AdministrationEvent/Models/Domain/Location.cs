#nullable enable
using System;
namespace Application
{
    public class Location
    {
        public long Id { get; set; }

        public string? Name { get; set; }

        public string? Address { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }
    }
}
