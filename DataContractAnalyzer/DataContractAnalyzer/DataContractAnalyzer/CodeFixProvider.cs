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

namespace DataContractAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DataContractAnalyzerCodeFixProvider)), Shared]
    public class DataContractAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string title = "Correct contact members";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DataContractAnalyzer.DiagnosticId); }
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
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => CorrectDataContractAsync(context.Document, declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Document> CorrectDataContractAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var identifierToken = typeDecl.Identifier;

            List<SyntaxNode> properties; // Do assignment inside because we'll have different object hashes after we get our new tree!!
            int i = 0;
            var oldRoot = await document.GetSyntaxRootAsync();

            do
            {
                properties = oldRoot.DescendantNodes().Where(node => node.IsKind(SyntaxKind.PropertyDeclaration)).ToList();
                PropertyDeclarationSyntax property = (PropertyDeclarationSyntax) properties[i];
                var propToCheck = property;
                bool attributeReplaced = false;
                i++;

                foreach (var attributelist in propToCheck.AttributeLists)
                {
                    var attributePart = attributelist.Attributes.FirstOrDefault();
                    IdentifierNameSyntax name = (IdentifierNameSyntax)attributePart.Name;

                    if (name.Identifier.Text == "DataMember")
                    {
                        attributeReplaced = true;

                        //remove that attribute. We'll create it from scratch.
                        var newProperty = propToCheck.RemoveNode(attributelist, SyntaxRemoveOptions.KeepTrailingTrivia);
                        //root.ReplaceNode(property,newProperty)

                        var arguments = SyntaxFactory.ParseAttributeArgumentList($"(Order = {i})");
                        var newDataMember = SyntaxFactory.Attribute(name, arguments);

                        var attributeSyntaxList = new SeparatedSyntaxList<AttributeSyntax>();
                        attributeSyntaxList = attributeSyntaxList.Add(newDataMember);
                        var attributeList = SyntaxFactory.AttributeList(attributeSyntaxList);

                        var syntaxList = new SyntaxList<AttributeListSyntax>();
                        var newWhateverElse = syntaxList.Add(attributeList);

                        var superNewProperty = newProperty.WithAttributeLists(newWhateverElse);

                        oldRoot = oldRoot.ReplaceNode(propToCheck, superNewProperty);
                    }
                }
                if (!attributeReplaced)
                {
                    var name = SyntaxFactory.ParseName("DataMember");
                    var arguments = SyntaxFactory.ParseAttributeArgumentList($"(Order = {i})");

                    var newDataMember = SyntaxFactory.Attribute(name, arguments);


                    var attributeList = new SeparatedSyntaxList<AttributeSyntax>();

                    attributeList = attributeList.Add(newDataMember);

                    var list = SyntaxFactory.AttributeList(attributeList);

                    var newList = new SyntaxList<AttributeListSyntax>();
                    var newWhateverElse = newList.Add(list);
                    var theFinalThing = propToCheck.WithAttributeLists(newWhateverElse);
                    oldRoot = oldRoot.ReplaceNode(propToCheck, theFinalThing);
                    //We didn't replace it above, so we need to ADD it here.
                }
                attributeReplaced = false;
            }
            while (i < properties.Count());

            return document.WithSyntaxRoot(oldRoot);
        }
    }
}