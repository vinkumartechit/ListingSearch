export interface ScoreBreakdown {
  budgetFit: number | null;
  recency: number;
}

export interface Listing {
  id: string;
  source: string;
  address: string;
  city: string;
  state: string;
  zip: string;
  price: number;
  bedrooms: number;
  bathrooms: number;
  sqft: number;
  listedDate: string;
  status: string;
  description: string;
  score: number;
  scoreBreakdown: ScoreBreakdown;
}

export interface SearchResponse {
  items: Listing[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

/** The filter inputs, kept as raw strings exactly as typed so the server is the single source of validation. */
export interface Filters {
  minPrice: string;
  maxPrice: string;
  minBedrooms: string;
  city: string;
  keyword: string;
  targetBudget: string;
  status: string;
}

export const emptyFilters: Filters = {
  minPrice: "",
  maxPrice: "",
  minBedrooms: "",
  city: "",
  keyword: "",
  targetBudget: "",
  status: "",
};
