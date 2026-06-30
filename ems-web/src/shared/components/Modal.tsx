"use client";

import { cn } from "@/shared/utils/cn";
import { useEffect, useRef, type ReactNode } from "react";
import { useModalFocusTrap } from "@/shared/hooks/useUnsavedChangesWarning";

type ModalProps = {
  open: boolean;
  title: string;
  children: ReactNode;
  onClose: () => void;
  footer?: ReactNode;
  className?: string;
};

export function Modal({ open, title, children, onClose, footer, className }: ModalProps) {
  const dialogRef = useRef<HTMLDivElement>(null);
  useModalFocusTrap(open, dialogRef);

  useEffect(() => {
    if (!open) return;
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <button
        type="button"
        className="absolute inset-0 bg-black/40 backdrop-blur-sm"
        aria-label="Close dialog overlay"
        onClick={onClose}
      />
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-title"
        className={cn(
          "relative z-10 max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-2xl border border-border bg-card p-6 shadow-soft-lg",
          "animate-in fade-in zoom-in-95 duration-200",
          className,
        )}
      >
        <div className="mb-4 flex items-start justify-between gap-4">
          <h2 id="modal-title" className="text-lg font-semibold tracking-tight text-foreground">
            {title}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-1.5 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
            aria-label="Close"
          >
            ✕
          </button>
        </div>
        <div className="text-foreground">{children}</div>
        {footer ? <div className="mt-6 flex flex-wrap justify-end gap-2 border-t border-border pt-4">{footer}</div> : null}
      </div>
    </div>
  );
}
