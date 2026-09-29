import type { FormEvent } from "react";
import type { Filters } from "../types";

interface Props {
  values: Filters;
  cities: string[];
  fieldErrors: Record<string, string[]>;
  loading: boolean;
  onChange: (values: Filters) => void;
  onSubmit: () => void;
  onReset: () => void;
}

export default function SearchForm({ values, cities, fieldErrors, loading, onChange, onSubmit, onReset }: Props) {
  const set = (key: keyof Filters) => (value: string) => onChange({ ...values, [key]: value });

  const submit = (e: FormEvent) => {
    e.preventDefault();
    onSubmit();
  };

  const field = (
    key: keyof Filters,
    label: string,
    opts: { type?: string; placeholder?: string; list?: string; min?: number; step?: string } = {},
  ) => {
    const errors = fieldErrors[key];
    return (
      <label className={`field ${errors ? "has-error" : ""}`}>
        <span>{label}</span>
        <input
          type={opts.type ?? "text"}
          value={values[key]}
          placeholder={opts.placeholder}
          list={opts.list}
          min={opts.min}
          step={opts.step}
          inputMode={opts.type === "number" ? "decimal" : undefined}
          aria-invalid={errors ? true : undefined}
          onChange={(e) => set(key)(e.target.value)}
        />
        {errors?.map((msg) => (
          <small key={msg} className="field-error" role="alert">
            {msg}
          </small>
        ))}
      </label>
    );
  };

  return (
    <form className="search-form" onSubmit={submit} noValidate>
      <div className="grid">
        {field("city", "City", { list: "city-options", placeholder: "Any city" })}
        {field("minPrice", "Min price ($)", { type: "number", min: 0, step: "1000" })}
        {field("maxPrice", "Max price ($)", { type: "number", min: 0, step: "1000" })}
        {field("minBedrooms", "Min bedrooms", { type: "number", min: 0, step: "1" })}
        {field("keyword", "Keyword", { placeholder: "e.g. pool garage" })}
        {field("targetBudget", "Target budget ($)", { type: "number", min: 0, step: "1000" })}

        <label className={`field ${fieldErrors.status ? "has-error" : ""}`}>
  <span>Status</span>
  <select value={values.status} onChange={(e) => set("status")(e.target.value)}>
    <option value="">Any</option>
    <option value="active">Active</option>
    <option value="pending">Pending</option>
    <option value="sold">Sold</option>
  </select>
  {fieldErrors.status?.map((msg) => (
    <small key={msg} className="field-error" role="alert">{msg}</small>
  ))}
</label>

      </div>
      <datalist id="city-options">
        {cities.map((c) => (
          <option key={c} value={c} />
        ))}
      </datalist>
      <div className="actions">
        <button type="submit" disabled={loading}>
          {loading ? "Searching…" : "Search"}
        </button>
        <button type="button" className="secondary" onClick={onReset}>
          Reset
        </button>
      </div>
    </form>
  );
}
