using CloudProperty.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace CloudProperty
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<LookupToken> LookupTokens { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<FileStorage> Blobs { get; set; }
    }
}
