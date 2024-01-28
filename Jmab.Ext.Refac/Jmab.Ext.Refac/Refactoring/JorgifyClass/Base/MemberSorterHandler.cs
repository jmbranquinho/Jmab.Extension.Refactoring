using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jmab.Ext.Refac.Refactoring.JorgifyClass.Base
{
    public abstract class MemberSorterHandler : ChainOfResponsibilityMember
    {
        protected static IEnumerable<MemberDeclarationSyntax> AdjustMembersSpacing<T>(List<T> members, MemberSpacing spacing) where T : MemberDeclarationSyntax
        {
            if (!members.Any()) return Enumerable.Empty<MemberDeclarationSyntax>();

            var firstMember = members.First();
            var list = new List<MemberDeclarationSyntax> { firstMember };

            var leadingTrivia = firstMember.GetLeadingTrivia().Where(trivia => !trivia.IsKind(SyntaxKind.EndOfLineTrivia));

            foreach (var member in members.Skip(1))
            {
                IEnumerable<SyntaxTrivia> adjustedTrailingTrivia = AdjustTrailingTrivia(member, spacing);

                var adjustedLeadingTrivia = member.GetLeadingTrivia().Where(trivia => !trivia.IsKind(SyntaxKind.EndOfLineTrivia));

                list.Add(member.WithLeadingTrivia(adjustedLeadingTrivia).WithTrailingTrivia(adjustedTrailingTrivia));
            }

            return list;
        }

        protected enum MemberSpacing
        {
            NoSpace,
            Space
        }

        protected int GetVisibilityOrder(SyntaxTokenList modifiers)
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

            return visibilityOrder["private"];
        }

        private static IEnumerable<SyntaxTrivia> AdjustTrailingTrivia<T>(T member, MemberSpacing spacing) where T : MemberDeclarationSyntax
        {
            switch (spacing)
            {
                case MemberSpacing.NoSpace:
                    return new[] { SyntaxFactory.EndOfLine(Environment.NewLine) };

                case MemberSpacing.Space:
                    return new[] { SyntaxFactory.EndOfLine(Environment.NewLine), SyntaxFactory.EndOfLine(Environment.NewLine) };

                default:
                    throw new ArgumentOutOfRangeException(nameof(spacing), spacing, null);
            }
        }
    }
}
