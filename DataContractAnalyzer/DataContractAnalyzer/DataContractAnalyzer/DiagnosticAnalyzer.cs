using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DataContractAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DataContractAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DataContractAnalyzer";

        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Attributes";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.Attribute);
        }

        private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            AttributeSyntax attribute = (AttributeSyntax)context.Node;
            var location = context.Node.GetLocation();
            var nameSyntax = attribute.Name as IdentifierNameSyntax;
            if (nameSyntax == null)
                return;

            if (nameSyntax.Identifier.Value.ToString() != "DataContract")
                return;

            var propertyNodes = attribute.Parent.Parent.DescendantNodes().Where(node => node.IsKind(SyntaxKind.PropertyDeclaration));
            Diagnostic diagnosticToAdd = null;
            int i = 0;
            int attributeCount = 0;
            foreach (PropertyDeclarationSyntax node in propertyNodes)
            {
                i++;
                attributeCount = 0;
                foreach (var attributeList in node.AttributeLists)
                {
                    attributeCount++;
                    var firstAttribute = attributeList.Attributes.FirstOrDefault();
                    IdentifierNameSyntax name = (IdentifierNameSyntax)firstAttribute.Name;

                    if (name.Identifier.Text == "DataMember")
                    {
                        if (firstAttribute.ArgumentList.ChildNodes().Count() > 0)
                        {
                            foreach (var argument in firstAttribute.ArgumentList.Arguments)
                            {
                                var nameEq = argument.NameEquals.Name;
                                if (nameEq.Identifier.Text == "Order")
                                {
                                    LiteralExpressionSyntax expression = (LiteralExpressionSyntax)argument.Expression;
                                    int value = (int)expression.Token.Value;
                                    if (value != i)
                                    {
                                        diagnosticToAdd = Diagnostic.Create(Rule, location);
                                        context.ReportDiagnostic(diagnosticToAdd);
                                        continue;
                                    }
                                }
                                if (argument == null)
                                    return;
                            }
                        }

                    }
                    // Zero means we already know we're not specifying order correctly. Just fall through and write it below.
                }
                if (attributeCount==0)
                {
                    diagnosticToAdd = Diagnostic.Create(Rule, location);
                    context.ReportDiagnostic(diagnosticToAdd);
                    //On this property, we had no attributes. That's a problem!
                }
            }

            //This means we checked at least one property and got a thumbs up.
            if (i>0)
                return;
            //Otherwise, there were no 
            diagnosticToAdd = Diagnostic.Create(Rule, location);
            context.ReportDiagnostic(diagnosticToAdd);

        }
    }
}
