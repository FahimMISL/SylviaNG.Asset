namespace RMS.Application.Features.Notifications.Services;

/// <summary>Feature 9: plain {{Tag}} substitution - no templating library needed for a fixed,
/// known set of merge tags. A tag with no matching value in the dictionary is left as-is (rather than
/// silently blanked), which makes a template typo visible instead of hiding it.</summary>
public static class MergeTagRenderer
{
    public static string Render(string template, IReadOnlyDictionary<string, string> mergeTags)
    {
        var result = template;
        foreach (var (key, value) in mergeTags)
        {
            result = result.Replace("{{" + key + "}}", value);
        }
        return result;
    }
}
