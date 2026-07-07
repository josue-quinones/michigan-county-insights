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
  Code2,
  LineChart,
  Linkedin,
  Plug,
  Wrench
} from "lucide-react";
import "./styles.css";

const DemoPage = React.lazy(() => import("./DemoPage"));
const CountyDetailPage = React.lazy(() => import("./pages/CountyDetailPage"));
const CountyComparisonPage = React.lazy(() => import("./pages/CountyComparisonPage"));

// Contact + source links. Set githubUrl to a public repo URL to reveal the GitHub link.
const linkedInUrl = "https://www.linkedin.com/in/josuequin/";
const githubUrl: string | null = "https://github.com/josue-quinones/michigan-county-insights";

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
          Michigan County Insights is a working demonstration — a full reporting-system stack built
          end to end on real U.S. Census data, served through a deployed .NET API and an interactive
          React dashboard.
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

function DemoTeaser() {
  return (
    <section className="band" id="live-demo">
      <div className="container demo-teaser">
        <div className="demo-teaser-copy">
          <p className="eyebrow">Live demo</p>
          <h2 className="section-title">Explore the county metrics dashboard</h2>
          <p className="section-lede">
            Real U.S. Census ACS 5-Year data for Michigan&apos;s 83 counties, served by the deployed
            .NET reporting API. Rank counties for any metric, open a county&apos;s detail, and
            compare two counties side by side — plus the full architecture and engineering approach.
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

// Load Microsoft Clarity only when a project id is configured (production build).
// Keeps local dev free of tracking and avoids a broken script tag when unset.
function useClarity() {
  React.useEffect(() => {
    const projectId = import.meta.env.VITE_CLARITY_PROJECT_ID;
    if (!projectId || document.getElementById("mci-clarity")) {
      return;
    }

    const script = document.createElement("script");
    script.id = "mci-clarity";
    script.type = "text/javascript";
    script.text = `(function(c,l,a,r,i,t,y){c[a]=c[a]||function(){(c[a].q=c[a].q||[]).push(arguments)};t=l.createElement(r);t.async=1;t.src="https://www.clarity.ms/tag/"+i;y=l.getElementsByTagName(r)[0];y.parentNode.insertBefore(t,y);})(window,document,"clarity","script","${projectId}");`;
    document.head.appendChild(script);
  }, []);
}

function RouteFallback({ label }: { label: string }) {
  return (
    <div className="route-fallback" role="status">
      {label}
    </div>
  );
}

function App() {
  useClarity();

  return (
    <BrowserRouter>
      <ScrollManager />
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route
          path="/demo"
          element={
            <Suspense fallback={<RouteFallback label="Loading the live demo…" />}>
              <DemoPage />
            </Suspense>
          }
        />
        <Route
          path="/counties/:fips"
          element={
            <Suspense fallback={<RouteFallback label="Loading county detail…" />}>
              <CountyDetailPage />
            </Suspense>
          }
        />
        <Route
          path="/compare"
          element={
            <Suspense fallback={<RouteFallback label="Loading comparison…" />}>
              <CountyComparisonPage />
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
