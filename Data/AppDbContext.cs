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
    }
}
