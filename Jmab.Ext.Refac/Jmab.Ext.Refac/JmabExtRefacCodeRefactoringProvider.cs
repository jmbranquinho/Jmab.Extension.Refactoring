using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jmab.Ext.Refac
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(JmabExtRefacCodeRefactoringProvider)), Shared]
    internal class JmabExtRefacCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var node = root.FindNode(context.Span);

            var methodNode = node as MethodDeclarationSyntax;
            if (methodNode == null)
            {
                return;
            }

            var action = CodeAction.Create("Jmab - Add await", c => AddAwaitable(root, context.Document, methodNode, c));

            context.RegisterRefactoring(action);
        }

        private async Task<Solution> AddAwaitable(SyntaxNode root, Document document, MethodDeclarationSyntax methodNode, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Create a modified version of the method node
            //var copyMethodNode = methodNode;
            var methodNode1 = AddAsyncAndReturnTypeTask(methodNode);
            //var methodNode2 = AddAwaitToAsyncCalls(semanticModel, methodNode1);
            //var methodNode2 = AwaitReferences(methodNode, root, semanticModel, cancellationToken);

            // Replace the original method node in the root syntax tree
            var newRoot = ReplaceOriginalNode(root, methodNode, methodNode1);

            var newDocument = document.WithSyntaxRoot(newRoot);
            var newSolution = newDocument.Project.Solution;

            return newSolution;
        }

        private static SyntaxNode ReplaceOriginalNode(SyntaxNode root, MethodDeclarationSyntax methodNode, MethodDeclarationSyntax modifiedMethodNode)
        {
            var originalMethodNode = root.DescendantNodes()
                                         .OfType<MethodDeclarationSyntax>()
                                         .First(n => n.IsEquivalentTo(methodNode, topLevel: false));
            var newRoot = root.ReplaceNode(originalMethodNode, modifiedMethodNode);
            return newRoot;
        }

        private static MethodDeclarationSyntax AddAsyncAndReturnTypeTask(MethodDeclarationSyntax methodNode)
        {
            // Add 'async' modifier
            var asyncModifier = SyntaxFactory.Token(SyntaxKind.AsyncKeyword);
            var modifiedMethodNode = methodNode.AddModifiers(asyncModifier);

            // Change return type if it's 'void' or a non-Task type
            if (modifiedMethodNode.ReturnType is PredefinedTypeSyntax returnTypeSyntax && returnTypeSyntax.Keyword.IsKind(SyntaxKind.VoidKeyword))
            {
                // If current return type is 'void', change to 'Task'
                modifiedMethodNode = modifiedMethodNode.WithReturnType(SyntaxFactory.ParseTypeName("Task"));
            }
            else if (!(modifiedMethodNode.ReturnType.ToString().StartsWith("Task")))
            {
                // If return type is not a Task type, wrap the existing return type with 'Task<>'
                var newReturnType = SyntaxFactory.ParseTypeName($"Task<{modifiedMethodNode.ReturnType}>");
                modifiedMethodNode = modifiedMethodNode.WithReturnType(newReturnType);
            }

            return modifiedMethodNode;
        }

        public static MethodDeclarationSyntax AddAwaitToAsyncCalls(SemanticModel semanticModel, MethodDeclarationSyntax methodDeclaration)
        {
            // Clone the method to work on
            var newMethod = methodDeclaration;

            foreach (var statement in newMethod.Body.Statements)
            {
                // Check if the statement is an expression statement
                if (statement is ExpressionStatementSyntax expressionStatement)
                {
                    var expression = expressionStatement.Expression;

                    // Get the type info of the expression
                    var typeInfo = semanticModel.GetTypeInfo(expression);

                    // Check if the expression type is Task or Task<T>
                    if (typeInfo.Type is INamedTypeSymbol namedTypeSymbol &&
                        (namedTypeSymbol.Name == "Task" || (namedTypeSymbol.Name == "Task" && namedTypeSymbol.IsGenericType)))
                    {
                        // Create a new await expression
                        var awaitExpression = SyntaxFactory.AwaitExpression(expression)
                                                           .WithLeadingTrivia(expression.GetLeadingTrivia())
                                                           .WithTrailingTrivia(expression.GetTrailingTrivia());

                        // Replace the original expression with the new await expression
                        newMethod = newMethod.ReplaceNode(expression, awaitExpression);
                    }
                }
            }

            return newMethod;
            // Replace the original method with the new method in the syntax tree
            //var newRoot = root.ReplaceNode(methodDeclaration, newMethod);
            //var newDocument = document.WithSyntaxRoot(newRoot);
            //return newDocument;
        }

        private static MethodDeclarationSyntax AwaitReferences(MethodDeclarationSyntax methodNode, SyntaxNode root, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var modifiedMethodNode = methodNode;

            // Iterate through all return statements in the method
            var returnStatements = modifiedMethodNode.DescendantNodes().OfType<ReturnStatementSyntax>();
            foreach (var returnStatement in returnStatements)
            {
                var expression = returnStatement.Expression;

                // Determine if the return type is a Task that is not already awaited
                if (IsReturnTypeTask(semanticModel, returnStatement, cancellationToken) && !IsExpressionAwaited(expression))
                {
                    // Modify the return statement to include 'await'
                    var awaitedExpression = SyntaxFactory.AwaitExpression(expression.WithoutTrivia()).WithTriviaFrom(expression);
                    var newReturnStatement = returnStatement.WithExpression(awaitedExpression);

                    // Replace the old return statement with the new one in the method body
                    modifiedMethodNode = modifiedMethodNode.ReplaceNode(returnStatement, newReturnStatement);
                }
            }

            return modifiedMethodNode;
        }

        //private static Dictionary<SyntaxNode, SyntaxNode> AwaitReferences(MethodDeclarationSyntax methodNode, SyntaxNode root, SemanticModel semanticModel, CancellationToken cancellationToken)
        //{
        //    var returnStatements = methodNode.DescendantNodes().OfType<ReturnStatementSyntax>();

        //    var replacements = new Dictionary<SyntaxNode, SyntaxNode>();

        //    foreach (var returnStatement in returnStatements)
        //    {
        //        var isTask = IsReturnTypeTask(semanticModel, returnStatement, cancellationToken);

        //        if (!isTask) continue;

        //        var isAwaited = IsExpressionAwaited(returnStatement.Expression);

        //        if (isAwaited) continue;

        //        replacements = MergeDict(replacements, AwaitMethod(methodNode, semanticModel));
        //    }

        //    return replacements;
        //}

        private static Dictionary<SyntaxNode, SyntaxNode> AwaitMethod(MethodDeclarationSyntax methodNode, SemanticModel semanticModel)
        {
            var replacements = new Dictionary<SyntaxNode, SyntaxNode>();
            var methodCalls = methodNode.DescendantNodes().OfType<InvocationExpressionSyntax>();

            foreach (var methodCall in methodCalls)
            {
                var typeInfo = semanticModel.GetTypeInfo(methodCall, CancellationToken.None);
                var returnType = typeInfo.Type;

                if (returnType != null && returnType.OriginalDefinition.ToString() == "System.Threading.Tasks.Task<TResult>"
                    && !IsExpressionAwaited(methodCall))
                {
                    var awaitedExpression = SyntaxFactory.AwaitExpression(methodCall.WithoutTrivia())
                                                             .WithTriviaFrom(methodCall);
                    replacements.Add(methodCall, awaitedExpression);
                }
            }

            return replacements;
        }

        private static bool IsExpressionAwaited(ExpressionSyntax expression)
        {
            var parent = expression.Parent;
            while (parent != null)
            {
                if (parent is AwaitExpressionSyntax)
                    return true;

                parent = parent.Parent;
            }
            return false;
        }

        private static bool IsReturnTypeTask(SemanticModel semanticModel, ReturnStatementSyntax returnStatement, CancellationToken cancellationToken)
        {
            var returnExpression = returnStatement.Expression;
            if (returnExpression != null)
            {
                var typeInfo = semanticModel.GetTypeInfo(returnExpression, cancellationToken);
                var returnType = typeInfo.Type;

                return returnType != null && returnType.OriginalDefinition.ToString() == "System.Threading.Tasks.Task<TResult>";
            }

            return false;
        }

        private static Dictionary<SyntaxNode, SyntaxNode> MergeDict(Dictionary<SyntaxNode, SyntaxNode> dict1, Dictionary<SyntaxNode, SyntaxNode> dict2)
        {
            foreach (var item in dict2)
            {
                if (!dict1.ContainsKey(item.Key))
                {
                    dict1[item.Key] = item.Value;
                }
            }

            return dict1;
        }
    }
}
