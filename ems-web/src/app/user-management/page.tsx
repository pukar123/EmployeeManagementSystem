import Link from "next/link";

const links = [
  { href: "/user-management/users", title: "Users", description: "Directory and role assignment." },
  { href: "/user-management/roles", title: "Roles", description: "Create and manage application roles." },
] as const;

export default function UserManagementIndexPage() {
  return (
    <main className="mx-auto max-w-3xl flex-1 px-4 py-10 sm:px-6">
      <h1 className="text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-50">
        User management
      </h1>
      <p className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">
        Choose a section to manage users and roles.
      </p>
      <ul className="mt-8 grid gap-4 sm:grid-cols-1">
        {links.map(({ href, title, description }) => (
          <li key={href}>
            <Link
              href={href}
              className="block rounded-xl border border-zinc-200 bg-white p-5 shadow-sm transition hover:border-zinc-300 hover:shadow dark:border-zinc-700 dark:bg-zinc-950 dark:hover:border-zinc-600"
            >
              <span className="font-medium text-zinc-900 dark:text-zinc-50">{title}</span>
              <span className="mt-1 block text-sm text-zinc-600 dark:text-zinc-400">{description}</span>
            </Link>
          </li>
        ))}
      </ul>
    </main>
  );
}
