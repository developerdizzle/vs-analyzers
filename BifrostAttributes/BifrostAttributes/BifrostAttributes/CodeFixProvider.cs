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

namespace BifrostAttributes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BifrostAttributesCodeFixProvider)), Shared]
    public class BifrostAttributesCodeFixProvider : CodeFixProvider
    {
        private const string title = "Add [BifrostService] attribute";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(BifrostAttributesAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InterfaceDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => AddBifrostServiceAttributeAsync(context.Document, declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Document> AddBifrostServiceAttributeAsync(Document document, InterfaceDeclarationSyntax interfaceDeclaration, CancellationToken cancellationToken)
        {
            AttributeSyntax bifrost = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("BifrostService"));
            AttributeListSyntax bifrostAttrList = SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(new[] { bifrost }));

            InterfaceDeclarationSyntax newInterface = interfaceDeclaration.AddAttributeLists(bifrostAttrList);

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = oldRoot.ReplaceNode(interfaceDeclaration, newInterface.WithAdditionalAnnotations(Formatter.Annotation));

            return document.WithSyntaxRoot(newRoot);
        }
    }
}