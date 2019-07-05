using Gu.Roslyn.Asserts;
[assembly: TransitiveMetadataReferences(
    typeof(Microsoft.EntityFrameworkCore.DbContext),
    typeof(EntityFrameworkRocket.Tests.EFCoreLib.TestDbContext))]
[assembly: TransitiveMetadataReferences(typeof(Microsoft.CodeAnalysis.CSharp.CSharpCompilation))]
[assembly: MetadataReferences(
    typeof(System.Linq.Enumerable),
    typeof(System.Linq.IQueryable),
    typeof(System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute))]