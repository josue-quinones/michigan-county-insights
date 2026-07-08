// Generates public/demo-snapshot.json from the live reporting API at deploy time.
// The demo dashboard then paints from this static file (served off the CDN),
// avoiding the API/DB cold-start latency on first load. Non-fatal: if the API is
// unreachable, the deploy proceeds without a snapshot and the app falls back to
// the live API.

import { writeFile } from "node:fs/promises";

const apiBase = (process.env.VITE_API_BASE_URL ?? "").replace(/\/$/, "");
const releaseYear = process.env.SNAPSHOT_RELEASE_YEAR ?? "2024";
const outputUrl = new URL("../public/demo-snapshot.json", import.meta.url);

if (!apiBase) {
  console.warn("[snapshot] VITE_API_BASE_URL is not set; skipping snapshot generation.");
  process.exit(0);
}

async function getJson(path, { retries = 5, timeoutMs = 120000 } = {}) {
  const url = `${apiBase}${path}`;

  for (let attempt = 1; attempt <= retries; attempt += 1) {
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), timeoutMs);
    try {
      const response = await fetch(url, { signal: controller.signal });
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }
      return await response.json();
    } catch (error) {
      // The API and DB may be cold on the first calls; back off and retry.
      console.warn(`[snapshot] ${path} attempt ${attempt}/${retries} failed: ${error.message}`);
      if (attempt === retries) {
        throw error;
      }
      await new Promise((resolve) => setTimeout(resolve, 5000 * attempt));
    } finally {
      clearTimeout(timer);
    }
  }

  throw new Error("unreachable");
}

try {
  console.log(`[snapshot] Generating from ${apiBase} (release ${releaseYear})`);

  const [metrics, counties] = await Promise.all([
    getJson("/api/reporting/metrics"),
    getJson("/api/reporting/counties"),
  ]);

  const observations = {};
  const summaries = {};

  // Sequential to stay gentle on the single small API replica.
  for (const metric of metrics) {
    const params = `metricCode=${encodeURIComponent(metric.code)}&releaseYear=${releaseYear}`;
    observations[metric.code] = await getJson(`/api/reporting/current-observations?${params}`);
    summaries[metric.code] = await getJson(`/api/reporting/current-observations/summary?${params}`);
  }

  const snapshot = {
    generatedAtUtc: new Date().toISOString(),
    releaseYear: Number(releaseYear),
    metrics,
    counties,
    observations,
    summaries,
  };

  await writeFile(outputUrl, JSON.stringify(snapshot));
  console.log(
    `[snapshot] Wrote demo-snapshot.json: ${metrics.length} metrics, ${counties.length} counties, ` +
      `${Object.keys(observations).length} observation sets.`
  );
} catch (error) {
  console.warn(
    `[snapshot] Generation failed; deploying without a snapshot (app falls back to the live API): ${error.message}`
  );
  process.exit(0);
}
