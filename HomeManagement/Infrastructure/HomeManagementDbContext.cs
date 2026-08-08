using HomeManagement.Shared;
using Microsoft.EntityFrameworkCore;

namespace HomeManagement.Infrastructure;

public class HomeManagementDbContext(DbContextOptions<HomeManagementDbContext> options) : DbContext(options)
{
    public DbSet<Device> Devices => Set<Device>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var device = modelBuilder.Entity<Device>();
        device.ToTable("Devices");
        device.HasKey(d => d.Name);
        device.Property(d => d.Name).HasMaxLength(100).IsRequired();
        device.Property(d => d.Address).HasMaxLength(100).IsRequired();

        device.OwnsMany(d => d.Actions, a =>
        {
            a.WithOwner().HasForeignKey("DeviceName");
            a.ToTable("DeviceActions");
            a.Property(x => x.Action).HasMaxLength(50).IsRequired();
            a.Property(x => x.Command).IsRequired();

            a.Property<int>("Id").ValueGeneratedOnAdd();
            a.HasKey("Id");
        });

        device.OwnsMany(d => d.Configurations, a =>
        {
            a.WithOwner().HasForeignKey("DeviceName");
            a.ToTable("DeviceConfigurations");
            a.Property(x => x.Name).HasMaxLength(50).IsRequired();
            a.Property(x => x.Value).IsRequired();

            a.Property<int>("Id").ValueGeneratedOnAdd();
            a.HasKey("Id");
        });
    }
}