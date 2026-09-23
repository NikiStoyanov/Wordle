namespace Wordle.Server.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Core.Entities;

public class WordleDbContext : DbContext
{
    public WordleDbContext(DbContextOptions<WordleDbContext> options) : base(options) { }

    public DbSet<Word> Words { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<DailyChallenge> DailyChallenges { get; set; }

    public DbSet<UserDailyAttempt> UserDailyAttempts { get; set; }

    public DbSet<MultiplayerMatch> MultiplayerMatches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Word>()
            .Property(w => w.Text)
            .HasMaxLength(7)
            .IsRequired();

        modelBuilder.Entity<Word>()
            .HasIndex(w => w.Text)
            .IsUnique();

        modelBuilder.Entity<Word>()
            .HasIndex(w => w.Length);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<DailyChallenge>()
            .HasIndex(d => d.Date);

        modelBuilder.Entity<DailyChallenge>()
            .HasOne(d => d.Word)
            .WithMany()
            .HasForeignKey(d => d.WordId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserDailyAttempt>()
            .HasOne(u => u.User)
            .WithMany()
            .HasForeignKey(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserDailyAttempt>()
            .HasOne(u => u.DailyChallenge)
            .WithMany()
            .HasForeignKey(u => u.DailyChallengeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MultiplayerMatch>()
            .HasIndex(m => m.RoomCode);

        modelBuilder.Entity<MultiplayerMatch>()
            .HasOne(m => m.Host)
            .WithMany()
            .HasForeignKey(m => m.HostId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MultiplayerMatch>()
            .HasOne(m => m.Guest)
            .WithMany()
            .HasForeignKey(m => m.GuestId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasQueryFilter(u => !u.IsDeleted);
    }
}