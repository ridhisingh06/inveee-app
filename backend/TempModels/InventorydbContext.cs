using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace invmgmt.web.TempModels;

public partial class InventorydbContext : DbContext
{
    public InventorydbContext()
    {
    }

    public InventorydbContext(DbContextOptions<InventorydbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ApprovalLog> ApprovalLogs { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Bill> Bills { get; set; }

    public virtual DbSet<BillItem> BillItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<InventoryStock> InventoryStocks { get; set; }

    public virtual DbSet<IssueLog> IssueLogs { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<OrderSummary> OrderSummaries { get; set; }

    public virtual DbSet<OrderSummaryItem> OrderSummaryItems { get; set; }

    public virtual DbSet<Personnel> Personnel { get; set; }

    public virtual DbSet<ReceivedLog> ReceivedLogs { get; set; }

    public virtual DbSet<RegistrationRequest> RegistrationRequests { get; set; }

    public virtual DbSet<Request> Requests { get; set; }

    public virtual DbSet<RequestItem> RequestItems { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RoleItemLimit> RoleItemLimits { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=inveee-postgres.citg4maasb05.us-east-1.rds.amazonaws.com;Port=5432;Database=inventorydb;Username=postgres;Password=ridhisingh2003;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=20;Connection Idle Lifetime=30;SSL Mode=Require");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApprovalLog>(entity =>
        {
            entity.HasIndex(e => e.RequestId, "IX_ApprovalLogs_RequestId");

            entity.HasIndex(e => e.UserId, "IX_ApprovalLogs_UserId");

            entity.HasOne(d => d.Request).WithMany(p => p.ApprovalLogs).HasForeignKey(d => d.RequestId);

            entity.HasOne(d => d.User).WithMany(p => p.ApprovalLogs).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasIndex(e => e.BillNo, "IX_Bills_BillNo").IsUnique();

            entity.HasIndex(e => e.CreatedAt, "IX_Bills_CreatedAt");

            entity.HasIndex(e => e.CreatedByUserId, "IX_Bills_CreatedByUserId");

            entity.Property(e => e.BillNo).HasMaxLength(50);
            entity.Property(e => e.VendorName).HasMaxLength(200);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Bills)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BillItem>(entity =>
        {
            entity.HasIndex(e => e.BillId, "IX_BillItems_BillId");

            entity.HasIndex(e => e.ItemId, "IX_BillItems_ItemId");

            entity.HasOne(d => d.Bill).WithMany(p => p.BillItems).HasForeignKey(d => d.BillId);

            entity.HasOne(d => d.Item).WithMany(p => p.BillItems)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryStock>(entity =>
        {
            entity.HasIndex(e => e.ItemId, "IX_InventoryStocks_ItemId").IsUnique();

            entity.HasOne(d => d.Item).WithOne(p => p.InventoryStock).HasForeignKey<InventoryStock>(d => d.ItemId);
        });

        modelBuilder.Entity<IssueLog>(entity =>
        {
            entity.HasIndex(e => e.RequestId, "IX_IssueLogs_RequestId");

            entity.HasIndex(e => e.UserId, "IX_IssueLogs_UserId");

            entity.HasOne(d => d.Request).WithMany(p => p.IssueLogs).HasForeignKey(d => d.RequestId);

            entity.HasOne(d => d.User).WithMany(p => p.IssueLogs).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasIndex(e => e.CategoryId, "IX_Items_CategoryId");

            entity.HasIndex(e => e.Name, "IX_Items_Name").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(255);

            entity.HasOne(d => d.Category).WithMany(p => p.Items).HasForeignKey(d => d.CategoryId);
        });

        modelBuilder.Entity<OrderSummary>(entity =>
        {
            entity.HasIndex(e => e.ApprovedByUserId, "IX_OrderSummaries_ApprovedByUserId");

            entity.HasIndex(e => e.IssuedByUserId, "IX_OrderSummaries_IssuedByUserId");

            entity.HasIndex(e => e.ReceivedDate, "IX_OrderSummaries_ReceivedDate");

            entity.HasIndex(e => e.RequestId, "IX_OrderSummaries_RequestId").IsUnique();

            entity.HasIndex(e => e.Status, "IX_OrderSummaries_Status");

            entity.HasIndex(e => e.UserId, "IX_OrderSummaries_UserId");

            entity.HasOne(d => d.ApprovedByUser).WithMany(p => p.OrderSummaryApprovedByUsers)
                .HasForeignKey(d => d.ApprovedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.IssuedByUser).WithMany(p => p.OrderSummaryIssuedByUsers)
                .HasForeignKey(d => d.IssuedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Request).WithOne(p => p.OrderSummary)
                .HasForeignKey<OrderSummary>(d => d.RequestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.User).WithMany(p => p.OrderSummaryUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderSummaryItem>(entity =>
        {
            entity.HasIndex(e => e.ItemId, "IX_OrderSummaryItems_ItemId");

            entity.HasIndex(e => e.OrderSummaryId, "IX_OrderSummaryItems_OrderSummaryId");

            entity.HasIndex(e => e.RequestItemId, "IX_OrderSummaryItems_RequestItemId");

            entity.HasOne(d => d.Item).WithMany(p => p.OrderSummaryItems)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.OrderSummary).WithMany(p => p.OrderSummaryItems).HasForeignKey(d => d.OrderSummaryId);

            entity.HasOne(d => d.RequestItem).WithMany(p => p.OrderSummaryItems)
                .HasForeignKey(d => d.RequestItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Personnel>(entity =>
        {
            entity.HasIndex(e => e.Email, "IX_Personnel_Email").IsUnique();

            entity.Property(e => e.Building).HasMaxLength(100);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Designation).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Icnumber)
                .HasMaxLength(20)
                .HasColumnName("ICNumber");
            entity.Property(e => e.IdCardNumber).HasMaxLength(30);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.OfficePhone).HasMaxLength(20);
            entity.Property(e => e.PhotoPath).HasMaxLength(500);
            entity.Property(e => e.ReportingOfficer).HasMaxLength(100);
            entity.Property(e => e.ResidentialPhone).HasMaxLength(20);
        });

        modelBuilder.Entity<ReceivedLog>(entity =>
        {
            entity.HasIndex(e => e.RequestId, "IX_ReceivedLogs_RequestId");

            entity.HasIndex(e => e.UserId, "IX_ReceivedLogs_UserId");

            entity.HasOne(d => d.Request).WithMany(p => p.ReceivedLogs).HasForeignKey(d => d.RequestId);

            entity.HasOne(d => d.User).WithMany(p => p.ReceivedLogs).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<RegistrationRequest>(entity =>
        {
            entity.HasIndex(e => e.ApprovedUserId, "IX_RegistrationRequests_ApprovedUserId");

            entity.HasIndex(e => e.DepartmentId, "IX_RegistrationRequests_DepartmentId");

            entity.HasIndex(e => e.RoleId, "IX_RegistrationRequests_RoleId");

            entity.HasIndex(e => e.Status, "IX_RegistrationRequests_Status");

            entity.HasIndex(e => new { e.Status, e.CreatedAt }, "IX_RegistrationRequests_Status_CreatedAt");

            entity.HasOne(d => d.ApprovedUser).WithMany(p => p.RegistrationRequests).HasForeignKey(d => d.ApprovedUserId);

            entity.HasOne(d => d.Department).WithMany(p => p.RegistrationRequests).HasForeignKey(d => d.DepartmentId);

            entity.HasOne(d => d.Role).WithMany(p => p.RegistrationRequests).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<Request>(entity =>
        {
            entity.HasIndex(e => e.CategoryId, "IX_Requests_CategoryId");

            entity.HasIndex(e => new { e.Status, e.CreatedAt }, "IX_Requests_Status_CreatedAt");

            entity.HasIndex(e => new { e.UserId, e.Status }, "IX_Requests_UserId_Status");

            entity.HasOne(d => d.Category).WithMany(p => p.Requests).HasForeignKey(d => d.CategoryId);

            entity.HasOne(d => d.User).WithMany(p => p.Requests).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<RequestItem>(entity =>
        {
            entity.HasIndex(e => e.ItemId, "IX_RequestItems_ItemId");

            entity.HasIndex(e => new { e.RequestId, e.Status }, "IX_RequestItems_RequestId_Status");

            entity.HasOne(d => d.Item).WithMany(p => p.RequestItems).HasForeignKey(d => d.ItemId);

            entity.HasOne(d => d.Request).WithMany(p => p.RequestItems).HasForeignKey(d => d.RequestId);
        });

        modelBuilder.Entity<RoleItemLimit>(entity =>
        {
            entity.HasIndex(e => e.ItemId, "IX_RoleItemLimits_ItemId");

            entity.HasIndex(e => e.RoleId, "IX_RoleItemLimits_RoleId");

            entity.HasOne(d => d.Item).WithMany(p => p.RoleItemLimits).HasForeignKey(d => d.ItemId);

            entity.HasOne(d => d.Role).WithMany(p => p.RoleItemLimits).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.DepartmentId, "IX_Users_DepartmentId");

            entity.HasIndex(e => new { e.IsApproved, e.CreatedAt }, "IX_Users_IsApproved_CreatedAt");

            entity.HasOne(d => d.Department).WithMany(p => p.Users).HasForeignKey(d => d.DepartmentId);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasIndex(e => e.RoleId, "IX_UserRoles_RoleId");

            entity.HasIndex(e => e.UserId, "IX_UserRoles_UserId");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles).HasForeignKey(d => d.RoleId);

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles).HasForeignKey(d => d.UserId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
