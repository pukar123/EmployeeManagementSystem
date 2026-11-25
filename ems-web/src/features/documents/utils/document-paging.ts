/** Rows-per-page choices for document lists (the server allows 1 to 100). */
export const DOCUMENT_PAGE_SIZES = [25, 50, 100] as const;

export const DEFAULT_DOCUMENT_PAGE_SIZE = 25;

/** True when the API refused the request (403), so the UI shows its permission message instead of an error. */
export function isForbiddenError(error: unknown): boolean {
  return (error as { response?: { status?: number } } | null)?.response?.status === 403;
}

/** Previous/Next state for a page of `totalPages` (at least 1). */
export function pagingState(page: number, totalPages: number): {
  showPaging: boolean;
  canGoBack: boolean;
  canGoForward: boolean;
} {
  return {
    showPaging: totalPages > 1,
    canGoBack: page > 1,
    canGoForward: page < totalPages,
  };
}
