using Microsoft.AspNetCore.Identity;

namespace AssignMate.Data;

public sealed class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
}
