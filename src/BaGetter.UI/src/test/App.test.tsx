import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router";
import { SearchPage } from "@/pages/SearchPage";

function renderWithProviders(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>{ui}</MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("SearchPage", () => {
  it("renders the search filters", () => {
    renderWithProviders(<SearchPage />);
    expect(screen.getByText("Include prerelease")).toBeInTheDocument();
  });

  it("renders package type filter options", () => {
    renderWithProviders(<SearchPage />);
    expect(screen.getByText("Any Type")).toBeInTheDocument();
    expect(screen.getByText(".NET tool")).toBeInTheDocument();
  });
});
