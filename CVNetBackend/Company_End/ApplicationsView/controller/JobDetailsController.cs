using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using CVNetBackend.Company_End.ApplicationsView.Models;
using CVNetBackend.Company_End.ApplicationsView.Services;

namespace CVNetBackend.Company_End.ApplicationsView.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobDetailsController : ControllerBase
{
    private readonly JobDetailsService _service;

    public JobDetailsController(JobDetailsService service) { _service = service; }

    [HttpGet("{jobId}")]
    public async Task<IActionResult> GetJobDashboard(string jobId)
    {
        try
        {
            var details = await _service.GetJobDetailsAsync(jobId);
            if (details == null) return NotFound("Job not found.");
            var applicants = await _service.GetApplicantsAsync(jobId);
            return Ok(new { details, applicants });
        }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{jobId}/close")]
    public async Task<IActionResult> CloseJob(string jobId)
    {
        try
        {
            var hrEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            await _service.CloseJobAndRejectPendingAsync(jobId, hrEmail);
            return Ok(new { message = "Job closed and pending applicants rejected." });
        }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{jobId}/repost")]
    public async Task<IActionResult> RepostJob(string jobId)
    {
        try
        {
            var newId = await _service.RepostJobAsync(jobId);
            return Ok(new { newJobId = newId });
        }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("applicant/{appId}/interview")]
    public async Task<IActionResult> CallForInterview(string appId, [FromBody] InterviewRequestDto dto)
    {
        try
        {
            await _service.CallForInterviewAsync(appId, dto);
            return Ok();
        }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("applicant/{appId}/reject")]
    public async Task<IActionResult> RejectApplicant(string appId, [FromBody] RejectRequestDto dto)
    {
        try
        {
            await _service.RejectApplicantAsync(appId, dto);
            return Ok();
        }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("applicant-profile/{appId}")]
    public async Task<IActionResult> GetApplicantFullProfile(string appId)
    {
        try
        {
            var profile = await _service.GetFullApplicantProfileAsync(appId);
            if (profile == null) return NotFound(new { error = "Applicant profile not found." });
            return Ok(profile);
        }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }
}