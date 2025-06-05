import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import { toast } from 'react-hot-toast';
import type {
  TableInfo,
  ColumnInfo,
  QueryRequest,
  QueryResult,
  SavedQuery,
  User,
  SystemHealth,
  PerformanceMetrics,
  AuditLog,
  AuditLogQuery,
  PaginatedResult,
  ApiResponse,
  ApiErrorResponse
} from '../types/query';

// API客户端配置
class ApiClient {
  private instance: AxiosInstance;

  constructor() {
    this.instance = axios.create({
      baseURL: '/api',
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json'
      }
    });

    this.setupInterceptors();
  }

  private setupInterceptors() {
    // 请求拦截器
    this.instance.interceptors.request.use(
      (config) => {
        // 添加认证令牌（如果需要）
        const token = localStorage.getItem('auth_token');
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }

        // 添加请求ID用于追踪
        config.headers['X-Request-ID'] = this.generateRequestId();

        return config;
      },
      (error) => {
        return Promise.reject(error);
      }
    );

    // 响应拦截器
    this.instance.interceptors.response.use(
      (response) => {
        return response;
      },
      (error) => {
        this.handleApiError(error);
        return Promise.reject(error);
      }
    );
  }

  private generateRequestId(): string {
    return Math.random().toString(36).substring(2) + Date.now().toString(36);
  }

  private handleApiError(error: any) {
    if (error.response) {
      const errorData: ApiErrorResponse = error.response.data;
      
      switch (error.response.status) {
        case 401:
          toast.error('登录已过期，请重新登录');
          // 重定向到登录页面
          window.location.href = '/Account/Login';
          break;
        case 403:
          toast.error('没有权限执行此操作');
          break;
        case 404:
          toast.error('请求的资源不存在');
          break;
        case 408:
          toast.error('请求超时，请稍后重试');
          break;
        case 429:
          toast.error('请求过于频繁，请稍后重试');
          break;
        case 500:
          toast.error('服务器内部错误');
          break;
        default:
          toast.error(errorData?.message || '请求失败');
      }
    } else if (error.request) {
      toast.error('网络连接失败，请检查网络');
    } else {
      toast.error('请求配置错误');
    }
  }

  // 通用请求方法
  async get<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.instance.get<T>(url, config);
    return response.data;
  }

  async post<T>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.instance.post<T>(url, data, config);
    return response.data;
  }

  async put<T>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.instance.put<T>(url, data, config);
    return response.data;
  }

  async delete<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.instance.delete<T>(url, config);
    return response.data;
  }

  // 文件下载
  async downloadFile(url: string, filename: string, config?: AxiosRequestConfig): Promise<void> {
    const response = await this.instance.get(url, {
      ...config,
      responseType: 'blob'
    });

    const blob = new Blob([response.data]);
    const downloadUrl = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = downloadUrl;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(downloadUrl);
  }
}

const apiClient = new ApiClient();

// 查询构建器API
export const QueryBuilderAPI = {
  // 获取用户可访问的表
  getUserTables: (userId: string): Promise<TableInfo[]> =>
    apiClient.get(`/QueryBuilder/GetUserTables/${userId}`),

  // 获取表的列信息
  getTableColumns: (tableName: string): Promise<ColumnInfo[]> =>
    apiClient.get(`/QueryBuilder/GetTableColumns/${tableName}`),

  // 执行查询
  executeQuery: (userId: string, request: QueryRequest): Promise<QueryResult> =>
    apiClient.post(`/QueryBuilder/ExecuteQuery/${userId}`, request),

  // 分页查询
  executePaginatedQuery: (
    userId: string, 
    request: QueryRequest, 
    pageNumber: number = 1, 
    pageSize: number = 100
  ): Promise<PaginatedResult<any>> =>
    apiClient.post(`/QueryBuilder/ExecutePaginatedQuery/${userId}`, {
      ...request,
      pageNumber,
      pageSize
    }),

  // 生成SQL预览
  generatePreviewSQL: (request: QueryRequest): string => {
    // 这里实现SQL生成逻辑
    // 简化实现，实际应该在后端处理
    return 'SELECT * FROM ' + request.tables.join(', ');
  },

  // 保存查询
  saveQuery: (userId: string, queryData: {
    name: string;
    description: string;
    request: QueryRequest;
  }): Promise<SavedQuery> =>
    apiClient.post(`/QueryBuilder/SaveQuery/${userId}`, queryData),

  // 获取保存的查询
  getSavedQueries: (userId: string): Promise<SavedQuery[]> =>
    apiClient.get(`/QueryBuilder/GetSavedQueries/${userId}`),

  // 删除保存的查询
  deleteSavedQuery: (queryId: number): Promise<void> =>
    apiClient.delete(`/QueryBuilder/DeleteSavedQuery/${queryId}`),

  // 导出查询结果
  exportQuery: (
    userId: string, 
    request: QueryRequest, 
    format: 'excel' | 'csv'
  ): Promise<Blob> =>
    apiClient.post(`/QueryBuilder/ExportQuery/${userId}`, 
      { ...request, format }, 
      { responseType: 'blob' }
    ),

  // 分享查询
  shareQuery: (queryId: number, userIds: string[]): Promise<void> =>
    apiClient.post(`/QueryBuilder/ShareQuery/${queryId}`, { userIds }),

  // 获取查询性能信息
  getQueryPerformance: (userId: string, request: QueryRequest): Promise<any> =>
    apiClient.post(`/QueryBuilder/GetQueryPerformance/${userId}`, request)
};

// 用户管理API
export const UserAPI = {
  // 获取当前用户信息
  getCurrentUser: (): Promise<User> =>
    apiClient.get('/User/GetCurrentUser'),

  // 获取所有用户
  getAllUsers: (): Promise<User[]> =>
    apiClient.get('/User/GetAllUsers'),

  // 创建用户
  createUser: (userData: any): Promise<User> =>
    apiClient.post('/User/CreateUser', userData),

  // 更新用户
  updateUser: (userId: string, userData: any): Promise<User> =>
    apiClient.put(`/User/UpdateUser/${userId}`, userData),

  // 删除用户
  deleteUser: (userId: string): Promise<void> =>
    apiClient.delete(`/User/DeleteUser/${userId}`),

  // 重置密码
  resetPassword: (userId: string, newPassword: string): Promise<void> =>
    apiClient.post(`/User/ResetPassword/${userId}`, { newPassword }),

  // 获取用户权限
  getUserPermissions: (userId: string): Promise<string[]> =>
    apiClient.get(`/User/GetUserPermissions/${userId}`),

  // 更新用户权限
  updateUserPermissions: (userId: string, permissions: string[]): Promise<void> =>
    apiClient.post(`/User/UpdateUserPermissions/${userId}`, { permissions })
};

// 表管理API
export const TableAPI = {
  // 获取所有表
  getAllTables: (): Promise<TableInfo[]> =>
    apiClient.get('/Table/GetAllTables'),

  // 刷新表结构
  refreshTableSchema: (): Promise<void> =>
    apiClient.post('/Table/RefreshSchema'),

  // 设置表权限
  setTablePermissions: (tableName: string, permissions: any): Promise<void> =>
    apiClient.post(`/Table/SetPermissions/${tableName}`, permissions),

  // 创建视图
  createView: (viewData: any): Promise<void> =>
    apiClient.post('/Table/CreateView', viewData),

  // 删除视图
  deleteView: (viewName: string): Promise<void> =>
    apiClient.delete(`/Table/DeleteView/${viewName}`)
};

// 系统监控API
export const MonitoringAPI = {
  // 获取系统健康状态
  getSystemHealth: (): Promise<SystemHealth> =>
    apiClient.get('/Monitoring/Health'),

  // 获取性能指标
  getPerformanceMetrics: (): Promise<PerformanceMetrics> =>
    apiClient.get('/Monitoring/Performance'),

  // 获取用户活动统计
  getUserActivity: (fromDate?: string, toDate?: string): Promise<any> =>
    apiClient.get('/Monitoring/UserActivity', {
      params: { fromDate, toDate }
    }),

  // 获取系统日志
  getSystemLogs: (level: string = 'Error', hours: number = 24, limit: number = 100): Promise<any[]> =>
    apiClient.get('/Monitoring/Logs', {
      params: { level, hours, limit }
    }),

  // 清理系统
  cleanupSystem: (): Promise<any> =>
    apiClient.post('/Monitoring/Cleanup')
};

// 审计日志API
export const AuditAPI = {
  // 获取用户审计日志
  getUserAuditLogs: (userId: string, fromDate?: string, toDate?: string): Promise<AuditLog[]> =>
    apiClient.get(`/Audit/GetUserLogs/${userId}`, {
      params: { fromDate, toDate }
    }),

  // 获取系统审计日志
  getSystemAuditLogs: (query: AuditLogQuery): Promise<PaginatedResult<AuditLog>> =>
    apiClient.post('/Audit/GetSystemLogs', query),

  // 获取审计统计
  getAuditStatistics: (fromDate?: string, toDate?: string): Promise<any> =>
    apiClient.get('/Audit/GetStatistics', {
      params: { fromDate, toDate }
    })
};

// 认证API
export const AuthAPI = {
  // 登录
  login: (username: string, password: string): Promise<any> =>
    apiClient.post('/Account/Login', { username, password }),

  // 登出
  logout: (): Promise<void> =>
    apiClient.post('/Account/Logout'),

  // 检查认证状态
  checkAuth: (): Promise<boolean> =>
    apiClient.get('/Account/CheckAuth'),

  // 修改密码
  changePassword: (currentPassword: string, newPassword: string): Promise<void> =>
    apiClient.post('/Account/ChangePassword', { currentPassword, newPassword })
};

// 导出默认API客户端
export default apiClient; 