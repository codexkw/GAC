using Microsoft.AspNetCore.Html;

namespace GAC.Web.Infrastructure;

public static class DockIcons
{
    public static readonly IReadOnlyList<string> Keys =
        new[] { "whatsapp", "test-drive", "quote", "brochure", "location", "mail", "phone" };

    private static readonly Dictionary<string, string> Svgs = new()
    {
        ["whatsapp"] = "<svg viewBox=\"0 0 24 24\" fill=\"currentColor\"><path d=\"M12 2a10 10 0 0 0-8.6 15l-1.4 5 5.1-1.3A10 10 0 1 0 12 2zm0 18a8 8 0 0 1-4.1-1.1l-.3-.2-3 .8.8-2.9-.2-.3A8 8 0 1 1 12 20zm4.4-5.6c-.2-.1-1.4-.7-1.6-.8-.2-.1-.4-.1-.6.1-.2.3-.6.8-.8 1-.1.1-.3.2-.5 0-.7-.3-1.4-.6-2-1.2-.5-.5-.9-1.1-1.2-1.7-.1-.2 0-.4.1-.5l.4-.5c.1-.1.1-.3.2-.4 0-.2 0-.3 0-.4l-.7-1.7c-.2-.5-.4-.4-.6-.4h-.5c-.2 0-.4.1-.6.3-.6.6-.9 1.4-.9 2.2.1 1 .5 1.9 1.1 2.7.9 1.3 2 2.3 3.4 2.9.5.2.9.3 1.3.4.5.1 1 .1 1.4 0 .5-.1 1.4-.6 1.6-1.2.2-.5.2-1 .1-1.1z\"/></svg>",
        ["test-drive"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M3 13l2-5a2 2 0 0 1 1.9-1.3h10.2A2 2 0 0 1 19 8l2 5\"/><path d=\"M3 13h18v4a1 1 0 0 1-1 1h-2a1 1 0 0 1-1-1v-1H7v1a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1z\"/><path d=\"M6.5 15.5h.01M17.5 15.5h.01\"/></svg>",
        ["quote"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z\"/><path d=\"M14 3v5h5\"/><path d=\"M9 13h6M9 17h4\"/></svg>",
        ["brochure"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M12 3v12\"/><path d=\"m7 10 5 5 5-5\"/><path d=\"M5 21h14\"/></svg>",
        ["location"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M12 21s-7-5.3-7-11a7 7 0 0 1 14 0c0 5.7-7 11-7 11z\"/><circle cx=\"12\" cy=\"10\" r=\"2.5\"/></svg>",
        ["mail"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><rect x=\"3\" y=\"5\" width=\"18\" height=\"14\" rx=\"2\"/><path d=\"m3.5 6.5 8.5 6 8.5-6\"/></svg>",
        ["phone"] = "<svg viewBox=\"0 0 24 24\" fill=\"currentColor\"><path d=\"M6.6 10.8c1.4 2.8 3.8 5.1 6.6 6.6l2.2-2.2c.3-.3.7-.4 1-.2 1.1.4 2.3.6 3.6.6.6 0 1 .4 1 1V20c0 .6-.4 1-1 1C10.6 21 3 13.4 3 4c0-.6.4-1 1-1h3.5c.6 0 1 .4 1 1 0 1.2.2 2.4.6 3.6.1.4 0 .8-.3 1l-2.2 2.2z\"/></svg>",
    };

    public static IHtmlContent Render(string? key)
        => new HtmlString(key is not null && Svgs.TryGetValue(key, out var svg) ? svg : Svgs["quote"]);
}
