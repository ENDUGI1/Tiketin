using Markdig;
using Microsoft.AspNetCore.Html;

namespace Tiketin.Web.Infrastructure;

/// <summary>Renders KB article markdown. Raw HTML in the source is disabled (stored XSS guard).</summary>
public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    public static IHtmlContent Render(string markdown)
        => new HtmlString(Markdown.ToHtml(markdown, Pipeline));
}
