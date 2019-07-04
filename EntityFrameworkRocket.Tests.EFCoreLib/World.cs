using System;
using System.Collections.Generic;
using System.Text;

namespace EntityFrameworkRocket.Tests.EFCoreLib
{
    public class World
    {
        public int Id { get; set; }
        public ICollection<Thing> Things { get; set; }
    }
}
