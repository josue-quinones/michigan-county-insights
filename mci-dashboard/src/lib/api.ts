// Shared API base-url resolution and fetch helper. In production VITE_API_BASE_URL
// points at the deployed API; locally it is empty and Vite proxies /api to :5087.

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? "").replace(/\/$/, "");

export function apiUrl(path: string): string {
  return `${apiBaseUrl}${path}`;
}

export async function getJson<T>(url: string): Promise<T> {
  const response = await fetch(url);

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status} ${response.statusText}`);
  }

  return response.json() as Promise<T>;
}
