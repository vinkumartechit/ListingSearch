import type { Filters, SearchResponse } from "./types";

/** An error from the API. `fieldErrors` is populated for 400 validation problems. */
export class ApiError extends Error {
  readonly status: number;
  readonly fieldErrors: Record<string, string[]>;

  constructor(message: string, status: number, fieldErrors: Record<string, string[]> = {}) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

async function toApiError(res: Response): Promise<ApiError> {
  try {
    const body = await res.json();
    return new ApiError(body.title ?? `Request failed (${res.status})`, res.status, body.errors ?? {});
  } catch {
    return new ApiError(`Request failed (${res.status})`, res.status);
  }
}

export async function searchListings(
  filters: Filters,
  page: number,
  pageSize: number,
  signal?: AbortSignal,
): Promise<SearchResponse> {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(filters)) {
    if (value.trim() !== "") params.set(key, value.trim());
  }
  params.set("page", String(page));
  params.set("pageSize", String(pageSize));

  const res = await fetch(`/api/listings/search?${params}`, { signal });
  if (!res.ok) throw await toApiError(res);
  return (await res.json()) as SearchResponse;
}

export async function fetchCities(signal?: AbortSignal): Promise<string[]> {
  const res = await fetch("/api/cities", { signal });
  if (!res.ok) throw await toApiError(res);
  return (await res.json()) as string[];
}
