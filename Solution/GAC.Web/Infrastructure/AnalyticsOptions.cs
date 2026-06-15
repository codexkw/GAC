namespace GAC.Web.Infrastructure;

/// <summary>Bound from the "Analytics" config section. IDs are non-secret; empty => nothing rendered.</summary>
public sealed class AnalyticsOptions
{
    public string Ga4MeasurementId { get; set; } = "";
    public string GtmContainerId { get; set; } = "";
}
