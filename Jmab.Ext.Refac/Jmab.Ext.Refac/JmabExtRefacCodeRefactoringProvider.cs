using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Jmab.Ext.Refac.Refactoring.CommandHandlerRefac;
using Jmab.Ext.Refac.Refactoring.JorgifyClass;
using Jmab.Ext.Refac.Refactoring.MakeMethodAsync;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Jmab.Ext.Refac
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(JmabExtRefacCodeRefactoringProvider)), Shared]
    internal class JmabExtRefacCodeRefactoringProvider : CodeRefactoringProvider
    {
        private const string Prefix = "Jmab - ";

        public sealed override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            AddAction(context, MakeMethodAsyncAndAwaitReferences.Command, MakeMethodAsyncAndAwaitReferences.ApplyRefactoring);
            AddAction(context, JorgifyClassRefactor.Command, JorgifyClassRefactor.ApplyRefactoring);
            AddAction(context, AddCommandHandlerLogic.Command, AddCommandHandlerLogic.ApplyRefactoring);
            //AddAction(context, CreateUnitTestsRefactor.Command, CreateUnitTestsRefactor.ApplyRefactoring);

            return Task.CompletedTask;
        }

        private void AddAction(
            CodeRefactoringContext context,
            string actionName,
            Func<CodeRefactoringContext, CancellationToken, Task<Solution>> func)
        {
            var action = CodeAction.Create($"{Prefix}{actionName}", cancellationToken => func(context, cancellationToken));
            context.RegisterRefactoring(action);
        }
    }
}
