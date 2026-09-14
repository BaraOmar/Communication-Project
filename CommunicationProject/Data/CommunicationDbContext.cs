using CommunicationProject.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using static CommunicationProject.Models.E1;
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

    public DbSet<CommunicationPath> CommunicationPaths { get; set; }
    public DbSet<CommunicationPathSegment> CommunicationPathSegments { get; set; }


    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerConnection> CustomerConnections { get; set; }
    public DbSet<CustomerConnectionSegment> CustomerConnectionSegments { get; set; }


    public DbSet<Mux> Muxes { get; set; }
    public DbSet<CardType> CardTypes { get; set; }
    public DbSet<MuxCard> MuxCards { get; set; }
    public DbSet<MuxPort> MuxPorts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureSite(modelBuilder);
        ConfigureCommunicationLink(modelBuilder);
        ConfigureStm(modelBuilder);
        ConfigureE1(modelBuilder);
        ConfigureMux(modelBuilder);
        ConfigureCardType(modelBuilder);
        ConfigureMuxCard(modelBuilder);
        ConfigureMuxPort(modelBuilder);
        ConfigureCommunicationPath(modelBuilder);
        ConfigureCommunicationPathSegment(modelBuilder);
        ConfigureCustomer(modelBuilder);
        ConfigureCustomerConnection(modelBuilder);
        ConfigureCustomerConnectionSegment(modelBuilder);
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
                link.SiteToId,
                link.Name
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
        modelBuilder.Entity<E1>()
    .Property(e1 => e1.Status)
    .HasConversion<string>()
    .HasMaxLength(32)
    .HasDefaultValue(E1OperationalStatus.Available)
    .IsRequired();
        modelBuilder.Entity<E1>()
    .Property(e1 => e1.ConnectionType)
    .HasConversion<string>()
    .HasMaxLength(32)
    .HasDefaultValue(E1ConnectionType.Physical)
    .IsRequired();
    }
    private static void ConfigureSite(
    ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Site>()
            .Property(site => site.Id)
            .HasMaxLength(100)
            .IsRequired();
    }
    private static void ConfigureMux(
    ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Mux>()
            .HasOne(mux => mux.Site)
            .WithMany()
            .HasForeignKey(mux => mux.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Mux>()
            .HasOne(mux => mux.CommunicationLink)
            .WithMany()
            .HasForeignKey(mux => mux.CommunicationLinkId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Mux>()
            .HasIndex(mux => new
            {
                mux.CommunicationLinkId,
                mux.SiteId,
                mux.Name
            })
            .IsUnique();
    }
    private static void ConfigureCardType(
    ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CardType>()
            .Property(cardType => cardType.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        modelBuilder.Entity<CardType>()
            .HasIndex(cardType => cardType.Name)
            .IsUnique();
    }
    private static void ConfigureMuxCard(
    ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MuxCard>()
            .HasOne(card => card.Mux)
            .WithMany(mux => mux.Cards)
            .HasForeignKey(card => card.MuxId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MuxCard>()
            .HasOne(card => card.CardType)
            .WithMany(cardType => cardType.Cards)
            .HasForeignKey(card => card.CardTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MuxCard>()
            .HasIndex(card => new
            {
                card.MuxId,
                card.SlotNumber
            })
            .IsUnique();
    }
    private static void ConfigureMuxPort(
    ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MuxPort>()
            .HasOne(port => port.MuxCard)
            .WithMany(card => card.Ports)
            .HasForeignKey(port => port.MuxCardId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MuxPort>()
            .HasOne(port => port.E1)
            .WithMany()
            .HasForeignKey(port => port.E1Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MuxPort>()
            .Property(port => port.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(MuxPortStatus.Available)
            .IsRequired();

        modelBuilder.Entity<MuxPort>()
            .HasIndex(port => new
            {
                port.MuxCardId,
                port.PortNumber
            })
            .IsUnique();

        modelBuilder.Entity<MuxPort>()
            .HasIndex(port => port.E1Id)
            .IsUnique()
            .HasFilter("[E1Id] IS NOT NULL");
    }
    private static void ConfigureCommunicationPath(
    ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommunicationPath>()
            .HasIndex(path => path.Name)
            .IsUnique();

        modelBuilder.Entity<CommunicationPath>()
            .Property(path => path.IsActive)
            .HasDefaultValue(true);
    }
    private static void ConfigureCommunicationPathSegment(
    ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommunicationPathSegment>()
            .HasOne(segment => segment.CommunicationPath)
            .WithMany(path => path.Segments)
            .HasForeignKey(segment => segment.CommunicationPathId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CommunicationPathSegment>()
            .HasOne(segment => segment.CommunicationLink)
            .WithMany()
            .HasForeignKey(segment => segment.CommunicationLinkId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CommunicationPathSegment>()
            .HasIndex(segment => new
            {
                segment.CommunicationPathId,
                segment.Order
            })
            .IsUnique();
    }
    private static void ConfigureCustomer(
    ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>()
            .HasIndex(customer => customer.Name)
            .IsUnique();

        modelBuilder.Entity<Customer>()
            .Property(customer => customer.IsActive)
            .HasDefaultValue(true);
    }
    private static void ConfigureCustomerConnection(
    ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerConnection>()
            .HasOne(connection => connection.Customer)
            .WithMany(customer => customer.Connections)
            .HasForeignKey(connection => connection.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CustomerConnection>()
            .HasOne(connection => connection.CommunicationPath)
            .WithMany(path => path.CustomerConnections)
            .HasForeignKey(connection => connection.CommunicationPathId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CustomerConnection>()
            .HasIndex(connection => connection.ConnectionGroupId)
            .IsUnique();

        modelBuilder.Entity<CustomerConnection>()
            .Property(connection => connection.IsActive)
            .HasDefaultValue(true);
    }
    private static void ConfigureCustomerConnectionSegment(
    ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerConnectionSegment>()
            .HasOne(segment => segment.CustomerConnection)
            .WithMany(connection => connection.Segments)
            .HasForeignKey(segment => segment.CustomerConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CustomerConnectionSegment>()
            .HasOne(segment => segment.CommunicationPathSegment)
            .WithMany()
            .HasForeignKey(segment => segment.CommunicationPathSegmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CustomerConnectionSegment>()
            .HasOne(segment => segment.E1)
            .WithMany()
            .HasForeignKey(segment => segment.E1Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CustomerConnectionSegment>()
            .HasIndex(segment => new
            {
                segment.CustomerConnectionId,
                segment.CommunicationPathSegmentId
            })
            .IsUnique();

        modelBuilder.Entity<CustomerConnectionSegment>()
            .HasIndex(segment => segment.E1Id)
            .IsUnique();
    }
}