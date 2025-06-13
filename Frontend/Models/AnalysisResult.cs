using System;

namespace Frontend.Models
{
    public class AnalysisResult
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime AnalysisDate { get; set; }
        public string Status { get; set; }
        public string ResultData { get; set; }
    }
} 