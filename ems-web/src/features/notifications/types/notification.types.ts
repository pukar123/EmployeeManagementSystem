export type NotificationSeverity = "Low" | "Normal" | "High" | "Critical";

export type NotificationItem = {
  id: number;
  typeKey: string;
  title: string;
  body: string;
  actionUrl: string | null;
  metadataJson: string | null;
  severity: NotificationSeverity;
  isRead: boolean;
  createdAtUtc: string;
  readAtUtc: string | null;
  expiresAtUtc: string | null;
};

export type NotificationUnreadCount = {
  count: number;
};

export type NotificationQueryParams = {
  take?: number;
  skip?: number;
  unreadOnly?: boolean;
};
