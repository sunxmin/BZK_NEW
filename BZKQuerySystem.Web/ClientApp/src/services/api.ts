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

// API�ͻ�������
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
    // ����������
    this.instance.interceptors.request.use(
      (config) => {
        // �����֤���ƣ������Ҫ��
        const token = localStorage.getItem('auth_token');
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }

        // �������ID����׷��
        config.headers['X-Request-ID'] = this.generateRequestId();

        return config;
      },
      (error) => {
        return Promise.reject(error);
      }
    );

    // ��Ӧ������
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
          toast.error('��¼�ѹ��ڣ������µ�¼');
          // �ض��򵽵�¼ҳ��
          window.location.href = '/Account/Login';
          break;
        case 403:
          toast.error('û��Ȩ��ִ�д˲���');
          break;
        case 404:
          toast.error('�������Դ������');
          break;
        case 408:
          toast.error('����ʱ�����Ժ�����');
          break;
        case 429:
          toast.error('�������Ƶ�������Ժ�����');
          break;
        case 500:
          toast.error('�������ڲ�����');
          break;
        default:
          toast.error(errorData?.message || '����ʧ��');
      }
    } else if (error.request) {
      toast.error('��������ʧ�ܣ���������');
    } else {
      toast.error('�������ô���');
    }
  }

  // ͨ�����󷽷�
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

  // �ļ�����
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

// ��ѯ������API
export const QueryBuilderAPI = {
  // ��ȡ�û��ɷ��ʵı�
  getUserTables: (userId: string): Promise<TableInfo[]> =>
    apiClient.get(`/QueryBuilder/GetUserTables/${userId}`),

  // ��ȡ�������Ϣ
  getTableColumns: (tableName: string): Promise<ColumnInfo[]> =>
    apiClient.get(`/QueryBuilder/GetTableColumns/${tableName}`),

  // ִ�в�ѯ
  executeQuery: (userId: string, request: QueryRequest): Promise<QueryResult> =>
    apiClient.post(`/QueryBuilder/ExecuteQuery/${userId}`, request),

  // ��ҳ��ѯ
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

  // ����SQLԤ��
  generatePreviewSQL: (request: QueryRequest): string => {
    // ����ʵ��SQL�����߼�
    // ��ʵ�֣�ʵ��Ӧ���ں�˴���
    return 'SELECT * FROM ' + request.tables.join(', ');
  },

  // �����ѯ
  saveQuery: (userId: string, queryData: {
    name: string;
    description: string;
    request: QueryRequest;
  }): Promise<SavedQuery> =>
    apiClient.post(`/QueryBuilder/SaveQuery/${userId}`, queryData),

  // ��ȡ����Ĳ�ѯ
  getSavedQueries: (userId: string): Promise<SavedQuery[]> =>
    apiClient.get(`/QueryBuilder/GetSavedQueries/${userId}`),

  // ɾ������Ĳ�ѯ
  deleteSavedQuery: (queryId: number): Promise<void> =>
    apiClient.delete(`/QueryBuilder/DeleteSavedQuery/${queryId}`),

  // ������ѯ���
  exportQuery: (
    userId: string, 
    request: QueryRequest, 
    format: 'excel' | 'csv'
  ): Promise<Blob> =>
    apiClient.post(`/QueryBuilder/ExportQuery/${userId}`, 
      { ...request, format }, 
      { responseType: 'blob' }
    ),

  // �����ѯ
  shareQuery: (queryId: number, userIds: string[]): Promise<void> =>
    apiClient.post(`/QueryBuilder/ShareQuery/${queryId}`, { userIds }),

  // ��ȡ��ѯ������Ϣ
  getQueryPerformance: (userId: string, request: QueryRequest): Promise<any> =>
    apiClient.post(`/QueryBuilder/GetQueryPerformance/${userId}`, request)
};

// �û�����API
export const UserAPI = {
  // ��ȡ��ǰ�û���Ϣ
  getCurrentUser: (): Promise<User> =>
    apiClient.get('/User/GetCurrentUser'),

  // ��ȡ�����û�
  getAllUsers: (): Promise<User[]> =>
    apiClient.get('/User/GetAllUsers'),

  // �����û�
  createUser: (userData: any): Promise<User> =>
    apiClient.post('/User/CreateUser', userData),

  // �����û�
  updateUser: (userId: string, userData: any): Promise<User> =>
    apiClient.put(`/User/UpdateUser/${userId}`, userData),

  // ɾ���û�
  deleteUser: (userId: string): Promise<void> =>
    apiClient.delete(`/User/DeleteUser/${userId}`),

  // ��������
  resetPassword: (userId: string, newPassword: string): Promise<void> =>
    apiClient.post(`/User/ResetPassword/${userId}`, { newPassword }),

  // ��ȡ�û�Ȩ��
  getUserPermissions: (userId: string): Promise<string[]> =>
    apiClient.get(`/User/GetUserPermissions/${userId}`),

  // �����û�Ȩ��
  updateUserPermissions: (userId: string, permissions: string[]): Promise<void> =>
    apiClient.post(`/User/UpdateUserPermissions/${userId}`, { permissions })
};

// �����API
export const TableAPI = {
  // ��ȡ���б�
  getAllTables: (): Promise<TableInfo[]> =>
    apiClient.get('/Table/GetAllTables'),

  // ˢ�±�ṹ
  refreshTableSchema: (): Promise<void> =>
    apiClient.post('/Table/RefreshSchema'),

  // ���ñ�Ȩ��
  setTablePermissions: (tableName: string, permissions: any): Promise<void> =>
    apiClient.post(`/Table/SetPermissions/${tableName}`, permissions),

  // ������ͼ
  createView: (viewData: any): Promise<void> =>
    apiClient.post('/Table/CreateView', viewData),

  // ɾ����ͼ
  deleteView: (viewName: string): Promise<void> =>
    apiClient.delete(`/Table/DeleteView/${viewName}`)
};

// ϵͳ���API
export const MonitoringAPI = {
  // ��ȡϵͳ����״̬
  getSystemHealth: (): Promise<SystemHealth> =>
    apiClient.get('/Monitoring/Health'),

  // ��ȡ����ָ��
  getPerformanceMetrics: (): Promise<PerformanceMetrics> =>
    apiClient.get('/Monitoring/Performance'),

  // ��ȡ�û��ͳ��
  getUserActivity: (fromDate?: string, toDate?: string): Promise<any> =>
    apiClient.get('/Monitoring/UserActivity', {
      params: { fromDate, toDate }
    }),

  // ��ȡϵͳ��־
  getSystemLogs: (level: string = 'Error', hours: number = 24, limit: number = 100): Promise<any[]> =>
    apiClient.get('/Monitoring/Logs', {
      params: { level, hours, limit }
    }),

  // ����ϵͳ
  cleanupSystem: (): Promise<any> =>
    apiClient.post('/Monitoring/Cleanup')
};

// �����־API
export const AuditAPI = {
  // ��ȡ�û������־
  getUserAuditLogs: (userId: string, fromDate?: string, toDate?: string): Promise<AuditLog[]> =>
    apiClient.get(`/Audit/GetUserLogs/${userId}`, {
      params: { fromDate, toDate }
    }),

  // ��ȡϵͳ�����־
  getSystemAuditLogs: (query: AuditLogQuery): Promise<PaginatedResult<AuditLog>> =>
    apiClient.post('/Audit/GetSystemLogs', query),

  // ��ȡ���ͳ��
  getAuditStatistics: (fromDate?: string, toDate?: string): Promise<any> =>
    apiClient.get('/Audit/GetStatistics', {
      params: { fromDate, toDate }
    })
};

// ��֤API
export const AuthAPI = {
  // ��¼
  login: (username: string, password: string): Promise<any> =>
    apiClient.post('/Account/Login', { username, password }),

  // �ǳ�
  logout: (): Promise<void> =>
    apiClient.post('/Account/Logout'),

  // �����֤״̬
  checkAuth: (): Promise<boolean> =>
    apiClient.get('/Account/CheckAuth'),

  // �޸�����
  changePassword: (currentPassword: string, newPassword: string): Promise<void> =>
    apiClient.post('/Account/ChangePassword', { currentPassword, newPassword })
};

// ����Ĭ��API�ͻ���
export default apiClient; 