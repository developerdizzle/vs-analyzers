using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ControllerUsageLogAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ControllerUsageLogAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ControllerUsageLog";
        private const string Title = "Missing logging";
        private const string MessageFormat = "All controller actions should have `Usage` attribute";
        private const string Category = "Logging";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

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

            if (namedTypeSymbol.TypeKind != TypeKind.Class) return;

            if (!namedTypeSymbol.Name.EndsWith("Controller")) return;

            var actions = namedTypeSymbol.GetMembers()
                .Where(r => r.Kind == SymbolKind.Method)
                .Where(r => r.Name != ".ctor")
                .Where(r => r.DeclaredAccessibility == Accessibility.Public)
                .ToArray();

            foreach (var action in actions)
            {
                var attributes = action.GetAttributes();

                if (attributes.Any(r => r.AttributeClass.Name == "Usage")) return;

                var diagnostic = Diagnostic.Create(Rule, action.Locations[0], action.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
