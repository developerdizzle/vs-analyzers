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

namespace CamelCaseConstants
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CamelCaseConstantsCodeFixProvider)), Shared]
    public class CamelCaseConstantsCodeFixProvider : CodeFixProvider
    {
        private const string title = "Make camelCase";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CamelCaseConstantsAnalyzer.DiagnosticId); }
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
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<FieldDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => MakeCamelCaseAsync(context.Document, declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Document> MakeCamelCaseAsync(Document document, FieldDeclarationSyntax fieldDeclaration, CancellationToken cancellationToken)
        {
            var fieldName = fieldDeclaration.Declaration.Variables.First().Identifier.Text;

            // Split on underscores
            // Lowercase everything
            // Uppercase first character

            var upperWords = fieldName.Split('_');

            var camelWords = new List<string>();

            foreach (var upperWord in upperWords)
            {
                var firstLetter = upperWord.ToCharArray().First();
                var camelWord = upperWord.ToLower().Remove(0, 1).Insert(0, firstLetter.ToString());

                camelWords.Add(camelWord);
            }

            string camelCaseFieldName = string.Join("", camelWords);

            var newIdentifier = SyntaxFactory.IdentifierName(camelCaseFieldName);

            var newVariable = fieldDeclaration.Declaration.Variables.First().WithIdentifier(newIdentifier.Identifier);
            var newVariableList = fieldDeclaration.Declaration.Variables.Replace(fieldDeclaration.Declaration.Variables.First(), newVariable);

            var newVariables = fieldDeclaration.Declaration.WithVariables(newVariableList);
            var newField = fieldDeclaration.WithDeclaration(newVariables);

            // Replace the old local declaration with the new local declaration.
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = oldRoot.ReplaceNode(fieldDeclaration, newField);

            // Return document with transformed tree.
            return document.WithSyntaxRoot(newRoot);
        }
    }
}