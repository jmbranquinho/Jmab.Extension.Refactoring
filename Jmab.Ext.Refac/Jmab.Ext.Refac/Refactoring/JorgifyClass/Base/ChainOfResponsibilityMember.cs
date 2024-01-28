using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Jmab.Ext.Refac.Refactoring.JorgifyClass.Base
{
    public abstract class ChainOfResponsibilityMember
    {
        protected ChainOfResponsibilityMember _nextHandler;

        public void SetNext(ChainOfResponsibilityMember nextHandler)
        {
            _nextHandler = nextHandler;
        }

        public abstract List<MemberDeclarationSyntax> Handle(IEnumerable<MemberDeclarationSyntax> members);
    }
}
