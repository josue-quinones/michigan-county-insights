import * as React from "react";
import { Link } from "react-router-dom";
import {
  ArrowDownUp,
  ArrowLeft,
  ArrowLeftRight,
  BarChart3,
  Database,
  RefreshCw
} from "lucide-react";
import { ChartDatum, TopCountiesChart } from "./TopCountiesChart";
import { ProjectStory } from "./ProjectStory";
import { formatMetricValue } from "./lib/formatMetricValue";
import { apiUrl, getJson } from "./lib/api";
import { loadDemoSnapshot } from "./lib/snapshot";
import type { Metric, County, Observation, Summary, MetricPayload } from "./lib/demoTypes";

type RankedObservation = Observation & { rank: number };
type SortKey = "rank" | "countyName" | "estimateValue" | "marginOfError";
type SortDirection = "asc" | "desc";

const defaultReleaseYear = 2024;
const chartLimit = 12;

// In-memory cache keyed by metric code. Re-selecting a previously loaded metric
// renders instantly; a never-seen metric still shows the loading skeleton.
const metricCache = new Map<string, MetricPayload>();

async function fetchMetricPayload(metricCode: string): Promise<MetricPayload> {
  const params = new URLSearchParams({
    metricCode,
    releaseYear: String(defaultReleaseYear)
  });

  const [observations, summary] = await Promise.all([
    getJson<Observation[]>(apiUrl(`/api/reporting/current-observations?${params}`)),
    getJson<Summary[]>(apiUrl(`/api/reporting/current-observations/summary?${params}`))
  ]);

  return { observations, summary };
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
  const [showRanking, setShowRanking] = React.useState(false);
  const [rankingRevealing, setRankingRevealing] = React.useState(false);
  const [bootstrapped, setBootstrapped] = React.useState(false);
  const [fromSnapshot, setFromSnapshot] = React.useState(false);

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

  // Load (or refresh) the selected metric. Uses the cache unless forceReload is
  // set (the Refresh button busts it so a manual refresh always re-queries).
  const loadObservations = React.useCallback(
    async (forceReload = false) => {
      if (forceReload) {
        metricCache.delete(selectedMetric);
      }

      const cached = metricCache.get(selectedMetric);
      if (cached) {
        setObservations(cached.observations);
        setSummary(cached.summary);
        return;
      }

      const payload = await fetchMetricPayload(selectedMetric);
      metricCache.set(selectedMetric, payload);
      setObservations(payload.observations);
      setSummary(payload.summary);
    },
    [selectedMetric]
  );

  // Bootstrap: paint instantly from the static snapshot when present (no API/DB
  // cold start), otherwise fall back to the live API. Runs once on mount.
  React.useEffect(() => {
    let isActive = true;

    async function bootstrap() {
      const snapshot = await loadDemoSnapshot();

      // Bail if the effect was torn down (e.g. StrictMode's dev double-invoke)
      // so we neither update state nor spuriously fall back to the live API.
      if (!isActive) {
        return;
      }

      if (snapshot) {
        for (const metric of snapshot.metrics) {
          metricCache.set(metric.code, {
            observations: snapshot.observations[metric.code] ?? [],
            summary: snapshot.summaries[metric.code] ?? []
          });
        }

        setMetrics(snapshot.metrics);
        setCounties(snapshot.counties);

        const initialMetric = snapshot.metrics.some((metric) => metric.code === selectedMetric)
          ? selectedMetric
          : snapshot.metrics[0]?.code ?? selectedMetric;
        const cached = metricCache.get(initialMetric);
        if (cached) {
          setObservations(cached.observations);
          setSummary(cached.summary);
        }

        setSelectedMetric(initialMetric);
        setFromSnapshot(true);
        setBootstrapped(true);
        setIsLoading(false);
        return;
      }

      // No snapshot: load filter data from the live API.
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
          setBootstrapped(true);
        }
      }
    }

    bootstrap();

    return () => {
      isActive = false;
    };
    // Intentionally run once; selectedMetric is read as the initial default only.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [loadFilterData]);

  // React to metric changes. Snapshot-primed metrics are served from the cache
  // instantly; a cache miss (live mode, or Refresh) fetches from the API.
  React.useEffect(() => {
    if (!bootstrapped || !selectedMetric) {
      return;
    }

    let isActive = true;

    async function load() {
      if (metricCache.has(selectedMetric)) {
        const cached = metricCache.get(selectedMetric)!;
        setObservations(cached.observations);
        setSummary(cached.summary);
        setIsLoading(false);
        return;
      }

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

    load();

    return () => {
      isActive = false;
    };
  }, [bootstrapped, loadObservations, selectedMetric]);

  const handleRefresh = React.useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);
      await loadObservations(true);
      setFromSnapshot(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unable to refresh reporting observations.");
    } finally {
      setIsLoading(false);
    }
  }, [loadObservations]);

  const revealRanking = React.useCallback(() => {
    setShowRanking(true);
    setRankingRevealing(true);
    const timer = window.setTimeout(() => setRankingRevealing(false), 350);
    return () => window.clearTimeout(timer);
  }, []);

  const selectedMetricInfo = metrics.find((metric) => metric.code === selectedMetric);
  const selectedSummary = summary[0];
  const releaseLabel = selectedSummary?.dataReleaseDisplayName ?? "2020-2024 ACS 5-Year";
  const periodLabel = selectedSummary
    ? `${selectedSummary.dataReleaseDisplayName} (${selectedSummary.releaseYear})`
    : "2020-2024 ACS 5-Year";

  const format = React.useCallback(
    (value: number) => formatMetricValue(value, selectedMetricInfo),
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

  const chartLoading = isLoading && chartData.length === 0;
  const summaryLoading = isLoading && !selectedSummary;
  const tableLoading = rankingRevealing || (isLoading && tableRows.length === 0);

  return (
    <div className="app-shell demo-page">
      <div className="demo-topbar">
        <Link className="back-link" to="/">
          <ArrowLeft size={16} aria-hidden="true" />
          Back to portfolio
        </Link>
        <Link className="back-link" to="/compare">
          <ArrowLeftRight size={16} aria-hidden="true" />
          Compare counties
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

        <button
          className="icon-button"
          type="button"
          onClick={handleRefresh}
          title={fromSnapshot ? "Fetch the latest values from the live API" : "Refresh data"}
        >
          <RefreshCw size={18} aria-hidden="true" />
          <span>{fromSnapshot ? "Refresh (live)" : "Refresh"}</span>
        </button>
      </section>

      {fromSnapshot && (
        <p className="data-source-note">
          Showing a cached snapshot for instant load. Use <strong>Refresh (live)</strong> to pull
          current values straight from the deployed API.
        </p>
      )}

      {error && <div className="alert">{error}</div>}

      <section className="summary-grid" aria-label="Selected metric summary" aria-busy={summaryLoading}>
        {summaryLoading ? (
          <>
            <SummaryTileSkeleton />
            <SummaryTileSkeleton />
            <SummaryTileSkeleton />
            <SummaryTileSkeleton />
          </>
        ) : (
          <>
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
          </>
        )}
      </section>

      <section className="metric-context">
        <div>
          <BarChart3 size={18} aria-hidden="true" />
          <h2>{selectedMetricInfo?.displayName ?? "Metric"}</h2>
        </div>
        <p>{selectedMetricInfo?.description}</p>
        <p className="guidance">{selectedMetricInfo?.comparisonGuidance}</p>
      </section>

      {chartLoading ? (
        <ChartSkeleton />
      ) : (
        <TopCountiesChart
          data={chartData}
          focusFips={focusCounty}
          totalCounties={selectedSummary?.countyCount ?? rankedByValue.length}
          format={format}
        />
      )}

      <section className="ranking-section">
        {!showRanking ? (
          <div className="ranking-reveal">
            <div>
              <h2>Full county ranking</h2>
              <p>See every Michigan county ranked for the selected metric, with margins of error.</p>
            </div>
            <button className="button outline" type="button" onClick={revealRanking}>
              View full ranking
            </button>
          </div>
        ) : (
          <div className="table-section">
            <div className="table-heading">
              <h2>{focusCounty ? "Selected county" : "All counties"}</h2>
              <span>{tableLoading ? "Loading…" : `${tableRows.length} rows`}</span>
            </div>

            {tableLoading ? (
              <TableSkeleton />
            ) : (
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
                      <th aria-label="Actions" />
                    </tr>
                  </thead>
                  <tbody>
                    {tableRows.map((observation) => (
                      <tr
                        key={observation.observationId}
                        className={observation.countyFipsCode === focusCounty ? "row-focus" : undefined}
                      >
                        <td>{observation.rank}</td>
                        <td>
                          <Link className="county-link" to={`/counties/${observation.countyFipsCode}`}>
                            {observation.countyName}
                          </Link>
                        </td>
                        <td>{observation.countyFipsCode}</td>
                        <td>{format(observation.estimateValue)}</td>
                        <td>
                          {observation.marginOfError === null ? "N/A" : format(observation.marginOfError)}
                        </td>
                        <td>{`${observation.periodStartYear}-${observation.periodEndYear}`}</td>
                        <td className="row-actions">
                          <Link to={`/counties/${observation.countyFipsCode}`}>View</Link>
                          <Link to={`/compare?left=${observation.countyFipsCode}`}>Compare</Link>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}
      </section>

      <ProjectStory />
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

function SummaryTileSkeleton() {
  return (
    <div>
      <span className="summary-label">
        <span className="skeleton skeleton-text skeleton-sm" />
      </span>
      <strong>
        <span className="skeleton skeleton-text" />
      </strong>
    </div>
  );
}

function ChartSkeleton() {
  return (
    <div className="chart-card" aria-busy="true">
      <div className="chart-head">
        <h3>Top counties</h3>
        <p>Loading the selected metric across all Michigan counties…</p>
      </div>
      <div className="bar-rows">
        {Array.from({ length: 8 }).map((_, index) => (
          <div className="bar-row skeleton-bar-row" key={index}>
            <span className="skeleton skeleton-text skeleton-sm" />
            <span className="skeleton skeleton-text" />
            <div className="bar-track">
              <div className="skeleton skeleton-fill" style={{ width: `${90 - index * 9}%` }} />
            </div>
            <span className="skeleton skeleton-text skeleton-sm" />
          </div>
        ))}
      </div>
    </div>
  );
}

function TableSkeleton() {
  return (
    <div className="table-wrap" aria-busy="true">
      <div className="table-skeleton">
        {Array.from({ length: 8 }).map((_, index) => (
          <div className="table-skeleton-row" key={index}>
            <span className="skeleton skeleton-text" />
          </div>
        ))}
      </div>
    </div>
  );
}
