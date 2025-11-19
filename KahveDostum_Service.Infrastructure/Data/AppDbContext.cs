using KahveDostum_Service.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KahveDostum_Service.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>(); 
    public DbSet<Friendship> Friendships => Set<Friendship>();         

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.UserName).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasOne(rt => rt.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(rt => rt.UserId);
        });

        modelBuilder.Entity<FriendRequest>(entity =>
        {
            entity.HasOne(fr => fr.FromUser)
                  .WithMany()
                  .HasForeignKey(fr => fr.FromUserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(fr => fr.ToUser)
                  .WithMany()
                  .HasForeignKey(fr => fr.ToUserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(fr => new { fr.FromUserId, fr.ToUserId, fr.Status });
        });

        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.HasOne(f => f.User)
                  .WithMany()
                  .HasForeignKey(f => f.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.FriendUser)
                  .WithMany()
                  .HasForeignKey(f => f.FriendUserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(f => new { f.UserId, f.FriendUserId }).IsUnique();
        });
    }
}
