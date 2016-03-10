using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SimpleComments
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SimpleCommentsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SimpleComments";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Formatting";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(HandleSyntaxTree);
        }

        private void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            SyntaxNode root = context.Tree.GetCompilationUnitRoot(context.CancellationToken);
            var commentNodes = from node in root.DescendantTrivia()
                               where node.IsKind(SyntaxKind.SingleLineCommentTrivia)
                               select node;

            if (!commentNodes.Any())
                return;

            foreach (var node in commentNodes)
            {
                bool requiresDiatnostic = false;

                var commentText = node.ToString().TrimStart('/');
                if (commentText.Length == 0)
                    continue;

                if (!commentText.StartsWith(" "))
                    requiresDiatnostic = true;
                
                var firstChar = commentText.TrimStart(' ').ToCharArray().First();

                // Check if the comment does not begin with a capital letter
                if (!char.IsUpper(firstChar))
                    requiresDiatnostic = true;

                if (requiresDiatnostic)
                {
                    var diagnostic = Diagnostic.Create(Rule, node.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
