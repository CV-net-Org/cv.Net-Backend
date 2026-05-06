using Npgsql;

namespace CVNetBackend.Services;

public class DatabaseService
{
    private readonly string _connString = "Host=35.245.28.42;Username=postgres;Password=CV.Net2026@capstone;Database=cvnet2026-capstone-2-database";
    
    public async Task SaveToPostgres(string uid, string firstName, string lastName, string email)
    {
        using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        // Added full_name and employment_status to satisfy the database rules
        var sql = @"
            INSERT INTO public.""user"" 
            (id, email, full_name, employment_status, created_at, updated_at) 
            VALUES 
            (@id, @email, @fullName, @employmentStatus, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)";
        
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", uid);
        cmd.Parameters.AddWithValue("email", email);
        
        // Combine first and last name for the database
        cmd.Parameters.AddWithValue("fullName", $"{firstName} {lastName}");
        
        // Set a default employment status for new signups
        cmd.Parameters.AddWithValue("employmentStatus", "Unspecified"); 
        
        await cmd.ExecuteNonQueryAsync();
    }
}