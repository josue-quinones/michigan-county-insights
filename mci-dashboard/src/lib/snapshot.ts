import type { Metric, County, Observation, Summary } from "./demoTypes";

// A pre-generated snapshot of the demo's reporting data, produced at deploy time
// and served as a static file from the Static Web App CDN. It lets the demo paint
// instantly without waking the scale-to-zero API or the auto-paused serverless
// database. The live API is still used for Refresh and the detail/comparison views.

export type DemoSnapshot = {
  generatedAtUtc: string;
  releaseYear: number;
  metrics: Metric[];
  counties: County[];
  observations: Record<string, Observation[]>;
  summaries: Record<string, Summary[]>;
};

const SNAPSHOT_URL = "/demo-snapshot.json";

/**
 * Load the static demo snapshot. Returns null when it is absent (e.g. local dev,
 * or a deploy where generation was skipped) so callers fall back to the live API.
 */
export async function loadDemoSnapshot(): Promise<DemoSnapshot | null> {
  try {
    const response = await fetch(SNAPSHOT_URL, { cache: "no-store" });
    if (!response.ok) {
      return null;
    }

    const data = (await response.json()) as DemoSnapshot;
    if (!Array.isArray(data?.metrics) || !data?.observations) {
      return null;
    }

    return data;
  } catch {
    return null;
  }
}
