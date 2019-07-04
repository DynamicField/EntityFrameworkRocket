using Gu.Roslyn.Asserts;
[assembly: TransitiveMetadataReferences(
    typeof(Microsoft.EntityFrameworkCore.DbContext))]
[assembly: MetadataReference(typeof(object), new[] { "global", "corlib", "mscorlib" })]
[assembly: MetadataReference(typeof(System.Diagnostics.Debug), new[] { "global", "System" })]
[assembly: TransitiveMetadataReferences(typeof(Microsoft.CodeAnalysis.CSharp.CSharpCompilation))]
[assembly: MetadataReferences(
    typeof(System.Linq.Enumerable),
    typeof(System.Linq.IQueryable),
    typeof(EntityFrameworkRocket.Tests.EFCoreLib.TestDbContext))]