import React, { useState, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { 
  ChartBarIcon, 
  ComputerDesktopIcon, 
  ClockIcon,
  ExclamationTriangleIcon,
  CheckCircleIcon,
  XCircleIcon
} from '@heroicons/react/24/outline';
import { MonitoringAPI } from '../../services/api';
import type { SystemHealth, PerformanceMetrics } from '../../types/query';

interface SystemMonitorProps {
  className?: string;
}

export const SystemMonitor: React.FC<SystemMonitorProps> = ({ className = '' }) => {
  const [refreshInterval, setRefreshInterval] = useState(30000); // 30��
  const [autoRefresh, setAutoRefresh] = useState(true);

  // ��ȡϵͳ����״̬
  const { 
    data: healthData, 
    isLoading: healthLoading, 
    error: healthError,
    refetch: refetchHealth 
  } = useQuery({
    queryKey: ['systemHealth'],
    queryFn: MonitoringAPI.getSystemHealth,
    refetchInterval: autoRefresh ? refreshInterval : false,
    retry: 2
  });

  // ��ȡ����ָ��
  const { 
    data: performanceData, 
    isLoading: performanceLoading,
    refetch: refetchPerformance 
  } = useQuery({
    queryKey: ['performanceMetrics'],
    queryFn: MonitoringAPI.getPerformanceMetrics,
    refetchInterval: autoRefresh ? refreshInterval : false,
    retry: 2
  });

  // �ֶ�ˢ��
  const handleManualRefresh = () => {
    refetchHealth();
    refetchPerformance();
  };

  // ״ָ̬ʾ�����
  const StatusIndicator: React.FC<{ status: string }> = ({ status }) => {
    switch (status.toLowerCase()) {
      case 'healthy':
        return <CheckCircleIcon className="w-5 h-5 text-green-500" />;
      case 'warning':
        return <ExclamationTriangleIcon className="w-5 h-5 text-yellow-500" />;
      case 'critical':
        return <XCircleIcon className="w-5 h-5 text-red-500" />;
      default:
        return <ClockIcon className="w-5 h-5 text-gray-500" />;
    }
  };

  // ϵͳ������Ƭ
  const SystemHealthCard: React.FC<{ health: SystemHealth }> = ({ health }) => (
    <div className="bg-white rounded-lg shadow-md p-6">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold text-gray-900">ϵͳ����״̬</h3>
        <div className="flex items-center space-x-2">
          <StatusIndicator status={health.overallStatus} />
          <span className={`font-medium ${
            health.overallStatus === 'Healthy' ? 'text-green-600' :
            health.overallStatus === 'Warning' ? 'text-yellow-600' : 'text-red-600'
          }`}>
            {health.overallStatus === 'Healthy' ? '����' :
             health.overallStatus === 'Warning' ? '����' : '����'}
          </span>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {/* ���ݿ�״̬ */}
        <div className="border rounded-lg p-4">
          <div className="flex items-center justify-between mb-2">
            <h4 className="font-medium text-gray-700">���ݿ�</h4>
            <StatusIndicator status={health.databaseStatus.isConnected ? 'healthy' : 'critical'} />
          </div>
          <div className="text-sm text-gray-600 space-y-1">
            <div>����ʱ��: {health.databaseStatus.connectionTime}ms</div>
            <div>��С: {health.databaseStatus.databaseSizeMB}MB</div>
          </div>
        </div>

        {/* ϵͳ��Դ */}
        <div className="border rounded-lg p-4">
          <div className="flex items-center justify-between mb-2">
            <h4 className="font-medium text-gray-700">ϵͳ��Դ</h4>
            <StatusIndicator status={health.systemResources.status?.toLowerCase() || 'unknown'} />
          </div>
          <div className="text-sm text-gray-600 space-y-1">
            <div>�ڴ�: {health.systemResources.memoryUsageMB}MB</div>
            <div>CPU: {health.systemResources.cpuUsagePercent}%</div>
            <div>�߳�: {health.systemResources.threadCount}</div>
          </div>
        </div>

        {/* Ӧ��״̬ */}
        <div className="border rounded-lg p-4">
          <div className="flex items-center justify-between mb-2">
            <h4 className="font-medium text-gray-700">Ӧ�ó���</h4>
            <StatusIndicator status={health.applicationStatus.status?.toLowerCase() || 'unknown'} />
          </div>
          <div className="text-sm text-gray-600 space-y-1">
            <div>�汾: {health.applicationStatus.version}</div>
            <div>����: {health.applicationStatus.environment}</div>
            <div>����: {new Date(health.applicationStatus.startTime).toLocaleString()}</div>
          </div>
        </div>
      </div>

      {/* ������� */}
      {health.recentErrors && health.recentErrors.length > 0 && (
        <div className="mt-4">
          <h4 className="font-medium text-gray-700 mb-2">�������</h4>
          <div className="space-y-2">
            {health.recentErrors.slice(0, 3).map((error, index) => (
              <div key={index} className="text-sm bg-red-50 border border-red-200 rounded p-2">
                <div className="flex justify-between">
                  <span className="text-red-800">{error.message}</span>
                  <span className="text-red-600 text-xs">
                    {new Date(error.timestamp).toLocaleString()}
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );

  // ����ָ�꿨Ƭ
  const PerformanceCard: React.FC<{ metrics: PerformanceMetrics }> = ({ metrics }) => (
    <div className="bg-white rounded-lg shadow-md p-6">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold text-gray-900">����ָ��</h3>
        <ChartBarIcon className="w-6 h-6 text-gray-400" />
      </div>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="text-center">
          <div className="text-2xl font-bold text-blue-600">{metrics.cpuUsage}%</div>
          <div className="text-sm text-gray-600">CPUʹ����</div>
        </div>
        <div className="text-center">
          <div className="text-2xl font-bold text-green-600">{metrics.memoryUsage}MB</div>
          <div className="text-sm text-gray-600">�ڴ�ʹ��</div>
        </div>
        <div className="text-center">
          <div className="text-2xl font-bold text-yellow-600">{metrics.diskUsage}%</div>
          <div className="text-sm text-gray-600">����ʹ����</div>
        </div>
        <div className="text-center">
          <div className="text-2xl font-bold text-purple-600">
            {metrics.applicationMetrics.activeSessions}
          </div>
          <div className="text-sm text-gray-600">��Ծ�Ự</div>
        </div>
      </div>

      {/* ���ݿ�ָ�� */}
      <div className="mt-6">
        <h4 className="font-medium text-gray-700 mb-3">���ݿ�ָ��</h4>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="text-center">
            <div className="text-lg font-semibold text-blue-600">
              {metrics.databaseMetrics.activeConnections}
            </div>
            <div className="text-xs text-gray-600">��Ծ����</div>
          </div>
          <div className="text-center">
            <div className="text-lg font-semibold text-green-600">
              {metrics.databaseMetrics.activeQueries}
            </div>
            <div className="text-xs text-gray-600">��Ծ��ѯ</div>
          </div>
          <div className="text-center">
            <div className="text-lg font-semibold text-yellow-600">
              {metrics.databaseMetrics.queryExecutionTime}ms
            </div>
            <div className="text-xs text-gray-600">ƽ����ѯʱ��</div>
          </div>
          <div className="text-center">
            <div className="text-lg font-semibold text-purple-600">
              {Math.round(metrics.databaseMetrics.databaseSizeBytes / 1024 / 1024)}MB
            </div>
            <div className="text-xs text-gray-600">���ݿ��С</div>
          </div>
        </div>
      </div>
    </div>
  );

  return (
    <div className={`space-y-6 ${className}`}>
      {/* ������� */}
      <div className="bg-white rounded-lg shadow-md p-4">
        <div className="flex items-center justify-between">
          <h2 className="text-xl font-bold text-gray-900">ϵͳ���</h2>
          <div className="flex items-center space-x-4">
            {/* �Զ�ˢ�¿��� */}
            <label className="flex items-center space-x-2">
              <input
                type="checkbox"
                checked={autoRefresh}
                onChange={(e) => setAutoRefresh(e.target.checked)}
                className="rounded border-gray-300"
              />
              <span className="text-sm text-gray-700">�Զ�ˢ��</span>
            </label>

            {/* ˢ�¼�� */}
            {autoRefresh && (
              <select
                value={refreshInterval}
                onChange={(e) => setRefreshInterval(Number(e.target.value))}
                className="text-sm border-gray-300 rounded-md"
              >
                <option value={10000}>10��</option>
                <option value={30000}>30��</option>
                <option value={60000}>1����</option>
                <option value={300000}>5����</option>
              </select>
            )}

            {/* �ֶ�ˢ�°�ť */}
            <button
              onClick={handleManualRefresh}
              disabled={healthLoading || performanceLoading}
              className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50"
            >
              {healthLoading || performanceLoading ? 'ˢ����...' : '�ֶ�ˢ��'}
            </button>
          </div>
        </div>
      </div>

      {/* ������ʾ */}
      {healthError && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <div className="flex">
            <XCircleIcon className="w-5 h-5 text-red-400" />
            <div className="ml-3">
              <h3 className="text-sm font-medium text-red-800">�޷���ȡϵͳ״̬</h3>
              <div className="mt-2 text-sm text-red-700">
                {healthError instanceof Error ? healthError.message : 'δ֪����'}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ����״̬ */}
      {(healthLoading || performanceLoading) && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <div className="flex items-center">
            <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
            <span className="ml-2 text-blue-800">���ڼ���ϵͳ״̬...</span>
          </div>
        </div>
      )}

      {/* ϵͳ����״̬ */}
      {healthData && <SystemHealthCard health={healthData} />}

      {/* ����ָ�� */}
      {performanceData && <PerformanceCard metrics={performanceData} />}

      {/* ������ʱ�� */}
      <div className="text-center text-sm text-gray-500">
        ������: {new Date().toLocaleString()}
      </div>
    </div>
  );
}; 