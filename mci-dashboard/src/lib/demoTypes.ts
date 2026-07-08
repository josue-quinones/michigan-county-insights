// Shared shapes for the demo dashboard and the pre-generated snapshot. These
// mirror the reporting API responses exactly so snapshot data and live data are
// interchangeable.

export type Metric = {
  code: string;
  displayName: string;
  description: string;
  category: string;
  unit: string;
  decimalPlaces: number;
  calculationType: string;
  comparisonGuidance: string;
  requiresDollarNormalization: boolean;
  supportsAdjacentReleaseComparison: boolean;
};

export type County = {
  fipsCode: string;
  name: string;
  stateCode: string;
  stateName: string;
};

export type Observation = {
  observationId: number;
  countyFipsCode: string;
  countyName: string;
  metricCode: string;
  metricDisplayName: string;
  category: string;
  unit: string;
  decimalPlaces: number;
  estimateValue: number;
  marginOfError: number | null;
  releaseYear: number;
  periodStartYear: number;
  periodEndYear: number;
  dataReleaseDisplayName: string;
  comparisonGuidance: string;
  importedAtUtc: string;
};

export type Summary = {
  metricCode: string;
  metricDisplayName: string;
  category: string;
  unit: string;
  decimalPlaces: number;
  releaseYear: number;
  dataReleaseDisplayName: string;
  observationCount: number;
  countyCount: number;
  minimumEstimateValue: number;
  maximumEstimateValue: number;
  importedAtUtc: string;
};

export type MetricPayload = { observations: Observation[]; summary: Summary[] };
