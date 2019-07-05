using System;

namespace EntityFrameworkRocket.Tests.EFCoreLib
{
    public class Thing
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public DateTime CreatedDate { get; set; }
        public bool IsGood { get; set; }

        public World World { get; set; }
        public int WorldId { get; set; }

    }
}
