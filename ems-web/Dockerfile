# EMS Web — Next.js production image (standalone output)
FROM node:20-alpine AS base
RUN apk add --no-cache libc6-compat
WORKDIR /app

FROM base AS deps
COPY package.json package-lock.json ./
RUN npm ci

FROM base AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .

# Public env vars baked into the client bundle at build time (browser-reachable origins)
ARG NEXT_PUBLIC_EMS_API_BASE_URL=http://localhost:5246
ARG NEXT_PUBLIC_USER_MANAGEMENT_API_BASE_URL=http://localhost:5137
ENV NEXT_PUBLIC_EMS_API_BASE_URL=$NEXT_PUBLIC_EMS_API_BASE_URL
ENV NEXT_PUBLIC_USER_MANAGEMENT_API_BASE_URL=$NEXT_PUBLIC_USER_MANAGEMENT_API_BASE_URL

RUN npm run build

FROM base AS runner
ENV NODE_ENV=production
ENV PORT=3000
ENV HOSTNAME=0.0.0.0

RUN addgroup --system --gid 1001 nodejs && adduser --system --uid 1001 nextjs

COPY --from=builder /app/public ./public
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

USER nextjs
EXPOSE 3000
CMD ["node", "server.js"]
