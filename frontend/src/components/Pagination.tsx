interface Props {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  disabled: boolean;
  onPage: (page: number) => void;
  onPageSize: (size: number) => void;
}

const PAGE_SIZES = [5, 10, 20, 50];

/** Page numbers to show: first, last, and a window around the current page, with "…" for gaps. */
export function pageWindow(current: number, total: number): (number | "…")[] {
  const wanted = new Set<number>([1, total, current - 1, current, current + 1]);
  const pages = [...wanted].filter((p) => p >= 1 && p <= total).sort((a, b) => a - b);
  const out: (number | "…")[] = [];
  pages.forEach((p, i) => {
    if (i > 0 && p - pages[i - 1] > 1) out.push("…");
    out.push(p);
  });
  return out;
}

export default function Pagination({ page, pageSize, totalCount, totalPages, disabled, onPage, onPageSize }: Props) {
  const first = (page - 1) * pageSize + 1;
  const last = Math.min(page * pageSize, totalCount);

  return (
    <nav className="pagination" aria-label="Pagination">
      <span className="muted">
        Showing {first}–{last} of {totalCount}
      </span>

      <div className="pages">
        <button type="button" disabled={disabled || page <= 1} onClick={() => onPage(page - 1)}>
          ‹ Prev
        </button>
        {pageWindow(page, totalPages).map((p, i) =>
          p === "…" ? (
            <span key={`gap-${i}`} className="muted">
              …
            </span>
          ) : (
            <button
              key={p}
              type="button"
              className={p === page ? "current" : "secondary"}
              aria-current={p === page ? "page" : undefined}
              disabled={disabled}
              onClick={() => onPage(p)}
            >
              {p}
            </button>
          ),
        )}
        <button type="button" disabled={disabled || page >= totalPages} onClick={() => onPage(page + 1)}>
          Next ›
        </button>
      </div>

      <label className="page-size">
        Per page{" "}
        <select value={pageSize} disabled={disabled} onChange={(e) => onPageSize(Number(e.target.value))}>
          {PAGE_SIZES.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
      </label>
    </nav>
  );
}
