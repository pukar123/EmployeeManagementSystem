"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { UserCircle } from "lucide-react";
import { Dropdown } from "@/components/tailadmin/dropdown/Dropdown";
import { DropdownItem } from "@/components/tailadmin/dropdown/DropdownItem";
import { useAuth } from "@/providers/AuthProvider";

export function EmsUserDropdown() {
  const [isOpen, setIsOpen] = useState(false);
  const router = useRouter();
  const { logout, user } = useAuth();

  function toggleDropdown(e: React.MouseEvent<HTMLButtonElement>) {
    e.stopPropagation();
    setIsOpen((prev) => !prev);
  }

  function closeDropdown() {
    setIsOpen(false);
  }

  async function handleSignOut() {
    closeDropdown();
    await logout();
    router.replace("/login");
  }

  const email = user?.email ?? "Signed in";

  return (
    <div className="relative">
      <button
        type="button"
        onClick={toggleDropdown}
        className="dropdown-toggle flex items-center text-gray-700 dark:text-gray-400"
      >
        <span className="mr-3 h-11 w-11 overflow-hidden rounded-full border border-gray-200 bg-gray-100 dark:border-gray-700 dark:bg-gray-800">
          <span className="flex h-full w-full items-center justify-center text-gray-500 dark:text-gray-400">
            <UserCircle className="h-8 w-8" strokeWidth={1.25} />
          </span>
        </span>

        <span className="mr-1 block max-w-[140px] truncate font-medium text-theme-sm">{email}</span>

        <svg
          className={`stroke-gray-500 transition-transform duration-200 dark:stroke-gray-400 ${isOpen ? "rotate-180" : ""}`}
          width="18"
          height="20"
          viewBox="0 0 18 20"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
        >
          <path
            d="M4.3125 8.65625L9 13.3437L13.6875 8.65625"
            stroke="currentColor"
            strokeWidth="1.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      </button>

      <Dropdown
        isOpen={isOpen}
        onClose={closeDropdown}
        className="absolute right-0 mt-[17px] flex w-[260px] flex-col rounded-2xl border border-gray-200 bg-white p-3 shadow-theme-lg dark:border-gray-800 dark:bg-gray-dark"
      >
        <div>
          <span className="block font-medium text-gray-700 text-theme-sm dark:text-gray-400">{email}</span>
        </div>

        <ul className="flex flex-col gap-1 border-b border-gray-200 py-3 pb-3 dark:border-gray-800">
          <li>
            <DropdownItem
              tag="a"
              href="/change-password"
              onItemClick={closeDropdown}
              className="flex items-center gap-3 rounded-lg px-3 py-2 font-medium text-gray-700 text-theme-sm hover:bg-gray-100 hover:text-gray-700 dark:text-gray-400 dark:hover:bg-white/5 dark:hover:text-gray-300"
            >
              Change password
            </DropdownItem>
          </li>
        </ul>
        <DropdownItem
          tag="button"
          onItemClick={() => void handleSignOut()}
          className="mt-3 flex items-center gap-3 rounded-lg px-3 py-2 font-medium text-gray-700 text-theme-sm hover:bg-gray-100 hover:text-gray-700 dark:text-gray-400 dark:hover:bg-white/5 dark:hover:text-gray-300"
        >
          Sign out
        </DropdownItem>
      </Dropdown>
    </div>
  );
}
