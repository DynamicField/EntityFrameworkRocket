using System;
using System.Collections.Generic;
using System.Text;

namespace EntityFrameworkRocket.Tests.EFCoreLib
{
    public class ChildThing : Thing
    {
        public string ChildProperty { get; set; }
    }

    public class ChildThingDto
    {
        public int Id { get; set; }
        public string ChildProperty { get; set; }
    }
}
