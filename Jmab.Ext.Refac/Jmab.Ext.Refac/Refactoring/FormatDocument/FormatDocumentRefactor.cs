using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jmab.Ext.Refac.Refactoring.FormatDocument
{
    public class FormatDocumentRefactor
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

            var sortedFields = classDeclaration.Members
                .OfType<FieldDeclarationSyntax>()
                .OrderBy(m => GetVisibilityOrder(m))
                .ToList();

            var sortedProperties = classDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .OrderBy(m => GetVisibilityOrder(m))
                .ToList();

            var sortedConstructors = classDeclaration.Members
                .OfType<ConstructorDeclarationSyntax>()
                .OrderBy(m => GetVisibilityOrder(m))
                .ToList();

            var sortedMethods = classDeclaration.Members
                .OfType<MethodDeclarationSyntax>()
                .OrderBy(m => GetVisibilityOrder(m))
                .ToList();

            // Concatenate the sorted members with required spacing
            var sortedMembers = ConcatenateMembersWithSpacing(sortedFields, sortedProperties, sortedConstructors, sortedMethods);

            var newClassDeclaration = classDeclaration.WithMembers(SyntaxFactory.List(sortedMembers));

            var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);

            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument.Project.Solution;
        }

        private static IEnumerable<MemberDeclarationSyntax> ConcatenateMembersWithSpacing(
           List<FieldDeclarationSyntax> fields,
           List<PropertyDeclarationSyntax> properties,
           List<ConstructorDeclarationSyntax> constructors,
           List<MethodDeclarationSyntax> methods)
        {
            var allMembers = new List<MemberDeclarationSyntax>();
            allMembers.AddRange(AdjustMembersSpacing(fields));
            allMembers.AddRange(AdjustMembersSpacing(properties));
            allMembers.AddRange(AdjustMembersSpacing(constructors));
            allMembers.AddRange(AdjustMembersSpacing(methods));

            return allMembers;
        }

        private static IEnumerable<MemberDeclarationSyntax> AdjustMembersSpacing<T>(List<T> members) where T : MemberDeclarationSyntax
        {
            //if (!members.Any()) return Enumerable.Empty<MemberDeclarationSyntax>();

            //var parentIndent = GetParentIndentation(members.First());
            //var indentation = IncrementIndentation(parentIndent);


            //var adjustedMembers = new List<MemberDeclarationSyntax>
            //{
            //    AddCorrectSpacing(members.First(), string.Empty, isFirstMember: true),
            //};

            //var x = true;
            //foreach (var member in members.Skip(1))
            //{
            //    adjustedMembers.Add(AddCorrectSpacing(member, indentation, x));
            //    x = false;
            //}

            //return adjustedMembers;
            return members;
        }

        private static string IncrementIndentation(string value)
        {
            return value + "    ";//TODO
        }

        private static T AddCorrectSpacing<T>(T member, string indentation, bool isFirstMember = false) where T : MemberDeclarationSyntax
        {
            var leadingTrivia = member.GetLeadingTrivia();

            // Filter out only XML documentation and end-of-line trivia (preserve documentation comments)
            var filteredTrivia = leadingTrivia
                                              //.Where(trivia => trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                                              //                                              || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)
                                              //                                              || trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                                              .ToSyntaxTriviaList();


            // Clear existing whitespace trivia (indentation)
            //filteredTrivia = filteredTrivia.Where(trivia => !trivia.IsKind(SyntaxKind.WhitespaceTrivia)).ToSyntaxTriviaList();

            if (!isFirstMember)
            {
                // Add a new line before adding indentation, but only if the last trivia is not a new line
                if (filteredTrivia.LastOrDefault().Kind() != SyntaxKind.EndOfLineTrivia)
                {
                    filteredTrivia = filteredTrivia.Add(SyntaxFactory.CarriageReturnLineFeed);
                }
            }

            // Add the specified indentation for all members
            var indentationTrivia = SyntaxFactory.Whitespace(indentation);
            filteredTrivia = filteredTrivia.Add(indentationTrivia);

            return member.WithLeadingTrivia(filteredTrivia);
        }

        private static string GetParentIndentation(SyntaxNode node)
        {
            var parent = node.Parent;
            if (parent == null)
            {
                return string.Empty;
            }

            var leadingTrivia = parent.GetLeadingTrivia();
            var indentationTrivia = leadingTrivia.FirstOrDefault(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia));
            return indentationTrivia.ToFullString();
        }

        //private static string GetLeadingIndentation(SyntaxNode member)
        //{
        //    var leadingTrivia = member.GetLeadingTrivia();
        //    var indentationTrivia = leadingTrivia.FirstOrDefault(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia));
        //    return indentationTrivia.ToFullString();
        //}

        private static T AddLeadingNewLine<T>(T member) where T : MemberDeclarationSyntax
        {
            var leadingTrivia = member.GetLeadingTrivia();
            var newLineTrivia = SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed);
            if (!leadingTrivia.Any(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia)))
            {
                newLineTrivia = newLineTrivia.AddRange(leadingTrivia);
            }
            return member.WithLeadingTrivia(newLineTrivia);
        }

        private static T RemoveLeadingTrivia<T>(T member) where T : MemberDeclarationSyntax
        {
            return member.WithLeadingTrivia(SyntaxFactory.Whitespace(""));
        }

        private static int GetVisibilityOrder(BaseFieldDeclarationSyntax field)
        {
            return GetVisibilityOrder(field.Modifiers);
        }

        private static int GetVisibilityOrder(BasePropertyDeclarationSyntax property)
        {
            return GetVisibilityOrder(property.Modifiers);
        }

        private static int GetVisibilityOrder(BaseMethodDeclarationSyntax method)
        {
            return GetVisibilityOrder(method.Modifiers);
        }

        private static int GetVisibilityOrder(SyntaxTokenList modifiers)
        {
            var visibilityOrder = new Dictionary<string, int>
            {
                ["public"] = 1,
                ["protected internal"] = 2,
                ["protected"] = 3,
                ["internal"] = 4,
                ["private protected"] = 5,
                ["private"] = 6
            };

            var modifiersStr = modifiers.ToString();
            foreach (var kvp in visibilityOrder)
            {
                if (modifiersStr.Contains(kvp.Key))
                    return kvp.Value;
            }

            // Default visibility is private
            return visibilityOrder["private"];
        }
    }
}
