using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace BookShoppingCartMvcUI.TagHelpers;

// server side validation only turns the field red, screen readers don't get anything.
// these link each asp-for field to its asp-validation-for message: the message gets an id ("Email-error")
// and a field with an error gets aria-invalid + aria-describedby so the error is read out with it
public static class ValidationIds
{
    public static string For(string fullFieldName) => TagBuilder.CreateSanitizedId(fullFieldName, "_") + "-error";
}

[HtmlTargetElement("input", Attributes = ForAttribute)]
[HtmlTargetElement("select", Attributes = ForAttribute)]
[HtmlTargetElement("textarea", Attributes = ForAttribute)]
public class AriaInvalidTagHelper : TagHelper
{
    private const string ForAttribute = "asp-for";

    [HtmlAttributeName(ForAttribute)]
    public ModelExpression For { get; set; } = default!;

    [ViewContext, HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    // run after the built in Input/Select/TextArea helpers have set id/name/type
    public override int Order => 1000;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (output.Attributes.TryGetAttribute("type", out var type) && string.Equals(type.Value?.ToString(), "hidden", StringComparison.OrdinalIgnoreCase))
            return;

        var name = ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(For.Name);
        if (!ViewContext.ViewData.ModelState.TryGetValue(name, out var entry) || entry.Errors.Count == 0)
            return;

        output.Attributes.SetAttribute("aria-invalid", "true");

        // keep any hint that's already linked, error goes first
        var errorId = ValidationIds.For(name);
        var existing = output.Attributes.TryGetAttribute("aria-describedby", out var describedBy) ? describedBy.Value?.ToString() : null;
        output.Attributes.SetAttribute("aria-describedby", string.IsNullOrWhiteSpace(existing) ? errorId : $"{errorId} {existing}");
    }
}

[HtmlTargetElement("span", Attributes = ForAttribute)]
[HtmlTargetElement("div", Attributes = ForAttribute)]
public class ValidationMessageIdTagHelper : TagHelper
{
    private const string ForAttribute = "asp-validation-for";

    [HtmlAttributeName(ForAttribute)]
    public ModelExpression For { get; set; } = default!;

    [ViewContext, HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!output.Attributes.ContainsName("id"))
            output.Attributes.SetAttribute("id", ValidationIds.For(ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(For.Name)));
    }
}
