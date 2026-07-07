import { StrictMode } from "react";
import * as React from "react";
import { createRoot } from "react-dom/client";
import {
  ArrowDown,
  ArrowDownUp,
  ArrowRight,
  BarChart3,
  Bug,
  Code2,
  Database,
  LineChart,
  Linkedin,
  Plug,
  RefreshCw,
  ShieldCheck,
  Users,
  Workflow,
  Wrench
} from "lucide-react";
import "./styles.css";

// Contact + source links. Set githubUrl to a public repo URL to reveal the GitHub link.
const linkedInUrl = "https://www.linkedin.com/in/josuequin/";
const githubUrl: string | null = "https://github.com/josue-quinones/michigan-county-insights";

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

function DashboardDemo() {
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
    const params = new URLSearchParams({
      metricCode: selectedMetric,
      releaseYear: String(defaultReleaseYear)
    });

    if (selectedCounty) {
      params.set("countyFipsCode", selectedCounty);
    }

    const [observationData, summaryData] = await Promise.all([
      getJson<Observation[]>(apiUrl(`/api/reporting/current-observations?${params}`)),
      getJson<Summary[]>(apiUrl(`/api/reporting/current-observations/summary?${params}`))
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
    <div className="app-shell dashboard-demo">
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

  return new Intl.NumberFormat("en-US", {
    maximumFractionDigits: decimalPlaces,
    minimumFractionDigits: decimalPlaces,
    style: unit === "Currency" ? "currency" : "decimal"
  }).format(value) + (unit === "Percentage" ? "%" : "");
}

const buildStages = [
  "Requirements",
  "Design",
  "Database",
  "API",
  "Frontend",
  "Testing",
  "Deployment"
];

const architectureStages = [
  "Census ACS API",
  "Import worker",
  "SQL Server",
  "Application services",
  "REST · GraphQL · MCP",
  "React dashboard"
];

const practices = [
  {
    icon: Workflow,
    title: "How I approach problems",
    body: "I start from the decision, not the code. Before writing anything I pinned down the grain of the data — one metric, one county, one Census release, one successful import run — because getting that wrong makes every layer above it wrong. I scope deliberately: V1 is 8 metrics and one state, not every Census variable, so the system stays coherent instead of becoming portfolio padding. I would rather ship a narrow slice that works end to end than a broad one that half-works."
  },
  {
    icon: Bug,
    title: "How I debug",
    body: "I make the system observable before I need it to be. Every import run records its status, timing, and row counts — fetched, staged, inserted, rejected — and every validation or load problem becomes a queryable import issue with a severity and a stage. So when something is off I can ask the database what failed, where, and how badly instead of guessing from a stack trace. I reproduce before I fix, and I keep raw staging data around so I can compare source values against loaded values."
  },
  {
    icon: Users,
    title: "How I work with stakeholders",
    body: "I translate business questions into system design. The dashboard exists to answer real questions — which counties grew, who has the highest income, how does Washtenaw compare to Kent — and I let those questions drive the API and schema. I am explicit about what the system does not do, so expectations are set honestly. And I flag statistical nuance a non-technical stakeholder would otherwise get wrong: ACS 5-Year releases are rolling estimates, so I refuse to label adjacent releases as year-over-year growth."
  },
  {
    icon: ShieldCheck,
    title: "How I validate data",
    body: "I never let bad or missing data silently become zero. Before any value reaches the reporting tables it passes explicit checks: every county present, every required Census variable present, values parse correctly, percentages stay within 0–100, derived denominators are greater than zero, no duplicate rows. Successful imports are immutable and retries create new runs, so history stays auditable. Large swings raise a warning for review rather than an automatic failure — because in real reporting systems, surprising-but-correct and wrong look the same until a human looks."
  }
];

const techGroups = [
  { label: "Backend", items: ["C#", ".NET 9", "ASP.NET Core", "EF Core", "SQL Server"] },
  { label: "APIs", items: ["REST", "Hot Chocolate GraphQL", "Read-only MCP"] },
  { label: "Frontend", items: ["React", "TypeScript", "Vite"] },
  { label: "Delivery", items: ["Docker", "Azure Container Apps", "Azure SQL", "Static Web Apps"] }
];

const services = [
  {
    icon: LineChart,
    title: "Reporting dashboards & BI",
    body: "Internal analytics tools that turn raw operational data into rankings, comparisons, trends, and exports the business actually uses — like the live demo below."
  },
  {
    icon: Plug,
    title: "Data integrations & ETL pipelines",
    body: "Reliable imports from external APIs and systems: staging, validation, transformation, run history, and clear failure handling instead of silent bad data."
  },
  {
    icon: Code2,
    title: "REST & GraphQL APIs",
    body: "Well-structured .NET APIs over a clean data model, with shared services so every interface — REST, GraphQL, MCP — stays consistent and easy to extend."
  },
  {
    icon: Wrench,
    title: "Internal business software",
    body: "Practical line-of-business applications and back-office tools built to fit how a team already works, without overbuilding them into an enterprise platform."
  }
];

function StageFlow({ stages }: { stages: string[] }) {
  return (
    <div className="stage-flow">
      {stages.map((stage, index) => (
        <React.Fragment key={stage}>
          <span className="stage-chip">{stage}</span>
          {index < stages.length - 1 && (
            <ArrowRight className="stage-arrow" size={16} aria-hidden="true" />
          )}
        </React.Fragment>
      ))}
    </div>
  );
}

function Hero() {
  return (
    <header className="hero">
      <div className="container hero-inner">
        <p className="availability-badge">
          <span className="availability-dot" aria-hidden="true" />
          Available for contract &amp; freelance work
        </p>
        <p className="hero-eyebrow">Josue Quinones · Software Portfolio</p>
        <h1 className="hero-title">
          I build reporting systems, integrations, APIs, and internal business software.
        </h1>
        <p className="hero-lede">
          Michigan County Insights is a working demonstration of that — a full reporting-system
          stack built end to end on real U.S. Census data. It stages raw Census values, validates
          them, loads clean county metric facts into SQL Server, and serves them through a deployed
          .NET API, GraphQL, a read-only MCP server, and the React dashboard below.
        </p>
        <div className="hero-actions">
          <a className="button primary" href="#contact">
            Work with me
            <ArrowRight size={16} aria-hidden="true" />
          </a>
          <a className="button ghost" href="#live-demo">
            View the live demo
            <ArrowDown size={16} aria-hidden="true" />
          </a>
        </div>
      </div>
    </header>
  );
}

function Services() {
  return (
    <section className="band band-tint" id="services">
      <div className="container">
        <p className="eyebrow">Work with me</p>
        <h2 className="section-title">What I build for clients</h2>
        <p className="section-lede">
          I take on contract and freelance work building reporting systems, integrations, and
          internal business software. Michigan County Insights is a self-directed demonstration of
          the same stack I bring to client projects.
        </p>
        <div className="service-grid">
          {services.map((service) => {
            const Icon = service.icon;
            return (
              <article className="service-card" key={service.title}>
                <div className="service-icon">
                  <Icon size={20} aria-hidden="true" />
                </div>
                <h3>{service.title}</h3>
                <p>{service.body}</p>
              </article>
            );
          })}
        </div>
        <a className="button primary services-cta" href="#contact">
          Start a conversation
          <ArrowRight size={16} aria-hidden="true" />
        </a>
      </div>
    </section>
  );
}

function Contact() {
  return (
    <section className="contact" id="contact">
      <div className="container contact-inner">
        <p className="eyebrow contact-eyebrow">Available for contract &amp; freelance work</p>
        <h2 className="contact-title">Have a reporting or integration project? Let&apos;s talk.</h2>
        <p className="contact-lede">
          Tell me what your team needs to see, measure, or connect — and I&apos;ll help you build
          the system that gets you there. Reach out on LinkedIn to start a conversation.
        </p>
        <a className="button primary" href={linkedInUrl} target="_blank" rel="noreferrer noopener">
          <Linkedin size={16} aria-hidden="true" />
          Message me on LinkedIn
        </a>
      </div>
    </section>
  );
}

function HowIBuild() {
  return (
    <section className="band">
      <div className="container">
        <p className="eyebrow">How I build software</p>
        <h2 className="section-title">A deliberate path from requirements to production</h2>
        <StageFlow stages={buildStages} />
        <div className="practice-grid">
          {practices.map((practice) => {
            const Icon = practice.icon;
            return (
              <article className="practice-card" key={practice.title}>
                <div className="practice-head">
                  <Icon size={18} aria-hidden="true" />
                  <h3>{practice.title}</h3>
                </div>
                <p>{practice.body}</p>
              </article>
            );
          })}
        </div>
      </div>
    </section>
  );
}

function Architecture() {
  return (
    <section className="band band-tint">
      <div className="container">
        <p className="eyebrow">Architecture</p>
        <h2 className="section-title">One connected platform, a modular monolith</h2>
        <p className="section-lede">
          The REST API, GraphQL API, and MCP tools all run over the same application services and
          the same SQL Server model — no interface recalculates rankings or comparisons on its own.
          Raw staging data is retained for troubleshooting but is never queried by the public
          surface.
        </p>
        <StageFlow stages={architectureStages} />
        <div className="tech-strip">
          {techGroups.map((group) => (
            <div className="tech-group" key={group.label}>
              <span className="tech-label">{group.label}</span>
              <div className="tech-tags">
                {group.items.map((item) => (
                  <span className="tech-tag" key={item}>
                    {item}
                  </span>
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

function DemoSection() {
  return (
    <section className="band" id="live-demo">
      <div className="container">
        <p className="eyebrow">Live demo</p>
        <h2 className="section-title">County metrics, served by the API</h2>
        <p className="section-lede">
          Real U.S. Census ACS 5-Year data for Michigan&apos;s 83 counties, loaded through the
          import pipeline and served by the deployed .NET reporting API. Filter by metric and
          county, then sort the current observations.
        </p>
      </div>
      <DashboardDemo />
    </section>
  );
}

function Footer() {
  return (
    <footer className="site-footer">
      <div className="container footer-inner">
        <p>Built by Josue Quinones · Data from the U.S. Census Bureau ACS 5-Year Detailed Tables.</p>
        <div className="footer-links">
          <a href={linkedInUrl} target="_blank" rel="noreferrer noopener">
            <Linkedin size={15} aria-hidden="true" />
            LinkedIn
          </a>
          {githubUrl && (
            <a href={githubUrl} target="_blank" rel="noreferrer noopener">
              GitHub
            </a>
          )}
        </div>
      </div>
    </footer>
  );
}

function Page() {
  return (
    <>
      <Hero />
      <main>
        <Services />
        <HowIBuild />
        <Architecture />
        <DemoSection />
        <Contact />
      </main>
      <Footer />
    </>
  );
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <Page />
  </StrictMode>
);
