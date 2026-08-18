namespace RMS.Application.Features.Approvals.Services;

/// <summary>SLA state computed on read, not persisted - per the Feature 3 plan.</summary>
public enum SlaState
{
    /// <summary>No SLA configured on this stage.</summary>
    None = 0,
    Green = 1,
    Yellow = 2,
    Red = 3,
}

public static class SlaStateCalculator
{
    /// <summary>Green &lt;50% elapsed, Yellow 50-99%, Red >=100% (breached). While SlaPausedAtUtc is set
    /// (a clarification is outstanding), elapsed time is frozen at the pause moment instead of advancing
    /// with "now" - the pause freezes the SLA clock exactly where it stood.</summary>
    public static SlaState Compute(DateTime? slaStartUtc, DateTime? slaDueUtc, DateTime? slaPausedAtUtc, DateTime nowUtc)
    {
        if (slaStartUtc is null || slaDueUtc is null)
        {
            return SlaState.None;
        }

        var effectiveNow = slaPausedAtUtc ?? nowUtc;
        var total = slaDueUtc.Value - slaStartUtc.Value;
        var elapsed = effectiveNow - slaStartUtc.Value;

        if (total <= TimeSpan.Zero || elapsed >= total)
        {
            return SlaState.Red;
        }

        var ratio = elapsed.TotalSeconds / total.TotalSeconds;
        return ratio >= 0.5 ? SlaState.Yellow : SlaState.Green;
    }
}
