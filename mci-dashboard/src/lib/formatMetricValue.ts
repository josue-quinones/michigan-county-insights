// Shared, unit-aware value formatting for every reporting surface (dashboard,
// county detail, comparison). Single source of truth so the currency-metric
// crash can never come back: Intl.NumberFormat with style "currency" REQUIRES a
// currency code — omitting it throws during render.

export type FormattableMetric = {
  unit?: string;
  decimalPlaces?: number;
};

/**
 * Format a raw numeric estimate using the metric's unit and decimal places.
 * Currency -> "$92,741"; Percentage -> "11.8%"; Count/other -> "1,234".
 */
export function formatMetricValue(value: number, metric?: FormattableMetric): string {
  const decimalPlaces = metric?.decimalPlaces ?? 0;
  const unit = metric?.unit;

  if (unit === "Currency") {
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: "USD",
      maximumFractionDigits: decimalPlaces,
      minimumFractionDigits: decimalPlaces
    }).format(value);
  }

  const formatted = new Intl.NumberFormat("en-US", {
    maximumFractionDigits: decimalPlaces,
    minimumFractionDigits: decimalPlaces
  }).format(value);

  // Percentage values arrive on a 0-100 scale, so append the sign rather than
  // using style:"percent" (which would multiply by 100).
  return unit === "Percentage" ? `${formatted}%` : formatted;
}

/**
 * Format a margin of error for a metric. Returns "N/A" when absent.
 */
export function formatMarginOfError(moe: number | null | undefined, metric?: FormattableMetric): string {
  if (moe === null || moe === undefined) {
    return "N/A";
  }

  return formatMetricValue(moe, metric);
}
