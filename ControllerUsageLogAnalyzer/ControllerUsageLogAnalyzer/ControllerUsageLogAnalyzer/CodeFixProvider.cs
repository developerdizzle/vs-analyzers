using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;

namespace ControllerUsageLogAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ControllerUsageLogAnalyzerCodeFixProvider)), Shared]
    public class ControllerUsageLogAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string title = "Add Usage attribute";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(ControllerUsageLogAnalyzerAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();

            var declaration = root.FindToken(context.Span.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => AddUsageLog(context.Document, declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Document> AddUsageLog(Document document, MethodDeclarationSyntax declaration, CancellationToken cancellationToken)
        {
            var methodName = declaration.Identifier.Text;
            var classDeclaration = declaration.Parent as ClassDeclarationSyntax;
            var className = classDeclaration.Identifier.Text;

            var opValue = className.Replace("Controller", String.Empty);

            var usage = SyntaxFactory.Attribute(
                SyntaxFactory.ParseName("Usage"),
                SyntaxFactory.ParseAttributeArgumentList($"(Func=\"{methodName}\",Op=\"{opValue}\")")
            );

            var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(new[] { usage }));

            var newDeclaration = declaration.AddAttributeLists(attributeList);

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = oldRoot.ReplaceNode(declaration, newDeclaration.WithAdditionalAnnotations(Formatter.Annotation));

            return document.WithSyntaxRoot(newRoot);
        }
    }
}