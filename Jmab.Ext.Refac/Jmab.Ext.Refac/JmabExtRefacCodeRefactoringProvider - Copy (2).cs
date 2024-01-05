//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CodeActions;
//using Microsoft.CodeAnalysis.CodeRefactorings;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using System.Collections.Generic;
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
//            var replacements = new Dictionary<SyntaxNode, SyntaxNode>();

//            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

//            var originalMethodNode = root.DescendantNodes()
//                 .OfType<MethodDeclarationSyntax>()
//                 .First(n => n.IsEquivalentTo(methodNode, topLevel: false));

//            replacements = MergeDict(replacements, AddAsyncAndReturnTypeTask(root, methodNode));
//            replacements = MergeDict(replacements, AwaitReferences(methodNode, root, semanticModel, cancellationToken));

//            var newRoot = root.ReplaceNodes(replacements.Keys, (original, rewritten) => replacements[original]);

//            var newDocument = document.WithSyntaxRoot(newRoot);
//            var newSolution = newDocument.Project.Solution;

//            return newSolution;
//        }

//        private static Dictionary<SyntaxNode, SyntaxNode> AddAsyncAndReturnTypeTask(SyntaxNode originalRoot, MethodDeclarationSyntax methodNode)
//        {
//            var replacements = new Dictionary<SyntaxNode, SyntaxNode>();

//            var originalMethodNode = originalRoot.DescendantNodes()
//                                 .OfType<MethodDeclarationSyntax>()
//                                 .First(n => n.IsEquivalentTo(methodNode, topLevel: false));

//            var asyncModifier = SyntaxFactory.Token(SyntaxKind.AsyncKeyword);
//            methodNode = methodNode.AddModifiers(asyncModifier);

//            if (methodNode.ReturnType is PredefinedTypeSyntax returnTypeSyntax && returnTypeSyntax.Keyword.IsKind(SyntaxKind.VoidKeyword))
//            {
//                methodNode = methodNode.WithReturnType(SyntaxFactory.ParseTypeName("System.Threading.Tasks.Task"));
//            }
//            else if (!(methodNode.ReturnType.ToString().StartsWith("System.Threading.Tasks.Task")))
//            {
//                var newReturnType = SyntaxFactory.ParseTypeName($"System.Threading.Tasks.Task<{methodNode.ReturnType}>");
//                methodNode = methodNode.WithReturnType(newReturnType);
//            }

//            replacements.Add(originalMethodNode, methodNode);
//            return replacements;
//        }

//        private static Dictionary<SyntaxNode, SyntaxNode> AwaitReferences(MethodDeclarationSyntax methodNode, SyntaxNode root, SemanticModel semanticModel, CancellationToken cancellationToken)
//        {
//            var returnStatements = methodNode.DescendantNodes().OfType<ReturnStatementSyntax>();

//            var replacements = new Dictionary<SyntaxNode, SyntaxNode>();

//            foreach (var returnStatement in returnStatements)
//            {
//                var isTask = IsReturnTypeTask(semanticModel, returnStatement, cancellationToken);

//                if (!isTask) continue;

//                var isAwaited = IsExpressionAwaited(returnStatement.Expression);

//                if (isAwaited) continue;

//                replacements = MergeDict(replacements, AwaitMethod(methodNode, semanticModel));
//            }

//            return replacements;
//        }

//        private static Dictionary<SyntaxNode, SyntaxNode> AwaitMethod(MethodDeclarationSyntax methodNode, SemanticModel semanticModel)
//        {
//            var replacements = new Dictionary<SyntaxNode, SyntaxNode>();
//            var methodCalls = methodNode.DescendantNodes().OfType<InvocationExpressionSyntax>();

//            foreach (var methodCall in methodCalls)
//            {
//                var typeInfo = semanticModel.GetTypeInfo(methodCall, CancellationToken.None);
//                var returnType = typeInfo.Type;

//                if (returnType != null && returnType.OriginalDefinition.ToString() == "System.Threading.Tasks.Task<TResult>"
//                    && !IsExpressionAwaited(methodCall))
//                {
//                    var awaitedExpression = SyntaxFactory.AwaitExpression(methodCall.WithoutTrivia())
//                                                             .WithTriviaFrom(methodCall);
//                    replacements.Add(methodCall, awaitedExpression);
//                }
//            }

//            // Apply all replacements at once
//            //var newRoot = root.ReplaceNodes(replacements.Keys, (original, rewritten) => replacements[original]);


//            return replacements;
//        }

//        private static bool IsExpressionAwaited(ExpressionSyntax expression)
//        {
//            var parent = expression.Parent;
//            while (parent != null)
//            {
//                if (parent is AwaitExpressionSyntax)
//                    return true;

//                parent = parent.Parent;
//            }
//            return false;
//        }

//        private static bool IsReturnTypeTask(SemanticModel semanticModel, ReturnStatementSyntax returnStatement, CancellationToken cancellationToken)
//        {
//            var returnExpression = returnStatement.Expression;
//            if (returnExpression != null)
//            {
//                var typeInfo = semanticModel.GetTypeInfo(returnExpression, cancellationToken);
//                var returnType = typeInfo.Type;

//                return returnType != null && returnType.OriginalDefinition.ToString() == "System.Threading.Tasks.Task<TResult>";
//            }

//            return false;
//        }

//        private static Dictionary<SyntaxNode, SyntaxNode> MergeDict(Dictionary<SyntaxNode, SyntaxNode> dict1, Dictionary<SyntaxNode, SyntaxNode> dict2)
//        {
//            foreach (var item in dict2)
//            {
//                if (!dict1.ContainsKey(item.Key))
//                {
//                    dict1[item.Key] = item.Value;
//                }
//            }

//            return dict1;
//        }
//    }
//}
