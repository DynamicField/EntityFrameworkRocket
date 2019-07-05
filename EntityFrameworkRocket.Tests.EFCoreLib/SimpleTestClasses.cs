using System.Collections.Generic;

namespace EntityFrameworkRocket.Tests.EFCoreLib
{
    public class CollectionEntity
    {
        public ICollection<World> Worlds { get; set; }
    }
    public class CollectionEntityDto
    {
        public ICollection<World> Worlds { get; set; }
    }

    public class SimpleEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class SimpleEntityDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class SimpleEntityReadOnlyIdDto
    {
        public int Id { get; }
        public string Name { get; set; }
    }
    public class SetOnlyNameEntity
    {
        public int Id { get; set; }
        public string Name { set { } }
    }

    public class SetOnlyNameEntityDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}