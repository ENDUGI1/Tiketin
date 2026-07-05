using Microsoft.AspNetCore.Html;

namespace Tiketin.Web.Infrastructure;

/// <summary>
/// Inline Lucide icons (static SVG, no CDN). One family, uniform 2px stroke.
/// https://lucide.dev - ISC license.
/// </summary>
public static class Icons
{
    private static readonly Dictionary<string, string> Paths = new()
    {
        ["ticket"] = """<path d="M2 9a3 3 0 0 1 0 6v2a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-2a3 3 0 0 1 0-6V7a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2Z"/><path d="M13 5v2"/><path d="M13 17v2"/><path d="M13 11v2"/>""",
        ["inbox"] = """<polyline points="22 12 16 12 14 15 10 15 8 12 2 12"/><path d="M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z"/>""",
        ["book-open"] = """<path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z"/><path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z"/>""",
        ["layout-dashboard"] = """<rect width="7" height="9" x="3" y="3" rx="1"/><rect width="7" height="5" x="14" y="3" rx="1"/><rect width="7" height="9" x="14" y="12" rx="1"/><rect width="7" height="5" x="3" y="16" rx="1"/>""",
        ["users"] = """<path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/>""",
        ["log-out"] = """<path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" x2="9" y1="12" y2="12"/>""",
        ["plus"] = """<path d="M5 12h14"/><path d="M12 5v14"/>""",
        ["search"] = """<circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/>""",
        ["paperclip"] = """<path d="m21.44 11.05-9.19 9.19a6 6 0 0 1-8.49-8.49l8.57-8.57A4 4 0 1 1 18 8.84l-8.59 8.57a2 2 0 0 1-2.83-2.83l8.49-8.48"/>""",
        ["clock"] = """<circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>""",
        ["user"] = """<path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/>""",
        ["star"] = """<polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/>""",
        ["chevron-left"] = """<path d="m15 18-6-6 6-6"/>""",
        ["chevron-right"] = """<path d="m9 18 6-6-6-6"/>""",
        ["lock"] = """<rect width="18" height="11" x="3" y="11" rx="2" ry="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>""",
        ["alert-triangle"] = """<path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"/><path d="M12 9v4"/><path d="M12 17h.01"/>""",
        ["check"] = """<path d="M20 6 9 17l-5-5"/>"""
    };

    public static IHtmlContent Render(string name, int size = 16)
    {
        if (!Paths.TryGetValue(name, out var path))
        {
            throw new ArgumentException($"Unknown icon '{name}'.", nameof(name));
        }

        return new HtmlString(
            $"""<svg xmlns="http://www.w3.org/2000/svg" width="{size}" height="{size}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">{path}</svg>""");
    }
}
