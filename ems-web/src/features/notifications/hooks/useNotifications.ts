import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/providers/AuthProvider";
import { notificationService } from "../services/notificationService";
import { notificationKeys } from "../services/query-keys";
import type { NotificationQueryParams } from "../types/notification.types";

export function useNotifications(params: NotificationQueryParams = {}) {
  const { isAuthenticated, user } = useAuth();

  return useQuery({
    queryKey: notificationKeys.list(user?.id ?? 0, { unreadOnly: params.unreadOnly }),
    queryFn: () => notificationService.getMine(params),
    enabled: isAuthenticated && user != null,
  });
}
