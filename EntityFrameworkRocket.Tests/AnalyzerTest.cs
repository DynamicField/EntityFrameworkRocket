using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EntityFrameworkRocket.Tests
{
    public abstract class AnalyzerTest<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new()
    {
        protected static readonly TAnalyzer Analyzer = new TAnalyzer();
    }
    public abstract class CodeFixTest<TCodeFix, TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new() where TCodeFix : CodeFixProvider, new()
    {
        protected static readonly TAnalyzer Analyzer = new TAnalyzer();
        protected static readonly TCodeFix  Fix = new TCodeFix();
    }
}