using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Xml.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Jmab.Ext.Refac.Refactoring.CreateUnitTests
{
    public static class CreateUnitTestsRefactor
    {
        public const string Command = "Create unit tests";

        public static async Task<Solution> ApplyRefactoring(CodeRefactoringContext context, CancellationToken cancellationToken)
        {
            throw new Exception();
        }

        public static bool IsControlFlowNode(SyntaxNode node)
        {
            return node is IfStatementSyntax 
                   || node is ConditionalExpressionSyntax 
                   || node is SwitchStatementSyntax 
                   || node is TryStatementSyntax 
                   || node is BinaryExpressionSyntax binaryExpr 
                        && (binaryExpr.IsKind(SyntaxKind.LogicalAndExpression) 
                            || binaryExpr.IsKind(SyntaxKind.LogicalOrExpression)) 
                   || node is ConditionalAccessExpressionSyntax;
        }

        public static Tuple<IEnumerable<SyntaxNode>, IEnumerable<SyntaxNode>> GetControlFlowPaths(SyntaxNode node)
        {
            switch (node)
            {
                case IfStatementSyntax ifStatementSyntax:
                    var truePath = ifStatementSyntax.Statement.DescendantNodes();
                    var falsePath = ifStatementSyntax.Else?.Statement.DescendantNodes() ?? Enumerable.Empty<SyntaxNode>();
                    return new Tuple<IEnumerable<SyntaxNode>, IEnumerable<SyntaxNode>>(truePath, falsePath);

                case ConditionalExpressionSyntax conditionalExpressionSyntax:
                    var whenTruePath = conditionalExpressionSyntax.WhenTrue.DescendantNodes();
                    var whenFalsePath = conditionalExpressionSyntax.WhenFalse.DescendantNodes();
                    return new Tuple<IEnumerable<SyntaxNode>, IEnumerable<SyntaxNode>>(whenTruePath, whenFalsePath);

                case SwitchStatementSyntax switchStatementSyntax:
                    var firstCasePath = switchStatementSyntax.Sections.FirstOrDefault()?.DescendantNodes() ?? Enumerable.Empty<SyntaxNode>();
                    var defaultCasePath = switchStatementSyntax.Sections.FirstOrDefault(section => section.Labels.OfType<DefaultSwitchLabelSyntax>().Any())?.DescendantNodes() ?? Enumerable.Empty<SyntaxNode>();
                    return new Tuple<IEnumerable<SyntaxNode>, IEnumerable<SyntaxNode>>(firstCasePath, defaultCasePath);

                case TryStatementSyntax tryStatementSyntax:
                    var tryBlockPath = tryStatementSyntax.Block.DescendantNodes();
                    var catchBlockPath = tryStatementSyntax.Catches.FirstOrDefault()?.Block.DescendantNodes() ?? Enumerable.Empty<SyntaxNode>();
                    return new Tuple<IEnumerable<SyntaxNode>, IEnumerable<SyntaxNode>>(tryBlockPath, catchBlockPath);

                // TODO: Implement BinaryExpressionSyntax with && or || operators
                // TODO: Implement ConditionalAccessExpressionSyntax for ?. and ?[]
                // TODO: Implement CoalesceExpressionSyntax for ??

                default:
                    return new Tuple<IEnumerable<SyntaxNode>, IEnumerable<SyntaxNode>>(null, null);
            }
        }
    }
}
