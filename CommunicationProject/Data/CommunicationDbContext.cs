using CommunicationProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace CommunicationProject.Data;

public class CommunicationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public CommunicationDbContext(
        DbContextOptions<CommunicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Site> Sites { get; set; }

    public DbSet<LinkType> LinkTypes { get; set; }

    public DbSet<CommunicationLink> CommunicationLinks { get; set; }

    public DbSet<Stm> Stms { get; set; }

    public DbSet<E1> E1s { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureSite(modelBuilder);
        ConfigureCommunicationLink(modelBuilder);
        ConfigureStm(modelBuilder);
        ConfigureE1(modelBuilder);
    }

    private static void ConfigureCommunicationLink(
        ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<CommunicationLink>()
    .Property(link => link.SiteFromId)
    .HasMaxLength(100)
    .IsRequired();

        modelBuilder.Entity<CommunicationLink>()
            .Property(link => link.SiteToId)
            .HasMaxLength(100)
            .IsRequired();
        /*
         * Link Type → Communication Links
         */
        modelBuilder.Entity<CommunicationLink>()
            .HasOne(link => link.LinkType)
            .WithMany(type => type.Links)
            .HasForeignKey(link => link.LinkTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        /*
         * SiteFrom → Communication Links
         */
        modelBuilder.Entity<CommunicationLink>()
            .HasOne(link => link.SiteFrom)
            .WithMany(site => site.LinksFrom)
            .HasForeignKey(link => link.SiteFromId)
            .OnDelete(DeleteBehavior.Restrict);

        /*
         * SiteTo → Communication Links
         */
        modelBuilder.Entity<CommunicationLink>()
            .HasOne(link => link.SiteTo)
            .WithMany(site => site.LinksTo)
            .HasForeignKey(link => link.SiteToId)
            .OnDelete(DeleteBehavior.Restrict);

        /*
         * Reciprocal directional links:
         *
         * Amman → Zarqa
         *       ↕
         * Zarqa → Amman
         */
        modelBuilder.Entity<CommunicationLink>()
            .HasOne(link => link.ConnectedLink)
            .WithOne()
            .HasForeignKey<CommunicationLink>(
                link => link.ConnectedLinkId)
            .OnDelete(DeleteBehavior.Restrict);

        /*
         * Each directional record may reference only one
         * reverse directional record.
         */
        modelBuilder.Entity<CommunicationLink>()
            .HasIndex(link => link.ConnectedLinkId)
            .IsUnique();

        /*
         * Two records with the same visible name are allowed:
         *
         * Name + IsPrimary = true
         * Name + IsPrimary = false
         *
         * But another logical link cannot use the same name.
         */
        modelBuilder.Entity<CommunicationLink>()
            .HasIndex(link => new
            {
                link.Name,
                link.IsPrimary
            })
            .IsUnique();

        /*
         * Prevent duplicate directional routes.
         */
        modelBuilder.Entity<CommunicationLink>()
            .HasIndex(link => new
            {
                link.LinkTypeId,
                link.SiteFromId,
                link.SiteToId
            })
            .IsUnique();
    }

    private static void ConfigureStm(
        ModelBuilder modelBuilder)
    {
        /*
         * Communication Link → STMs
         *
         * The STM location is Link.SiteFrom.
         */
        modelBuilder.Entity<Stm>()
            .HasOne(stm => stm.Link)
            .WithMany(link => link.Stms)
            .HasForeignKey(stm => stm.LinkId)
            .OnDelete(DeleteBehavior.Restrict);

        /*
         * Reciprocal STM relationship:
         *
         * Amman STM 1
         *      ↕
         * Zarqa STM 1
         */
        modelBuilder.Entity<Stm>()
            .HasOne(stm => stm.ConnectedStm)
            .WithOne()
            .HasForeignKey<Stm>(
                stm => stm.ConnectedStmId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Stm>()
            .HasIndex(stm => stm.ConnectedStmId)
            .IsUnique();

        /*
         * STM number is unique inside one directional link.
         */
        modelBuilder.Entity<Stm>()
            .HasIndex(stm => new
            {
                stm.LinkId,
                stm.Number
            })
            .IsUnique();
    }

    private static void ConfigureE1(
        ModelBuilder modelBuilder)
    {
        /*
         * STM → E1 channels
         */
        modelBuilder.Entity<E1>()
            .HasOne(e1 => e1.Stm)
            .WithMany(stm => stm.E1Channels)
            .HasForeignKey(e1 => e1.StmId)
            .OnDelete(DeleteBehavior.Restrict);

        /*
         * Reciprocal E1 relationship:
         *
         * Amman STM 1 / E1 1.1.1
         *             ↕
         * Zarqa STM 1 / E1 1.1.1
         */
        modelBuilder.Entity<E1>()
            .HasOne(e1 => e1.ConnectedE1)
            .WithOne()
            .HasForeignKey<E1>(
                e1 => e1.ConnectedE1Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<E1>()
            .HasIndex(e1 => e1.ConnectedE1Id)
            .IsUnique();

        /*
         * E1 number is unique inside its STM.
         */
        modelBuilder.Entity<E1>()
            .HasIndex(e1 => new
            {
                e1.StmId,
                e1.E1Number
            })
            .IsUnique();
        modelBuilder.Entity<E1>()
    .HasIndex(e1 => new
    {
        e1.PathId,
        e1.PathOrder
    })
    .IsUnique()
    .HasFilter(
        "[PathId] IS NOT NULL AND [PathOrder] IS NOT NULL");

        modelBuilder.Entity<E1>()
    .Property(e1 => e1.CrossConnectionState)
    .HasConversion<string>()
    .HasMaxLength(32)
    .HasDefaultValue(E1CrossConnectionState.Available)
    .IsRequired();

        modelBuilder.Entity<E1>()
    .ToTable(table =>
        table.HasCheckConstraint(
            "CK_E1s_CrossConnectionState",
            "[CrossConnectionState] IN " +
            "('Available', 'CrossConnected', 'ExtendExistingPath')"));
    }
    private static void ConfigureSite(
    ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Site>()
            .Property(site => site.Id)
            .HasMaxLength(100)
            .IsRequired();
    }
}