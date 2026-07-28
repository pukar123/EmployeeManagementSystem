import type { NextConfig } from "next";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const svgrLoader = require.resolve("@svgr/webpack");

/**
 * When `NEXT_PUBLIC_EMS_API_BASE_URL` is unset, the browser uses same-origin paths; rewrites
 * forward to EMS.API (default `http://127.0.0.1:5246`). User Management is never proxied —
 * set `NEXT_PUBLIC_USER_MANAGEMENT_API_BASE_URL` so auth and identity calls hit UM Host directly.
 */
const nextConfig: NextConfig = {
  /* Enables Docker image using standalone output (see ems-web/Dockerfile). */
  output: "standalone",
  webpack(config) {
    config.module.rules.push({
      test: /\.svg$/,
      use: [svgrLoader],
    });
    return config;
  },
  turbopack: {
    rules: {
      "*.svg": {
        loaders: [svgrLoader],
        as: "*.js",
      },
    },
  },
  async rewrites() {
    const publicBase =
      process.env.NEXT_PUBLIC_EMS_API_BASE_URL?.trim() ||
      process.env.NEXT_PUBLIC_API_BASE_URL?.trim();
    if (publicBase) {
      return [];
    }
    const internal =
      process.env.EMS_API_INTERNAL_URL?.replace(/\/$/, "") ?? "http://127.0.0.1:5246";
    return [
      {
        source: "/api/:path*",
        destination: `${internal}/api/:path*`,
      },
      {
        source: "/attachments/:path*",
        destination: `${internal}/attachments/:path*`,
      },
    ];
  },
};

export default nextConfig;
