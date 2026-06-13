import type { Component } from 'vue';
import type { Router } from 'vue-router';

import type { AdminPermission } from '@/shared/auth/permissions';

export interface AdminMenuMeta {
  group?: string;
  groupOrder?: number;
  groupTitle?: string;
  order?: number;
}

declare module 'vue-router' {
  interface RouteMeta {
    icon?: Component;
    menu?: AdminMenuMeta;
    public?: boolean;
    requiredPermissions?: ReadonlyArray<AdminPermission | string>;
    title?: string;
  }
}

export interface NavigationItem {
  icon?: Component;
  order: number;
  path: string;
  title: string;
}

export interface NavigationGroup {
  children: NavigationItem[];
  key: string;
  order: number;
  title: string;
}

export type NavigationEntry = NavigationGroup | NavigationItem;

export function isNavigationGroup(entry: NavigationEntry): entry is NavigationGroup {
  return 'children' in entry;
}

export function buildNavigation(
  router: Router,
  canAccess: (requiredPermissions?: ReadonlyArray<AdminPermission | string>) => boolean,
) {
  const groups = new Map<string, NavigationGroup>();
  const entries: NavigationEntry[] = [];

  for (const route of router.getRoutes()) {
    if (!route.meta.menu || !route.meta.title || !canAccess(route.meta.requiredPermissions)) {
      continue;
    }

    const item: NavigationItem = {
      icon: route.meta.icon,
      order: route.meta.menu.order ?? 0,
      path: route.path,
      title: route.meta.title,
    };

    if (!route.meta.menu.group) {
      entries.push(item);
      continue;
    }

    const groupKey = route.meta.menu.group;
    let group = groups.get(groupKey);

    if (!group) {
      group = {
        children: [],
        key: groupKey,
        order: route.meta.menu.groupOrder ?? 0,
        title: route.meta.menu.groupTitle ?? groupKey,
      };
      groups.set(groupKey, group);
      entries.push(group);
    }

    group.children.push(item);
  }

  for (const group of groups.values()) {
    group.children.sort(sortByOrder);
  }

  return entries.sort(sortByOrder);
}

function sortByOrder(left: { order: number }, right: { order: number }) {
  return left.order - right.order;
}
