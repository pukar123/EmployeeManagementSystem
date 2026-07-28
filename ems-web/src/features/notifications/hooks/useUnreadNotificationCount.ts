import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/providers/AuthProvider";
import { notificationService } from "../services/notificationService";
import { notificationKeys } from "../services/query-keys";

export function useUnreadNotificationCount(enabled = true) {
  const { isAuthenticated, user } = useAuth();

  return useQuery({
    queryKey: notificationKeys.unreadCount(user?.id ?? 0),
    queryFn: () => notificationService.getUnreadCount(),
    enabled: enabled && isAuthenticated && user != null,
    refetchInterval: 60_000,
  });
}
