﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
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
            allMembers.AddRange(AdjustMembersSpacing(fields, MemberSpacing.NoSpace));
            allMembers.AddRange(AdjustMembersSpacing(properties, MemberSpacing.Space));
            allMembers.AddRange(AdjustMembersSpacing(constructors, MemberSpacing.Space));
            allMembers.AddRange(AdjustMembersSpacing(methods, MemberSpacing.Space));

            return allMembers;
        }

        private static IEnumerable<MemberDeclarationSyntax> AdjustMembersSpacing<T>(List<T> members, MemberSpacing spacing) where T : MemberDeclarationSyntax
        {
            if (!members.Any()) return Enumerable.Empty<MemberDeclarationSyntax>();

            var firstMember = members.First();

            var list = new List<MemberDeclarationSyntax>
            {
                firstMember
            };

            var leadingTrivia = firstMember.GetLeadingTrivia().Where(trivia => !trivia.IsKind(SyntaxKind.EndOfLineTrivia));
            var trailingTrivia = firstMember.GetTrailingTrivia();
            var endOfLineTrivia = trailingTrivia.Where(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia)).ToList();
            if (endOfLineTrivia.Count != 1)
            {
                endOfLineTrivia = trailingTrivia.Where(trivia => !trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                                               .Concat(new[] { SyntaxFactory.EndOfLine(Environment.NewLine) })
                                               .ToList();
            }

            foreach (var member in members.Skip(1))
            {
                if (spacing == MemberSpacing.NoSpace)
                {
                    list.Add(member.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(trailingTrivia));
                }
                else
                {
                    list.Add(member);
                }
            }

            return list;
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

        protected enum MemberSpacing
        {
            NoSpace,
            Space
        }
    }
}
