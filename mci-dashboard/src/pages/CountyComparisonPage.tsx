import * as React from "react";
import { Link, useSearchParams } from "react-router-dom";
import { ArrowLeft, ArrowLeftRight } from "lucide-react";
import {
  fetchComparison,
  fetchCounties,
  type CountyComparison,
  type CountyOption,
  type ComparisonMetric
} from "../api/countyInsightsClient";
import { formatMetricValue, formatMarginOfError } from "../lib/formatMetricValue";
import { formatDifference } from "../lib/formatDifference";

const primaryMetricCodes = [
  "population",
  "median_household_income",
  "poverty_rate",
  "median_home_value"
];

type ComparisonState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "error"; message: string }
  | { status: "loaded"; comparison: CountyComparison };

export default function CountyComparisonPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const left = searchParams.get("left") ?? "";
  const right = searchParams.get("right") ?? "";
  const release = searchParams.get("release");

  const [counties, setCounties] = React.useState<CountyOption[]>([]);
  const [state, setState] = React.useState<ComparisonState>({ status: "idle" });

  React.useEffect(() => {
    let isActive = true;
    fetchCounties()
      .then((data) => isActive && setCounties(data))
      .catch(() => undefined);
    return () => {
      isActive = false;
    };
  }, []);

  const sameCounty = left !== "" && left === right;

  const load = React.useCallback(() => {
    if (!left || !right || sameCounty) {
      setState({ status: "idle" });
      return;
    }

    let isActive = true;
    setState({ status: "loading" });

    fetchComparison(left, right, release)
      .then((comparison) => isActive && setState({ status: "loaded", comparison }))
      .catch((error) =>
        isActive &&
        setState({
          status: "error",
          message: error instanceof Error ? error.message : "Unable to load comparison."
        })
      );

    return () => {
      isActive = false;
    };
  }, [left, right, release, sameCounty]);

  React.useEffect(() => load(), [load]);

  function updateSelection(side: "left" | "right", value: string) {
    const next = new URLSearchParams(searchParams);
    if (value) {
      next.set(side, value);
    } else {
      next.delete(side);
    }
    setSearchParams(next);
  }

  function swap() {
    if (!left && !right) {
      return;
    }
    const next = new URLSearchParams(searchParams);
    next.set("left", right);
    next.set("right", left);
    setSearchParams(next);
  }

  return (
    <div className="app-shell compare-page">
      <div className="demo-topbar">
        <Link className="back-link" to="/demo">
          <ArrowLeft size={16} aria-hidden="true" />
          Back to county metrics
        </Link>
      </div>

      <header className="app-header">
        <div>
          <p className="eyebrow">Michigan County Insights · Compare</p>
          <h1>County comparison</h1>
        </div>
      </header>

      <section className="compare-controls" aria-label="Select counties">
        <label>
          <span>Left county</span>
          <select value={left} onChange={(event) => updateSelection("left", event.target.value)}>
            <option value="">Select a county</option>
            {counties.map((county) => (
              <option key={county.fipsCode} value={county.fipsCode}>
                {county.name}
              </option>
            ))}
          </select>
        </label>

        <button className="icon-button swap-button" type="button" onClick={swap} title="Swap counties">
          <ArrowLeftRight size={18} aria-hidden="true" />
          <span>Swap</span>
        </button>

        <label>
          <span>Right county</span>
          <select value={right} onChange={(event) => updateSelection("right", event.target.value)}>
            <option value="">Select a county</option>
            {counties.map((county) => (
              <option key={county.fipsCode} value={county.fipsCode}>
                {county.name}
              </option>
            ))}
          </select>
        </label>
      </section>

      {sameCounty && (
        <div className="alert">A county cannot be compared with itself. Choose two different counties.</div>
      )}

      {!sameCounty && (!left || !right) && (
        <div className="empty-state subtle">
          <p>Select two Michigan counties to compare their current metrics side by side.</p>
        </div>
      )}

      {state.status === "loading" && <ComparisonSkeleton />}

      {state.status === "error" && (
        <div className="empty-state">
          <p>{state.message}</p>
          <button className="button solid" type="button" onClick={load}>
            Retry
          </button>
        </div>
      )}

      {state.status === "loaded" && <ComparisonContent comparison={state.comparison} />}
    </div>
  );
}

function ComparisonContent({ comparison }: { comparison: CountyComparison }) {
  const { leftCounty, rightCounty, release, metrics } = comparison;
  const byCode = new Map(metrics.map((metric) => [metric.code, metric]));
  const summary = primaryMetricCodes
    .map((code) => byCode.get(code))
    .filter((metric): metric is ComparisonMetric => metric !== undefined);

  return (
    <>
      <div className="compare-summary-head">
        <h2>
          {leftCounty.name} County vs. {rightCounty.name} County
        </h2>
        <p>
          {release.displayName} · Difference: {leftCounty.name} minus {rightCounty.name}
        </p>
      </div>

      <section className="summary-grid compare-cards" aria-label="Key differences">
        {summary.map((metric) => (
          <article className="snapshot-card" key={metric.code}>
            <span className="snapshot-label">{metric.displayName}</span>
            <div className="compare-values">
              <span>
                {leftCounty.name}:{" "}
                {metric.leftValue !== null ? formatMetricValue(metric.leftValue, metric) : "Unavailable"}
              </span>
              <span>
                {rightCounty.name}:{" "}
                {metric.rightValue !== null ? formatMetricValue(metric.rightValue, metric) : "Unavailable"}
              </span>
            </div>
            <strong className="snapshot-value compare-diff">{formatDifference(metric, metric)}</strong>
          </article>
        ))}
      </section>

      <section className="table-section">
        <div className="table-heading">
          <h2>All metrics</h2>
          <span>{metrics.length} metrics</span>
        </div>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Category</th>
                <th>Metric</th>
                <th>{leftCounty.name}</th>
                <th>{rightCounty.name}</th>
                <th>Difference</th>
              </tr>
            </thead>
            <tbody>
              {metrics.map((metric) => (
                <tr key={metric.code}>
                  <td>{metric.category}</td>
                  <td>{metric.displayName}</td>
                  <td>
                    {metric.leftValue !== null ? formatMetricValue(metric.leftValue, metric) : "Unavailable"}
                    {metric.leftMarginOfError !== null && (
                      <span className="cell-moe"> ± {formatMarginOfError(metric.leftMarginOfError, metric)}</span>
                    )}
                  </td>
                  <td>
                    {metric.rightValue !== null ? formatMetricValue(metric.rightValue, metric) : "Unavailable"}
                    {metric.rightMarginOfError !== null && (
                      <span className="cell-moe"> ± {formatMarginOfError(metric.rightMarginOfError, metric)}</span>
                    )}
                  </td>
                  <td>{formatDifference(metric, metric)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <p className="data-note">
          {release.displayName}. Differences are {leftCounty.name} minus {rightCounty.name}. Percentage
          metrics use percentage points.
        </p>
      </section>
    </>
  );
}

function ComparisonSkeleton() {
  return (
    <section className="summary-grid compare-cards" aria-busy="true">
      {Array.from({ length: 4 }).map((_, index) => (
        <article className="snapshot-card" key={index}>
          <span className="skeleton skeleton-text skeleton-sm" />
          <span className="skeleton skeleton-text" />
          <span className="skeleton skeleton-text skeleton-title" />
        </article>
      ))}
    </section>
  );
}
