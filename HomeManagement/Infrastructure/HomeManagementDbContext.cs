using HomeManagement.Shared;
using Microsoft.EntityFrameworkCore;

namespace HomeManagement.Infrastructure;

public class HomeManagementDbContext(DbContextOptions<HomeManagementDbContext> options) : DbContext(options)
{
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

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

        var workflow = modelBuilder.Entity<Workflow>();
        workflow.ToTable("Workflows");
        workflow.HasKey(x => x.Name);
        workflow.Property(x => x.Name).HasMaxLength(100).IsRequired();
        workflow.Property(x => x.Description).HasMaxLength(400);
        workflow.Property(x => x.ConditionOperator).IsRequired();
        workflow.Property(x => x.LastTriggeredAtUtc);
        workflow.Property(x => x.LastConditionMatched);

        workflow.OwnsMany(x => x.TriggerConditions, c =>
        {
            c.WithOwner().HasForeignKey("WorkflowName");
            c.ToTable("WorkflowTriggerConditions");
            c.Property(x => x.TriggerType).IsRequired();
            c.Property(x => x.TriggerDeviceName).HasMaxLength(100);
            c.Property(x => x.TriggerSourceActionName).HasMaxLength(100);
            c.Property(x => x.TriggerPropertyPath).HasMaxLength(200);
            c.Property(x => x.TriggerExpectedValue).HasMaxLength(500);

            c.Property<int>("Id").ValueGeneratedOnAdd();
            c.HasKey("Id");
        });

        workflow.OwnsMany(x => x.Steps, s =>
        {
            s.WithOwner().HasForeignKey("WorkflowName");
            s.ToTable("WorkflowSteps");
            s.Property(x => x.StepType).IsRequired();
            s.Property(x => x.DeviceName).HasMaxLength(100);
            s.Property(x => x.ActionName).HasMaxLength(100);
            s.Property(x => x.NotifyTitle).HasMaxLength(200);
            s.Property(x => x.NotifyMessageTemplate).HasMaxLength(2000);

            s.Property<int>("Id").ValueGeneratedOnAdd();
            s.HasKey("Id");
        });

        var appSetting = modelBuilder.Entity<AppSetting>();
        appSetting.ToTable("AppSettings");
        appSetting.HasKey(x => x.Name);
        appSetting.Property(x => x.Name).HasMaxLength(100).IsRequired();
        appSetting.Property(x => x.Value).HasMaxLength(2000).IsRequired();
        appSetting.Property(x => x.ValueType).IsRequired();
    }
}
