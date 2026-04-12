export type MenuDto = {
  id: number;
  key: string;
  label: string;
  routePath: string;
  parentMenuId: number | null;
  sortOrder: number;
  iconKey: string | null;
  children: MenuDto[];
};
