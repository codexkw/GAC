using GAC.Core.Content;
using Xunit;

namespace GAC.Tests;

public class SlugTests
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("  GS4 MAX  ", "gs4-max")]                       // trimmed + spaced
    [InlineData("Multiple   spaces & symbols!", "multiple-spaces-symbols")]
    [InlineData("Already-a-slug", "already-a-slug")]
    [InlineData("--Hello--", "hello")]                            // leading/trailing hyphens stripped
    [InlineData("GS4 MAX 2026", "gs4-max-2026")]                  // digits preserved
    [InlineData("Café crème", "caf-cr-me")]                       // non-ASCII letters act as separators
    public void From_NormalizesToUrlSafeSlug(string input, string expected)
        => Assert.Equal(expected, Slug.From(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("أخبار")]            // Arabic-only → no ASCII alphanumerics to keep
    public void From_ReturnsEmpty_WhenNoAsciiAlnum(string? input)
        => Assert.Equal(string.Empty, Slug.From(input));
}
