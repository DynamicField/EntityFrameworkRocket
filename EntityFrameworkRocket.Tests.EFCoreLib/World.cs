using System;
using System.Collections.Generic;

namespace EntityFrameworkRocket.Tests.EFCoreLib
{
    public class World
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal Money { get; set; }
        public ICollection<Thing> Things { get; set; }
    }
}
