namespace EntityFrameworkRocket.Tests
{
    public static class CodeTemplates
    {
        public static string LinqContext(string code)
        => @"
using System;
using EntityFrameworkRocket.Tests.EFCoreLib;
using System.Linq;
using Microsoft.EntityFrameworkCore;
namespace Tests
{
    class Test
    {
        public void Execute()
        {
            using (var context = new TestDbContext())
            {
                " + code + @"
            }
        }
    }
}";
    }
}