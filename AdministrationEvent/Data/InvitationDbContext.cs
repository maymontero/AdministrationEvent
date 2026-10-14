using Application;
using Microsoft.EntityFrameworkCore;

public class InvitationDbContext : DbContext
{
    public InvitationDbContext(
        DbContextOptions<InvitationDbContext> options)
        : base(options)
    {
    }

    public DbSet<GuestResponse> GuestResponses => Set<GuestResponse>();
    public DbSet<GuestTag> GuestTags => Set<GuestTag>();
    public DbSet<SeatingTable> SeatingTables => Set<SeatingTable>();
    public DbSet<GuestTagAssignment> GuestTagAssignments => Set<GuestTagAssignment>();
    public DbSet<Guest> Guests => Set<Guest>();

    public DbSet<InvitationSection> InvitationSections => Set<InvitationSection>();

    public DbSet<Invitation> Invitations => Set<Invitation>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<InvitationTheme> InvitationThemes => Set<InvitationTheme>();

    public DbSet<ColorResource> ColorResources => Set<ColorResource>();

    public DbSet<FontResource> FontResources => Set<FontResource>();

    public DbSet<ImageResource> ImageResources => Set<ImageResource>();

    public DbSet<AudioResource> AudioResources => Set<AudioResource>();

    public DbSet<RSVPConfiguration> RSVPConfigurations =>
        Set<RSVPConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureInvitation(modelBuilder);
        modelBuilder.Entity<GuestTag>(e => {
            e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(80).IsRequired(); e.Property(x => x.Color).HasMaxLength(7).IsRequired();
            e.HasIndex(x => new { x.InvitationId, x.Name }).IsUnique();
            e.HasOne<Invitation>().WithMany().HasForeignKey(x => x.InvitationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<SeatingTable>(e => {
            e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            e.HasIndex(x => new { x.InvitationId, x.Name }).IsUnique();
            e.HasOne<Invitation>().WithMany().HasForeignKey(x => x.InvitationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<GuestTagAssignment>(e => {
            e.HasKey(x => new { x.GuestId, x.TagId });
            e.HasOne<Guest>().WithMany().HasForeignKey(x => x.GuestId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<GuestTag>().WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Guest>(entity => {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Contact).HasMaxLength(200);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasIndex(x => new { x.InvitationId, x.IsArchived });
            entity.HasOne<SeatingTable>().WithMany().HasForeignKey(x => x.TableId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Invitation>().WithMany().HasForeignKey(x => x.InvitationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<GuestResponse>(entity => {
            entity.Property(x => x.DietaryRestrictions).HasMaxLength(500);
            entity.Property(x => x.SongRequest).HasMaxLength(200);
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Contact).HasMaxLength(200);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasIndex(x => new { x.InvitationId, x.SubmittedAtUtc });
            entity.HasOne<Invitation>().WithMany().HasForeignKey(x => x.InvitationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Guest>().WithMany().HasForeignKey(x => x.GuestId).OnDelete(DeleteBehavior.SetNull);
        });
        ConfigureEvent(modelBuilder);
        ConfigureTheme(modelBuilder);
        ConfigureResources(modelBuilder);
        ConfigureRSVP(modelBuilder);
        modelBuilder.Entity<Location>().Property(x => x.Latitude).HasColumnType("decimal(10,7)");
        modelBuilder.Entity<Location>().Property(x => x.Longitude).HasColumnType("decimal(10,7)");
        modelBuilder.Entity<InvitationSection>(entity => {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Content).HasMaxLength(5000).HasColumnType("longtext");
            entity.Property(x => x.ImageUrl).HasMaxLength(2000);
            entity.Property(x => x.LinkUrl).HasMaxLength(2000);
            entity.Property(x => x.LinkLabel).HasMaxLength(80);
            entity.HasIndex(x => new { x.InvitationId, x.Order });
            entity.HasOne(x => x.Invitation).WithMany(x => x.Sections).HasForeignKey(x => x.InvitationId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureInvitation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.PublicId).IsUnique();
            entity.Property(x => x.TimeZone).HasMaxLength(100).HasDefaultValue("America/Montevideo");
            entity.Property(x => x.HasConfirmedTime).HasDefaultValue(true);
            entity.Property(x => x.EventType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Hosts).HasMaxLength(200);
            entity.Property(x => x.Message).HasMaxLength(2000);
            entity.Property(x => x.CoverImageUrl).HasMaxLength(2000);

            entity.Property(x => x.Title)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(x => x.Slug)
                .HasMaxLength(250)
                .IsRequired();

            entity.HasIndex(x => x.Slug)
                .IsUnique();

            entity.HasMany(x => x.Events)
                .WithOne(x => x.Invitation)
                .HasForeignKey(x => x.InvitationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .HasMaxLength(250);

            entity.Property(x => x.DressCode)
                .HasMaxLength(500);

            entity.HasOne(x => x.ParentEvent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentEventId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Location)
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => new
            {
                x.InvitationId,
                x.Order
            });
        });
    }

    private static void ConfigureTheme(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InvitationTheme>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Layout).HasMaxLength(20).HasDefaultValue("classic");
            entity.HasOne(x => x.Invitation)
                .WithOne(x => x.Theme)
                .HasForeignKey<InvitationTheme>(
                    x => x.InvitationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.InvitationId)
                .IsUnique();
        });
    }

    private static void ConfigureResources(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ColorResource>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Key)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Value)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasOne(x => x.Theme)
                .WithMany(x => x.Colors)
                .HasForeignKey(x => x.InvitationThemeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new
            {
                x.InvitationThemeId,
                x.Key
            }).IsUnique();
        });

        modelBuilder.Entity<FontResource>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Key)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Url).HasMaxLength(2000);

            entity.Property(x => x.FontFamily)
                .HasMaxLength(250)
                .IsRequired();

            entity.HasOne(x => x.Theme)
                .WithMany(x => x.Fonts)
                .HasForeignKey(x => x.InvitationThemeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new
            {
                x.InvitationThemeId,
                x.Key
            }).IsUnique();
        });

        modelBuilder.Entity<ImageResource>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Key)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Url)
                .HasMaxLength(2000)
                .IsRequired();

            entity.HasOne(x => x.Theme)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.InvitationThemeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new
            {
                x.InvitationThemeId,
                x.Key
            }).IsUnique();
        });

        modelBuilder.Entity<AudioResource>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Key)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Url)
                .HasMaxLength(2000)
                .IsRequired();

            entity.HasOne(x => x.Theme)
                .WithMany(x => x.Audios)
                .HasForeignKey(x => x.InvitationThemeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new
            {
                x.InvitationThemeId,
                x.Key
            }).IsUnique();
        });
    }

    private static void ConfigureRSVP(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RSVPConfiguration>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Invitation)
                .WithOne(x => x.RSVP)
                .HasForeignKey<RSVPConfiguration>(
                    x => x.InvitationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.InvitationId)
                .IsUnique();
        });
    }
}
