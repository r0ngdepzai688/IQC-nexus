using Microsoft.EntityFrameworkCore;
using IqcQms.Domain.Entities.Auth;
using IqcQms.Domain.Entities.HR;
using IqcQms.Domain.Entities.Equipment;
using IqcQms.Domain.Entities.Standards;

namespace IqcQms.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Equipment> Equipments { get; set; }
        public DbSet<Part> Parts { get; set; }
        public DbSet<InspectionStandard> InspectionStandards { get; set; }
        public DbSet<InspectionItem> InspectionItems { get; set; }
        public DbSet<DynamicForm> DynamicForms { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Explicit table mappings and relationships
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId);

            modelBuilder.Entity<InspectionStandard>()
                .HasOne(i => i.Part)
                .WithMany()
                .HasForeignKey(i => i.PartId);

            modelBuilder.Entity<InspectionItem>()
                .HasOne(i => i.InspectionStandard)
                .WithMany(s => s.Items)
                .HasForeignKey(i => i.InspectionStandardId);
        }
    }
}
