using AssignMate.Data;
using AssignMate.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AssignMate.Services;

/// <summary>Provides authenticated, user-scoped task and profile persistence.</summary>
public sealed class TaskStore(ApplicationDbContext database, AuthenticationStateProvider authentication)
{
    private readonly List<TaskItem> tasks = [];
    private string? userId;
    private bool initialized;
    private ProfileSettings profile = new();

    public IReadOnlyList<TaskItem> Tasks => tasks;
    public ProfileSettings Profile => profile;

    // Loads the authenticated user's tasks and profile once so page components can render consistent data without repeated queries.
    public async Task InitializeAsync()
    {
        if (initialized)
        {
            return;
        }

        var state = await authentication.GetAuthenticationStateAsync();
        userId = state.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            initialized = true;
            return;
        }

        tasks.AddRange(await database.Tasks.Where(task => task.UserId == userId).OrderBy(task => task.DueDate).ToListAsync());
        var user = await database.Users.FindAsync(userId);
        if (user is not null)
        {
            profile = new ProfileSettings
            {
                Name = user.FullName,
                Email = user.Email ?? string.Empty,
                Program = user.Program,
                Semester = user.Semester
            };
        }

        initialized = true;
    }

    // Persists a newly created assignment and keeps the in-memory collection synchronized with the current view.
    public async Task AddAsync(TaskItem task)
    {
        EnsureAuthenticated();
        task.UserId = userId!;
        task.CreatedAtUtc = DateTime.UtcNow;
        task.UpdatedAtUtc = task.CreatedAtUtc;
        database.Tasks.Add(task);
        await database.SaveChangesAsync();
        tasks.Add(task);
    }

    // Updates the stored details for an existing assignment without permitting cross-user edits.
    public async Task UpdateAsync(TaskItem task)
    {
        EnsureAuthenticated();
        var existing = await database.Tasks.SingleOrDefaultAsync(item => item.Id == task.Id && item.UserId == userId);
        if (existing is null) return;
        existing.Title = task.Title.Trim();
        existing.Course = task.Course.Trim();
        existing.Notes = task.Notes.Trim();
        existing.DueDate = task.DueDate.Date;
        existing.Priority = task.Priority;
        existing.Status = task.Status;
        existing.UpdatedAtUtc = DateTime.UtcNow;
        await database.SaveChangesAsync();
        task.UpdatedAtUtc = existing.UpdatedAtUtc;
    }

    // Removes a task from the database and the local cache once the delete action is confirmed.
    public async Task DeleteAsync(Guid id)
    {
        EnsureAuthenticated();
        var task = tasks.FirstOrDefault(item => item.Id == id);
        if (task is null) return;
        database.Tasks.Remove(task);
        await database.SaveChangesAsync();
        tasks.Remove(task);
    }

    // Toggles the completion state while preserving the same record and updating the timestamp for audit purposes.
    public async Task ToggleCompletionAsync(TaskItem task)
    {
        EnsureAuthenticated();
        task.Status = task.IsCompleted ? AssignmentStatus.NotStarted : AssignmentStatus.Completed;
        task.UpdatedAtUtc = DateTime.UtcNow;
        await database.SaveChangesAsync();
    }

    // Saves user profile edits to the identity record and keeps the in-memory profile model synchronized.
    public async Task UpdateProfileAsync(ProfileSettings settings)
    {
        EnsureAuthenticated();
        var user = await database.Users.FindAsync(userId);
        if (user is null) return;
        user.FullName = settings.Name.Trim();
        user.Email = settings.Email.Trim();
        user.UserName = user.Email;
        user.Program = settings.Program.Trim();
        user.Semester = settings.Semester.Trim();
        await database.SaveChangesAsync();
        profile = new ProfileSettings { Name = user.FullName, Email = user.Email, Program = user.Program, Semester = user.Semester };
    }

    private void EnsureAuthenticated()
    {
        if (userId is null) throw new InvalidOperationException("An authenticated user is required.");
    }
}
