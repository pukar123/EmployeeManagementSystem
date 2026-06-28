import { cn } from "@/shared/utils/cn";
import type { ReactNode } from "react";
import { EmptyState } from "./EmptyState";

type DataTableProps = {
  children: ReactNode;
  className?: string;
  emptyMessage?: string;
  isEmpty?: boolean;
};

export function DataTable({ children, className, emptyMessage = "No records found.", isEmpty }: DataTableProps) {
  return (
    <div className={cn("overflow-hidden rounded-2xl border border-border bg-card shadow-soft", className)}>
      <div className="overflow-x-auto">{children}</div>
      {isEmpty ? <EmptyState message={emptyMessage} className="border-t border-border" /> : null}
    </div>
  );
}

export function DataTableElement({ className, children, ...props }: React.TableHTMLAttributes<HTMLTableElement>) {
  return (
    <table className={cn("min-w-full divide-y divide-border text-left text-sm", className)} {...props}>
      {children}
    </table>
  );
}

export function DataTableHead({ className, children, ...props }: React.HTMLAttributes<HTMLTableSectionElement>) {
  return (
    <thead className={cn("bg-muted/50", className)} {...props}>
      {children}
    </thead>
  );
}

export function DataTableHeaderCell({ className, children, ...props }: React.ThHTMLAttributes<HTMLTableCellElement>) {
  return (
    <th
      className={cn(
        "px-4 py-3 text-xs font-semibold uppercase tracking-wide text-muted-foreground",
        className,
      )}
      {...props}
    >
      {children}
    </th>
  );
}

export function DataTableBody({ className, children, ...props }: React.HTMLAttributes<HTMLTableSectionElement>) {
  return (
    <tbody className={cn("divide-y divide-border", className)} {...props}>
      {children}
    </tbody>
  );
}

export function DataTableRow({ className, children, ...props }: React.HTMLAttributes<HTMLTableRowElement>) {
  return (
    <tr
      className={cn("bg-card transition-colors hover:bg-muted/40", className)}
      {...props}
    >
      {children}
    </tr>
  );
}

export function DataTableCell({ className, children, ...props }: React.TdHTMLAttributes<HTMLTableCellElement>) {
  return (
    <td className={cn("px-4 py-3 text-foreground", className)} {...props}>
      {children}
    </td>
  );
}
