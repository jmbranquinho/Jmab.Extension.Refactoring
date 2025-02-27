﻿using Jmab.Ext.Refac.Refactoring.JorgifyClass.Base;
using Jmab.Ext.Refac.Refactoring.JorgifyClass.Constructors;
using Jmab.Ext.Refac.Refactoring.JorgifyClass.Fields;
using Jmab.Ext.Refac.Refactoring.JorgifyClass.Methods;
using Jmab.Ext.Refac.Refactoring.JorgifyClass.Properties;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Jmab.Ext.Refac.Refactoring.JorgifyClass
{
    public class JorgifyClassHandler
    {
        public static ClassDeclarationSyntax JorgifyClass(ClassDeclarationSyntax classDeclaration)
        {
            var fieldHandler = GetFieldHandler();
            var propertyHandler = GetPropertyHandler();
            var constructorHandler = GetCtorHandler();
            var methodHandler = GetMethodHandler();

            var updatedFields = fieldHandler.Handle(FetchOfType<FieldDeclarationSyntax>(classDeclaration));
            var updatedProperties = propertyHandler.Handle(FetchOfType<PropertyDeclarationSyntax>(classDeclaration));
            var updatedConstructors = constructorHandler.Handle(FetchOfType<ConstructorDeclarationSyntax>(classDeclaration));
            var updatedMethods = methodHandler.Handle(FetchOfType<MethodDeclarationSyntax>(classDeclaration));

            var members = ConcatenateAllMembers(updatedFields, updatedProperties, updatedConstructors, updatedMethods);

            return classDeclaration.WithMembers(SyntaxFactory.List(members));
        }

        private static ChainOfResponsibilityMember GetMethodHandler()
        {
            return new MethodSorterHandler();
        }

        private static ChainOfResponsibilityMember GetCtorHandler()
        {
            return new ConstructorSorterHandler();
        }

        private static ChainOfResponsibilityMember GetPropertyHandler()
        {
            return new PropertySorterHandler();
        }

        private static ChainOfResponsibilityMember GetFieldHandler()
        {
            var sortHandler = new FieldSorterHandler();
            //sortHandler.SetNext(new PrivateFieldRenameHandler());
            return sortHandler;
        }

        private static List<MemberDeclarationSyntax> FetchOfType<T>(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Members
                .OfType<T>()
                .Cast<MemberDeclarationSyntax>()
                .ToList();
        }

        private static IEnumerable<MemberDeclarationSyntax> ConcatenateAllMembers(
            IEnumerable<MemberDeclarationSyntax> updatedFields,
            IEnumerable<MemberDeclarationSyntax> updatedProperties,
            IEnumerable<MemberDeclarationSyntax> updatedConstructors,
            IEnumerable<MemberDeclarationSyntax> updatedMethods)
        {
            var allMembers = new List<MemberDeclarationSyntax>();
            allMembers.AddRange(updatedFields);
            allMembers.AddRange(updatedProperties);
            allMembers.AddRange(updatedConstructors);
            allMembers.AddRange(updatedMethods);
            return allMembers;
        }
    }
}
