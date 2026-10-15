
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartHelpdesk.DAL.Entities;
using SmartHelpdesk.DAL.Entities.Ticket;

namespace SmartHelpdesk.DAL.Context;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    #region Ctor

    public AppDbContext(DbContextOptions<AppDbContext> options): base(options)
    {
    }

    #endregion

    #region DbSets

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<TicketComment> TicketComments => Set<TicketComment>();

    public DbSet<TicketStatusHistory> TicketStatusHistories=> Set<TicketStatusHistory>();

    public DbSet<TicketHistory> TicketHistories => Set<TicketHistory>();

    #endregion

    #region Model Configuration

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        #region Refresh Token Configuration

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(token => token.Id);

            entity.Property(token => token.UserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(token => token.TokenHash)
                .IsRequired()
                .HasMaxLength(64);

            entity.HasIndex(token => token.TokenHash)
                .IsUnique();

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        #endregion

        #region Permission Configuration

        builder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");

            entity.HasKey(permission => permission.Id);

            entity.Property(permission => permission.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(permission => permission.Description)
                .HasMaxLength(250);

            entity.HasIndex(permission => permission.Name)
                .IsUnique();
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");

            entity.HasKey(rolePermission => new
            {
                rolePermission.RoleId,
                rolePermission.PermissionId
            });

            entity.Property(rolePermission => rolePermission.RoleId)
                .IsRequired()
                .HasMaxLength(450);

            entity.HasOne(rolePermission => rolePermission.Role)
                .WithMany()
                .HasForeignKey(rolePermission => rolePermission.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rolePermission => rolePermission.Permission)
                .WithMany(permission => permission.RolePermissions)
                .HasForeignKey(rolePermission => rolePermission.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        #endregion

        #region Ticket Configuration

        builder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Tickets");

            entity.HasKey(ticket => ticket.Id);

            entity.HasOne(ticket => ticket.CreatedByUser)
                .WithMany()
                .HasForeignKey(ticket => ticket.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ticket => ticket.AssignedToUser)
                .WithMany()
                .HasForeignKey(ticket => ticket.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        #endregion

        #region Ticket Comment Configuration

        builder.Entity<TicketComment>(entity =>
        {
            entity.ToTable("TicketComments");

            entity.HasKey(comment => comment.Id);

            entity.Property(comment => comment.UserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(comment => comment.CommentText)
                .IsRequired()
                .HasMaxLength(4000);

            entity.Property(comment => comment.CreatedAt)
                .IsRequired();

            entity.HasOne(comment => comment.Ticket)
                .WithMany(ticket => ticket.Comments)
                .HasForeignKey(comment => comment.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(comment => comment.User)
                .WithMany()
                .HasForeignKey(comment => comment.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(comment => new
            {
                comment.TicketId,
                comment.CreatedAt
            });
        });

        #endregion

        #region Ticket Status History Configuration

        builder.Entity<TicketStatusHistory>(entity =>
        {
            entity.ToTable("TicketStatusHistories");

            entity.HasKey(history => history.Id);

            entity.Property(history => history.OldStatus)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(history => history.NewStatus)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(history => history.ChangedByUserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(history => history.ChangedAt)
                .IsRequired();

            entity.HasOne(history => history.Ticket)
                .WithMany(ticket => ticket.StatusHistory)
                .HasForeignKey(history => history.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(history => history.ChangedByUser)
                .WithMany()
                .HasForeignKey(history => history.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(history => new
            {
                history.TicketId,
                history.ChangedAt
            });
        });

        #endregion
    }

    #endregion
}
