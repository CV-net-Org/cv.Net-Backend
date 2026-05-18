using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FirebaseAdmin.Auth;
using CVNetBackend.LoginManagement.Models;
using CVNetBackend.Services;
using System.Security.Claims;

namespace CVNetBackend.LoginManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly DatabaseService _db;
    private readonly FirestoreService _fs;

    public AuthController(DatabaseService db, FirestoreService fs)
    {
        _db = db;
        _fs = fs;
    }

    [HttpPost("signup")]
    [Authorize] // 👈 FORCE token verification before processing data
    public async Task<IActionResult> SignUp([FromBody] SignupRequest request)
    {
        if (request.Agreement != "Agreed")
            return BadRequest(new { error = "Terms and Privacy Policy must be accepted." });

        try
        {
            // 👈 SECURELY EXTRACT THE REAL UID FROM THE VALIDATED TOKEN
            var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(uid))
                return Unauthorized(new { error = "Identity token validation failed." });

            // 1. Sync to Firestore (Pass the verified UID)
            await _fs.CreateUserDocument(uid, request.FirstName, request.LastName, request.Email, request.Agreement);
            
            // 2. Sync to PostgreSQL (Pass the verified UID)
            await _db.UpsertUserToPostgres(uid, request.Email, $"{request.FirstName} {request.LastName}", request.Agreement);

            return Ok(new { message = "User successfully synchronized everywhere!", uid = uid });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] TokenAuthRequest request)
    {
        try
        {
            FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken);
            string uid = decodedToken.Uid;
            
            string email = decodedToken.Claims.ContainsKey("email") 
                ? decodedToken.Claims["email"]?.ToString() ?? "" 
                : "";
            
            string name = decodedToken.Claims.ContainsKey("name") 
                ? decodedToken.Claims["name"]?.ToString() ?? "CV User" 
                : "CV User";

            await _db.UpsertUserToPostgres(uid, email, name, request.Agreement ?? "Agreed");

            return Ok(new { 
                message = "Login and Sync Successful!", 
                uid = uid,
                email = email
            });
        }
        catch (Exception ex)
        {
            return Unauthorized(new { error = "Authentication failed", details = ex.Message });
        }
    }
}