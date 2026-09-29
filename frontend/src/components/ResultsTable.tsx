import type { Listing } from "../types";

const money = new Intl.NumberFormat("en-US", { style: "currency", currency: "USD", maximumFractionDigits: 0 });

function scoreTitle(l: Listing): string {
  const { budgetFit, recency } = l.scoreBreakdown;
  const parts = [budgetFit === null ? "budget fit: n/a (no target budget)" : `budget fit: ${budgetFit.toFixed(3)}`];
  parts.push(`recency: ${recency.toFixed(3)}`);
  return parts.join(" · ");
}

export default function ResultsTable({ items }: { items: Listing[] }) {
  return (
    <div className="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Address</th>
            <th className="num">Price</th>
            <th className="num">Beds</th>
            <th className="num">Baths</th>
            <th>Listed</th>
            <th>Status</th>
            <th className="num">Relevance</th>
          </tr>
        </thead>
        <tbody>
          {items.map((l) => (
            <tr key={`${l.source}:${l.id}`}>
              <td>
                <div>{l.address}</div>
                <div className="muted">
                  {l.city}, {l.state} {l.zip} · {l.source}
                </div>
              </td>
              <td className="num">{money.format(l.price)}</td>
              <td className="num">{l.bedrooms}</td>
              <td className="num">{l.bathrooms}</td>
              <td>{l.listedDate}</td>
              <td>
                <span className={`badge ${l.status}`}>{l.status}</span>
              </td>
              <td className="num" title={scoreTitle(l)}>
                <strong>{l.score.toFixed(3)}</strong>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
