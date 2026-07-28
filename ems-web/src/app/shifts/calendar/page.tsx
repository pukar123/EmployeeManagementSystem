import { ShiftsSection } from "@/features/shifts/components/ShiftsSection";

export default function ShiftsCalendarPage() {
  return (
    <main className="mx-auto max-w-6xl flex-1 px-4 py-10 sm:px-6">
      <ShiftsSection initialViewMode="calendar" />
    </main>
  );
}
