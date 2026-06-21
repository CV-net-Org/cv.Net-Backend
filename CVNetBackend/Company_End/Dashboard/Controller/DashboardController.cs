using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CVNetBackend.Company_End.Services;
using CVNetBackend.Company_End.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace CVNetBackend.Company_End.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompanyDashboardController : ControllerBase
{
    private readonly CompanyDashboardService _dashboardService;

    public CompanyDashboardController(CompanyDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("📊 DASHBOARD API HIT - Starting Data Fetch");

            var email = User.Claims.FirstOrDefault(c => c.Type == "email" || c.Type == ClaimTypes.Email)?.Value;
            
            if (string.IsNullOrEmpty(email)) 
            {
                Console.WriteLine("❌ ERROR: Email claim missing from JWT token.");
                return Unauthorized(new { error = "Invalid token: Security context is missing the email claim." });
            }

            Console.WriteLine($"✅ HR Email Extracted: {email}");

            var dashboardData = await _dashboardService.GetDashboardDataAsync(email);
            
            Console.WriteLine("✅ Dashboard Data Successfully Assembled! Returning 200 OK.");
            Console.WriteLine("========================================\n");
            
            return Ok(dashboardData);
        }
        catch (Exception ex)
        {
            // 🔥 PRINT THE EXACT ERROR TO THE TERMINAL 🔥
            Console.WriteLine("\n🚨🚨🚨 DASHBOARD CRASH REPORT 🚨🚨🚨");
            Console.WriteLine($"ERROR MESSAGE: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"INNER DB ERROR: {ex.InnerException.Message}");
            Console.WriteLine($"STACK TRACE:\n{ex.StackTrace}");
            Console.WriteLine("=======================================\n");

            return BadRequest(new { error = ex.Message, details = ex.InnerException?.Message });
        }
    }
}