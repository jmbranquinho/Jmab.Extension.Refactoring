using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;
using System.Threading.Tasks;

namespace Jmab.Ext.Refac.Refactoring.JorgifyClass
{
    public class JorgifyClassRefactor
    {
        public const string Command = "Jorgify class";

        public static async Task<Solution> ApplyRefactoring(CodeRefactoringContext context, CancellationToken cancellationToken)
        {
            var (root, document, semanticModel, node)
                = await RefactoringHelper.GetRootDocumentAndNode(context, cancellationToken);

            var classDeclaration = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDeclaration == null)
            {
                return document.Project.Solution;
            }

            //var newClassDeclaration = FormatDocumentRefactor.FormatDocument(classDeclaration);
            var newRoot = root.ReplaceNode(classDeclaration, JorgifyClassHandler.JorgifyClass(classDeclaration));

            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument.Project.Solution;
        }
    }
}
