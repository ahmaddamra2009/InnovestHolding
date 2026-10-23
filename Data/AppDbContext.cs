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
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();

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

        // Public lists filter on IsActive and order by SortOrder.
        modelBuilder.Entity<Event>()
            .HasIndex(e => new { e.IsActive, e.SortOrder });

        modelBuilder.Entity<News>()
            .HasIndex(n => new { n.IsActive, n.SortOrder });

        // Bool flags have no model default (EF would skip an explicit false on insert); the migration gives existing
        // rows 1 through the column default.
        modelBuilder.Entity<Platform>().HasIndex(p => new { p.IsActive, p.SortOrder });
        modelBuilder.Entity<Partner>().HasIndex(p => new { p.IsActive, p.SortOrder });

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

        modelBuilder.Entity<Page>(e =>
        {
            e.HasIndex(p => p.Key).IsUnique();
            e.Property(p => p.Key).IsUnicode(false);
            e.Property(p => p.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<ContentItem>(e =>
        {
            e.HasIndex(i => new { i.Group, i.IsActive, i.SortOrder });
            e.Property(i => i.Group).IsUnicode(false);
            e.Property(i => i.IconKey).IsUnicode(false);
            e.Property(i => i.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }
}
