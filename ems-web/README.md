# EMS Web (Next.js)

Browser client for **EMS.API** and **Pukar.Usermanagement.Host**: feature-based folders, **React Query**, **Zustand**, **Tailwind**, and two Axios clients.

**Full frontend documentation:** **[docs/FRONTEND.md](docs/FRONTEND.md)** (architecture, dual APIs, env, Docker, troubleshooting).

---

## Quick start

**Prerequisites:** Node.js **20.9+**, [.NET 9 SDK](https://dotnet.microsoft.com/download) if you use the combined scripts below, and CORS on **both** APIs for `http://localhost:3000`.

```bash
cd ems-web
npm install
cp .env.example .env.local
```

Edit `.env.local`:

| Variable | Purpose |
|----------|---------|
| `NEXT_PUBLIC_EMS_API_BASE_URL` | EMS.API origin (e.g. `http://localhost:5246` for the `http` launch profile). |
| `NEXT_PUBLIC_USER_MANAGEMENT_API_BASE_URL` | User Management Host origin (e.g. `http://localhost:5137`). |

**First run:** if the EMS database has no organization yet, the app opens **`/setup`**. Sign-in and invitation acceptance go to User Management; HR data goes to EMS.

**Option A — UM, EMS, and web together (one terminal):**

```bash
npm run dev:all
```

Starts User Management Host (`http://localhost:5137`), EMS.API (`http://localhost:5246`), and `next dev` (`http://localhost:3000`).

**Option B — Next.js only (APIs already running):**

```bash
npm run dev
```

Open **http://localhost:3000**.

---

## Scripts

| Command | Description |
|---------|-------------|
| `npm run dev:all` | **UM Host**, **EMS.API**, and **`next dev`** via `concurrently` |
| `npm run dev:https` | Same with HTTPS launch profiles |
| `npm run dev:api` | EMS.API only |
| `npm run dev:um` | User Management Host only |
| `npm run dev` | Next.js only |
| `npm run test` | Vitest (API routing + availability classification) |
| `npm run build` | Production build |
| `npm run start` | Serve production build |
| `npm run lint` | ESLint |

---

## Docker

From the **solution root**: `docker compose up -d --build` runs SQL Server (separate `EMSDevDB` and `UserManagementDb`), MongoDB, Redis, **usermanagement-api**, **ems-api**, and **ems-web**. Details: [docs/FRONTEND.md](docs/FRONTEND.md).

---

## Related

| Doc | |
|-----|---|
| Frontend (detailed) | [docs/FRONTEND.md](docs/FRONTEND.md) |
| EMS ↔ UM integration | [../docs/ems-um-http-integration.md](../docs/ems-um-http-integration.md) |
| Backend & Compose | [../README.md](../README.md) |
