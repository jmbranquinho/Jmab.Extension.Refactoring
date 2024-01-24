using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jmab.Ext.Refac.Refactoring.MakeMethodAsync
{
    public static class MakeMethodAsyncAndAwaitReferences
    {
        public const string Command = "Make async";

        private const string _task = "Task";

        public static async Task<Solution> ApplyRefactoring(CodeRefactoringContext context, CancellationToken cancellationToken)
        {
            var (root, document, semanticModel, node)
                = await RefactoringHelper.GetRootDocumentAndNode(context, cancellationToken);

            if (!(node is MethodDeclarationSyntax methodNode))
            {
                return document.Project.Solution;
            }

            var newMethod = AddAwaitToAsyncCalls(semanticModel, methodNode);
            newMethod = AddAsyncAndReturnTypeTask(newMethod);

            return RefactoringHelper.ReplaceOriginalNode(document, root, methodNode, newMethod);
        }

        private static MethodDeclarationSyntax AddAsyncAndReturnTypeTask(MethodDeclarationSyntax methodNode)
        {
            var modifiedMethodNode = AddAsyncModifier(methodNode);
            modifiedMethodNode = AddTaskReturnType(modifiedMethodNode);

            return modifiedMethodNode;
        }

        private static MethodDeclarationSyntax AddTaskReturnType(MethodDeclarationSyntax modifiedMethodNode)
        {
            var isVoid = modifiedMethodNode.ReturnType is PredefinedTypeSyntax returnTypeSyntax
                            && returnTypeSyntax.Keyword.IsKind(SyntaxKind.VoidKeyword);

            Func<bool> isNonAwaitedTask = () => !modifiedMethodNode.ReturnType.ToString().StartsWith(_task);

            if (isVoid)
            {
                modifiedMethodNode = modifiedMethodNode.WithReturnType(SyntaxFactory.ParseTypeName(_task));
            }
            else if (isNonAwaitedTask())
            {
                var newReturnType = SyntaxFactory.ParseTypeName($"{_task}<{modifiedMethodNode.ReturnType}>");
                modifiedMethodNode = modifiedMethodNode.WithReturnType(newReturnType);
            }

            return modifiedMethodNode;
        }

        private static MethodDeclarationSyntax AddAsyncModifier(MethodDeclarationSyntax methodNode)
        {
            var asyncModifier = SyntaxFactory.Token(SyntaxKind.AsyncKeyword);
            var modifiedMethodNode = methodNode.AddModifiers(asyncModifier);
            return modifiedMethodNode;
        }

        public static MethodDeclarationSyntax AddAwaitToAsyncCalls(
            SemanticModel semanticModel,
            MethodDeclarationSyntax methodDeclaration)
        {
            var newMethod = methodDeclaration;

            foreach (var statement in newMethod.Body.Statements)
            {
                var expression = GetExpressionFromStatement(statement);
                if (expression == null) continue;

                var typeInfo = semanticModel.GetTypeInfo(expression);
                if (!HasReturnTypeTask(typeInfo)) continue;

                var awaitExpression = AddAwaitToReference(expression);
                newMethod = newMethod.ReplaceNode(expression, awaitExpression);
            }

            return newMethod;
        }

        private static AwaitExpressionSyntax AddAwaitToReference(ExpressionSyntax expression)
        {
            return SyntaxFactory.AwaitExpression(expression)
                    .WithLeadingTrivia(expression.GetLeadingTrivia())
                    .WithTrailingTrivia(expression.GetTrailingTrivia());
        }

        private static bool HasReturnTypeTask(TypeInfo typeInfo)
        {
            return typeInfo.Type is INamedTypeSymbol namedTypeSymbol
                && (namedTypeSymbol.Name == _task
                    || (namedTypeSymbol.Name == _task
                        && namedTypeSymbol.IsGenericType));
        }

        private static ExpressionSyntax GetExpressionFromStatement(SyntaxNode node)
        {
            switch (node)
            {
                case ExpressionStatementSyntax expressionStatement:
                    return expressionStatement.Expression;
                case ReturnStatementSyntax returnStatement:
                    return returnStatement.Expression;
                case LocalDeclarationStatementSyntax localDeclaration:
                    return localDeclaration.Declaration.Variables.FirstOrDefault()?.Initializer?.Value;
                case AssignmentExpressionSyntax assignmentExpression:
                    return assignmentExpression;
                case InvocationExpressionSyntax invocationExpression:
                    return invocationExpression;
                case BinaryExpressionSyntax binaryExpression:
                    return binaryExpression;
                case ConditionalExpressionSyntax conditionalExpression:
                    return conditionalExpression;
                case LambdaExpressionSyntax lambdaExpression:
                    return lambdaExpression;
                case ParenthesizedExpressionSyntax parenthesizedExpression:
                    return parenthesizedExpression;
                case CastExpressionSyntax castExpression:
                    return castExpression;
                case ObjectCreationExpressionSyntax objectCreationExpression:
                    return objectCreationExpression;
                case InitializerExpressionSyntax initializerExpression:
                    return initializerExpression;
                case AwaitExpressionSyntax awaitExpression:
                    return awaitExpression;
                case ElementAccessExpressionSyntax elementAccessExpression:
                    return elementAccessExpression;
                case ArgumentSyntax argument:
                    return argument.Expression;
                case SwitchExpressionSyntax switchExpression:
                    return switchExpression;
                case QueryExpressionSyntax queryExpression:
                    return queryExpression;
                default:
                    return null;
            }
        }
    }
}
