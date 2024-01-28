using Jmab.Ext.Refac.Refactoring.JorgifyClass.Base;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Jmab.Ext.Refac.Refactoring.JorgifyClass.Fields
{
    public class FieldSorterHandler : MemberSorterHandler
    {
        public override List<MemberDeclarationSyntax> Handle(IEnumerable<MemberDeclarationSyntax> members)
        {
            var sortedMembers = members
                .OrderBy(m => GetVisibilityOrder(m.Modifiers))
                .ToList();

            var updatedMembers = AdjustMembersSpacing(sortedMembers, MemberSpacing.NoSpace)
                .ToList();

            return _nextHandler != null
                ? _nextHandler.Handle(updatedMembers)
                : updatedMembers;
        }
    }
}
