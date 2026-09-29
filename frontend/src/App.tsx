import { useEffect, useState } from "react";
import { ApiError, fetchCities, searchListings } from "./api";
import Pagination from "./components/Pagination";
import ResultsTable from "./components/ResultsTable";
import SearchForm from "./components/SearchForm";
import { emptyFilters, type Filters, type SearchResponse } from "./types";

type Status = "loading" | "success" | "error";

export default function App() {
  const [draft, setDraft] = useState<Filters>(emptyFilters); // what's typed in the form
  const [applied, setApplied] = useState<Filters>(emptyFilters); // what the current results are for
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [retryKey, setRetryKey] = useState(0);

  const [status, setStatus] = useState<Status>("loading");
  const [data, setData] = useState<SearchResponse | null>(null);
  const [error, setError] = useState<ApiError | Error | null>(null);
  const [cities, setCities] = useState<string[]>([]);

  useEffect(() => {
    const ctrl = new AbortController();
    fetchCities(ctrl.signal).then(setCities).catch(() => {
      /* autocomplete is a nicety; the search still works without it */
    });
    return () => ctrl.abort();
  }, []);

  useEffect(() => {
    const ctrl = new AbortController(); // cancels stale requests if the user changes filters/pages quickly
    setStatus("loading");
    searchListings(applied, page, pageSize, ctrl.signal)
      .then((res) => {
        setData(res);
        setError(null);
        setStatus("success");
      })
      .catch((err: unknown) => {
        if (err instanceof DOMException && err.name === "AbortError") return;
        setError(err instanceof Error ? err : new Error(String(err)));
        setStatus("error");
      });
    return () => ctrl.abort();
  }, [applied, page, pageSize, retryKey]);

  const submit = () => {
    setApplied(draft);
    setPage(1);
    setRetryKey((k) => k + 1); // re-run even if the filters are unchanged
  };

  const reset = () => {
    setDraft(emptyFilters);
    setApplied(emptyFilters);
    setPage(1);
  };

  const apiError = error instanceof ApiError ? error : null;
  const fieldErrors = apiError?.fieldErrors ?? {};
  const hasFieldErrors = Object.keys(fieldErrors).length > 0;
  const isNetworkError = status === "error" && !apiError;
  const loading = status === "loading";

  return (
    <main>
      <h1>Listing Search</h1>

      <SearchForm
        values={draft}
        cities={cities}
        fieldErrors={status === "error" ? fieldErrors : {}}
        loading={loading}
        onChange={setDraft}
        onSubmit={submit}
        onReset={reset}
      />

      {status === "error" && (
        <div className="banner error" role="alert">
          <strong>
            {hasFieldErrors
              ? "Please fix the highlighted fields."
              : isNetworkError
                ? "Couldn't reach the server."
                : (apiError?.message ?? "Something went wrong.")}
          </strong>
          {isNetworkError && <div className="muted">Check that the API is running on http://localhost:5080.</div>}
          {!hasFieldErrors && (
            <button type="button" className="secondary" onClick={() => setRetryKey((k) => k + 1)}>
              Try again
            </button>
          )}
          {hasFieldErrors && page > 1 && fieldErrors.page && (
            <button type="button" className="secondary" onClick={() => setPage(1)}>
              Back to page 1
            </button>
          )}
        </div>
      )}

      {loading && !data && <p className="status">Loading listings…</p>}

      {data && status !== "error" && (
        <section aria-busy={loading}>
          {data.totalCount === 0 ? (
            <div className="banner empty">
              <strong>No listings match your filters.</strong>
              <div className="muted">Try widening the price range or removing the keyword.</div>
            </div>
          ) : (
            <>
              {applied.targetBudget.trim() === "" && (
                <p className="muted">No target budget set, so results are ranked by recency only.</p>
              )}
              <div className={loading ? "dim" : undefined}>
                <ResultsTable items={data.items} />
              </div>
              <Pagination
                page={data.page}
                pageSize={data.pageSize}
                totalCount={data.totalCount}
                totalPages={data.totalPages}
                disabled={loading}
                onPage={setPage}
                onPageSize={(s) => {
                  setPageSize(s);
                  setPage(1);
                }}
              />
            </>
          )}
        </section>
      )}
    </main>
  );
}
