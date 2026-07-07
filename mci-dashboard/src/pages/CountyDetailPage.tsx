import * as React from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { ArrowLeft, ArrowLeftRight, Database } from "lucide-react";
import {
  fetchCountyDetail,
  NotFoundError,
  type CountyDetail,
  type DetailMetric
} from "../api/countyInsightsClient";
import { formatMetricValue, formatMarginOfError } from "../lib/formatMetricValue";

const primaryMetricCodes = [
  "population",
  "median_household_income",
  "poverty_rate",
  "median_home_value"
];

type LoadState =
  | { status: "loading" }
  | { status: "not-found" }
  | { status: "error"; message: string }
  | { status: "loaded"; detail: CountyDetail };

export default function CountyDetailPage() {
  const { fips = "" } = useParams();
  const [searchParams] = useSearchParams();
  const release = searchParams.get("release");
  const [state, setState] = React.useState<LoadState>({ status: "loading" });

  const load = React.useCallback(() => {
    let isActive = true;
    setState({ status: "loading" });

    fetchCountyDetail(fips, release)
      .then((detail) => {
        if (isActive) {
          setState({ status: "loaded", detail });
        }
      })
      .catch((error) => {
        if (!isActive) {
          return;
        }
        if (error instanceof NotFoundError) {
          setState({ status: "not-found" });
        } else {
          setState({
            status: "error",
            message: error instanceof Error ? error.message : "Unable to load county detail."
          });
        }
      });

    return () => {
      isActive = false;
    };
  }, [fips, release]);

  React.useEffect(() => load(), [load]);

  const compareHref = `/compare?left=${fips}${release ? `&release=${release}` : ""}`;

  return (
    <div className="app-shell detail-page">
      <div className="demo-topbar">
        <Link className="back-link" to="/demo">
          <ArrowLeft size={16} aria-hidden="true" />
          Back to county metrics
        </Link>
      </div>

      {state.status === "loading" && <DetailSkeleton />}

      {state.status === "not-found" && (
        <div className="empty-state">
          <h1>County not found</h1>
          <p>We couldn&apos;t find a Michigan county for FIPS code {fips}.</p>
          <Link className="button solid" to="/demo">
            Back to county metrics
          </Link>
        </div>
      )}

      {state.status === "error" && (
        <div className="empty-state">
          <h1>Something went wrong</h1>
          <p>{state.message}</p>
          <button className="button solid" type="button" onClick={load}>
            Retry
          </button>
        </div>
      )}

      {state.status === "loaded" && (
        <DetailContent detail={state.detail} compareHref={compareHref} />
      )}
    </div>
  );
}

function DetailContent({ detail, compareHref }: { detail: CountyDetail; compareHref: string }) {
  const { county, release, metrics, lastSuccessfulImportAtUtc } = detail;
  const byCode = new Map(metrics.map((metric) => [metric.code, metric]));
  const snapshot = primaryMetricCodes
    .map((code) => byCode.get(code))
    .filter((metric): metric is DetailMetric => metric !== undefined);

  const categories = groupByCategory(metrics);
  const hasData = metrics.some((metric) => metric.isAvailable);

  return (
    <>
      <header className="app-header detail-header">
        <div>
          <p className="eyebrow">Michigan · FIPS {county.fips}</p>
          <h1>{county.name} County</h1>
        </div>
        <div className="detail-header-meta">
          <div className="status-pill">
            <Database size={16} aria-hidden="true" />
            <span>{release.displayName}</span>
          </div>
          <Link className="button solid" to={compareHref}>
            <ArrowLeftRight size={16} aria-hidden="true" />
            Compare this county
          </Link>
        </div>
      </header>

      {!hasData ? (
        <div className="alert">Data unavailable for this release.</div>
      ) : (
        <section className="snapshot-cards" aria-label="Key measures">
          {snapshot.map((metric) => (
            <article className="snapshot-card" key={metric.code}>
              <span className="snapshot-label">{metric.displayName}</span>
              <strong className="snapshot-value">
                {metric.isAvailable && metric.estimateValue !== null
                  ? formatMetricValue(metric.estimateValue, metric)
                  : "Unavailable"}
              </strong>
              <span className="snapshot-meta">
                {metric.isAvailable && metric.marginOfError !== null
                  ? `± ${formatMarginOfError(metric.marginOfError, metric)} MOE`
                  : "No margin of error"}
              </span>
              <span className="snapshot-meta">
                {release.periodStartYear}–{release.periodEndYear}
              </span>
            </article>
          ))}
        </section>
      )}

      <section className="metric-profile" aria-label="County metric profile">
        <h2>County metric profile</h2>
        {categories.map(([category, categoryMetrics]) => (
          <div className="metric-group" key={category}>
            <h3>{category}</h3>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Metric</th>
                    <th>Estimate</th>
                    <th>MOE</th>
                    <th>Release</th>
                  </tr>
                </thead>
                <tbody>
                  {categoryMetrics.map((metric) => (
                    <tr key={metric.code}>
                      <td>{metric.displayName}</td>
                      <td>
                        {metric.isAvailable && metric.estimateValue !== null
                          ? formatMetricValue(metric.estimateValue, metric)
                          : "Unavailable"}
                      </td>
                      <td>{formatMarginOfError(metric.marginOfError, metric)}</td>
                      <td>
                        {release.periodStartYear}–{release.periodEndYear}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        ))}
      </section>

      <p className="data-note">
        ACS 5-Year estimates represent a rolling five-year period. Values on this page are from the
        selected release and should not be read as a single-year measurement.
        {lastSuccessfulImportAtUtc &&
          ` Last successful import: ${new Date(lastSuccessfulImportAtUtc).toLocaleDateString()}.`}
      </p>
    </>
  );
}

function groupByCategory(metrics: DetailMetric[]): [string, DetailMetric[]][] {
  const groups = new Map<string, DetailMetric[]>();
  for (const metric of metrics) {
    const existing = groups.get(metric.category);
    if (existing) {
      existing.push(metric);
    } else {
      groups.set(metric.category, [metric]);
    }
  }
  return [...groups.entries()];
}

function DetailSkeleton() {
  return (
    <div aria-busy="true">
      <div className="app-header detail-header">
        <div>
          <span className="skeleton skeleton-text skeleton-sm" />
          <span className="skeleton skeleton-text skeleton-title" />
        </div>
      </div>
      <section className="snapshot-cards">
        {Array.from({ length: 4 }).map((_, index) => (
          <article className="snapshot-card" key={index}>
            <span className="skeleton skeleton-text skeleton-sm" />
            <span className="skeleton skeleton-text skeleton-title" />
            <span className="skeleton skeleton-text skeleton-sm" />
          </article>
        ))}
      </section>
    </div>
  );
}
