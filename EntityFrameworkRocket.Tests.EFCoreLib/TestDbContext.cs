using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkRocket.Tests.EFCoreLib
{
    public class TestDbContext : DbContext
    {
        public DbSet<Thing> Things { get; set; }
        public DbSet<World> Worlds { get; set; }

    }
}
