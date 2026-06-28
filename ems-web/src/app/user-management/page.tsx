import Link from "next/link";

const links = [
  { href: "/user-management/users", title: "Users", description: "Directory and role assignment." },
  { href: "/user-management/roles", title: "Roles", description: "Create and manage application roles." },
  {
    href: "/user-management/menu-access",
    title: "Menu access",
    description: "Choose which menus each role can see in the sidebar.",
  },
] as const;

export default function UserManagementIndexPage() {
  return (
    <main className="mx-auto max-w-3xl flex-1 px-4 py-10 sm:px-6">
      <h1 className="text-2xl font-semibold tracking-tight text-foreground">
        User management
      </h1>
      <p className="mt-2 text-sm text-muted-foreground">
        Choose a section to manage users and roles.
      </p>
      <ul className="mt-8 grid gap-4 sm:grid-cols-1">
        {links.map(({ href, title, description }) => (
          <li key={href}>
            <Link
              href={href}
              className="block rounded-xl border border-border bg-card p-5 shadow-sm transition hover:border-input hover:shadow dark:bg-card dark:hover:border-input"
            >
              <span className="font-medium text-foreground">{title}</span>
              <span className="mt-1 block text-sm text-muted-foreground">{description}</span>
            </Link>
          </li>
        ))}
      </ul>
    </main>
  );
}
