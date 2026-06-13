export interface Permission {
  id: string;
  code: string;
  name: string;
  description: string | null;
  groupCode: string;
  groupName: string;
  resourceCode: string;
  resourceName: string;
  action: string;
  sortOrder: number;
  isEnabled: boolean;
}
