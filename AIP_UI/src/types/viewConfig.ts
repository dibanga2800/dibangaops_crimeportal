export type ViewPermission = "all" | "administrator" | "manager"

export interface ViewConfig {
  id: string
  name: string
  description: string
  enabled: boolean
  permission: ViewPermission
}

export interface PageViewConfig {
  pageId: string
  pageName: string
  views: ViewConfig[]
}