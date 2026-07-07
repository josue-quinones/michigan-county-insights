import { apiUrl, getJson } from "../lib/api";

export type CountyReference = {
  fips: string;
  name: string;
  stateCode: string;
};

export type ReleaseInfo = {
  year: number;
  periodStartYear: number;
  periodEndYear: number;
  displayName: string;
};

export type DetailMetric = {
  code: string;
  displayName: string;
  category: string;
  unit: string;
  decimalPlaces: number;
  estimateValue: number | null;
  marginOfError: number | null;
  isAvailable: boolean;
};

export type CountyDetail = {
  county: CountyReference;
  release: ReleaseInfo;
  metrics: DetailMetric[];
  lastSuccessfulImportAtUtc: string | null;
};

export type ComparisonMetric = {
  code: string;
  displayName: string;
  category: string;
  unit: string;
  decimalPlaces: number;
  leftValue: number | null;
  leftMarginOfError: number | null;
  rightValue: number | null;
  rightMarginOfError: number | null;
  difference: number | null;
  differenceKind: "Absolute" | "PercentagePoints";
  percentDifference: number | null;
  isAvailable: boolean;
};

export type CountyComparison = {
  leftCounty: CountyReference;
  rightCounty: CountyReference;
  release: ReleaseInfo;
  differenceDirection: string;
  metrics: ComparisonMetric[];
};

export type CountyOption = {
  fipsCode: string;
  name: string;
  stateCode: string;
  stateName: string;
};

/** Raised for a 404 so callers can show a "county not found" state. */
export class NotFoundError extends Error {}

async function getJsonOr404<T>(url: string): Promise<T> {
  const response = await fetch(url);

  if (response.status === 404) {
    throw new NotFoundError("Not found");
  }

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status} ${response.statusText}`);
  }

  return response.json() as Promise<T>;
}

export function fetchCounties(): Promise<CountyOption[]> {
  return getJson<CountyOption[]>(apiUrl("/api/reporting/counties"));
}

export function fetchCountyDetail(fips: string, release?: string | number | null): Promise<CountyDetail> {
  const params = new URLSearchParams();
  if (release) {
    params.set("release", String(release));
  }
  const query = params.toString();
  return getJsonOr404<CountyDetail>(apiUrl(`/api/counties/${fips}/detail${query ? `?${query}` : ""}`));
}

export function fetchComparison(
  left: string,
  right: string,
  release?: string | number | null
): Promise<CountyComparison> {
  const params = new URLSearchParams({ left, right });
  if (release) {
    params.set("release", String(release));
  }
  return getJson<CountyComparison>(apiUrl(`/api/comparisons?${params.toString()}`));
}
