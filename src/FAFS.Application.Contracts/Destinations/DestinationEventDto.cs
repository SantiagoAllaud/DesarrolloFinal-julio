using System;

namespace FAFS.Application.Contracts.Destinations
{
    public class DestinationEventDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Date { get; set; }
        public string? Time { get; set; }
        public string? Url { get; set; }
        public string? ImageUrl { get; set; }
        public string? VenueName { get; set; }
    }
}
