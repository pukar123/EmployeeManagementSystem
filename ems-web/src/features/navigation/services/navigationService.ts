import { httpClient } from "@/shared/api/http-client";
import type { MenuDto } from "../types";

const PATH = "/api/Navigation/menus";

export async function fetchNavigationMenus(): Promise<MenuDto[]> {
  const { data } = await httpClient.get<MenuDto[]>(PATH);
  return data;
}
