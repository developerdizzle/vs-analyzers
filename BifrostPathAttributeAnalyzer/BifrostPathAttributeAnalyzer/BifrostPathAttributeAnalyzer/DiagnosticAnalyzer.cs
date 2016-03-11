using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BifrostPathAttributeAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BifrostPathAttributeAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "BifrostPathAttributeAnalyzer";

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
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            // We only want methods for an Interface in *.Client.Services namespace with a BifrostServiceAttribute that doesn't have BifrostPathAttribute
            var methodSymbol = (IMethodSymbol)context.Symbol;

            if (methodSymbol.MethodKind != MethodKind.Ordinary)
                return;

            if (!methodSymbol.ContainingNamespace.ToString().EndsWith(".Client.Services"))
                return;

            if (methodSymbol.ContainingType.TypeKind != TypeKind.Interface)
                return;

            if (!methodSymbol.ContainingSymbol.Name.EndsWith("Service"))
                return;

            var attribute = methodSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass.Name.Contains("BifrostPathAttribute"));

            if (attribute != null)
                return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Symbol.Locations[0], methodSymbol.Name));
        }
    }
}
