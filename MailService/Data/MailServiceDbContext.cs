using MailService.Models;
using Microsoft.EntityFrameworkCore;

namespace MailService.Data;

public class MailServiceDbContext : DbContext
{
    public MailServiceDbContext(DbContextOptions<MailServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<MailLog> MailLogs { get; set; }
    public DbSet<MailTemplate> MailTemplates { get; set; }
    public DbSet<ClientApplication> ClientApplications { get; set; }
    public DbSet<OAuthClient> OAuthClients { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MailLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ClientIdName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.To).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
            entity.Property(e => e.RequestId).IsRequired();
            entity.HasIndex(e => e.RequestId);
            entity.HasIndex(e => e.ClientId);
            entity.HasIndex(e => e.ClientIdName);
            entity.HasIndex(e => e.SentDate);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<MailTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Body).IsRequired();
            entity.Property(e => e.Version).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedDate).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<ClientApplication>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ClientId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DailyLimit).IsRequired().HasDefaultValue(1000);
            entity.Property(e => e.MinuteLimit).IsRequired().HasDefaultValue(100);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedDate).IsRequired();
            entity.HasIndex(e => e.ClientId).IsUnique();
        });

        modelBuilder.Entity<OAuthClient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ClientId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ClientSecret).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Scopes).IsRequired().HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedDate).IsRequired();
            entity.HasIndex(e => e.ClientId).IsUnique();

            // Seed Data
            entity.HasData(
                new OAuthClient
                {
                    Id = 1,
                    ClientId = "crm-api",
                    ClientSecret = "crm-secret-123",
                    Name = "CRM Application",
                    Scopes = "mail.send",
                    IsActive = true,
                    CreatedDate = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc)
                },
                new OAuthClient
                {
                    Id = 2,
                    ClientId = "erp-api",
                    ClientSecret = "erp-secret-456",
                    Name = "ERP Application",
                    Scopes = "mail.send mail.bulk.send",
                    IsActive = true,
                    CreatedDate = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc)
                },
                new OAuthClient
                {
                    Id = 3,
                    ClientId = "mail-admin",
                    ClientSecret = "admin-secret-789",
                    Name = "Mail Admin Portal",
                    Scopes = "mail.send mail.bulk.send mail.template.read mail.template.write mail.admin",
                    IsActive = true,
                    CreatedDate = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        });
    }
}
