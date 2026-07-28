import path from "node:path";
import { defineConfig } from "vitest/config";

export default defineConfig({
  test: {
    environment: "node",
    include: ["src/**/*.test.ts"],
    env: {
      NEXT_PUBLIC_EMS_API_BASE_URL: "http://ems.test",
      NEXT_PUBLIC_USER_MANAGEMENT_API_BASE_URL: "http://um.test",
    },
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
});
