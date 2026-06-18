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

            modelBuilder.Entity<Item>()
                .HasOne(i => i.InventoryStock)
                .WithOne(s => s.Item)
                .HasForeignKey<InventoryStock>(s => s.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RequestItem>()
                .HasOne(ri => ri.Item)
                .WithMany(i => i.RequestItems)
                .HasForeignKey(ri => ri.ItemId);

            // Add indexes on RequestItems to improve query/filter performance
            modelBuilder.Entity<RequestItem>()
                .HasIndex(ri => ri.ItemId);
            modelBuilder.Entity<RequestItem>()
                .HasIndex(ri => ri.RequestId);
            modelBuilder.Entity<RequestItem>()
                .HasIndex(ri => ri.Status);

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
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BillItem>()
                .HasIndex(bi => bi.BillId);
        }
    }
}
