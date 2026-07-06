import { StrictMode } from "react";
import * as React from "react";
import { createRoot } from "react-dom/client";
import { ArrowDownUp, BarChart3, Database, RefreshCw } from "lucide-react";
import "./styles.css";

type Metric = {
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

type County = {
  fipsCode: string;
  name: string;
  stateCode: string;
  stateName: string;
};

type Observation = {
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

type Summary = {
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

type SortKey = "rank" | "countyName" | "estimateValue" | "marginOfError";
type SortDirection = "asc" | "desc";

const defaultReleaseYear = 2024;

async function getJson<T>(url: string): Promise<T> {
  const response = await fetch(url);

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status} ${response.statusText}`);
  }

  return response.json() as Promise<T>;
}

function App() {
  const [metrics, setMetrics] = React.useState<Metric[]>([]);
  const [counties, setCounties] = React.useState<County[]>([]);
  const [observations, setObservations] = React.useState<Observation[]>([]);
  const [summary, setSummary] = React.useState<Summary[]>([]);
  const [selectedMetric, setSelectedMetric] = React.useState("population");
  const [selectedCounty, setSelectedCounty] = React.useState("");
  const [sortKey, setSortKey] = React.useState<SortKey>("rank");
  const [sortDirection, setSortDirection] = React.useState<SortDirection>("asc");
  const [isLoading, setIsLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);

  const loadFilterData = React.useCallback(async () => {
    const [metricData, countyData] = await Promise.all([
      getJson<Metric[]>("/api/reporting/metrics"),
      getJson<County[]>("/api/reporting/counties")
    ]);

    setMetrics(metricData);
    setCounties(countyData);

    setSelectedMetric((currentMetric) =>
      metricData.some((metric) => metric.code === currentMetric) || metricData.length === 0
        ? currentMetric
        : metricData[0].code
    );
  }, []);

  const loadObservations = React.useCallback(async () => {
    const params = new URLSearchParams({
      metricCode: selectedMetric,
      releaseYear: String(defaultReleaseYear)
    });

    if (selectedCounty) {
      params.set("countyFipsCode", selectedCounty);
    }

    const [observationData, summaryData] = await Promise.all([
      getJson<Observation[]>(`/api/reporting/current-observations?${params}`),
      getJson<Summary[]>(`/api/reporting/current-observations/summary?${params}`)
    ]);

    setObservations(observationData);
    setSummary(summaryData);
  }, [selectedCounty, selectedMetric]);

  React.useEffect(() => {
    let isActive = true;

    async function load() {
      try {
        setIsLoading(true);
        setError(null);
        await loadFilterData();
      } catch (err) {
        if (isActive) {
          setError(err instanceof Error ? err.message : "Unable to load reporting filters.");
        }
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    }

    load();

    return () => {
      isActive = false;
    };
  }, [loadFilterData]);

  React.useEffect(() => {
    let isActive = true;

    async function load() {
      try {
        setIsLoading(true);
        setError(null);
        await loadObservations();
      } catch (err) {
        if (isActive) {
          setError(err instanceof Error ? err.message : "Unable to load reporting observations.");
        }
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    }

    if (selectedMetric) {
      load();
    }

    return () => {
      isActive = false;
    };
  }, [loadObservations, selectedMetric]);

  const selectedMetricInfo = metrics.find((metric) => metric.code === selectedMetric);
  const selectedSummary = summary[0];
  const releaseLabel = selectedSummary?.dataReleaseDisplayName ?? "2020-2024 ACS 5-Year";
  const periodLabel = selectedSummary
    ? `${selectedSummary.dataReleaseDisplayName} (${selectedSummary.releaseYear})`
    : "2020-2024 ACS 5-Year";

  const rankedObservations = React.useMemo(() => {
    const ranked = [...observations]
      .sort((a, b) => b.estimateValue - a.estimateValue)
      .map((observation, index) => ({ ...observation, rank: index + 1 }));

    return ranked.sort((a, b) => {
      const direction = sortDirection === "asc" ? 1 : -1;

      if (sortKey === "rank") {
        return (a.rank - b.rank) * direction;
      }

      if (sortKey === "countyName") {
        return a.countyName.localeCompare(b.countyName) * direction;
      }

      const aValue = a[sortKey] ?? Number.NEGATIVE_INFINITY;
      const bValue = b[sortKey] ?? Number.NEGATIVE_INFINITY;
      return (aValue - bValue) * direction;
    });
  }, [observations, sortDirection, sortKey]);

  function updateSort(nextSortKey: SortKey) {
    if (sortKey === nextSortKey) {
      setSortDirection((current) => (current === "asc" ? "desc" : "asc"));
      return;
    }

    setSortKey(nextSortKey);
    setSortDirection(nextSortKey === "countyName" ? "asc" : "desc");
  }

  return (
    <main className="app-shell">
      <header className="app-header">
        <div>
          <p className="eyebrow">Michigan County Insights</p>
          <h1>County Metrics Dashboard</h1>
        </div>
        <div className="status-pill">
          <Database size={16} aria-hidden="true" />
          <span>{periodLabel}</span>
        </div>
      </header>

      <section className="toolbar" aria-label="Reporting filters">
        <label>
          <span>Metric</span>
          <select value={selectedMetric} onChange={(event) => setSelectedMetric(event.target.value)}>
            {metrics.map((metric) => (
              <option key={metric.code} value={metric.code}>
                {metric.displayName}
              </option>
            ))}
          </select>
        </label>

        <label>
          <span>County</span>
          <select value={selectedCounty} onChange={(event) => setSelectedCounty(event.target.value)}>
            <option value="">All Michigan counties</option>
            {counties.map((county) => (
              <option key={county.fipsCode} value={county.fipsCode}>
                {county.name}
              </option>
            ))}
          </select>
        </label>

        <button className="icon-button" type="button" onClick={loadObservations} title="Refresh data">
          <RefreshCw size={18} aria-hidden="true" />
          <span>Refresh</span>
        </button>
      </section>

      {error && <div className="alert">{error}</div>}

      <section className="summary-grid" aria-label="Selected metric summary">
        <div>
          <span className="summary-label">Release</span>
          <strong>{releaseLabel}</strong>
        </div>
        <div>
          <span className="summary-label">Counties</span>
          <strong>{selectedSummary?.countyCount ?? rankedObservations.length}</strong>
        </div>
        <div>
          <span className="summary-label">Range</span>
          <strong>
            {selectedSummary
              ? `${formatValue(selectedSummary.minimumEstimateValue, selectedMetricInfo)} - ${formatValue(
                  selectedSummary.maximumEstimateValue,
                  selectedMetricInfo
                )}`
              : "Not loaded"}
          </strong>
        </div>
        <div>
          <span className="summary-label">Metric Type</span>
          <strong>{selectedMetricInfo?.calculationType ?? "Metric"}</strong>
        </div>
      </section>

      <section className="metric-context">
        <div>
          <BarChart3 size={18} aria-hidden="true" />
          <h2>{selectedMetricInfo?.displayName ?? "Metric"}</h2>
        </div>
        <p>{selectedMetricInfo?.description}</p>
        <p className="guidance">{selectedMetricInfo?.comparisonGuidance}</p>
      </section>

      <section className="table-section">
        <div className="table-heading">
          <h2>Current Observations</h2>
          <span>{isLoading ? "Loading..." : `${rankedObservations.length} rows`}</span>
        </div>

        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <SortableHeader label="Rank" active={sortKey === "rank"} onClick={() => updateSort("rank")} />
                <SortableHeader
                  label="County"
                  active={sortKey === "countyName"}
                  onClick={() => updateSort("countyName")}
                />
                <th>FIPS</th>
                <SortableHeader
                  label="Estimate"
                  active={sortKey === "estimateValue"}
                  onClick={() => updateSort("estimateValue")}
                />
                <SortableHeader
                  label="MOE"
                  active={sortKey === "marginOfError"}
                  onClick={() => updateSort("marginOfError")}
                />
                <th>Release Period</th>
              </tr>
            </thead>
            <tbody>
              {rankedObservations.map((observation) => (
                <tr key={observation.observationId}>
                  <td>{observation.rank}</td>
                  <td>{observation.countyName}</td>
                  <td>{observation.countyFipsCode}</td>
                  <td>{formatValue(observation.estimateValue, selectedMetricInfo)}</td>
                  <td>{observation.marginOfError === null ? "N/A" : formatValue(observation.marginOfError, selectedMetricInfo)}</td>
                  <td>{`${observation.periodStartYear}-${observation.periodEndYear}`}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </main>
  );
}

function SortableHeader({
  active,
  label,
  onClick
}: {
  active: boolean;
  label: string;
  onClick: () => void;
}) {
  return (
    <th>
      <button className={active ? "sort-button active" : "sort-button"} type="button" onClick={onClick}>
        <span>{label}</span>
        <ArrowDownUp size={14} aria-hidden="true" />
      </button>
    </th>
  );
}

function formatValue(value: number, metric?: Metric): string {
  const decimalPlaces = metric?.decimalPlaces ?? 0;
  const unit = metric?.unit;

  return new Intl.NumberFormat("en-US", {
    maximumFractionDigits: decimalPlaces,
    minimumFractionDigits: decimalPlaces,
    style: unit === "Currency" ? "currency" : "decimal"
  }).format(value) + (unit === "Percentage" ? "%" : "");
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <App />
  </StrictMode>
);
