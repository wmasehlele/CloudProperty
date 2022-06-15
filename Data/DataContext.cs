using CloudProperty.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudProperty.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<LookupToken> LookupTokens { get; set; }
        public DbSet<Category> Categories { get; set; }

    }
}
