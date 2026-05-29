using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CVNetBackend.Services;
using System.Security.Claims;
using Npgsql;
using Dapper;

namespace CVNetBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
private readonly DashboardService _dashboardService;
private readonly IConfiguration _config;
private readonly string _connString;


public DashboardController(
    DashboardService dashboardService,
    IConfiguration config
)
{
    _dashboardService = dashboardService;
    _config = config;

    string host =
        Environment.GetEnvironmentVariable("DB_HOST")
        ?? "localhost";

    string port =
        Environment.GetEnvironmentVariable("DB_PORT")
        ?? "5432";

    string db =
        Environment.GetEnvironmentVariable("DB_NAME")
        ?? "postgres";

    string user =
        Environment.GetEnvironmentVariable("DB_USER")
        ?? "postgres";

    string pass =
        Environment.GetEnvironmentVariable("DB_PASSWORD")
        ?? "postgres";

    _connString =
        $"Host={host};Port={port};Database={db};Username={user};Password={pass};";
}

// ==========================================================
// DELETE ROLE
// ==========================================================

[HttpDelete("roles/{profileId}")]
public async Task<IActionResult> DeleteRole(
    string profileId,
    [FromQuery] bool force = false
)
{
    try
    {
        var userId =
            User.FindFirst(ClaimTypes.NameIdentifier)
            ?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new
            {
                error = "Invalid user token."
            });
        }

        var result =
            await _dashboardService
                .TryDeleteProfileAsync(
                    userId,
                    profileId,
                    force
                );

        return Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[DASHBOARD ERROR] {ex.Message}"
        );

        return BadRequest(new
        {
            error = ex.Message
        });
    }
}

// ==========================================================
// AVAILABLE TRACKS
// ==========================================================

[HttpGet("available-tracks")]
public async Task<IActionResult> GetAvailableTracks()
{
    try
    {
        var tracks =
            await _dashboardService
                .GetAvailableJobTracksAsync();

        return Ok(tracks);
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"\n🚨 [DB ERROR in Tracks] {ex.Message}\n"
        );

        return BadRequest(new
        {
            error = ex.Message
        });
    }
}

// ==========================================================
// DASHBOARD SUMMARY
// ==========================================================

[HttpGet("summary")]
public async Task<IActionResult> GetSummary(
    [FromQuery] string? profileId
)
{
    try
    {
        var userId =
            User.FindFirst(ClaimTypes.NameIdentifier)
            ?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var data =
            await _dashboardService
                .GetDashboardDataAsync(
                    userId,
                    profileId
                );

        return Ok(data);
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"\n🚨 [DB ERROR in Summary] {ex.Message}\n"
        );

        return BadRequest(new
        {
            error = ex.Message
        });
    }
}

// ==========================================================
// ADD ROLE
// ==========================================================

[HttpPost("roles")]
public async Task<IActionResult> AddRole(
    [FromBody]
    System.Text.Json.JsonElement body
)
{
    try
    {
        var userId =
            User.FindFirst(ClaimTypes.NameIdentifier)
            ?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        string jobRole =
            body.GetProperty("jobRole")
            .GetString()
            ?? "";

        string category =
            body.GetProperty("category")
            .GetString()
            ?? "";

        var success =
            await _dashboardService
                .AddTargetRoleProfileAsync(
                    userId,
                    jobRole,
                    category
                );

        return success
            ? Ok(new
            {
                message =
                    "Role added successfully."
            })
            : BadRequest(
                "Failed to map track layout."
            );
    }
    catch (Exception ex)
    {
        return BadRequest(new
        {
            error = ex.Message
        });
    }
}

// ==========================================================
// ✅ NEW READINESS MATRIX ENDPOINT
// ==========================================================

[HttpGet("readiness-matrix")]
public async Task<IActionResult> GetReadinessMatrix(
    [FromQuery] string profileId
)
{
    try
    {
        using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        // --------------------------------------------------
        // GET ACTIVE ROLE
        // --------------------------------------------------

        var profile =
            await conn
                .QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT
                        job_role
                    FROM public.target_role_profiles
                    WHERE id = @pid::uuid
                ", new
                {
                    pid = profileId
                });

        if (profile == null)
        {
            return Ok(new
            {
                breakdown =
                    new List<object>()
            });
        }

        string role =
            profile.job_role?.ToString()
            ?? "";

        // --------------------------------------------------
        // USER SKILLS
        // --------------------------------------------------

        var userSkills =
        (
            await conn.QueryAsync<dynamic>(@"
                SELECT
                    skill_name,
                    level
                FROM public.skill
                WHERE profile_id = @pid::uuid
            ", new
            {
                pid = profileId
            })
        ).ToList();

        // --------------------------------------------------
        // ROLE SKILLS (FIRST 4)
        // --------------------------------------------------

        var roleSkills =
        (
            await conn.QueryAsync<dynamic>(@"
                SELECT
                    skill_name,
                    level,
                    job_category_id
                FROM public.general_skills
                WHERE LOWER(job_role)=LOWER(@role)
                LIMIT 4
            ", new
            {
                role
            })
        ).ToList();

        // --------------------------------------------------
        // PERCENTAGE HELPER
        // --------------------------------------------------

        int ToPercent(string? lvl)
        {
            return lvl?.ToLower() switch
            {
                "expert" => 85,
                "intermediate" => 34,
                "beginner" => 8,
                _ => 0
            };
        }

        // --------------------------------------------------
        // BUILD FINAL ARRAY
        // --------------------------------------------------

        var breakdown =
            new List<object>();

        // --------------------------------------------------
        // FIRST 4 CORE ROLE SKILLS
        // --------------------------------------------------

        foreach (var skill in roleSkills)
        {
            string skillName =
                skill.skill_name.ToString();

            var userSkill =
                userSkills.FirstOrDefault(x =>
                    x.skill_name
                        .ToString()
                        .ToLower()
                    ==
                    skillName.ToLower()
                );

            string expected =
                skill.level.ToString();

            string actual =
                userSkill?.level?.ToString()
                ?? "Missing";

            breakdown.Add(new
            {
                skillName,

                requirementSource =
                    "Core Role Skill",

                expectedLevel = expected,

                expectedPercentage =
                    ToPercent(expected),

                userDeclaredLevel =
                    actual,

                userCalculatedPercentage =
                    ToPercent(actual)
            });
        }

        // --------------------------------------------------
        // CATEGORY SKILLS
        // --------------------------------------------------

        var categoryId =
            roleSkills
                .FirstOrDefault()
                ?.job_category_id;

        if (categoryId != null)
        {
            var category =
                await conn
                    .QueryFirstOrDefaultAsync<dynamic>(@"
                        SELECT
                            skills
                        FROM public.job_categories
                        WHERE id = @cid::uuid
                    ", new
                    {
                        cid = categoryId
                    });

            if (category != null)
            {
                // ------------------------------------------
                // REMOVE DUPLICATED TOP 4
                // ------------------------------------------

                var usedSkills =
                    roleSkills
                        .Select(x =>
                            x.skill_name
                                .ToString()
                                .ToLower()
                        )
                        .ToHashSet();

                var remaining =
                    ((string[])category.skills)
                        .Where(x =>
                            !usedSkills.Contains(
                                x.ToLower()
                            )
                        )
                        .ToList();

                // ------------------------------------------
                // ADD REMAINING CATEGORY SKILLS
                // ------------------------------------------

                foreach (var skill in remaining)
                {
                    var found =
                        userSkills
                            .FirstOrDefault(x =>
                                x.skill_name
                                    .ToString()
                                    .ToLower()
                                ==
                                skill.ToLower()
                            );

                    string actual =
                        found?.level?.ToString()
                        ?? "Missing";

                    breakdown.Add(new
                    {
                        skillName = skill,

                        requirementSource =
                            "Category Skill",

                        // ALWAYS INTERMEDIATE
                        expectedLevel =
                            "Intermediate",

                        expectedPercentage = 34,

                        userDeclaredLevel =
                            actual,

                        userCalculatedPercentage =
                            ToPercent(actual)
                    });
                }
            }
        }

        // --------------------------------------------------
        // FINAL RESPONSE
        // --------------------------------------------------

        return Ok(new
        {
            breakdown
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[READINESS MATRIX ERROR] {ex.Message}"
        );

        return BadRequest(new
        {
            error = ex.Message
        });
    }
}


}
