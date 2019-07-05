using EntityFrameworkRocket.Refactorings;
using Gu.Roslyn.Asserts;
using NUnit.Framework;

namespace EntityFrameworkRocket.Tests.Refactorings
{
    [TestFixture]
    public class AddNavigationPropertyKeyTests : RefactoringTest<AddNavigationPropertyKeyCodeRefactoringProvider>
    {
        [Test]
        public void Refactoring_NoPropertyKey_AddsKey()
        {
            const string code = @"
using System;
namespace Tests
{
    class Thing
    {
        public int Id { get; set; }
        public Thing Related {↓ get; set; }
    }
}
";
            const string fixedCode = @"
using System;
namespace Tests
{
    class Thing
    {
        public int Id { get; set; }
        public Thing Related { get; set; }
        public int RelatedId { get; set; }
    }
}
";
            RoslynAssert.Refactoring(Refactoring, code, fixedCode);
        }
        [Test]
        public void Refactoring_HasPropertyKey_NoRefactor()
        {
            const string code = @"
using System;
namespace Tests
{
    class Thing
    {
        public int Id { get; set; }
        public Thing Related {↓ get; set; }
        public int RelatedId { get; set; }
    }
}
";
            RoslynAssert.NoRefactoring(Refactoring, code);
        }
        [Test]
        public void Refactoring_NoId_NoRefactor()
        {
            const string code = @"
using System;
namespace Tests
{
    class Thing
    {
        public Thing Related {↓ get; set; }
    }
}
";
            RoslynAssert.NoRefactoring(Refactoring, code);
        }
    }
}