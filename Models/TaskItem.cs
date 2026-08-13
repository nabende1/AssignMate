using System.ComponentModel.DataAnnotations;

namespace AssignMate.Models;

/// <summary>Represents an assignment a student needs to complete.</summary>
public sealed class TaskItem
{
    // Unique record identifier used by EF Core and for task-specific actions in the UI.
    public Guid Id { get; init; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;
    [Required, StringLength(100)]
    public string Course { get; set; } = string.Empty;
    [StringLength(2000)]
    public string Notes { get; set; } = string.Empty;
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(7);
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public AssignmentStatus Status { get; set; } = AssignmentStatus.NotStarted;

    // These computed values power dashboard and list filtering for urgency and completion state.
    public bool IsCompleted => Status == AssignmentStatus.Completed;
    public bool IsDueSoon => !IsCompleted && DueDate.Date <= DateTime.Today.AddDays(3);
    public bool IsOverdue => !IsCompleted && DueDate.Date < DateTime.Today;
}

public enum TaskPriority
{
    Low,
    Medium,
    High
}

public enum AssignmentStatus
{
    NotStarted,
    InProgress,
    Completed
}

public sealed class ProfileSettings
{
    public string Name { get; set; } = "Alex Morgan";
    public string Email { get; set; } = "alex.morgan@example.com";
    public string Program { get; set; } = "Computer Science";
    public string Semester { get; set; } = "Fall 2026";
}
