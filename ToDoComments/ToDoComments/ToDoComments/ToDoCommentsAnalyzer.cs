using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ToDoComments
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ToDoCommentsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ToDoCommentWarnings";
        private const string Title = "TODO comments should trigger warnings";
        private const string MessageFormat = "Try not to leave TODO's in your code!";
        private const string Category = "Usage";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(AnalyzeSyntax);
        }

        private static void AnalyzeSyntax(SyntaxTreeAnalysisContext context)
        {
            var tree = context.Tree;

            var root = context.Tree.GetCompilationUnitRoot(context.CancellationToken);

            var comments = root.DescendantTrivia().Where(r => r.IsKind(SyntaxKind.SingleLineCommentTrivia));

            foreach (var comment in comments)
            {
                var commentText = comment.ToString();

                if (commentText.ToUpper().Contains("TODO"))
                {
                    var diagnostic = Diagnostic.Create(Rule, comment.GetLocation());

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
