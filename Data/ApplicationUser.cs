using Microsoft.AspNetCore.Identity;

namespace AssignMate.Data;

// Stores the additional student profile data associated with a signed-in AssignMate account.
public sealed class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
}
