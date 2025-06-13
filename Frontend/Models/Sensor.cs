using System;

namespace Frontend.Models
{
    public class Sensor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public DateTime LastUpdate { get; set; }
        public string Status { get; set; }
        public string Data { get; set; }
    }
} 