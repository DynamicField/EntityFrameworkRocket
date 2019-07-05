using System;

namespace EntityFrameworkRocket.Tests
{
    public static class CodeTemplates
    {
        public static string LinqContext(string code)
        => BaseCoreUsings + @"
namespace Tests
{
    class Test
    {
        public void Execute()
        {
            using (var context = new TestDbContext())
            {
" + code.Indent(4) + @"
            }
        }
    }
}";
        /// <summary>
        /// Base usings with: System, EFCoreLib, Linq, EFCore, Collections.Generic.
        /// </summary>
        public static string BaseCoreUsings
            => @"using System;
using EntityFrameworkRocket.Tests.EFCoreLib;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;";

        public static string Indent(this string code, int tabCount)
        {
            var tabs = new string(' ', tabCount * 4);
            return (tabs + code).Replace(Environment.NewLine, Environment.NewLine + tabs);
        }
    }
}