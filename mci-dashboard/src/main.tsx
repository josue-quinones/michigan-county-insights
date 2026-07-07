import { StrictMode, Suspense } from "react";
import * as React from "react";
import { createRoot } from "react-dom/client";
import {
  BrowserRouter,
  Link,
  Route,
  Routes,
  useLocation
} from "react-router-dom";
import {
  ArrowRight,
  BarChart3,
  Bug,
  Code2,
  LineChart,
  Linkedin,
  Plug,
  ShieldCheck,
  Users,
  Workflow,
  Wrench
} from "lucide-react";
import "./styles.css";

const DemoPage = React.lazy(() => import("./DemoPage"));

// Contact + source links. Set githubUrl to a public repo URL to reveal the GitHub link.
const linkedInUrl = "https://www.linkedin.com/in/josuequin/";
const githubUrl: string | null = "https://github.com/josue-quinones/michigan-county-insights";

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
    body: "Internal analytics tools that turn raw operational data into rankings, comparisons, trends, and exports the business actually uses — like the live demo."
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
          .NET API, GraphQL, a read-only MCP server, and an interactive React dashboard.
        </p>
        <div className="hero-actions">
          <a className="button primary" href="#contact">
            Work with me
            <ArrowRight size={16} aria-hidden="true" />
          </a>
          <Link className="button ghost" to="/demo">
            Open the live demo
            <ArrowRight size={16} aria-hidden="true" />
          </Link>
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

function DemoTeaser() {
  return (
    <section className="band" id="live-demo">
      <div className="container demo-teaser">
        <div className="demo-teaser-copy">
          <p className="eyebrow">Live demo</p>
          <h2 className="section-title">Explore the county metrics dashboard</h2>
          <p className="section-lede">
            Real U.S. Census ACS 5-Year data for Michigan&apos;s 83 counties, loaded through the
            import pipeline and served by the deployed .NET reporting API. Rank counties for any
            metric, focus a single county, and read the current observations — charts and table,
            straight off the live API.
          </p>
          <Link className="button primary" to="/demo">
            <BarChart3 size={16} aria-hidden="true" />
            Open the live demo
          </Link>
        </div>
        <div className="demo-teaser-visual" aria-hidden="true">
          <div className="teaser-bar" style={{ width: "94%" }} />
          <div className="teaser-bar" style={{ width: "78%" }} />
          <div className="teaser-bar" style={{ width: "63%" }} />
          <div className="teaser-bar" style={{ width: "51%" }} />
          <div className="teaser-bar" style={{ width: "44%" }} />
          <div className="teaser-bar" style={{ width: "32%" }} />
        </div>
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

function HomePage() {
  return (
    <>
      <Hero />
      <main>
        <Services />
        <HowIBuild />
        <Architecture />
        <DemoTeaser />
        <Contact />
      </main>
      <Footer />
    </>
  );
}

function ScrollManager() {
  const { pathname, hash } = useLocation();

  React.useEffect(() => {
    if (hash) {
      document.getElementById(hash.slice(1))?.scrollIntoView({ behavior: "smooth" });
      return;
    }

    window.scrollTo({ top: 0 });
  }, [pathname, hash]);

  return null;
}

function DemoFallback() {
  return (
    <div className="route-fallback" role="status">
      Loading the live demo…
    </div>
  );
}

function App() {
  return (
    <BrowserRouter>
      <ScrollManager />
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route
          path="/demo"
          element={
            <Suspense fallback={<DemoFallback />}>
              <DemoPage />
            </Suspense>
          }
        />
      </Routes>
    </BrowserRouter>
  );
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <App />
  </StrictMode>
);
