using Microsoft.AspNetCore.Mvc;
using CVNetBackend.Services;
using Npgsql;

namespace CVNetBackend.SchemaHandler.CompanyControls;

[ApiController]
[Route("api/company/jobs")]
public class JobManagementController : ControllerBase
{
    private readonly DatabaseService _db;
    public JobManagementController(DatabaseService db) => _db = db;

    [HttpPost("post-job")]
    public async Task<IActionResult> PostJob([FromBody] JobPostDto job)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var trans = await conn.BeginTransactionAsync();

        try {
            // 1. Insert Core Job
            var jobId = Guid.NewGuid();
            var sql = @"INSERT INTO public.jobs (id, company_id, job_category_id, title, employment_type, workplace_type, description, responsibilities, application_deadline, expire_date, hr_contact_email) 
                        VALUES (@id, @cid, @cat, @title, @etype, @wtype, @desc, @resp, @deadline, @expire, @email)";
            
            using var cmd = new NpgsqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("id", jobId);
            cmd.Parameters.AddWithValue("cid", job.CompanyId);
            cmd.Parameters.AddWithValue("cat", job.CategoryId);
            cmd.Parameters.AddWithValue("title", job.Title);
            cmd.Parameters.AddWithValue("etype", job.EmploymentType);
            cmd.Parameters.AddWithValue("wtype", job.WorkplaceType);
            cmd.Parameters.AddWithValue("desc", job.Description);
            cmd.Parameters.AddWithValue("resp", job.Responsibilities);
            cmd.Parameters.AddWithValue("deadline", job.Deadline);
            cmd.Parameters.AddWithValue("expire", job.Deadline.AddDays(30));
            cmd.Parameters.AddWithValue("email", job.HREmail);
            await cmd.ExecuteNonQueryAsync();

            // 2. Insert Skills
            foreach (var s in job.Skills) {
                var sCmd = new NpgsqlCommand("INSERT INTO public.job_skills (job_id, skill_name, required_level) VALUES (@jid, @name, @lvl)", conn, trans);
                sCmd.Parameters.AddWithValue("jid", jobId);
                sCmd.Parameters.AddWithValue("name", s.Name);
                sCmd.Parameters.AddWithValue("lvl", s.Level);
                await sCmd.ExecuteNonQueryAsync();
            }

            await trans.CommitAsync();
            return Ok(new { jobId });
        } catch (Exception ex) {
            await trans.RollbackAsync();
            return BadRequest(ex.Message);
        }
    }
}

public class JobPostDto { public Guid CompanyId { get; set; } public Guid CategoryId { get; set; } public string Title { get; set; } = ""; public string EmploymentType { get; set; } = ""; public string WorkplaceType { get; set; } = ""; public string Description { get; set; } = ""; public string Responsibilities { get; set; } = ""; public DateTime Deadline { get; set; } public string HREmail { get; set; } = ""; public List<SkillReqDto> Skills { get; set; } = new(); }
public class SkillReqDto { public string Name { get; set; } = ""; public string Level { get; set; } = ""; }