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

namespace SimpleComments
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SimpleCommentsCodeFixProvider)), Shared]
    public class SimpleCommentsCodeFixProvider : CodeFixProvider
    {
        private const string title = "Format comment";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(SimpleCommentsAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.

            var comment = root.FindTrivia(diagnosticSpan.Start);

            // Register a code action that will invoke the fix.
            var action = CodeAction.Create(title: title,
                                           createChangedDocument: c => FormatComment(context.Document, comment, c),
                                           equivalenceKey: title);

            context.RegisterCodeFix(action, diagnostic);
        }

        private async Task<Document> FormatComment(Document document, SyntaxTrivia originalCommentNode, CancellationToken cancellationToken)
        {
            // Just get rid of the // and any leading spaces, and capitalize the first character.
            var fullComment = originalCommentNode.ToString().TrimStart('/').TrimStart(' ');
            var firstChar = fullComment.ToCharArray().First();
            var upperChar = char.ToUpper(firstChar);

            fullComment = fullComment.Remove(0, 1);
            fullComment = fullComment.Insert(0, "// " + upperChar.ToString());

            var newCommentNode = SyntaxFactory.Comment(fullComment);


            // Add an annotation to format the new local declaration.
            var formattedComment = newCommentNode.WithAdditionalAnnotations(Formatter.Annotation);

            // Get the document root and replace the old declaration with the new one
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);

            var newRoot = oldRoot.ReplaceTrivia(originalCommentNode, formattedComment);

            // Return document with transformed tree
            return document.WithSyntaxRoot(newRoot);
        }

        //private async Task<Solution> FormatComment(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        //{
        //    // Compute new uppercase name.
        //    var identifierToken = typeDecl.Identifier;
        //    var newName = identifierToken.Text.ToUpperInvariant();

        //    // Get the symbol representing the type to be renamed.
        //    var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        //    var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

        //    // Produce a new solution that has all references to that type renamed, including the declaration.
        //    var originalSolution = document.Project.Solution;
        //    var optionSet = originalSolution.Workspace.Options;
        //    var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);

        //    // Return the new solution with the now-uppercase type name.
        //    return newSolution;
        //}
    }
}