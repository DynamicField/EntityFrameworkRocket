using System;
using System.Runtime.InteropServices.ComTypes;
using EntityFrameworkRocket.Refactorings;
using Gu.Roslyn.Asserts;
using NUnit.Framework;

namespace EntityFrameworkRocket.Tests.Refactorings
{
    public class MapPropertiesTests : RefactoringTest<MapPropertiesCodeRefactoringProvider>
    {
        private static string TestCode(string code)
            => CodeTemplates.BaseCoreUsings + @"
void Test(TestDbContext context)
{
" + code.Indent(1) + @"
}";

        [Test]
        public void Refactoring_ThingToThingDto_MapsAllProperties()
        {
            var code = TestCode(
@"context.Things.Select(x => new ThingDto
{↓
});");
            var fixedCode = TestCode(
@"context.Things.Select(x => new ThingDto
{
    Id = x.Id,
    Name = x.Name,
    IsGood = x.IsGood
});");

            RoslynAssert.Refactoring(Refactoring, code, fixedCode);
        }

        [Test]
        public void Refactoring_Collection_MapsAndAddsToList()
        {
            var code = TestCode(
@"Enumerable.Empty<CollectionEntity>().Select(x => new CollectionEntityDto
{↓
});");
            var fixedCode = TestCode(
@"Enumerable.Empty<CollectionEntity>().Select(x => new CollectionEntityDto
{
    Worlds = x.Worlds.ToList()
});");

            RoslynAssert.Refactoring(Refactoring, code, fixedCode);
        }

        [Test]
        public void Refactoring_WithReadOnlyDto_DoesNotMapReadOnlyProperty()
        {
            var code = TestCode(
@"Enumerable.Empty<SimpleEntity>().Select(x => new SimpleEntityReadOnlyIdDto
{↓
});");
            var fixedCode = TestCode(
@"Enumerable.Empty<SimpleEntity>().Select(x => new SimpleEntityReadOnlyIdDto
{
    Name = x.Name
});"); // Id is a readonly property in the dto

            RoslynAssert.Refactoring(Refactoring, code, fixedCode);
        }

        [Test]
        public void Refactoring_WithSetOnlyEntity_DoesNotMapSetOnlyProperty()
        {
            var code = TestCode(
                @"Enumerable.Empty<SetOnlyNameEntity>().Select(x => new SetOnlyNameEntity
{↓
});");
            var fixedCode = TestCode(
                @"Enumerable.Empty<SetOnlyNameEntity>().Select(x => new SetOnlyNameEntity
{
    Id = x.Id
});"); // Id is a readonly property in the dto

            RoslynAssert.Refactoring(Refactoring, code, fixedCode);
        }

        [Test]
        public void Refactoring_NoMappableProperties_DoesNotRefactor()
        {
            var code = CodeTemplates.BaseCoreUsings + @"
class Test
{
    public int Property { get; set; }
}
class TestDto
{
    public int UnrelatedProperty { get; set; }
}
Enumerable.Empty<Test>().Select(x => new TestDto { ↓ });
";
            RoslynAssert.NoRefactoring(Refactoring, code);
        }

        [Test]
        public void Refactoring_WithParenthesizedLambda_MapsAllProperties()
        {
            var code = TestCode(
@"Enumerable.Empty<SimpleEntity>().Select((x) => new SimpleEntityDto
{↓
});");
            var fixedCode = TestCode(
@"Enumerable.Empty<SimpleEntity>().Select((x) => new SimpleEntityDto
{
    Id = x.Id,
    Name = x.Name
});");
            RoslynAssert.Refactoring(Refactoring, code, fixedCode);
        }
    }
}