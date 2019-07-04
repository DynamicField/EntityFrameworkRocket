using System;
using System.Collections.Generic;
using System.Text;

namespace EntityFrameworkRocket.Tests.EFCoreLib
{
    public class Thing
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public World World { get; set; }
        public int WorldId { get; set; }
    }
}
