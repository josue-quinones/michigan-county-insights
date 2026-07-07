namespace Mci.Infrastructure.Reporting;

/// <summary>Result of a unit-aware, formatting-neutral metric difference.</summary>
public readonly record struct MetricDifferenceResult(
    decimal? Difference,
    string DifferenceKind,
    decimal? PercentDifference,
    bool IsAvailable);

/// <summary>
/// Pure difference math for county comparisons. Kept free of EF/DTO concerns so it
/// is directly unit-testable. Direction is always left minus right.
/// </summary>
public static class MetricDifferenceCalculator
{
    public const string Absolute = "Absolute";
    public const string PercentagePoints = "PercentagePoints";

    private const string PercentageUnit = "Percentage";

    /// <summary>
    /// Computes left - right for the metric's unit. Percentage metrics yield a
    /// percentage-point difference; count/currency metrics yield an absolute
    /// difference plus an optional percent difference (both values non-zero).
    /// A missing value on either side yields an unavailable, non-zero result.
    /// </summary>
    public static MetricDifferenceResult Compute(string unit, decimal? left, decimal? right)
    {
        if (left is null || right is null)
        {
            return new MetricDifferenceResult(null, Absolute, null, IsAvailable: false);
        }

        var difference = left.Value - right.Value;

        if (string.Equals(unit, PercentageUnit, StringComparison.OrdinalIgnoreCase))
        {
            return new MetricDifferenceResult(difference, PercentagePoints, null, IsAvailable: true);
        }

        // Count or Currency: absolute difference, plus percent difference only when
        // both values are non-zero (rule: percent difference needs a non-zero left,
        // and a non-zero right avoids dividing by zero).
        // Currency and Count share the same absolute-difference behavior.
        decimal? percentDifference = left.Value != 0 && right.Value != 0
            ? (difference / right.Value) * 100m
            : null;

        return new MetricDifferenceResult(difference, Absolute, percentDifference, IsAvailable: true);
    }
}
