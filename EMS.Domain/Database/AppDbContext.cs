using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EMS.Domain.Database
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<DbModels.Organization> Organizations { get; set; }
        public DbSet<DbModels.Department> Departments { get; set; }
        public DbSet<DbModels.Location> Locations { get; set; }
        public DbSet<DbModels.Employee> Employees { get; set; }
        public DbSet<DbModels.JobPosition> JobPositions { get; set; }
        public DbSet<DbModels.Document> Documents { get; set; }
        public DbSet<DbModels.DocumentType> DocumentTypes { get; set; }
        public DbSet<DbModels.EmployeeDocument> EmployeeDocuments { get; set; }
        public DbSet<DbModels.Site> Sites { get; set; }
        public DbSet<DbModels.EmployeeSite> EmployeeSites { get; set; }
        public DbSet<DbModels.Menu> Menus { get; set; }
        public DbSet<DbModels.RoleKeyPermission> RoleKeyPermissions { get; set; }
        public DbSet<DbModels.AttendanceRecord> AttendanceRecords { get; set; }
        public DbSet<DbModels.AttendanceBreak> AttendanceBreaks { get; set; }
        public DbSet<DbModels.AttendancePolicy> AttendancePolicies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
