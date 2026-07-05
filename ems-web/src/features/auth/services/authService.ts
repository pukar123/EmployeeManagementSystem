import axios from "axios";
import { userManagementApiBaseUrl, userManagementHttpClient } from "@/shared/api/http-client";
import type { AuthResponse } from "@/shared/auth/auth-types";
import { clearAuth, getRefreshToken, saveAuthResponse } from "@/shared/auth/auth-storage";

const LOGIN_PATH = "/api/auth/login";
const REVOKE_PATH = "/api/auth/revoke";
const CHANGE_PASSWORD_PATH = "/api/auth/change-password";
const ACCEPT_INVITATION_PATH = "/api/invitations/accept";

export const authService = {
  async login(email: string, password: string): Promise<AuthResponse> {
    const { data } = await userManagementHttpClient.post<AuthResponse>(LOGIN_PATH, { email, password });
    saveAuthResponse(data);
    return data;
  },

  async logout(): Promise<void> {
    const refreshToken = getRefreshToken();
    if (refreshToken) {
      try {
        const base = userManagementApiBaseUrl.replace(/\/$/, "");
        await axios.post(
          `${base}${REVOKE_PATH}`,
          { refreshToken },
          { headers: { "Content-Type": "application/json" } },
        );
      } catch {
        /* ignore revoke failures — local session is cleared either way */
      }
    }
    clearAuth();
  },

  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    await userManagementHttpClient.post(CHANGE_PASSWORD_PATH, { currentPassword, newPassword });
  },

  /** Invitation acceptance always targets User Management directly (never EMS.API). */
  async acceptInvitation(token: string, newPassword: string): Promise<void> {
    await userManagementHttpClient.post(ACCEPT_INVITATION_PATH, { token, newPassword });
  },
};
