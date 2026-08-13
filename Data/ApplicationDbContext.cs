using AssignMate.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AssignMate.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    // Exposes assignment records for the authenticated user.
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    // Defines the task schema so validation, relationships, and query behavior align with the application workflow.
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(task => task.Id);
            entity.Property(task => task.Title).HasMaxLength(200).IsRequired();
            entity.Property(task => task.Course).HasMaxLength(100).IsRequired();
            entity.Property(task => task.Notes).HasMaxLength(2000);
            entity.HasIndex(task => new { task.UserId, task.DueDate });
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(task => task.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
