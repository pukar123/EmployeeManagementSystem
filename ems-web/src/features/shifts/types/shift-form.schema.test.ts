import { describe, expect, it } from "vitest";
import { parseCreateShiftForm, parseUpdateShiftForm } from "./shift-form.schema";
import type { ShiftItem } from "./shift.types";
import { filterShifts, shiftOverlapsRange, toDatetimeLocalValue } from "../utils/shiftDisplay";

describe("shift form validation", () => {
  it("requires employee on create", () => {
    const result = parseCreateShiftForm(
      {
        title: "Morning",
        startAtLocal: "2026-07-10T09:00",
        endAtLocal: "2026-07-10T17:00",
      },
      1,
    );

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.message).toContain("Employee");
    }
  });

  it("rejects end before start", () => {
    const result = parseCreateShiftForm(
      {
        employeeId: 5,
        title: "Morning",
        startAtLocal: "2026-07-10T17:00",
        endAtLocal: "2026-07-10T09:00",
      },
      1,
    );

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.message).toContain("Start time");
    }
  });

  it("builds update payload with status", () => {
    const result = parseUpdateShiftForm(
      {
        title: "Evening",
        startAtLocal: "2026-07-10T17:00",
        endAtLocal: "2026-07-10T23:00",
      },
      2,
    );

    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.payload.status).toBe(2);
      expect(result.payload.title).toBe("Evening");
    }
  });
});

describe("shift filtering helpers", () => {
  const baseShift: ShiftItem = {
    id: 1,
    organizationId: 1,
    employeeId: 5,
    siteId: null,
    title: "Morning shift",
    description: null,
    startAtUtc: "2026-07-10T09:00:00.000Z",
    endAtUtc: "2026-07-10T17:00:00.000Z",
    status: 0,
    createdAtUtc: "2026-07-01T00:00:00.000Z",
    updatedAtUtc: "2026-07-01T00:00:00.000Z",
  };

  it("detects range overlap", () => {
    expect(shiftOverlapsRange(baseShift, new Date("2026-07-10T00:00:00Z"), new Date("2026-07-10T23:59:59Z"))).toBe(true);
    expect(shiftOverlapsRange(baseShift, new Date("2026-07-11T00:00:00Z"), new Date("2026-07-11T23:59:59Z"))).toBe(false);
  });

  it("filters by status and search text", () => {
    const cancelled: ShiftItem = { ...baseShift, id: 2, status: 3, title: "Cancelled block" };
    const filtered = filterShifts({
      shifts: [baseShift, cancelled],
      search: "morning",
      status: 0,
      rangeStart: null,
      rangeEnd: null,
      employeeLabelById: new Map([[5, "EMP001 - Alex"]]),
      siteLabelById: new Map(),
    });

    expect(filtered).toHaveLength(1);
    expect(filtered[0]?.title).toBe("Morning shift");
  });

  it("round-trips datetime-local values", () => {
    const local = toDatetimeLocalValue("2026-07-10T09:30:00.000Z");
    expect(local).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/);
  });
});
