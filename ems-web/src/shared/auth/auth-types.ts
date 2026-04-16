export type AuthUser = {
  id: number;
  email: string;
  userName: string | null;
};

export type AuthResponse = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
  tokenType: string;
  user: AuthUser;
  mustChangePassword: boolean;
};
