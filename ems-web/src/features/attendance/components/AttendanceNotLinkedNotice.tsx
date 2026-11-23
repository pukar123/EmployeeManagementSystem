/** Shown to signed-in users who are not attendance admins and have no linked employee profile. */
export function AttendanceNotLinkedNotice() {
  return (
    <div className="rounded-xl border border-border bg-muted/30 px-4 py-6 text-sm text-muted-foreground" role="status">
      <p className="font-medium text-foreground">Attendance</p>
      <p className="mt-2">Your account isn&apos;t linked to an employee profile, so attendance isn&apos;t available.</p>
    </div>
  );
}
