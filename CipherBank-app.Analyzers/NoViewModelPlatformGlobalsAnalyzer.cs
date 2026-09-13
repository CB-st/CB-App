// <copyright file="NoViewModelPlatformGlobalsAnalyzer.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CipherBank_app.Analyzers;

/// <summary>
/// Rejects direct MAUI and task-dispatch globals from ViewModels.
/// Use: High (every MAUI compilation). Scope: files below a ViewModels directory.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoViewModelPlatformGlobalsAnalyzer : DiagnosticAnalyzer
{
    private static readonly HashSet<string> ProhibitedRoots = new(StringComparer.Ordinal)
    {
        "Application",
        "Clipboard",
        "MainThread",
        "Preferences",
        "SecureStorage",
        "Shell",
        "Task",
    };

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(CipherBankDiagnostics.ViewModelPlatformGlobal);

    /// <summary>
    /// Registers invocation analysis for ViewModel source files.
    /// Use: High (every compilation). Scope: this analyzer.
    /// </summary>
    /// <param name="context">Analyzer initialization context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    /// <summary>
    /// Reports a prohibited global invocation from a ViewModel file.
    /// Use: High (each invocation). Scope: ViewModel source.
    /// </summary>
    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        string path = context.Node.SyntaxTree.FilePath.Replace('\\', '/');
        if (!path.Contains("/ViewModels/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;
        string? root = GetRootIdentifier(invocation.Expression);
        if (root is null || !ProhibitedRoots.Contains(root))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            CipherBankDiagnostics.ViewModelPlatformGlobal,
            invocation.GetLocation(),
            root));
    }

    /// <summary>
    /// Returns the leftmost identifier in a member-access chain.
    /// Use: High (candidate invocations). Scope: this analyzer.
    /// </summary>
    private static string? GetRootIdentifier(ExpressionSyntax expression)
    {
        ExpressionSyntax current = expression;
        while (current is MemberAccessExpressionSyntax memberAccess)
        {
            current = memberAccess.Expression;
        }

        return current is IdentifierNameSyntax identifier
            ? identifier.Identifier.ValueText
            : null;
    }
}
