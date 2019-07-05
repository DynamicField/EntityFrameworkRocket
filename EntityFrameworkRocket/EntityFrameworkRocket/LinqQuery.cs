using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace EntityFrameworkRocket
{
    internal class LinqQuery
    {
        public LinkedList<Step> Steps { get; } = new LinkedList<Step>();
        public ExpressionSyntax Expression { get; set; }
        public ExpressionSyntax SourceCollection { get; set; }
        public bool? IsTracked
        {
            get
            {
                var step = Steps.LastOrDefault(s => s.Name == "AsNoTracking" || s.Name == "AsTracking");
                if (step is null) return null;
                return step.Name == "AsTracking";
            }
        }
        public class Step
        {
            public IMethodSymbol Symbol { get; set; }
            public InvocationExpressionSyntax Invocation { get; set; }
            public string Name => Symbol.Name;
            private string Parameters => string.Join(",",
                Invocation.ArgumentList.Arguments.Select(x => x.Expression is LambdaExpressionSyntax ? "lambda" : x.ToString()));
            public override string ToString()
            {
                return $"{Name}({Parameters})";
            }
        }
    }
}
