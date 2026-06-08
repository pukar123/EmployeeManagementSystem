# EMS Web (Next.js)

Browser client for **EMS.API**: feature-based folders, **React Query**, **Zustand**, **Tailwind**, and Axios.

**Full frontend documentation:** **[docs/FRONTEND.md](docs/FRONTEND.md)** (architecture, folders, env, Docker, API alignment, troubleshooting).

---

## Quick start

**Prerequisites:** Node.js **20.9+**, [.NET 9 SDK](https://dotnet.microsoft.com/download) if you use the combined scripts below, and CORS on EMS.API for `http://localhost:3000`.

```bash
cd ems-web
npm install
cp .env.example .env.local
```

Edit `.env.local`:

| Variable | Purpose |
|----------|---------|
| `NEXT_PUBLIC_API_BASE_URL` | API origin, no trailing slash. Use `http://localhost:5246` when the API runs with the **`http`** launch profile; use `https://localhost:7056` when using **`https`**. |

**First run:** if the database has no organization yet, the app opens **`/setup`** so you can create the single organization for this instance. After that, the app uses that organization for employees, departments, and positions (no manual organization id in forms).

**Employee portal:** the home page shows the “Employee portal” shortcut for signed-in users. Personal shifts and leave appear when the login is linked to an employee record in EMS; otherwise the portal opens with a neutral empty state.

**Option A — API and web together (one terminal):**

```bash
npm run dev:all
```

This starts EMS.API (`http` profile, typically `http://localhost:5246`) and `next dev` (usually `http://localhost:3000`). Match `NEXT_PUBLIC_API_BASE_URL` to the API URL.

**Option B — Next.js only (API already running elsewhere):**

```bash
npm run dev
```

**HTTPS API profile:** use `npm run dev:https` and set `NEXT_PUBLIC_API_BASE_URL=https://localhost:7056` (see `EMS.API/Properties/launchSettings.json`).

Open **http://localhost:3000** and use the nav (Employees, Departments, Positions, etc.).

---

## Scripts

| Command | Description |
|---------|-------------|
| `npm run dev:all` | Runs **EMS.API** (`dotnet run`, `http` launch profile) and **`next dev`** together via `concurrently` |
| `npm run dev:https` | Same as `dev:all` but API uses the **`https`** launch profile |
| `npm run dev:api` | **EMS.API** only (`http` profile) |
| `npm run dev` | Next.js dev server only (Turbopack) |
| `npm run build` | Production build |
| `npm run start` | Serve production build |
| `npm run lint` | ESLint |

---

## Docker

From the **solution root** (parent folder): `docker compose up -d --build`, or **`start-ems-docker.bat`** / **`stop-ems-docker.bat`**. Details: [docs/FRONTEND.md §7](docs/FRONTEND.md#7-docker-production-style-image).

---

## Related

| Doc | |
|-----|---|
| Frontend (detailed) | [docs/FRONTEND.md](docs/FRONTEND.md) |
| Backend & Compose | [../README.md](../README.md) |
