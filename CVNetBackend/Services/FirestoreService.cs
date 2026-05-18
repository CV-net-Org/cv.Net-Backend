using Google.Cloud.Firestore;

namespace CVNetBackend.Services;

public class FirestoreService
{
    private readonly FirestoreDb _db;
    private const string CollectionName = "Users";

    public FirestoreService()
    {
        // Assumes GOOGLE_APPLICATION_CREDENTIALS is set in Program.cs
        _db = FirestoreDb.Create("cvnet2026-capstone");
    }

    public async Task UpdateUserField(string userId, string fieldName, object value)
    {
        DocumentReference userRef = _db.Collection(CollectionName).Document(userId);
        await userRef.UpdateAsync(fieldName, value);
    }

    // UPDATED: Now includes agreement field to match schema
    public async Task CreateUserDocument(string uid, string firstName, string lastName, string email, string agreement = "Agreed")
    {
        var docRef = _db.Collection(CollectionName).Document(uid);
        var userData = new Dictionary<string, object>
        {
            { "firstName", firstName },
            { "lastName", lastName },
            { "email", email },
            { "role", "candidate" },
            { "agreement", agreement }, // Recorded status
            { "createdAt", Timestamp.GetCurrentTimestamp() }
        };
        await docRef.SetAsync(userData);
    }
}