using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VerifyXunit;

namespace Entitas.Generators.Tests
{
    public static class TestHelper
    {
        // https://andrewlock.net/creating-a-source-generator-part-2-testing-an-incremental-generator-with-snapshot-testing/
        public static Task Verify(string source, IIncrementalGenerator generator)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            IEnumerable<PortableExecutableReference> references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IComponent).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MyApp.Library.ContextAttribute).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                "Tests",
                new[] { syntaxTree },
                references);

            var driver = CSharpGeneratorDriver.Create(generator).RunGenerators(compilation);
            return Verifier.Verify(driver).UseDirectory("snapshots");
        }

        public static void AssertUsesGlobalNamespaces(string code)
        {
            var patterns = new[]
            {
                "System",
                "Entitas"
            }.Select(word => $"(?<!\\busing )(?<!\\bstatic )(?<!\\bnamespace )(?<!\\bglobal::)(?<!\"){word}");

            foreach (var pattern in patterns)
            {
                Regex.Matches(code, pattern).Should().HaveCount(0);
            }
        }
    }
}
