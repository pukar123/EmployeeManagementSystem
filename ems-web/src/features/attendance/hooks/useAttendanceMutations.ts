import { useMutation, useQueryClient } from "@tanstack/react-query";
import { attendanceService } from "../services/attendanceService";
import { attendanceKeys } from "../services/query-keys";

type RefreshArgs = { employeeId: number; fromDate?: string; toDate?: string };

function refresh(queryClient: ReturnType<typeof useQueryClient>, args: RefreshArgs) {
  void queryClient.invalidateQueries({
    queryKey: attendanceKeys.employee(args.employeeId, args.fromDate, args.toDate),
  });
  if (args.fromDate && args.toDate) {
    void queryClient.invalidateQueries({
      queryKey: attendanceKeys.summary(args.employeeId, args.fromDate, args.toDate),
    });
  }
}

export function useAttendanceMutations(args: RefreshArgs) {
  const queryClient = useQueryClient();

  const checkIn = useMutation({
    mutationFn: attendanceService.checkIn,
    onSuccess: () => refresh(queryClient, args),
  });

  const checkOut = useMutation({
    mutationFn: attendanceService.checkOut,
    onSuccess: () => refresh(queryClient, args),
  });

  const startBreak = useMutation({
    mutationFn: attendanceService.startBreak,
    onSuccess: () => refresh(queryClient, args),
  });

  const endBreak = useMutation({
    mutationFn: attendanceService.endBreak,
    onSuccess: () => refresh(queryClient, args),
  });

  const manualEntry = useMutation({
    mutationFn: attendanceService.createManualEntry,
    onSuccess: () => refresh(queryClient, args),
  });

  return { checkIn, checkOut, startBreak, endBreak, manualEntry };
}
