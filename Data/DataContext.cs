﻿using CloudProperty.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudProperty.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
