using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BifrostAttributes
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BifrostAttributesAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "BifrostAttributes";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            if (namedTypeSymbol.TypeKind != TypeKind.Interface)
                return;

            if (!namedTypeSymbol.ContainingNamespace.ToString().EndsWith(".Client.Services"))
                return;

            if (namedTypeSymbol.GetAttributes().Any(a => a.AttributeClass.Name.Contains("BifrostServiceAttribute")))
                return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name));
        }

        //public override void Initialize(AnalysisContext context)
        //{
        //    context.RegisterSyntaxNodeAction(AnalyzeInterface, SyntaxKind.InterfaceDeclaration);
        //}

        //private void AnalyzeInterface(SyntaxNodeAnalysisContext context)
        //{
        //    var interfaceDeclaration = (InterfaceDeclarationSyntax)context.Node;

        //    // Check to see if we are in *.Client.Services namespace
        //    var namespaceDeclaration = (from a in interfaceDeclaration.Ancestors()
        //                                where a.IsKind(SyntaxKind.NamespaceDeclaration)
        //                                select a).First();

        //    var qualifiedName = namespaceDeclaration.ChildNodes().OfType<QualifiedNameSyntax>().First().ToString();

        //    // We only want to check interfaces in *.Client.Services namespace
        //    if (!qualifiedName.EndsWith(".Client.Services"))
        //        return;

        //    // Only show the squiggle if it doesn't have the BifrostService attribute
        //    var containsBifrostServiceAttribute = interfaceDeclaration.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString().Contains("BifrostService"));

        //    if (containsBifrostServiceAttribute)
        //        return;

        //    context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
        //}
    }
}
