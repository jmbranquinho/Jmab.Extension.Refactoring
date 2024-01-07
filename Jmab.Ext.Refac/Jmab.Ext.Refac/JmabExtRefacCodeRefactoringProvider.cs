using Jmab.Ext.Refac.Refactoring.MakeMethodAsync;
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
        private const string Prefix = "Jmab - ";

        public sealed override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var action = CodeAction.Create($"{Prefix}{MakeMethodAsyncAndAwaitReferences.Command}",
                cancellationToken => MakeMethodAsyncAndAwaitReferences.ApplyRefactoring(context, cancellationToken));
            context.RegisterRefactoring(action);

            return Task.CompletedTask;
        }
    }
}
