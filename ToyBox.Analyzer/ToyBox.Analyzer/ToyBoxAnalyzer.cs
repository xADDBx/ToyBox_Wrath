﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace ToyBox.Analyzer {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ToyBoxAnalyzer : DiagnosticAnalyzer {
        private const string Category = "Usage";
        public const string DiagnosticId = "LOC001";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description);
        public const string DiagnosticId2 = "LOC002";
        private static readonly LocalizableString Title2 = new LocalizableResourceString(nameof(Resources.AnalyzerTitle2), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat2 = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat2), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description2 = new LocalizableResourceString(nameof(Resources.AnalyzerDescription2), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor Rule2 = new DiagnosticDescriptor(DiagnosticId2, Title2, MessageFormat2, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description2);
        public const string DiagnosticId3 = "LOC003";
        private static readonly LocalizableString Title3 = new LocalizableResourceString(nameof(Resources.AnalyzerTitle3), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat3 = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat3), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description3 = new LocalizableResourceString(nameof(Resources.AnalyzerDescription3), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor Rule3 = new DiagnosticDescriptor(DiagnosticId3, Title3, MessageFormat3, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description3);


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create([Rule, Rule2, Rule3]); } }

        public override void Initialize(AnalysisContext context) {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }
        private void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context) {
            var literal = (LiteralExpressionSyntax)context.Node;
            var stringValue = literal.Token.ValueText;

            var localizedAttr = literal.AncestorsAndSelf().OfType<AttributeSyntax>().FirstOrDefault(attr => {
                var nameText = attr.Name.ToString();
                return nameText == "LocalizedString" || nameText.EndsWith(".LocalizedString");
            });

            if (localizedAttr != null) {
                AnalyzeAttributeStringLiteral(context);
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, literal.GetLocation(), stringValue));
        }
        private void AnalyzeAttributeStringLiteral(SyntaxNodeAnalysisContext context) {
            var literal = (LiteralExpressionSyntax)context.Node;
            if (!(literal.Parent is AttributeArgumentSyntax argument))
                return;

            if (!(argument.Parent is AttributeArgumentListSyntax argumentList) ||
                 argumentList.Arguments.First() != argument) {
                return;
            }

            var stringValue = literal.Token.ValueText;
            var candidateIdentifier = stringValue.Replace('.', '_');

            if (!SyntaxFacts.IsValidIdentifier(candidateIdentifier)) {
                context.ReportDiagnostic(Diagnostic.Create(Rule3, literal.GetLocation(), candidateIdentifier));
            }
        }
        private void AnalyzeInvocation(OperationAnalysisContext context) {
            var invocation = (IInvocationOperation)context.Operation;
            var targetMethod = invocation.TargetMethod;

            // Very naive check for arguments of methods calling GUILayout
            if (targetMethod.ContainingType.Name == "GUILayout") {
                foreach (var argument in invocation.Arguments) {
                    if (argument.Value is ILiteralOperation literalOp && literalOp.ConstantValue.HasValue && literalOp.Type.SpecialType == SpecialType.System_String) {
                        context.ReportDiagnostic(Diagnostic.Create(Rule2, argument.Syntax.GetLocation(), argument.ConstantValue.Value));
                    }
                }
            }
        }
    }
}
