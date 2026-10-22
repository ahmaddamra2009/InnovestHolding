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
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();
    public DbSet<PartnerGroup> PartnerGroups => Set<PartnerGroup>();
    public DbSet<SlugRedirect> SlugRedirects => Set<SlugRedirect>();
    public DbSet<Speaker> Speakers => Set<Speaker>();
    public DbSet<EventSpeaker> EventSpeakers => Set<EventSpeaker>();
    public DbSet<TvGuest> TvGuests => Set<TvGuest>();
    public DbSet<TvEpisode> TvEpisodes => Set<TvEpisode>();
    public DbSet<TvEpisodeGuest> TvEpisodeGuests => Set<TvEpisodeGuest>();
    public DbSet<InvestmentOpportunity> InvestmentOpportunities => Set<InvestmentOpportunity>();
    public DbSet<SponsorshipPackage> SponsorshipPackages => Set<SponsorshipPackage>();

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

        // ── Media and detail pages ──
        modelBuilder.Entity<MediaFile>(e =>
        {
            e.HasIndex(m => m.FileKey).IsUnique();
            e.Property(m => m.FileKey).IsUnicode(false).IsFixedLength();
            e.Property(m => m.Format).IsUnicode(false);
            e.Property(m => m.AltAr).HasDefaultValue("");
            e.Property(m => m.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // Several FKs from one table to MediaFiles must be NO ACTION on SQL Server; the app deletes a media row (and
        // its file) itself when the owner replaces or loses it (Infrastructure/Media/MediaStore).
        modelBuilder.Entity<News>(e =>
        {
            e.HasIndex(n => n.Slug).IsUnique();
            e.Property(n => n.Slug).IsUnicode(false);
            e.Property(n => n.BodyAr).HasDefaultValue("");
            e.Property(n => n.MetaTitleAr).HasDefaultValue("");
            e.Property(n => n.MetaDescriptionAr).HasDefaultValue("");
            e.Property(n => n.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(n => n.Image).WithMany().HasForeignKey(n => n.ImageId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => new { x.IsActive, x.StartsAt });
            e.Property(x => x.Slug).IsUnicode(false);
            e.Property(x => x.StartsAt).HasColumnType("datetimeoffset(0)");
            e.Property(x => x.EndsAt).HasColumnType("datetimeoffset(0)");
            e.Property(x => x.Status).HasConversion<byte>();
            e.Property(x => x.AttendanceMode).HasConversion<byte>();
            e.Property(x => x.SummaryAr).HasDefaultValue("");
            e.Property(x => x.CityAr).HasDefaultValue("");
            e.Property(x => x.AddressAr).HasDefaultValue("");
            e.Property(x => x.MetaTitleAr).HasDefaultValue("");
            e.Property(x => x.MetaDescriptionAr).HasDefaultValue("");
            // Bool with a model default is skipped on insert when false; RegistrationOpen=false must be written, so
            // no model default here — the migration gives existing rows 1 through the column default.
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Image).WithMany().HasForeignKey(x => x.ImageId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Partner>(e =>
        {
            e.HasOne(p => p.Logo).WithMany().HasForeignKey(p => p.LogoId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(p => p.PartnerGroup).WithMany().HasForeignKey(p => p.PartnerGroupId).OnDelete(DeleteBehavior.SetNull);
            e.Property(p => p.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(p => p.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<PartnerGroup>(e =>
        {
            e.HasIndex(g => new { g.IsActive, g.SortOrder });
            e.Property(g => g.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<Page>().HasOne(p => p.Image).WithMany().HasForeignKey(p => p.ImageId).OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SlugRedirect>(e =>
        {
            e.HasIndex(r => new { r.EntityType, r.OldSlug }).IsUnique();
            e.HasIndex(r => new { r.EntityType, r.EntityId });
            e.Property(r => r.EntityType).IsUnicode(false);
            e.Property(r => r.OldSlug).IsUnicode(false);
            e.Property(r => r.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

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

        // ── Ecosystem entities (speakers, TV, opportunities, packages) ──
        Person<Speaker>(modelBuilder);
        Person<TvGuest>(modelBuilder);

        modelBuilder.Entity<EventSpeaker>(e =>
        {
            e.HasKey(x => new { x.EventId, x.SpeakerId });
            e.HasIndex(x => x.SpeakerId);
            e.HasOne(x => x.Event).WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Speaker).WithMany().HasForeignKey(x => x.SpeakerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TvEpisode>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => new { x.IsActive, x.AiredOn });
            e.Property(x => x.Slug).IsUnicode(false);
            e.Property(x => x.VideoUrl).IsUnicode(false);
            Timestamps(e);
            e.HasOne(x => x.Thumbnail).WithMany().HasForeignKey(x => x.ThumbnailId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<TvEpisodeGuest>(e =>
        {
            e.HasKey(x => new { x.TvEpisodeId, x.TvGuestId });
            e.HasIndex(x => x.TvGuestId);
            e.HasOne(x => x.TvEpisode).WithMany().HasForeignKey(x => x.TvEpisodeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TvGuest).WithMany().HasForeignKey(x => x.TvGuestId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvestmentOpportunity>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => new { x.IsActive, x.SortOrder });
            e.Property(x => x.Slug).IsUnicode(false);
            Timestamps(e);
            e.HasOne(x => x.Image).WithMany().HasForeignKey(x => x.ImageId).OnDelete(DeleteBehavior.NoAction);
            // A sector in use cannot be deleted (the admin is told to deactivate it instead).
            e.HasOne(x => x.SectorItem).WithMany().HasForeignKey(x => x.SectorItemId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<SponsorshipPackage>(e =>
        {
            e.HasIndex(x => new { x.Unit, x.IsActive, x.SortOrder });
            e.Property(x => x.Unit).IsUnicode(false);
            Timestamps(e);
        });
    }

    private static void Timestamps<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> e) where T : class
    {
        e.Property<DateTime>("CreatedAt").HasDefaultValueSql("SYSUTCDATETIME()");
        e.Property<DateTime>("UpdatedAt").HasDefaultValueSql("SYSUTCDATETIME()");
    }

    private static void Person<T>(ModelBuilder modelBuilder) where T : PersonProfile =>
        modelBuilder.Entity<T>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => new { x.IsActive, x.SortOrder });
            e.Property(x => x.Slug).IsUnicode(false);
            e.Property(x => x.LinkedInUrl).IsUnicode(false);
            e.Property(x => x.XUrl).IsUnicode(false);
            Timestamps(e);
            e.HasOne(x => x.Photo).WithMany().HasForeignKey(x => x.PhotoId).OnDelete(DeleteBehavior.NoAction);
        });
}
