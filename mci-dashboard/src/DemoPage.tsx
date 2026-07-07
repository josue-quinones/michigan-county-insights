import * as React from "react";
import { Link } from "react-router-dom";
import { ArrowDownUp, ArrowLeft, BarChart3, Database, RefreshCw } from "lucide-react";
import { ChartDatum, TopCountiesChart } from "./TopCountiesChart";

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

type RankedObservation = Observation & { rank: number };
type SortKey = "rank" | "countyName" | "estimateValue" | "marginOfError";
type SortDirection = "asc" | "desc";

const defaultReleaseYear = 2024;
const chartLimit = 12;
const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? "").replace(/\/$/, "");

function apiUrl(path: string): string {
  return `${apiBaseUrl}${path}`;
}

async function getJson<T>(url: string): Promise<T> {
  const response = await fetch(url);

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status} ${response.statusText}`);
  }

  return response.json() as Promise<T>;
}

export default function DemoPage() {
  const [metrics, setMetrics] = React.useState<Metric[]>([]);
  const [counties, setCounties] = React.useState<County[]>([]);
  const [observations, setObservations] = React.useState<Observation[]>([]);
  const [summary, setSummary] = React.useState<Summary[]>([]);
  const [selectedMetric, setSelectedMetric] = React.useState("population");
  const [focusCounty, setFocusCounty] = React.useState("");
  const [sortKey, setSortKey] = React.useState<SortKey>("rank");
  const [sortDirection, setSortDirection] = React.useState<SortDirection>("asc");
  const [isLoading, setIsLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);

  const loadFilterData = React.useCallback(async () => {
    const [metricData, countyData] = await Promise.all([
      getJson<Metric[]>(apiUrl("/api/reporting/metrics")),
      getJson<County[]>(apiUrl("/api/reporting/counties"))
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
    // Fetch the full metric across every county once; the table and chart both
    // derive from this, and the county selector focuses rather than re-queries.
    const params = new URLSearchParams({
      metricCode: selectedMetric,
      releaseYear: String(defaultReleaseYear)
    });

    const [observationData, summaryData] = await Promise.all([
      getJson<Observation[]>(apiUrl(`/api/reporting/current-observations?${params}`)),
      getJson<Summary[]>(apiUrl(`/api/reporting/current-observations/summary?${params}`))
    ]);

    setObservations(observationData);
    setSummary(summaryData);
  }, [selectedMetric]);

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

  const format = React.useCallback(
    (value: number) => formatValue(value, selectedMetricInfo),
    [selectedMetricInfo]
  );

  // Rank every county by value once (rank is stable regardless of table sort).
  const rankedByValue = React.useMemo<RankedObservation[]>(
    () =>
      [...observations]
        .sort((a, b) => b.estimateValue - a.estimateValue)
        .map((observation, index) => ({ ...observation, rank: index + 1 })),
    [observations]
  );

  const chartData = React.useMemo<ChartDatum[]>(() => {
    const top = rankedByValue.slice(0, chartLimit);
    const focused = rankedByValue.find((row) => row.countyFipsCode === focusCounty);

    if (focused && !top.some((row) => row.countyFipsCode === focusCounty)) {
      top.push(focused);
    }

    return top.map((row) => ({
      fipsCode: row.countyFipsCode,
      name: row.countyName,
      value: row.estimateValue,
      rank: row.rank,
      marginOfError: row.marginOfError
    }));
  }, [rankedByValue, focusCounty]);

  const tableRows = React.useMemo(() => {
    const rows = focusCounty
      ? rankedByValue.filter((row) => row.countyFipsCode === focusCounty)
      : rankedByValue;

    return [...rows].sort((a, b) => {
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
  }, [rankedByValue, focusCounty, sortDirection, sortKey]);

  function updateSort(nextSortKey: SortKey) {
    if (sortKey === nextSortKey) {
      setSortDirection((current) => (current === "asc" ? "desc" : "asc"));
      return;
    }

    setSortKey(nextSortKey);
    setSortDirection(nextSortKey === "countyName" ? "asc" : "desc");
  }

  return (
    <div className="app-shell demo-page">
      <div className="demo-topbar">
        <Link className="back-link" to="/">
          <ArrowLeft size={16} aria-hidden="true" />
          Back to portfolio
        </Link>
      </div>

      <header className="app-header">
        <div>
          <p className="eyebrow">Michigan County Insights · Live demo</p>
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
          <span>Focus county</span>
          <select value={focusCounty} onChange={(event) => setFocusCounty(event.target.value)}>
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
          <strong>{selectedSummary?.countyCount ?? rankedByValue.length}</strong>
        </div>
        <div>
          <span className="summary-label">Range</span>
          <strong>
            {selectedSummary
              ? `${format(selectedSummary.minimumEstimateValue)} - ${format(
                  selectedSummary.maximumEstimateValue
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

      <TopCountiesChart
        data={chartData}
        focusFips={focusCounty}
        totalCounties={selectedSummary?.countyCount ?? rankedByValue.length}
        format={format}
      />

      <section className="table-section">
        <div className="table-heading">
          <h2>{focusCounty ? "Selected county" : "All counties"}</h2>
          <span>{isLoading ? "Loading..." : `${tableRows.length} rows`}</span>
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
              {tableRows.map((observation) => (
                <tr
                  key={observation.observationId}
                  className={observation.countyFipsCode === focusCounty ? "row-focus" : undefined}
                >
                  <td>{observation.rank}</td>
                  <td>{observation.countyName}</td>
                  <td>{observation.countyFipsCode}</td>
                  <td>{format(observation.estimateValue)}</td>
                  <td>
                    {observation.marginOfError === null ? "N/A" : format(observation.marginOfError)}
                  </td>
                  <td>{`${observation.periodStartYear}-${observation.periodEndYear}`}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
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

  return (
    new Intl.NumberFormat("en-US", {
      maximumFractionDigits: decimalPlaces,
      minimumFractionDigits: decimalPlaces,
      style: unit === "Currency" ? "currency" : "decimal"
    }).format(value) + (unit === "Percentage" ? "%" : "")
  );
}
