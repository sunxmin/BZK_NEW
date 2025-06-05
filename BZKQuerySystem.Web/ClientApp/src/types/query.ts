// 查询相关类型定义

export interface TableInfo {
  id: number;
  tableName: string;
  displayName: string;
  description: string;
  isView: boolean;
  columns: ColumnInfo[];
}

export interface ColumnInfo {
  id: number;
  tableId: number;
  columnName: string;
  displayName: string;
  dataType: string;
  description: string;
  isPrimaryKey: boolean;
  isNullable: boolean;
}

export interface QueryRequest {
  tables: string[];
  columns: ColumnSelection[];
  joins: JoinCondition[];
  filters: FilterCondition[];
  orderBy: OrderByClause[];
  isDistinct: boolean;
  topCount?: number;
}

export interface ColumnSelection {
  tableName: string;
  columnName: string;
  alias?: string;
}

export interface JoinCondition {
  joinType: 'INNER' | 'LEFT' | 'RIGHT' | 'FULL';
  leftTable: string;
  leftColumn: string;
  rightTable: string;
  rightColumn: string;
}

export interface FilterCondition {
  tableName: string;
  columnName: string;
  operator: '=' | '!=' | '>' | '<' | '>=' | '<=' | 'LIKE' | 'NOT LIKE' | 'IN' | 'NOT IN' | 'IS NULL' | 'IS NOT NULL';
  value: any;
  logicalOperator: 'AND' | 'OR';
}

export interface OrderByClause {
  tableName: string;
  columnName: string;
  direction: 'ASC' | 'DESC';
}

export interface QueryResult {
  data: any[];
  columns: ResultColumn[];
  totalCount: number;
  executionTime: number;
  sql: string;
}

export interface ResultColumn {
  name: string;
  displayName: string;
  dataType: string;
}

export interface PaginatedResult<T> {
  data: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface SavedQuery {
  id: number;
  userId: string;
  name: string;
  description: string;
  sqlQuery: string;
  tablesIncluded: string;
  columnsIncluded: string;
  filterConditions: string;
  sortOrder: string;
  joinConditions: string;
  createdAt: string;
  updatedAt: string;
  createdBy: string;
  isShared: boolean;
}

export interface QueryShare {
  id: number;
  queryId: number;
  userId: string;
  sharedAt: string;
  sharedBy: string;
}

// API响应类型
export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}

export interface ApiErrorResponse {
  statusCode: number;
  message: string;
  details: string;
  timestamp: string;
  traceId: string;
}

// 用户和权限相关
export interface User {
  id: string;
  userName: string;
  displayName: string;
  email: string;
  isActive: boolean;
  roles: Role[];
  permissions: string[];
}

export interface Role {
  id: string;
  name: string;
  normalizedName: string;
  description: string;
}

export interface Permission {
  name: string;
  description: string;
}

// 系统监控相关
export interface SystemHealth {
  overallStatus: 'Healthy' | 'Warning' | 'Critical';
  databaseStatus: DatabaseHealth;
  systemResources: SystemResources;
  applicationStatus: ApplicationStatus;
  recentErrors: ErrorSummary[];
  lastChecked: string;
}

export interface DatabaseHealth {
  isConnected: boolean;
  connectionTime: number;
  version: string;
  databaseSizeMB: number;
  status: string;
  errorMessage?: string;
}

export interface SystemResources {
  memoryUsageMB: number;
  memoryUsagePercent: number;
  cpuUsagePercent: number;
  threadCount: number;
  uptimeSeconds: number;
  status: string;
}

export interface ApplicationStatus {
  version: string;
  environment: string;
  machineName: string;
  processId: number;
  startTime: string;
  status: string;
}

export interface ErrorSummary {
  timestamp: string;
  message: string;
  userId: string;
  severity: string;
}

export interface PerformanceMetrics {
  cpuUsage: number;
  memoryUsage: number;
  diskUsage: number;
  databaseMetrics: DatabaseMetrics;
  applicationMetrics: ApplicationMetrics;
  timestamp: string;
}

export interface DatabaseMetrics {
  activeConnections: number;
  activeQueries: number;
  queryExecutionTime: number;
  databaseSizeBytes: number;
}

export interface ApplicationMetrics {
  requestsPerSecond: number;
  averageResponseTime: number;
  activeSessions: number;
  memoryUsageMB: number;
  threadCount: number;
}

// 审计日志相关
export interface AuditLog {
  id: number;
  userId: string;
  action: string;
  details: string;
  ipAddress: string;
  timestamp: string;
  eventType: AuditEventType;
}

export enum AuditEventType {
  UserAction = 1,
  QueryExecution = 2,
  DataExport = 3,
  SecurityEvent = 4,
  SystemEvent = 5
}

export interface AuditLogQuery {
  userId?: string;
  fromDate?: string;
  toDate?: string;
  eventType?: string;
  action?: string;
  pageNumber: number;
  pageSize: number;
}

// 表格组件相关
export interface TableColumn {
  key: string;
  title: string;
  dataType: string;
  sortable?: boolean;
  filterable?: boolean;
  width?: number;
  render?: (value: any, record: any) => React.ReactNode;
}

export interface TableProps {
  data: any[];
  columns: TableColumn[];
  loading?: boolean;
  pagination?: {
    current: number;
    pageSize: number;
    total: number;
    onChange: (page: number, pageSize: number) => void;
  };
  selection?: {
    selectedRowKeys: string[];
    onChange: (selectedRowKeys: string[]) => void;
  };
}

// 表单相关
export interface FormField {
  name: string;
  label: string;
  type: 'text' | 'number' | 'select' | 'multiselect' | 'date' | 'datetime' | 'textarea' | 'checkbox';
  required?: boolean;
  placeholder?: string;
  options?: SelectOption[];
  validation?: ValidationRule[];
}

export interface SelectOption {
  value: any;
  label: string;
  disabled?: boolean;
}

export interface ValidationRule {
  type: 'required' | 'minLength' | 'maxLength' | 'pattern' | 'custom';
  value?: any;
  message: string;
  validator?: (value: any) => boolean;
} 