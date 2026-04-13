import type { LucideIcon } from "lucide-react";
import {
  Briefcase,
  Building2,
  Circle,
  Clock3,
  Home,
  LayoutList,
  MapPin,
  Settings,
  Shield,
  Users,
} from "lucide-react";

const map: Record<string, LucideIcon> = {
  home: Home,
  users: Users,
  building2: Building2,
  briefcase: Briefcase,
  clock3: Clock3,
  mappin: MapPin,
  settings: Settings,
  shield: Shield,
  "layout-list": LayoutList,
};

export function getNavIcon(iconKey: string | null | undefined): LucideIcon {
  if (iconKey && map[iconKey]) return map[iconKey];
  return Circle;
}
