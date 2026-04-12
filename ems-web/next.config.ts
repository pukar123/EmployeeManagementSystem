import type { NextConfig } from "next";

/**
 * When `NEXT_PUBLIC_API_BASE_URL` is unset, the browser uses same-origin `/api/*` paths.
 * Rewrites forward those to EMS.API so login and other calls work with `npm run dev:all`
 * without a `.env.local` file. When the public URL is set, the client calls the API
 * directly and rewrites are disabled.
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
      process.env.EMS_API_INTERNAL_URL?.replace(/\/$/, "") ?? "http://localhost:5246";
    return [
      {
        source: "/api/:path*",
        destination: `${internal}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;
