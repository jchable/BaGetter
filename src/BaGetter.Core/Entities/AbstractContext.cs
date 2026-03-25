using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BaGetter.Core;

public abstract class AbstractContext<TContext> : IdentityDbContext<BaGetterUser, BaGetterRole, string>, IContext where TContext : DbContext
{
    public const int DefaultMaxStringLength = 4000;

    public const int MaxPackageIdLength = 128;
    public const int MaxPackageVersionLength = 64;
    public const int MaxPackageMinClientVersionLength = 44;
    public const int MaxPackageLanguageLength = 20;
    public const int MaxPackageTitleLength = 256;
    public const int MaxPackageTypeNameLength = 512;
    public const int MaxPackageTypeVersionLength = 64;
    public const int MaxRepositoryTypeLength = 100;
    public const int MaxTargetFrameworkLength = 256;

    public const int MaxPackageDependencyVersionRangeLength = 256;

    private readonly string? _tenantId;

    protected AbstractContext(DbContextOptions<TContext> efOptions, ITenantProvider? tenantProvider = null)
        : base(efOptions)
    {
        _tenantId = tenantProvider?.GetCurrentTenantId();
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<UserInvitation> UserInvitations { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<ApiKeyEntity> ApiKeys { get; set; }

    public DbSet<Package> Packages { get; set; }
    public DbSet<PackageDependency> PackageDependencies { get; set; }
    public DbSet<PackageType> PackageTypes { get; set; }
    public DbSet<TargetFramework> TargetFrameworks { get; set; }

    public Task<int> SaveChangesAsync() => SaveChangesAsync(default);

    public virtual async Task RunMigrationsAsync(CancellationToken cancellationToken)
        => await Database.MigrateAsync(cancellationToken);

    public abstract bool IsUniqueConstraintViolationException(DbUpdateException exception);

    public virtual bool SupportsLimitInSubqueries => true;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Tenant>(BuildTenantEntity);
        builder.Entity<Package>(BuildPackageEntity);
        builder.Entity<PackageDependency>(BuildPackageDependencyEntity);
        builder.Entity<PackageType>(BuildPackageTypeEntity);
        builder.Entity<TargetFramework>(BuildTargetFrameworkEntity);
        builder.Entity<UserInvitation>(BuildUserInvitationEntity);
        builder.Entity<AuditLog>(BuildAuditLogEntity);
        builder.Entity<ApiKeyEntity>(BuildApiKeyEntity);
    }

    private void BuildPackageEntity(EntityTypeBuilder<Package> package)
    {
        package.HasKey(p => p.Key);
        package.HasIndex(p => p.Id);
        package.HasIndex(p => new { p.TenantId, p.Id, p.NormalizedVersionString })
            .IsUnique();

        // Global query filter: when _tenantId is set, only return packages for that tenant
        package.HasQueryFilter(p => _tenantId == null || p.TenantId == _tenantId);

        package.Property(p => p.TenantId).HasMaxLength(450).IsRequired();
        package.HasOne(p => p.Tenant)
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        package.Property(p => p.Id)
            .HasMaxLength(MaxPackageIdLength)
            .IsRequired();

        package.Property(p => p.NormalizedVersionString)
            .HasColumnName("Version")
            .HasMaxLength(MaxPackageVersionLength)
            .IsRequired();

        package.Property(p => p.OriginalVersionString)
            .HasColumnName("OriginalVersion")
            .HasMaxLength(MaxPackageVersionLength);

        package.Property(p => p.ReleaseNotes)
            .HasColumnName("ReleaseNotes");

        package.Property(p => p.Authors)
            .HasMaxLength(DefaultMaxStringLength)
            .HasConversion(StringArrayToJsonConverter.Instance)
            .Metadata.SetValueComparer(StringArrayComparer.Instance);

        package.Property(p => p.IconUrl)
            .HasConversion(UriToStringConverter.Instance)
            .HasMaxLength(DefaultMaxStringLength);

        package.Property(p => p.LicenseUrl)
            .HasConversion(UriToStringConverter.Instance)
            .HasMaxLength(DefaultMaxStringLength);

        package.Property(p => p.ProjectUrl)
            .HasConversion(UriToStringConverter.Instance)
            .HasMaxLength(DefaultMaxStringLength);

        package.Property(p => p.RepositoryUrl)
            .HasConversion(UriToStringConverter.Instance)
            .HasMaxLength(DefaultMaxStringLength);

        package.Property(p => p.Tags)
            .HasMaxLength(DefaultMaxStringLength)
            .HasConversion(StringArrayToJsonConverter.Instance)
            .Metadata.SetValueComparer(StringArrayComparer.Instance);

        package.Property(p => p.Description).HasMaxLength(DefaultMaxStringLength);
        package.Property(p => p.Language).HasMaxLength(MaxPackageLanguageLength);
        package.Property(p => p.MinClientVersion).HasMaxLength(MaxPackageMinClientVersionLength);
        package.Property(p => p.Summary).HasMaxLength(DefaultMaxStringLength);
        package.Property(p => p.Title).HasMaxLength(MaxPackageTitleLength);
        package.Property(p => p.RepositoryType).HasMaxLength(MaxRepositoryTypeLength);

        package.Ignore(p => p.Version);
        package.Ignore(p => p.IconUrlString);
        package.Ignore(p => p.LicenseUrlString);
        package.Ignore(p => p.ProjectUrlString);
        package.Ignore(p => p.RepositoryUrlString);
        package.Ignore(p => p.Deprecation);

        // TODO: This is needed to make the dependency to package relationship required.
        // Unfortunately, this would generate a migration that drops a foreign key, which
        // isn't supported by SQLite. The migrations will be need to be recreated for this.
        // Consumers will need to recreate their database and reindex all their packages.
        // To make this transition easier, I'd like to finish this change:
        // https://github.com/loic-sharma/BaGet/pull/174
        //package.HasMany(p => p.Dependencies)
        //    .WithOne(d => d.Package)
        //    .IsRequired();

        package.HasMany(p => p.PackageTypes)
            .WithOne(d => d.Package)
            .IsRequired();

        package.HasMany(p => p.TargetFrameworks)
            .WithOne(d => d.Package)
            .IsRequired();

        package.Property(p => p.RowVersion).IsRowVersion();
    }

    private void BuildPackageDependencyEntity(EntityTypeBuilder<PackageDependency> dependency)
    {
        dependency.HasKey(d => d.Key);
        dependency.HasIndex(d => d.Id);

        dependency.Property(d => d.Id).HasMaxLength(MaxPackageIdLength);
        dependency.Property(d => d.VersionRange).HasMaxLength(MaxPackageDependencyVersionRangeLength);
        dependency.Property(d => d.TargetFramework).HasMaxLength(MaxTargetFrameworkLength);
    }

    private void BuildPackageTypeEntity(EntityTypeBuilder<PackageType> type)
    {
        type.HasKey(d => d.Key);
        type.HasIndex(d => d.Name);

        type.Property(d => d.Name).HasMaxLength(MaxPackageTypeNameLength);
        type.Property(d => d.Version).HasMaxLength(MaxPackageTypeVersionLength);
    }

    private void BuildTargetFrameworkEntity(EntityTypeBuilder<TargetFramework> targetFramework)
    {
        targetFramework.HasKey(f => f.Key);
        targetFramework.HasIndex(f => f.Moniker);

        targetFramework.Property(f => f.Moniker).HasMaxLength(MaxTargetFrameworkLength);
    }

    private static void BuildTenantEntity(EntityTypeBuilder<Tenant> tenant)
    {
        tenant.HasKey(t => t.Id);
        tenant.HasIndex(t => t.Slug).IsUnique();

        tenant.Property(t => t.Id).HasMaxLength(450);
        tenant.Property(t => t.Name).HasMaxLength(256).IsRequired();
        tenant.Property(t => t.Slug).HasMaxLength(128).IsRequired();
    }

    private static void BuildApiKeyEntity(EntityTypeBuilder<ApiKeyEntity> key)
    {
        key.HasKey(k => k.Id);
        key.HasIndex(k => k.KeyHash).IsUnique();
        key.HasIndex(k => k.UserId);

        key.Property(k => k.Name).HasMaxLength(256).IsRequired();
        key.Property(k => k.KeyHash).HasMaxLength(64).IsRequired();
        key.Property(k => k.KeyPrefix).HasMaxLength(8).IsRequired();
        key.Property(k => k.Role).HasMaxLength(64).IsRequired();
        key.Property(k => k.UserId).HasMaxLength(450).IsRequired();

        key.HasOne(k => k.User)
            .WithMany()
            .HasForeignKey(k => k.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void BuildAuditLogEntity(EntityTypeBuilder<AuditLog> audit)
    {
        audit.HasKey(a => a.Id);
        audit.HasIndex(a => a.Timestamp);
        audit.HasIndex(a => a.UserId);
        audit.HasIndex(a => a.Action);

        audit.Property(a => a.Action).HasMaxLength(128).IsRequired();
        audit.Property(a => a.UserName).HasMaxLength(256);
        audit.Property(a => a.UserId).HasMaxLength(450);
        audit.Property(a => a.ResourceType).HasMaxLength(128);
        audit.Property(a => a.ResourceId).HasMaxLength(512);
        audit.Property(a => a.Details).HasMaxLength(DefaultMaxStringLength);
        audit.Property(a => a.IpAddress).HasMaxLength(45);
    }

    private static void BuildUserInvitationEntity(EntityTypeBuilder<UserInvitation> invitation)
    {
        invitation.HasKey(i => i.Id);
        invitation.HasIndex(i => i.Token).IsUnique();
        invitation.HasIndex(i => i.Email);

        invitation.Property(i => i.Email).HasMaxLength(256).IsRequired();
        invitation.Property(i => i.Role).HasMaxLength(64).IsRequired();
        invitation.Property(i => i.Token).HasMaxLength(512).IsRequired();
        invitation.Property(i => i.InvitedById).HasMaxLength(450).IsRequired();

        invitation.HasOne(i => i.InvitedBy)
            .WithMany()
            .HasForeignKey(i => i.InvitedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
