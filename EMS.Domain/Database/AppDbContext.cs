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

        public DbSet<DbModels.Employee> Employees { get; set; }
        public DbSet<DbModels.JobPosition> JobPositions { get; set; }
        public DbSet<DbModels.Document> Documents { get; set; }
        public DbSet<DbModels.DocumentType> DocumentTypes { get; set; }
        public DbSet<DbModels.EmployeeDocument> EmployeeDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
