using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<RaceRoute> Routes => Set<RaceRoute>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.Property(u => u.FullName).HasMaxLength(100);
            e.Property(u => u.Email).HasMaxLength(200);
            e.Property(u => u.Role).HasMaxLength(20);
            e.HasIndex(u => u.Email).IsUnique();
        });

        b.Entity<Event>(e =>
        {
            e.Property(x => x.Distance).HasPrecision(8, 2);
            e.Property(x => x.EventType).HasMaxLength(10);
            e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents)
                .HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<RaceRoute>(e =>
        {
            e.HasKey(x => x.RouteId);
            e.Property(x => x.ElevationGain).HasPrecision(8, 2);
            e.HasOne(x => x.Event).WithMany(ev => ev.Routes)
                .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Category>(e =>
        {
            e.Property(x => x.Distance).HasPrecision(8, 2);
            e.Property(x => x.Price).HasPrecision(10, 2);
            e.HasOne(x => x.Event).WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        // Restrict avoids SQL Server's "multiple cascade paths" error
        b.Entity<Enrolment>(e =>
        {
            e.HasOne(x => x.Participant).WithMany(u => u.Enrolments)
                .HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Result>(e =>
        {
            e.HasOne(r => r.Enrolment).WithOne(en => en.Result)
                .HasForeignKey<Result>(r => r.EnrolmentId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
