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

namespace BifrostPathAttributeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BifrostPathAttributeAnalyzerCodeFixProvider)), Shared]
    public class BifrostPathAttributeAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string title = "Add [BifrostPath] attribute";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(BifrostPathAttributeAnalyzerAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            try
            {
                var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

                // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
                var diagnostic = context.Diagnostics.First();
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                // Find the type declaration identified by the diagnostic.
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedDocument: c => AddBifrostServiceAttributeAsync(context.Document, declaration, c),
                        equivalenceKey: title),
                    diagnostic);
            }
            catch (Exception)
            {
                return;
            }
        }

        private async Task<Document> AddBifrostServiceAttributeAsync(Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            try
            {
                var namespaceDeclaration = (NamespaceDeclarationSyntax)methodDeclaration.FirstAncestorOrSelf<SyntaxNode>(a => a.IsKind(SyntaxKind.NamespaceDeclaration));
                string fullNamespace = namespaceDeclaration.Name.ToString();
                var application = fullNamespace.Split('.').ElementAt(1);

                var interfaceDeclaration = (InterfaceDeclarationSyntax)methodDeclaration.FirstAncestorOrSelf<SyntaxNode>(a => a.IsKind(SyntaxKind.InterfaceDeclaration));
                var service = interfaceDeclaration.Identifier.ValueText.Remove(0, 1).Replace("Service", string.Empty).ToLower();

                var operation = methodDeclaration.Identifier.ValueText.ToLower();

                string path = $"bifrost/{application}/{service}/{operation}";

                var name = SyntaxFactory.ParseName("BifrostPath");
                var arguments = SyntaxFactory.ParseAttributeArgumentList($"(\"{path}\")");
                var attribute = SyntaxFactory.Attribute(name, arguments); //MyAttribute("some_param")

                var attributeList = new SeparatedSyntaxList<AttributeSyntax>();
                attributeList = attributeList.Add(attribute);
                var list = SyntaxFactory.AttributeList(attributeList);   //[MyAttribute("some_param")]

                MethodDeclarationSyntax newMethod = methodDeclaration.AddAttributeLists(list);

                var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
                var newRoot = oldRoot.ReplaceNode(methodDeclaration, newMethod.WithAdditionalAnnotations(Formatter.Annotation));

                return document.WithSyntaxRoot(newRoot);
            }
            catch (Exception)
            {

                return document;
            }
        }
    }
}