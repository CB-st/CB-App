// <copyright file="NoViewModelPlatformGlobalsAnalyzerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CipherBank_app.Analyzers.Tests;

public sealed class NoViewModelPlatformGlobalsAnalyzerTests
{
    [Theory]
    [InlineData("Preferences.Get(\"theme\", \"system\")")]
    [InlineData("Shell.Current.GoToAsync(\"//home\")")]
    [InlineData("Task.Run(() => 1)")]
    public async Task ReportsPlatformGlobalFromViewModel(string expression)
    {
        CSharpAnalyzerTest<NoViewModelPlatformGlobalsAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app/ViewModels/ProfileViewModel.cs", $$"""
                        class ProfileViewModel
                        {
                            void Execute()
                            {
                                _ = {|CB1005:{{expression}}|};
                            }
                        }
                        """),
                },
            },
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task IgnoresPlatformUseOutsideViewModels()
    {
        CSharpAnalyzerTest<NoViewModelPlatformGlobalsAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app/Services/PreferenceStore.cs", """
                        class PreferenceStore
                        {
                            object Read() => Preferences.Get("theme", "system");
                        }
                        """),
                },
            },
        };

        await test.RunAsync();
    }
}
