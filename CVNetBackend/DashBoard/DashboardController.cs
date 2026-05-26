using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CVNetBackend.Services;
using System.Security.Claims;

namespace CVNetBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // 🔒 Protects all dashboard actions
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    public DashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    // Handles: DELETE /api/Dashboard/roles/{profileId}?force=true/false
    [HttpDelete("roles/{profileId}")]
    public async Task<IActionResult> DeleteRole(string profileId, [FromQuery] bool force = false)
    {
        try
        {
            // Safely extract the ID of the person making the request
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) 
                return Unauthorized(new { error = "Invalid user token." });

            // Run the safety check and deletion logic
            var result = await _dashboardService.TryDeleteProfileAsync(userId, profileId, force);
            
            // Return the result object so the Next.js frontend knows if it needs to show a warning popup
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DASHBOARD ERROR] {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
    }
    [HttpGet("available-tracks")]
    public async Task<IActionResult> GetAvailableTracks()
    {
        try {
            var tracks = await _dashboardService.GetAvailableJobTracksAsync();
            return Ok(tracks);
        } catch (Exception ex) {
            // ✅ Log the exact database error to the terminal!
            Console.WriteLine($"\n🚨 [DB ERROR in Tracks] {ex.Message}\n"); 
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] string? profileId)
    {
        try {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var data = await _dashboardService.GetDashboardDataAsync(userId, profileId);
            return Ok(data);
        } catch (Exception ex) {
            // ✅ Log the exact database error to the terminal!
            Console.WriteLine($"\n🚨 [DB ERROR in Summary] {ex.Message}\n");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("roles")]
    public async Task<IActionResult> AddRole([FromBody] System.Text.Json.JsonElement body)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            string jobRole = body.GetProperty("jobRole").GetString() ?? "";
            string category = body.GetProperty("category").GetString() ?? "";

            var success = await _dashboardService.AddTargetRoleProfileAsync(userId, jobRole, category);
            return success ? Ok(new { message = "Role added successfully." }) : BadRequest("Failed to map track layout.");
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}