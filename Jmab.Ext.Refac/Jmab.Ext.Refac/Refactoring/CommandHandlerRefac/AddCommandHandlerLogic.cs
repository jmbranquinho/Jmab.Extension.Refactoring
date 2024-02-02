using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Jmab.Ext.Refac.Refactoring.CommandHandlerRefac
{
    public static class AddCommandHandlerLogic
    {
        public const string Command = "Create boilerplate handler";

        public static async Task<Solution> ApplyRefactoring(CodeRefactoringContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            var solution = document.Project.Solution;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var selectedSpan = context.Span;
            var classNode = root.FindToken(selectedSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();

            if (classNode == null)
            {
                return solution;
            }
            var (classname, @namespace, requestType) = GetClassInfo(classNode);
            var feature = classname.Replace("Command", "");
            var fileContent = GenerateHandlerClassCode(feature, requestType);
            var newFilePath = GetNewFilePath(document.FilePath, feature);

            // Write the new file to the disk
            File.WriteAllText(newFilePath, fileContent);

            // Add the new file to the Roslyn solution
            var newDocument = solution.Projects
                                      .First(p => p.Id == document.Project.Id)
                                      .AddDocument(classname + "CommandHandler.cs", fileContent);

            return newDocument.Project.Solution;
        }

        public static (string ClassName, string Namespace, string RequestType) GetClassInfo(SyntaxNode selectedClass)
        {
            var classDeclaration = selectedClass as ClassDeclarationSyntax;
            if (classDeclaration == null)
            {
                throw new InvalidOperationException("Selected item is not a class.");
            }

            var namespaceDeclaration = classDeclaration.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();

            // Find the IRequest<X> interface in the base list
            var requestTypeInterface = classDeclaration.BaseList?.Types
                .Select(b => b.Type)
                .OfType<GenericNameSyntax>()
                .FirstOrDefault(t => t.Identifier.ValueText == "IRequest");

            string requestType = null;
            if (requestTypeInterface != null && requestTypeInterface.TypeArgumentList.Arguments.Count > 0)
            {
                requestType = requestTypeInterface.TypeArgumentList.Arguments.First().ToString();
            }

            return (classDeclaration.Identifier.ValueText, namespaceDeclaration?.Name.ToString(), requestType);
        }

        public static string GetNewFilePath(string originalFilePath, string className)
        {
            var directory = Path.GetDirectoryName(originalFilePath);
            var newFileName = className + "CommandHandler.cs";
            return Path.Combine(directory, newFileName);
        }

        public static string GenerateHandlerClassCode(string featureName, string responseType)
        {
            var feature = ExtractSecondWord(featureName);
            var lowercaseFeature = FirstCharToLowerCase(feature);

            return $@"
public class {featureName}CommandHandler : IRequestHandler<{featureName}Command, {responseType}>
{{
    private readonly I{feature}Repository _{lowercaseFeature}Repository;

    public {featureName}CommandHandler(I{featureName}Repository {feature}Repository)
    {{
        _{lowercaseFeature}Repository = {lowercaseFeature}Repository;
    }}

    public async Task<{responseType}> Handle({featureName}Command command, CancellationToken cancellationToken = default)
    {{
        // Your implementation here
        await _{lowercaseFeature}Repository.X();
    }}
}}";
        }

        private static string ExtractSecondWord(string input)
        {
            var match = Regex.Match(input, @"[A-Z][a-z]+|[A-Z]+(?![a-z])");
            match = match.NextMatch(); // Move to second match
            return match.Value;
        }

        public static string FirstCharToLowerCase(string str)
        {
            if (!string.IsNullOrEmpty(str) && char.IsUpper(str[0]))
                return str.Length == 1 ? char.ToLower(str[0]).ToString() : char.ToLower(str[0]) + str.Substring(1);

            return str;
        }
    }
}
