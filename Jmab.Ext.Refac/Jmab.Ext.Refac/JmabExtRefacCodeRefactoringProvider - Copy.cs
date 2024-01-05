//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CodeActions;
//using Microsoft.CodeAnalysis.CodeRefactorings;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using System.Composition;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Jmab.Ext.Refac
//{
//    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(JmabExtRefacCodeRefactoringProvider)), Shared]
//    internal class JmabExtRefacCodeRefactoringProvider : CodeRefactoringProvider
//    {
//        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
//        {
//            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

//            var node = root.FindNode(context.Span);

//            var methodNode = node as MethodDeclarationSyntax;
//            if (methodNode == null)
//            {
//                return;
//            }

//            var action = CodeAction.Create("Jmab - Add await", c => AddAwaitable(root, context.Document, methodNode, c));

//            context.RegisterRefactoring(action);
//        }

//        private async Task<Solution> AddAwaitable(SyntaxNode root, Document document, MethodDeclarationSyntax methodNode, CancellationToken cancellationToken)
//        {
//            var updatedMethodNode = AddAsyncAndReturnTypeTask(methodNode);
//            updatedMethodNode = await AddAwaitToReferences(document, updatedMethodNode, cancellationToken);

//            var originalMethodNode = root.DescendantNodes()
//                                         .OfType<MethodDeclarationSyntax>()
//                                         .First(n => n.IsEquivalentTo(methodNode, topLevel: false));

//            var newRoot = root.ReplaceNode(originalMethodNode, updatedMethodNode);

//            var newDocument = document.WithSyntaxRoot(newRoot);
//            var newSolution = newDocument.Project.Solution;

//            return newSolution;
//        }

//        private async Task<MethodDeclarationSyntax> AddAwaitToReferences(Document document, MethodDeclarationSyntax methodNode, CancellationToken cancellationToken)
//        {
//            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
//            var invocations = methodNode.DescendantNodes().OfType<InvocationExpressionSyntax>();

//            foreach (var invocation in invocations)
//            {
//                var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

//                // Check if the method returns Task or Task<T>
//                if (methodSymbol?.ReturnType.OriginalDefinition.ToString().StartsWith("System.Threading.Tasks.Task") == true)
//                {
//                    // Create a new AwaitExpressionSyntax node
//                    var newInvocation = SyntaxFactory.AwaitExpression(invocation.WithoutTrivia())
//                                                     .WithLeadingTrivia(invocation.GetLeadingTrivia())
//                                                     .WithTrailingTrivia(invocation.GetTrailingTrivia());

//                    // Replace the old invocation with the new one
//                    methodNode = methodNode.ReplaceNode(invocation, newInvocation);
//                }
//            }

//            return methodNode;
//        }

//        private static MethodDeclarationSyntax AddAsyncAndReturnTypeTask(MethodDeclarationSyntax updatedMethodNode)
//        {
//            var asyncModifier = SyntaxFactory.Token(SyntaxKind.AsyncKeyword);
//            updatedMethodNode = updatedMethodNode.AddModifiers(asyncModifier);

//            if (updatedMethodNode.ReturnType is PredefinedTypeSyntax returnTypeSyntax && returnTypeSyntax.Keyword.IsKind(SyntaxKind.VoidKeyword))
//            {
//                updatedMethodNode = updatedMethodNode.WithReturnType(SyntaxFactory.ParseTypeName("System.Threading.Tasks.Task"));
//            }
//            else if (!(updatedMethodNode.ReturnType.ToString().StartsWith("System.Threading.Tasks.Task")))
//            {
//                var newReturnType = SyntaxFactory.ParseTypeName($"System.Threading.Tasks.Task<{updatedMethodNode.ReturnType}>");
//                updatedMethodNode = updatedMethodNode.WithReturnType(newReturnType);
//            }

//            return updatedMethodNode;
//        }
//    }
//}
