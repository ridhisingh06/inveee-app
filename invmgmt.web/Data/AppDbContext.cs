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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            modelBuilder.Entity<RequestItem>()
                .HasOne(ri => ri.Request)
                .WithMany(r => r.RequestItems)
                .HasForeignKey(ri => ri.RequestId);
        }
    }
}