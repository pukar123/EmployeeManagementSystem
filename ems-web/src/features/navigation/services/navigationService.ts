import { emsHttpClient } from "@/shared/api/http-client";
import type { MenuDto } from "../types";

const PATH = "/api/Navigation/menus";

export async function fetchNavigationMenus(): Promise<MenuDto[]> {
  const { data } = await emsHttpClient.get<MenuDto[]>(PATH);
  return data;
}
