using Microsoft.AspNetCore.Mvc;
using Frontend.Models;
using System.Collections.Generic;

namespace Frontend.Controllers
{
    public class AnalysisController : Controller
    {
        private readonly ILogger<AnalysisController> _logger;

        public AnalysisController(ILogger<AnalysisController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // TODO: Replace with actual data from your backend
            var results = new List<AnalysisResult>
            {
                new AnalysisResult
                {
                    Id = 1,
                    Title = "Sample Analysis",
                    Description = "This is a sample analysis result",
                    AnalysisDate = DateTime.Now,
                    Status = "Completed",
                    ResultData = "Sample data"
                }
            };

            return View(results);
        }

        public IActionResult Details(int id)
        {
            // TODO: Replace with actual data from your backend
            var result = new AnalysisResult
            {
                Id = id,
                Title = "Detailed Analysis",
                Description = "Detailed view of analysis result",
                AnalysisDate = DateTime.Now,
                Status = "Completed",
                ResultData = "Detailed sample data"
            };

            return View(result);
        }
    }
} 