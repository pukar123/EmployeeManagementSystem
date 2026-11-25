"use client";

import { Button } from "@/shared/components/Button";
import { DOCUMENT_PAGE_SIZES, pagingState } from "../utils/document-paging";

type DocumentPagerProps = {
  page: number;
  pageSize: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
};

/** Rows per page with Previous/Next and "Page X of Y", styled like the employee directory. */
export function DocumentPager({ page, pageSize, totalPages, onPageChange, onPageSizeChange }: DocumentPagerProps) {
  const { showPaging, canGoBack, canGoForward } = pagingState(page, totalPages);

  return (
    <div className="flex flex-wrap items-center justify-between gap-2">
      <div className="flex flex-wrap items-center gap-3 text-sm text-muted-foreground">
        <label className="flex items-center gap-2">
          <span>Rows per page</span>
          <select
            className="rounded-lg border border-input bg-background px-2 py-1 text-sm"
            value={pageSize}
            onChange={(e) => onPageSizeChange(Number(e.target.value))}
          >
            {DOCUMENT_PAGE_SIZES.map((size) => (
              <option key={size} value={size}>
                {size}
              </option>
            ))}
          </select>
        </label>
        {showPaging ? (
          <p>
            Page {page} of {totalPages}
          </p>
        ) : null}
      </div>
      {showPaging ? (
        <div className="flex gap-2">
          <Button type="button" variant="secondary" size="sm" disabled={!canGoBack} onClick={() => onPageChange(page - 1)}>
            Previous
          </Button>
          <Button
            type="button"
            variant="secondary"
            size="sm"
            disabled={!canGoForward}
            onClick={() => onPageChange(page + 1)}
          >
            Next
          </Button>
        </div>
      ) : null}
    </div>
  );
}
