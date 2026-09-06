using FluentAssertions;
using RMS.Application.Features.Notifications.Services;

namespace SylviaNG.Assets.Tests.Services;

public class MergeTagRendererTests
{
    [Fact]
    public void Render_SubstitutesAllKnownTags()
    {
        var result = MergeTagRenderer.Render(
            "Requisition {{RequisitionNumber}} was {{Status}} by {{ActorName}}.",
            new Dictionary<string, string> { ["RequisitionNumber"] = "REQ-2026-00042", ["Status"] = "Approved", ["ActorName"] = "Diana Head" });

        result.Should().Be("Requisition REQ-2026-00042 was Approved by Diana Head.");
    }

    [Fact]
    public void Render_LeavesUnknownTagUnreplaced()
    {
        var result = MergeTagRenderer.Render(
            "{{RequisitionNumber}} - {{NotProvided}}",
            new Dictionary<string, string> { ["RequisitionNumber"] = "REQ-2026-00042" });

        result.Should().Be("REQ-2026-00042 - {{NotProvided}}");
    }

    [Fact]
    public void Render_WithNoTagsInTemplate_ReturnsTemplateUnchanged()
    {
        var result = MergeTagRenderer.Render("Plain text, no placeholders.", new Dictionary<string, string> { ["Unused"] = "value" });

        result.Should().Be("Plain text, no placeholders.");
    }
}
