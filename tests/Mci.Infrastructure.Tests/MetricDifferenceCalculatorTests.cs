using Mci.Infrastructure.Reporting;

namespace Mci.Infrastructure.Tests;

public sealed class MetricDifferenceCalculatorTests
{
    [Fact]
    public void Currency_difference_is_absolute_with_percent_when_both_values_are_non_zero()
    {
        var result = MetricDifferenceCalculator.Compute("Currency", left: 92_810m, right: 79_257m);

        Assert.True(result.IsAvailable);
        Assert.Equal(MetricDifferenceCalculator.Absolute, result.DifferenceKind);
        Assert.Equal(13_553m, result.Difference);
        Assert.NotNull(result.PercentDifference);
        Assert.Equal(17.1m, Math.Round(result.PercentDifference!.Value, 1));
    }

    [Fact]
    public void Count_difference_is_absolute_left_minus_right()
    {
        var result = MetricDifferenceCalculator.Compute("Count", left: 372_258m, right: 330_158m);

        Assert.True(result.IsAvailable);
        Assert.Equal(MetricDifferenceCalculator.Absolute, result.DifferenceKind);
        Assert.Equal(42_100m, result.Difference);
    }

    [Fact]
    public void Percentage_difference_uses_percentage_points_and_no_percent_difference()
    {
        var result = MetricDifferenceCalculator.Compute("Percentage", left: 11.8m, right: 14.2m);

        Assert.True(result.IsAvailable);
        Assert.Equal(MetricDifferenceCalculator.PercentagePoints, result.DifferenceKind);
        Assert.Equal(-2.4m, result.Difference);
        Assert.Null(result.PercentDifference);
    }

    [Fact]
    public void Percent_difference_is_omitted_when_a_value_is_zero()
    {
        var result = MetricDifferenceCalculator.Compute("Currency", left: 1_000m, right: 0m);

        Assert.True(result.IsAvailable);
        Assert.Equal(1_000m, result.Difference);
        Assert.Null(result.PercentDifference);
    }

    [Theory]
    [InlineData("Currency")]
    [InlineData("Count")]
    [InlineData("Percentage")]
    public void Missing_value_on_either_side_is_unavailable_and_never_zero(string unit)
    {
        var missingRight = MetricDifferenceCalculator.Compute(unit, left: 100m, right: null);
        var missingLeft = MetricDifferenceCalculator.Compute(unit, left: null, right: 100m);

        Assert.False(missingRight.IsAvailable);
        Assert.Null(missingRight.Difference);
        Assert.False(missingLeft.IsAvailable);
        Assert.Null(missingLeft.Difference);
    }
}
