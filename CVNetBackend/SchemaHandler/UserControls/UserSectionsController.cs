using Microsoft.AspNetCore.Mvc;
using CVNetBackend.Services;
using Npgsql;

namespace CVNetBackend.SchemaHandler.UserControls;

[ApiController]
[Route("api/user/profile")]
public class UserProfileController : ControllerBase
{
    private readonly DatabaseService _db;
    public UserProfileController(DatabaseService db) => _db = db;

    [HttpPut("update-basics")]
    public async Task<IActionResult> UpdateBasics([FromBody] UserUpdateDto data)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        var sql = @"UPDATE public.""user"" SET phone=@p, address=@a, gpa=@g, updated_at=NOW() WHERE id=@id";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("p", data.Phone ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("a", data.Address ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("g", data.GPA ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("id", data.UserId);
        await cmd.ExecuteNonQueryAsync();
        return Ok(new { status = "Profile updated" });
    }
}

public class UserUpdateDto { public string UserId { get; set; } = ""; public string? Phone { get; set; } public string? Address { get; set; } public float? GPA { get; set; } }