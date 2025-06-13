using System;
using System.ComponentModel.DataAnnotations;

namespace Frontend.Models
{
    public class SensorData
    {
        [Key]
        public int Id { get; set; }
        public string WavyId { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Processed { get; set; }
    }
} 