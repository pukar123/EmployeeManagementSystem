export const notificationKeys = {
  all: ["notifications"] as const,
  list: (userId: number, params?: { unreadOnly?: boolean }) =>
    [...notificationKeys.all, "list", userId, params?.unreadOnly ?? false] as const,
  unreadCount: (userId: number) => [...notificationKeys.all, "unread-count", userId] as const,
};
