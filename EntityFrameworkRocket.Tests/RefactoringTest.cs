using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace EntityFrameworkRocket.Tests
{
    public abstract class RefactoringTest<TRefactoring> where TRefactoring : CodeRefactoringProvider, new()
    {
        protected static readonly TRefactoring Refactoring = new TRefactoring();
    }
}