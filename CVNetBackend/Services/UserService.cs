using Npgsql;

namespace CVNetBackend.Services;

public class UserService
{
    private readonly DatabaseService _db;
    public UserService(DatabaseService db) => _db = db;

    public async Task DeleteFullUserProfile(string uid)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var trans = await conn.BeginTransactionAsync();

        try {
            // All tables linked to User via foreign keys
            string[] profileTables = { 
                "sociallink", "skill", "experience", "education", "project", 
                "publication", "certification", "membership", "language", 
                "teachingexperience", "researchexperience", "award", "volunteer" 
            };

            foreach (var table in profileTables) {
                var cmd = new NpgsqlCommand($"DELETE FROM public.\"{table}\" WHERE user_id = @uid", conn, trans);
                cmd.Parameters.AddWithValue("uid", uid);
                await cmd.ExecuteNonQueryAsync();
            }

            var userCmd = new NpgsqlCommand("DELETE FROM public.\"user\" WHERE id = @uid", conn, trans);
            userCmd.Parameters.AddWithValue("uid", uid);
            await userCmd.ExecuteNonQueryAsync();

            await trans.CommitAsync();
        } catch {
            await trans.RollbackAsync();
            throw;
        }
    }
}