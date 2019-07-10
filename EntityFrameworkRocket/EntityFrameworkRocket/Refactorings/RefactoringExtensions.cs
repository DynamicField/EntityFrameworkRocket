using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace EntityFrameworkRocket.Refactorings
{
    public static class RefactoringExtensions
    {
        public static void Register(this CodeAction action, CodeRefactoringContext context) =>
            context.RegisterRefactoring(action);

        public static bool HasAnyEntityFramework(this Project project) =>
            project.HasEntityFrameworkClassic() || project.HasEntityFrameworkCore();

        public static bool HasEntityFrameworkCore(this Project project) =>
            project.MetadataReferences.HasEntityFrameworkCore();

        public static bool HasEntityFrameworkClassic(this Project project) =>
            project.MetadataReferences.HasEntityFrameworkClassic();

    }
}
