using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EntityFrameworkRocket.Walkers
{
    internal class LinqQuerySyntaxWalker : CSharpSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;

        public LinqQuerySyntaxWalker(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
        }
        private LinqQuery _query = new LinqQuery();
        public LinqQuery VisitQuery(ExpressionSyntax query, ExpressionSyntax source)
        {
            _query = new LinqQuery { Expression = query, SourceCollection = source };
            _query = new LinqQuery { Expression = query, SourceCollection = source };
            Visit(query);
            return _query;
        }
        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (_semanticModel.GetSymbolInfo(node).Symbol is IMethodSymbol symbol)
            {
                _query.Steps.AddFirst(new LinqQuery.Step(symbol, node));
            }
            base.Visit(node.Expression);
        }
    }
}