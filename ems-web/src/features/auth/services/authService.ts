import axios from "axios";
import { apiBaseUrl, httpClient } from "@/shared/api/http-client";
import type { AuthResponse } from "@/shared/auth/auth-types";
import { clearAuth, getRefreshToken, saveAuthResponse } from "@/shared/auth/auth-storage";

const LOGIN_PATH = "/api/auth/login";
const REVOKE_PATH = "/api/auth/revoke";

export const authService = {
  async login(email: string, password: string): Promise<AuthResponse> {
    const { data } = await httpClient.post<AuthResponse>(LOGIN_PATH, { email, password });
    saveAuthResponse(data);
    return data;
  },

  async logout(): Promise<void> {
    const refreshToken = getRefreshToken();
    if (refreshToken) {
      try {
        const base = apiBaseUrl.replace(/\/$/, "");
        await axios.post(`${base}${REVOKE_PATH}`, { refreshToken }, {
          headers: { "Content-Type": "application/json" },
        });
      } catch {
        /* ignore */
      }
    }
    clearAuth();
  },
};
