using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;

namespace InnovestHolding.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<SiteContent> SiteContents => Set<SiteContent>();
    public DbSet<NavItem> NavItems => Set<NavItem>();
    public DbSet<NavSubItem> NavSubItems => Set<NavSubItem>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<News> News => Set<News>();
    public DbSet<Platform> Platforms => Set<Platform>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Faq> Faqs => Set<Faq>();
    public DbSet<Stat> Stats => Set<Stat>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<InterestOption> InterestOptions => Set<InterestOption>();
    public DbSet<RegisterBenefit> RegisterBenefits => Set<RegisterBenefit>();
    public DbSet<SocialLink> SocialLinks => Set<SocialLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SiteContent>()
            .HasIndex(s => s.SectionKey)
            .IsUnique();

        modelBuilder.Entity<NavItem>()
            .HasMany(n => n.SubItems)
            .WithOne(s => s.NavItem)
            .HasForeignKey(s => s.NavItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NavItem>()
            .HasIndex(n => n.SortOrder);

        modelBuilder.Entity<Event>()
            .HasIndex(e => e.SortOrder);

        modelBuilder.Entity<Platform>()
            .HasIndex(p => p.SortOrder);

        modelBuilder.Entity<News>()
            .HasIndex(n => n.SortOrder);

        // Deleting an option keeps the registrations (their Interest text is a snapshot).
        modelBuilder.Entity<Registration>()
            .HasOne(r => r.InterestOption)
            .WithMany()
            .HasForeignKey(r => r.InterestOptionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Culture is written by the server; existing rows came from the Arabic-only page.
        modelBuilder.Entity<Registration>().Property(r => r.Culture).HasDefaultValue("ar");
        modelBuilder.Entity<ContactMessage>().Property(m => m.Culture).HasDefaultValue("ar");

        // Admin lists are paged newest first.
        modelBuilder.Entity<Registration>().HasIndex(r => r.CreatedAt);
        modelBuilder.Entity<ContactMessage>().HasIndex(m => m.CreatedAt);

        modelBuilder.Entity<InterestOption>().HasIndex(o => new { o.IsActive, o.SortOrder });
        modelBuilder.Entity<RegisterBenefit>().HasIndex(b => new { b.IsActive, b.SortOrder });
        modelBuilder.Entity<SocialLink>().HasIndex(s => s.Network).IsUnique();
    }
}
