using Microsoft.AspNetCore.Mvc;
using FirebaseAdmin.Auth;
using CVNetBackend.Models;
using CVNetBackend.Services;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly DatabaseService _db = new();
    private readonly FirestoreService _fs = new();

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] SignupRequest request)
    {
        try
        {
            // STEP 1: Firebase Auth
            var userArgs = new UserRecordArgs
            {
                Email = request.Email,
                Password = request.Password,
                DisplayName = $"{request.FirstName} {request.LastName}"
            };
            var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(userArgs);

            // STEP 2: Firestore
            await _fs.CreateUserDocument(userRecord.Uid, request.FirstName, request.LastName, request.Email);

            // STEP 3: PostgreSQL
            await _db.SaveToPostgres(userRecord.Uid, request.FirstName, request.LastName, request.Email);

            return Ok(new { message = "User successfully created everywhere!", uid = userRecord.Uid });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    [HttpPost("google-auth")]
public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthRequest request)
{
    try
    {
        // 1. Verify the Google ID Token
        FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken);
        string uid = decodedToken.Uid;
        
        // 2. Extract info from the Google Token
        string email = decodedToken.Claims.ContainsKey("email") ? decodedToken.Claims["email"].ToString() : "";
        string name = decodedToken.Claims.ContainsKey("name") ? decodedToken.Claims["name"].ToString() : "Google User";

        // 3. Triple-Sync Logic
        // Firestore (Checks internaly if exists, or overwrites)
        await _fs.CreateUserDocument(uid, name, "", email); 

        // PostgreSQL (Our new Upsert method)
        await _db.SyncGoogleUserToPostgres(uid, email, name);

        return Ok(new { 
            message = "Google User Synced Successfully!", 
            uid = uid,
            email = email
        });
    }
    catch (Exception ex)
    {
        return Unauthorized(new { error = "Invalid Google Token", details = ex.Message });
    }
}
}