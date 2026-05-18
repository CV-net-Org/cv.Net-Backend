using Npgsql;

namespace CVNetBackend.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        string host = Environment.GetEnvironmentVariable("DB_HOST") ?? "";
        string user = Environment.GetEnvironmentVariable("DB_USER") ?? "";
        string password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";
        string database = Environment.GetEnvironmentVariable("DB_NAME") ?? "";
        string port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";

        _connectionString = $"Host={host};Port={port};Username={user};Password={password};Database={database};";
    }

    public NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

    /**
     * UpsertUserToPostgres: Handles both Signup and Login Sync.
     * Ensures the 'agreement' status is recorded in the PostgreSQL table.
     */
    public async Task UpsertUserToPostgres(string uid, string email, string fullName, string agreement)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // SQL updated to include the 'agreement' column from your schema
        var sql = @"
            INSERT INTO public.""user"" (id, email, full_name, employment_status, agreement, created_at, updated_at)
            VALUES (@id, @email, @fullName, 'Unspecified', @agreement, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            ON CONFLICT (id) 
            DO UPDATE SET 
                email = EXCLUDED.email, 
                full_name = EXCLUDED.full_name, 
                agreement = EXCLUDED.agreement,
                updated_at = CURRENT_TIMESTAMP"; 

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", uid);
        cmd.Parameters.AddWithValue("email", email);
        cmd.Parameters.AddWithValue("fullName", fullName);
        cmd.Parameters.AddWithValue("agreement", agreement ?? "Agreed"); // Default to Agreed if coming from verified frontend

        await cmd.ExecuteNonQueryAsync();
    }

    // Profile Update (e.g., from Cloudinary)
    public async Task UpdateProfileImage(string uid, string imageUrl)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = @"UPDATE public.""user"" SET profile_image_url = @url, updated_at = CURRENT_TIMESTAMP WHERE id = @id";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("url", imageUrl);
        cmd.Parameters.AddWithValue("id", uid);

        await cmd.ExecuteNonQueryAsync();
    }
}