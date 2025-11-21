using KahveDostum_Service.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KahveDostum_Service.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>(); 
    public DbSet<Friendship> Friendships => Set<Friendship>();         
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageReceipt> MessageReceipts => Set<MessageReceipt>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<Cafe> Cafes => Set<Cafe>();

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

        // USER SESSION
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Cafe)
                .WithMany()
                .HasForeignKey(s => s.CafeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => new { s.UserId, s.CafeId, s.Status });
        });

        // CAFE
        modelBuilder.Entity<Cafe>(entity =>
        {
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Address).IsRequired().HasMaxLength(500);
        });
        // ConversationParticipant
        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            entity.HasOne(cp => cp.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(cp => cp.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cp => cp.User)
                .WithMany()
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(cp => new { cp.ConversationId, cp.UserId }).IsUnique();
        });

        // Message
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(m => new { m.ConversationId, m.CreatedAt });
        });

        // MessageReceipt
        modelBuilder.Entity<MessageReceipt>(entity =>
        {
            entity.HasOne(r => r.Message)
                .WithMany(m => m.Receipts)
                .HasForeignKey(r => r.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => new { r.MessageId, r.UserId }).IsUnique();
        });
    }
}
