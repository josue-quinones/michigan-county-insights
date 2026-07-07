// Formats an API-computed comparison difference. The API owns the math (unit
// awareness, percentage-points vs percent, missing = unavailable); this only
// turns its neutral numbers into a signed, human-readable label.

import { formatMetricValue, type FormattableMetric } from "./formatMetricValue";

export type DifferenceKind = "Absolute" | "PercentagePoints";

export type ComparisonDifference = {
  difference: number | null;
  differenceKind: DifferenceKind;
  percentDifference: number | null;
};

function signed(text: string, value: number): string {
  return value > 0 ? `+${text}` : text;
}

/**
 * Format a comparison difference with the correct unit and sign.
 * - Count/Currency: signed value, plus "(±x.x%)" when a percent difference exists.
 * - Percentage: signed percentage-point difference, e.g. "-2.4 percentage points".
 * Returns "Unavailable" when the API could not compute a difference.
 */
export function formatDifference(diff: ComparisonDifference, metric?: FormattableMetric): string {
  if (diff.difference === null) {
    return "Unavailable";
  }

  if (diff.differenceKind === "PercentagePoints") {
    const value = new Intl.NumberFormat("en-US", {
      maximumFractionDigits: metric?.decimalPlaces ?? 1,
      minimumFractionDigits: metric?.decimalPlaces ?? 1
    }).format(diff.difference);
    const points = Math.abs(diff.difference) === 1 ? "percentage point" : "percentage points";
    return `${signed(value, diff.difference)} ${points}`;
  }

  const base = signed(formatMetricValue(diff.difference, metric), diff.difference);

  if (diff.percentDifference === null) {
    return base;
  }

  const percent = new Intl.NumberFormat("en-US", {
    maximumFractionDigits: 1,
    minimumFractionDigits: 1
  }).format(diff.percentDifference);

  return `${base} (${signed(`${percent}%`, diff.percentDifference)})`;
}
