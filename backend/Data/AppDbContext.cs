using Microsoft.EntityFrameworkCore;
using invmgmt.web.Models;

namespace invmgmt.web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        //  User Management
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Department> Departments { get; set; }

        //  Inventory
        public DbSet<Category> Categories { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<InventoryStock> InventoryStocks { get; set; }
        public DbSet<RoleItemLimit> RoleItemLimits { get; set; }

        //  Requests
        public DbSet<Request> Requests { get; set; }
        public DbSet<RequestItem> RequestItems { get; set; }
        public DbSet<ApprovalLog> ApprovalLogs { get; set; }
        public DbSet<IssueLog> IssueLogs { get; set; }
        public DbSet<ReceivedLog> ReceivedLogs { get; set; }

        //  Order Summary (NEW - Enterprise Workflow)
        public DbSet<OrderSummary> OrderSummaries { get; set; }
        public DbSet<OrderSummaryItem> OrderSummaryItems { get; set; }

        //  Audit
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<RegistrationRequest> RegistrationRequests { get; set; }

        //  Personnel Management
        public DbSet<Personnel> Personnel { get; set; }

        //  Bills & Challan
        public DbSet<Bill> Bills { get; set; }
        public DbSet<BillItem> BillItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "User" },
                new Role { Id = 2, Name = "Issuer" },
                new Role { Id = 3, Name = "Admin" }
            );

            modelBuilder.Entity<Department>().HasData(
                new Department { Id = 1, Name = "Admin" },
                new Department { Id = 2, Name = "IT" },
                new Department { Id = 3, Name = "HR" },
                new Department { Id = 4, Name = "Finance" }
            );

            modelBuilder.Entity<Item>()
                .HasOne(i => i.Category)
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.CategoryId);

            // Configure alternate (unique) key for Item.ItemId to be used as principal key in related entities
            modelBuilder.Entity<Item>()
                .HasAlternateKey(i => i.ItemId);

            modelBuilder.Entity<Item>()
                .HasOne(i => i.InventoryStock)
                .WithOne(s => s.Item)
                .HasForeignKey<InventoryStock>(s => s.ItemId)
                                .HasPrincipalKey(nameof(Item.ItemId))
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RequestItem>()
                .HasOne(ri => ri.Item)
                .WithMany(i => i.RequestItems)
                .HasForeignKey(ri => ri.ItemId)
                .HasPrincipalKey(nameof(Item.ItemId));

            // Add indexes on RequestItems to improve query/filter performance
            modelBuilder.Entity<RequestItem>()
                .HasIndex(ri => ri.ItemId);
            modelBuilder.Entity<RequestItem>()
                .HasIndex(ri => new { ri.RequestId, ri.Status });

            modelBuilder.Entity<RequestItem>()
                .HasOne(ri => ri.Request)
                .WithMany(r => r.RequestItems)
                .HasForeignKey(ri => ri.RequestId);

            modelBuilder.Entity<RegistrationRequest>()
                .HasIndex(r => r.Status);

            // Personnel: unique email index
            modelBuilder.Entity<Personnel>()
                .HasIndex(p => p.Email)
                .IsUnique();

            // Bill relationships
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.CreatedByUser)
                .WithMany()
                .HasForeignKey(b => b.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Bill>()
                .HasMany(b => b.Items)
                .WithOne(bi => bi.Bill)
                .HasForeignKey(bi => bi.BillId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Bill>()
                .HasIndex(b => b.BillNo)
                .IsUnique();

            modelBuilder.Entity<Bill>()
                .HasIndex(b => b.CreatedAt);

            // BillItem relationships
            modelBuilder.Entity<BillItem>()
                .HasOne(bi => bi.Item)
                .WithMany()
                .HasForeignKey(bi => bi.ItemId)
                                .HasPrincipalKey(nameof(Item.ItemId))
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BillItem>()
                .HasIndex(bi => bi.BillId);

            // RoleItemLimit to Item relationship
            modelBuilder.Entity<RoleItemLimit>()
                .HasOne(rl => rl.Item)
                .WithMany()
                .HasForeignKey(rl => rl.ItemId)
                                .HasPrincipalKey(nameof(Item.ItemId))
                .OnDelete(DeleteBehavior.Cascade);

            // ========== ORDER SUMMARY RELATIONSHIPS (NEW) ==========

            // OrderSummary to Request (one-to-one: one order summary per request after receiving)
            modelBuilder.Entity<OrderSummary>()
                .HasOne(os => os.Request)
                .WithMany()
                .HasForeignKey(os => os.RequestId)
                .OnDelete(DeleteBehavior.Restrict);

            // OrderSummary to User (many-to-one: user can have many order summaries)
            modelBuilder.Entity<OrderSummary>()
                .HasOne(os => os.User)
                .WithMany()
                .HasForeignKey(os => os.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // OrderSummary to Issuer User
            modelBuilder.Entity<OrderSummary>()
                .HasOne(os => os.IssuedByUser)
                .WithMany()
                .HasForeignKey(os => os.IssuedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // OrderSummary to Approver User
            modelBuilder.Entity<OrderSummary>()
                .HasOne(os => os.ApprovedByUser)
                .WithMany()
                .HasForeignKey(os => os.ApprovedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // OrderSummary to OrderSummaryItem (one-to-many)
            modelBuilder.Entity<OrderSummary>()
                .HasMany(os => os.Items)
                .WithOne(osi => osi.OrderSummary)
                .HasForeignKey(osi => osi.OrderSummaryId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderSummaryItem to Item
            modelBuilder.Entity<OrderSummaryItem>()
                .HasOne(osi => osi.Item)
                .WithMany()
                .HasForeignKey(osi => osi.ItemId)
                                .HasPrincipalKey(nameof(Item.ItemId))
                .OnDelete(DeleteBehavior.Restrict);

            // OrderSummaryItem to RequestItem (reference to original request item)
            modelBuilder.Entity<OrderSummaryItem>()
                .HasOne(osi => osi.RequestItem)
                .WithMany()
                .HasForeignKey(osi => osi.RequestItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for OrderSummary for fast lookups
            modelBuilder.Entity<OrderSummary>()
                .HasIndex(os => os.UserId);
            modelBuilder.Entity<OrderSummary>()
                .HasIndex(os => os.RequestId)
                .IsUnique();
            modelBuilder.Entity<OrderSummary>()
                .HasIndex(os => os.ReceivedDate);
            modelBuilder.Entity<OrderSummary>()
                .HasIndex(os => os.Status);

            // Indexes for OrderSummaryItem
            modelBuilder.Entity<OrderSummaryItem>()
                .HasIndex(osi => osi.OrderSummaryId);
            modelBuilder.Entity<OrderSummaryItem>()
                .HasIndex(osi => osi.ItemId);
        }
    }
}
