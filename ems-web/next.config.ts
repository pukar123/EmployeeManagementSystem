import type { NextConfig } from "next";

/**
 * When `NEXT_PUBLIC_API_BASE_URL` is unset, the browser uses same-origin paths; rewrites
 * forward to EMS.API (default `http://127.0.0.1:5246`). That avoids direct browser→API
 * connection issues and matches `npm run dev:all`. `/attachments` is proxied too so logos
 * and document links work with an empty public API origin. Set `NEXT_PUBLIC_API_BASE_URL`
 * to call the API directly (e.g. Docker/production).
 */
const nextConfig: NextConfig = {
  /* Enables Docker image using standalone output (see ems-web/Dockerfile). */
  output: "standalone",
  async rewrites() {
    const publicBase = process.env.NEXT_PUBLIC_API_BASE_URL?.trim();
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
