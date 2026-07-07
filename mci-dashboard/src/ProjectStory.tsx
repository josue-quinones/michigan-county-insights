import * as React from "react";
import { ArrowRight, Bug, ShieldCheck, Users, Workflow } from "lucide-react";

// The "about this project" depth — deliberate-path flow, engineering philosophy,
// architecture, and stack. Lives on /demo (not the homepage), where visitors who
// want the technical story go looking for it.

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
    body: "I start from the decision, not the code — pinning down the grain of the data before building on top of it. I scope deliberately: a narrow slice that works end to end beats a broad one that half-works."
  },
  {
    icon: Bug,
    title: "How I debug",
    body: "I make the system observable before I need it to be. Every import run records its status, counts, and issues, so I can ask the database what failed and where instead of guessing from a stack trace."
  },
  {
    icon: Users,
    title: "How I work with stakeholders",
    body: "I translate business questions into system design and I'm explicit about what the system does not do. I flag statistical nuance a non-technical stakeholder would otherwise get wrong."
  },
  {
    icon: ShieldCheck,
    title: "How I validate data",
    body: "Bad or missing data never silently becomes zero. Values pass explicit checks before reaching the reporting tables, successful imports are immutable, and large swings raise a warning for review."
  }
];

const techGroups = [
  { label: "Backend", items: ["C#", ".NET 9", "ASP.NET Core", "EF Core", "SQL Server"] },
  { label: "APIs", items: ["REST", "Hot Chocolate GraphQL", "Read-only MCP"] },
  { label: "Frontend", items: ["React", "TypeScript", "Vite"] },
  { label: "Delivery", items: ["Docker", "Azure Container Apps", "Azure SQL", "Static Web Apps"] }
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

export function ProjectStory() {
  return (
    <section className="project-story" aria-label="About this project">
      <div className="band">
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

      <div className="band band-tint">
        <p className="eyebrow">Architecture</p>
        <h2 className="section-title">One connected platform, a modular monolith</h2>
        <p className="section-lede">
          The REST API, GraphQL API, and MCP tools all run over the same application services and the
          same SQL Server model — no interface recalculates rankings or comparisons on its own. Raw
          staging data is retained for troubleshooting but is never queried by the public surface.
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
