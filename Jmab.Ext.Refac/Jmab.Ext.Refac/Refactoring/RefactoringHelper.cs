using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jmab.Ext.Refac.Refactoring
{
    public static class RefactoringHelper
    {
        public static async Task<(
            SyntaxNode root,
            Document document, 
            SemanticModel SemanticModel, 
            SyntaxNode node)> GetRootDocumentAndNode(
            CodeRefactoringContext context, 
            CancellationToken cancellationToken)
        {
            var root = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var document = context.Document;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);

            return (root, document, semanticModel, node);
        }

        public static Solution ReplaceOriginalNode<T>(
            Document document,
            SyntaxNode root,
            T methodNode,
            T modifiedMethodNode) 
            where T : SyntaxNode
        {
            var originalMethodNode = root.DescendantNodes()
                                         .OfType<T>()
                                         .First(n => n.IsEquivalentTo(methodNode, topLevel: false));
            var newRoot = root.ReplaceNode(originalMethodNode, modifiedMethodNode);
            var newDocument = document.WithSyntaxRoot(newRoot);
            var newSolution = newDocument.Project.Solution;

            return newSolution;
        }
    }
}
