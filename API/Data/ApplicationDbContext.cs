using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shared.Model;

namespace API.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Test> Test { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Test>().HasData(
                new Test { ID = 1, Name = "Test Entry 1" },
                new Test { ID = 2, Name = "Test Entry 2" },
                new Test { ID = 3, Name = "Test Entry 3" }
            );

            base.OnModelCreating(modelBuilder);
        }
    }

    public class ApplicationUser : IdentityUser
    {
        // Propeties
    }

}