using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Frontend.Models;
using Frontend.Data;
using Microsoft.EntityFrameworkCore;

namespace Frontend.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            // Test database connection
            if (!await _context.Database.CanConnectAsync())
            {
                throw new Exception("Não foi possível conectar ao banco de dados. Verifique se o SQL Server está rodando e se a string de conexão está correta.");
            }

            var processedSensors = await _context.SensorDataProcessed
                .OrderByDescending(s => s.Timestamp)
                .ToListAsync();

            var unprocessedSensors = await _context.SensorData
                .OrderByDescending(s => s.Timestamp)
                .ToListAsync();

            ViewBag.ProcessedSensors = processedSensors;
            ViewBag.UnprocessedSensors = unprocessedSensors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sensor data: {Message}", ex.Message);
            ViewBag.Error = $"Erro ao carregar dados dos sensores: {ex.Message}";
            
            // Log the full exception details
            _logger.LogError("Full exception details: {Exception}", ex.ToString());
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
