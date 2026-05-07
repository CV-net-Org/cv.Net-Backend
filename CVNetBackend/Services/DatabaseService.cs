using Npgsql;

namespace CVNetBackend.Services;

public class DatabaseService
{
    private readonly string _connString = "Host=35.245.28.42;Username=postgres;Password=CV.Net2026@capstone;Database=cvnet2026-capstone-2-database";
    
    // 1. Existing method for Email/Password Signup
    public async Task SaveToPostgres(string uid, string firstName, string lastName, string email)
    {
        using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        var sql = @"
            INSERT INTO public.""user"" 
            (id, email, full_name, employment_status, created_at, updated_at) 
            VALUES 
            (@id, @email, @fullName, @employmentStatus, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)";
        
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", uid);
        cmd.Parameters.AddWithValue("email", email);
        cmd.Parameters.AddWithValue("fullName", $"{firstName} {lastName}");
        cmd.Parameters.AddWithValue("employmentStatus", "Unspecified"); 
        
        await cmd.ExecuteNonQueryAsync();
    }

    // 2. New method for Google Auth Sync (Upsert)
    public async Task SyncGoogleUserToPostgres(string uid, string email, string fullName)
    {
        using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        // ON CONFLICT (id) DO NOTHING prevents errors if the user logs in a second time
        var sql = @"
            INSERT INTO public.""user"" (id, email, full_name, employment_status, created_at, updated_at)
            VALUES (@id, @email, @fullName, 'Unspecified', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            ON CONFLICT (id) DO NOTHING"; 

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", uid);
        cmd.Parameters.AddWithValue("email", email);
        cmd.Parameters.AddWithValue("fullName", fullName);

        await cmd.ExecuteNonQueryAsync();
    }
}