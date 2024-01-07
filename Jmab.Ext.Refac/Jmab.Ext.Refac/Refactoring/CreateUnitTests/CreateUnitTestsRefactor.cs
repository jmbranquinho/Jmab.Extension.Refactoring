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
            var (root, document, semanticModel, node)
                = await RefactoringHelper.GetRootDocumentAndNode(context, cancellationToken);

            if (!(node is MethodDeclarationSyntax methodNode))
            {
                return document.Project.Solution;
            }

            var allPaths = new List<List<SyntaxNode>>();
            var currentPath = new List<SyntaxNode>();
            AnalyzeNode(methodNode.Body, currentPath, allPaths);
            //return allPaths;
            ;
            throw new NotImplementedException();
        }

        private static void AnalyzeNode(SyntaxNode node, List<SyntaxNode> currentPath, List<List<SyntaxNode>> allPaths)
        {
            if (node == null) return;

            currentPath.Add(node);

            if (IsControlFlowNode(node))
            {
                var controlFlowPaths = GetControlFlowPaths(node);
                foreach (var path in controlFlowPaths.Item1) // True/First path
                {
                    var newPath = new List<SyntaxNode>(currentPath);
                    AnalyzeNode(path, newPath, allPaths);
                }
                if (controlFlowPaths.Item2 != null && controlFlowPaths.Item2.Any())
                {
                    foreach (var path in controlFlowPaths.Item2) // False/Second path
                    {
                        var newPath = new List<SyntaxNode>(currentPath);
                        AnalyzeNode(path, newPath, allPaths);
                    }
                }
            }
            else
            {
                // Continue traversing
                foreach (var childNode in node.ChildNodes())
                {
                    AnalyzeNode(childNode, currentPath, allPaths);
                }
            }

            // If it's an end node, add the current path to allPaths
            if (IsEndNode(node))
            {
                allPaths.Add(new List<SyntaxNode>(currentPath));
            }

            // Backtrack
            currentPath.RemoveAt(currentPath.Count - 1);
        }

        private static bool IsEndNode(SyntaxNode node)
        {
            // Check if it's a return statement
            if (node is ReturnStatementSyntax)
                return true;

            // Check if it's a throw statement
            if (node is ThrowStatementSyntax)
                return true;

            // Check if it's a break statement (commonly used to exit loops)
            if (node is BreakStatementSyntax)
                return true;

            // Check if it's the last statement in a method
            if (IsLastStatementInMethod(node))
                return true;

            return false;
        }

        private static bool IsLastStatementInMethod(SyntaxNode node)
        {
            // Assuming 'node' is a statement and not a block or method itself
            // Check if the parent is a BlockSyntax and if 'node' is the last child
            if (node.Parent is BlockSyntax block)
            {
                var lastStatement = block.Statements.LastOrDefault();
                return lastStatement == node;
            }

            return false;
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
