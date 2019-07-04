using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkRocket.Tests
{
    internal static class TestCompilation
    {
        public static CompilationResult
            Create(string code, IEnumerable<MetadataReference> references = null, [CallerMemberName] string name = null)
        {
            name = name ?? "Test_" + Guid.NewGuid().ToString().Replace("-", "_");
            references = references ?? Array.Empty<MetadataReference>();
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create(name, new[] { tree },
                MetadataReferences.FromAttributes().Union(references));
            return new CompilationResult
            {
                Compilation = compilation,
                Tree = tree,
                Root = tree.GetRoot(),
                SemanticModel =  compilation.GetSemanticModel(tree)
            };
        }

        public class CompilationResult
        {
            public CSharpCompilation Compilation { get; set; }
            public SyntaxTree Tree { get; set; }
            public SyntaxNode Root { get; set; }
            public SemanticModel SemanticModel { get; set; }

            public void Deconstruct(out CSharpCompilation compilation, out SyntaxTree tree, out SyntaxNode root,
                out SemanticModel semanticModel)
            {
                compilation = Compilation;
                tree = Tree;
                root = Root;
                semanticModel = SemanticModel;
            }
            public void Deconstruct(out CSharpCompilation compilation, out SyntaxNode root,
                out SemanticModel semanticModel)
            {
                compilation = Compilation;
                root = Root;
                semanticModel = SemanticModel;
            }
            public void Deconstruct(out SyntaxNode root, out SemanticModel semanticModel)
            {
                root = Root;
                semanticModel = SemanticModel;
            }
            public void Deconstruct(out SyntaxNode root)
            {
                root = Root;
            }
        }
    }
}
