import { emsHttpClient } from "@/shared/api/http-client";
import type {
  NotificationItem,
  NotificationQueryParams,
  NotificationUnreadCount,
} from "../types/notification.types";

const PATH = "/api/Notifications";

export const notificationService = {
  getMine: async (params: NotificationQueryParams = {}): Promise<NotificationItem[]> => {
    const { data } = await emsHttpClient.get<NotificationItem[]>(PATH, { params });
    return data;
  },

  getUnreadCount: async (): Promise<NotificationUnreadCount> => {
    const { data } = await emsHttpClient.get<NotificationUnreadCount>(`${PATH}/unread-count`);
    return data;
  },

  markRead: async (id: number): Promise<NotificationItem> => {
    const { data } = await emsHttpClient.post<NotificationItem>(`${PATH}/${id}/read`);
    return data;
  },

  markAllRead: async (): Promise<{ count: number }> => {
    const { data } = await emsHttpClient.post<{ count: number }>(`${PATH}/read-all`);
    return data;
  },
};
