using Jmab.Ext.Refac.Refactoring.JorgifyClass.Base;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Jmab.Ext.Refac.Refactoring.JorgifyClass.Fields
{
    public class PrivateFieldRenameHandler : ChainOfResponsibilityMember
    {
        private Dictionary<string, string> renamedFields = new Dictionary<string, string>();

        public override List<MemberDeclarationSyntax> Handle(IEnumerable<MemberDeclarationSyntax> members)
        {
            // Reset the dictionary for each handling
            renamedFields.Clear();

            // Rename private fields
            var updatedMembers = members.Select(member => RenamePrivateField(member)).ToList();

            // Update references to these fields
            var rewriter = new UpdateFieldReferencesRewriter(renamedFields);
            updatedMembers = updatedMembers.Select(member => (MemberDeclarationSyntax)rewriter.Visit(member)).ToList();

            return _nextHandler != null
                ? _nextHandler.Handle(updatedMembers)
                : updatedMembers;
        }

        private MemberDeclarationSyntax RenamePrivateField(MemberDeclarationSyntax member)
        {
            var fieldDeclaration = member as FieldDeclarationSyntax;
            if (fieldDeclaration != null)
            {
                var isPrivate = fieldDeclaration.Modifiers.Any(m => m.Kind() == SyntaxKind.PrivateKeyword);

                if (isPrivate)
                {
                    var updatedVariables = fieldDeclaration.Declaration.Variables.Select(variable =>
                    {
                        var originalName = variable.Identifier.Text;
                        if (!originalName.StartsWith("_"))
                        {
                            var newName = "_" + originalName;
                            renamedFields[originalName] = newName;
                            variable = variable.WithIdentifier(SyntaxFactory.Identifier(newName));
                        }
                        return variable;
                    });

                    return fieldDeclaration.WithDeclaration(fieldDeclaration.Declaration.WithVariables(SyntaxFactory.SeparatedList(updatedVariables)));
                }
            }

            return fieldDeclaration;
        }
    }

    public class UpdateFieldReferencesRewriter : CSharpSyntaxRewriter
    {
        private readonly Dictionary<string, string> renamedFields;

        public UpdateFieldReferencesRewriter(Dictionary<string, string> renamedFields)
        {
            this.renamedFields = renamedFields;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (renamedFields.TryGetValue(node.Identifier.Text, out var newName))
            {
                return node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }

            return base.VisitIdentifierName(node);
        }
    }
}