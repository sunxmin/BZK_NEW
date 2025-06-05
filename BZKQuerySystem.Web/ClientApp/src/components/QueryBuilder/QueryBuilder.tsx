import React, { useState, useCallback, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm, Controller } from 'react-hook-form';
import Select from 'react-select';
import { toast } from 'react-hot-toast';
import { 
  Database, 
  Plus, 
  Trash2, 
  Play, 
  Download, 
  Save, 
  RotateCcw,
  Filter,
  SortAsc,
  SortDesc,
  Eye,
  AlertCircle
} from 'lucide-react';
import { QueryBuilderAPI } from '../../services/api';
import { VirtualizedTable } from '../Table/VirtualizedTable';
import { LoadingSpinner } from '../UI/LoadingSpinner';
import { ErrorBoundary } from '../UI/ErrorBoundary';
import type { 
  QueryRequest, 
  TableInfo, 
  ColumnInfo, 
  FilterCondition, 
  JoinCondition,
  QueryResult 
} from '../../types/query';

interface QueryBuilderProps {
  userId: string;
  onQuerySave?: (queryId: string) => void;
}

export const QueryBuilder: React.FC<QueryBuilderProps> = ({ userId, onQuerySave }) => {
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<'builder' | 'results' | 'sql'>('builder');
  const [queryResult, setQueryResult] = useState<QueryResult | null>(null);
  const [isExecuting, setIsExecuting] = useState(false);

  const { control, watch, setValue, reset, handleSubmit } = useForm<QueryRequest>({
    defaultValues: {
      tables: [],
      columns: [],
      joins: [],
      filters: [],
      orderBy: [],
      isDistinct: false,
      topCount: undefined
    }
  });

  const watchedValues = watch();

  // ��ȡ�û��ɷ��ʵı�
  const { data: availableTables, isLoading: tablesLoading } = useQuery({
    queryKey: ['user-tables', userId],
    queryFn: () => QueryBuilderAPI.getUserTables(userId),
    staleTime: 5 * 60 * 1000, // 5���ӻ���
  });

  // ��ȡѡ�б������Ϣ
  const { data: tableColumns } = useQuery({
    queryKey: ['table-columns', watchedValues.tables],
    queryFn: () => Promise.all(
      watchedValues.tables.map(tableName => 
        QueryBuilderAPI.getTableColumns(tableName)
      )
    ),
    enabled: watchedValues.tables.length > 0,
  });

  // ִ�в�ѯ
  const executeQueryMutation = useMutation({
    mutationFn: (request: QueryRequest) => QueryBuilderAPI.executeQuery(userId, request),
    onMutate: () => {
      setIsExecuting(true);
      toast.loading('����ִ�в�ѯ...', { id: 'query-execution' });
    },
    onSuccess: (result) => {
      setQueryResult(result);
      setActiveTab('results');
      toast.success(`��ѯ�ɹ��������� ${result.totalCount} ����¼`, { id: 'query-execution' });
    },
    onError: (error: any) => {
      console.error('��ѯִ��ʧ��:', error);
      toast.error(error.response?.data?.message || '��ѯִ��ʧ��', { id: 'query-execution' });
    },
    onSettled: () => {
      setIsExecuting(false);
    }
  });

  // �����ѯ
  const saveQueryMutation = useMutation({
    mutationFn: (queryData: { name: string; description: string; request: QueryRequest }) => 
      QueryBuilderAPI.saveQuery(userId, queryData),
    onSuccess: (savedQuery) => {
      toast.success('��ѯ����ɹ���');
      onQuerySave?.(savedQuery.id);
      queryClient.invalidateQueries({ queryKey: ['saved-queries', userId] });
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || '�����ѯʧ��');
    }
  });

  // ��������
  const exportDataMutation = useMutation({
    mutationFn: (format: 'excel' | 'csv') => 
      QueryBuilderAPI.exportQuery(userId, watchedValues, format),
    onSuccess: (blob, format) => {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `query_result_${new Date().getTime()}.${format}`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
      toast.success('�����ɹ���');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || '����ʧ��');
    }
  });

  // ��ӱ�
  const handleAddTable = useCallback((tableName: string) => {
    const currentTables = watchedValues.tables;
    if (!currentTables.includes(tableName)) {
      setValue('tables', [...currentTables, tableName]);
    }
  }, [watchedValues.tables, setValue]);

  // �Ƴ���
  const handleRemoveTable = useCallback((tableName: string) => {
    const newTables = watchedValues.tables.filter(t => t !== tableName);
    setValue('tables', newTables);
    
    // ������ص��С����Ӻ͹�������
    const newColumns = watchedValues.columns.filter(c => c.tableName !== tableName);
    const newJoins = watchedValues.joins.filter(j => 
      j.leftTable !== tableName && j.rightTable !== tableName
    );
    const newFilters = watchedValues.filters.filter(f => f.tableName !== tableName);
    
    setValue('columns', newColumns);
    setValue('joins', newJoins);
    setValue('filters', newFilters);
  }, [watchedValues, setValue]);

  // �����
  const handleAddColumn = useCallback((column: { tableName: string; columnName: string }) => {
    const currentColumns = watchedValues.columns;
    const exists = currentColumns.some(c => 
      c.tableName === column.tableName && c.columnName === column.columnName
    );
    
    if (!exists) {
      setValue('columns', [...currentColumns, column]);
    }
  }, [watchedValues.columns, setValue]);

  // ��ӹ�������
  const handleAddFilter = useCallback(() => {
    const newFilter: FilterCondition = {
      tableName: '',
      columnName: '',
      operator: '=',
      value: '',
      logicalOperator: 'AND'
    };
    setValue('filters', [...watchedValues.filters, newFilter]);
  }, [watchedValues.filters, setValue]);

  // �����������
  const handleAddJoin = useCallback(() => {
    if (watchedValues.tables.length < 2) {
      toast.error('������Ҫ��������������������');
      return;
    }
    
    const newJoin: JoinCondition = {
      joinType: 'INNER',
      leftTable: watchedValues.tables[0],
      leftColumn: '',
      rightTable: watchedValues.tables[1],
      rightColumn: ''
    };
    setValue('joins', [...watchedValues.joins, newJoin]);
  }, [watchedValues.tables, watchedValues.joins, setValue]);

  // ����Ԥ��SQL
  const previewSQL = useMemo(() => {
    if (watchedValues.tables.length === 0 || watchedValues.columns.length === 0) {
      return '';
    }

    try {
      return QueryBuilderAPI.generatePreviewSQL(watchedValues);
    } catch (error) {
      return '-- SQL����ʧ�ܣ������ѯ����';
    }
  }, [watchedValues]);

  // ִ�в�ѯ
  const handleExecuteQuery = useCallback(() => {
    if (watchedValues.tables.length === 0) {
      toast.error('������ѡ��һ����');
      return;
    }
    
    if (watchedValues.columns.length === 0) {
      toast.error('������ѡ��һ����');
      return;
    }

    executeQueryMutation.mutate(watchedValues);
  }, [watchedValues, executeQueryMutation]);

  // ���ò�ѯ
  const handleResetQuery = useCallback(() => {
    reset();
    setQueryResult(null);
    setActiveTab('builder');
    toast.success('��ѯ������');
  }, [reset]);

  if (tablesLoading) {
    return <LoadingSpinner size="lg" text="���ڼ��ر���Ϣ..." />;
  }

  return (
    <ErrorBoundary>
      <div className="h-full flex flex-col bg-white">
        {/* ������ */}
        <div className="flex items-center justify-between p-4 border-b bg-gray-50">
          <div className="flex items-center space-x-2">
            <Database className="w-6 h-6 text-blue-600" />
            <h1 className="text-xl font-semibold text-gray-900">��ѯ������</h1>
          </div>
          
          <div className="flex items-center space-x-2">
            <button
              onClick={handleExecuteQuery}
              disabled={isExecuting || watchedValues.tables.length === 0}
              className="flex items-center space-x-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <Play className="w-4 h-4" />
              <span>ִ�в�ѯ</span>
            </button>
            
            <button
              onClick={handleResetQuery}
              className="flex items-center space-x-2 px-4 py-2 bg-gray-500 text-white rounded-lg hover:bg-gray-600"
            >
              <RotateCcw className="w-4 h-4" />
              <span>����</span>
            </button>
            
            {queryResult && (
              <>
                <button
                  onClick={() => exportDataMutation.mutate('excel')}
                  disabled={exportDataMutation.isPending}
                  className="flex items-center space-x-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50"
                >
                  <Download className="w-4 h-4" />
                  <span>����Excel</span>
                </button>
                
                <button
                  onClick={() => {
                    const queryName = prompt('�������ѯ����:');
                    const queryDesc = prompt('�������ѯ����:');
                    if (queryName) {
                      saveQueryMutation.mutate({
                        name: queryName,
                        description: queryDesc || '',
                        request: watchedValues
                      });
                    }
                  }}
                  disabled={saveQueryMutation.isPending}
                  className="flex items-center space-x-2 px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:opacity-50"
                >
                  <Save className="w-4 h-4" />
                  <span>�����ѯ</span>
                </button>
              </>
            )}
          </div>
        </div>

        {/* ��ǩҳ */}
        <div className="flex border-b">
          <button
            onClick={() => setActiveTab('builder')}
            className={`px-4 py-2 font-medium ${
              activeTab === 'builder'
                ? 'text-blue-600 border-b-2 border-blue-600 bg-blue-50'
                : 'text-gray-600 hover:text-gray-900'
            }`}
          >
            ��ѯ������
          </button>
          <button
            onClick={() => setActiveTab('sql')}
            className={`px-4 py-2 font-medium ${
              activeTab === 'sql'
                ? 'text-blue-600 border-b-2 border-blue-600 bg-blue-50'
                : 'text-gray-600 hover:text-gray-900'
            }`}
          >
            SQLԤ��
          </button>
          {queryResult && (
            <button
              onClick={() => setActiveTab('results')}
              className={`px-4 py-2 font-medium ${
                activeTab === 'results'
                  ? 'text-blue-600 border-b-2 border-blue-600 bg-blue-50'
                  : 'text-gray-600 hover:text-gray-900'
              }`}
            >
              ��ѯ��� ({queryResult.totalCount} ��)
            </button>
          )}
        </div>

        {/* �������� */}
        <div className="flex-1 overflow-hidden">
          {activeTab === 'builder' && (
            <QueryBuilderForm 
              control={control}
              watchedValues={watchedValues}
              availableTables={availableTables || []}
              tableColumns={tableColumns || []}
              onAddTable={handleAddTable}
              onRemoveTable={handleRemoveTable}
              onAddColumn={handleAddColumn}
              onAddFilter={handleAddFilter}
              onAddJoin={handleAddJoin}
            />
          )}
          
          {activeTab === 'sql' && (
            <div className="h-full p-4">
              <div className="h-full bg-gray-900 text-white p-4 rounded-lg font-mono text-sm overflow-auto">
                <pre>{previewSQL || '-- �����ò�ѯ����������SQL'}</pre>
              </div>
            </div>
          )}
          
          {activeTab === 'results' && queryResult && (
            <div className="h-full p-4">
              <VirtualizedTable 
                data={queryResult.data} 
                columns={queryResult.columns}
                totalCount={queryResult.totalCount}
                onPageChange={(page) => {
                  // �����ҳ
                }}
              />
            </div>
          )}
        </div>
      </div>
    </ErrorBoundary>
  );
};

// ���������ѯ��������
interface QueryBuilderFormProps {
  control: any;
  watchedValues: QueryRequest;
  availableTables: TableInfo[];
  tableColumns: ColumnInfo[][];
  onAddTable: (tableName: string) => void;
  onRemoveTable: (tableName: string) => void;
  onAddColumn: (column: { tableName: string; columnName: string }) => void;
  onAddFilter: () => void;
  onAddJoin: () => void;
}

const QueryBuilderForm: React.FC<QueryBuilderFormProps> = ({
  control,
  watchedValues,
  availableTables,
  tableColumns,
  onAddTable,
  onRemoveTable,
  onAddColumn,
  onAddFilter,
  onAddJoin
}) => {
  const tableOptions = availableTables.map(table => ({
    value: table.tableName,
    label: `${table.displayName} (${table.tableName})`
  }));

  return (
    <div className="h-full overflow-auto">
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 p-6">
        {/* ������ */}
        <div className="space-y-6">
          {/* ��ѡ�� */}
          <div className="bg-white border rounded-lg p-4">
            <div className="flex items-center space-x-2 mb-4">
              <Database className="w-5 h-5 text-blue-600" />
              <h3 className="text-lg font-medium">���ݱ�</h3>
            </div>
            
            <Select
              isMulti
              options={tableOptions}
              placeholder="ѡ�����ݱ�..."
              value={watchedValues.tables.map(tableName => ({
                value: tableName,
                label: availableTables.find(t => t.tableName === tableName)?.displayName || tableName
              }))}
              onChange={(selected) => {
                const newTables = selected?.map(option => option.value) || [];
                const currentTables = watchedValues.tables;
                
                // ����±�
                newTables.forEach(table => {
                  if (!currentTables.includes(table)) {
                    onAddTable(table);
                  }
                });
                
                // �Ƴ�ɾ���ı�
                currentTables.forEach(table => {
                  if (!newTables.includes(table)) {
                    onRemoveTable(table);
                  }
                });
              }}
              className="react-select-container"
              classNamePrefix="react-select"
            />
            
            {watchedValues.tables.length > 0 && (
              <div className="mt-4 space-y-2">
                {watchedValues.tables.map(tableName => {
                  const table = availableTables.find(t => t.tableName === tableName);
                  return (
                    <div key={tableName} className="flex items-center justify-between p-2 bg-gray-50 rounded">
                      <span className="text-sm font-medium">{table?.displayName || tableName}</span>
                      <button
                        onClick={() => onRemoveTable(tableName)}
                        className="text-red-500 hover:text-red-700"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          {/* ��ѡ�� */}
          <div className="bg-white border rounded-lg p-4">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center space-x-2">
                <Eye className="w-5 h-5 text-green-600" />
                <h3 className="text-lg font-medium">ѡ����</h3>
              </div>
              <span className="text-sm text-gray-500">
                ��ѡ�� {watchedValues.columns.length} ��
              </span>
            </div>
            
            {watchedValues.tables.length === 0 ? (
              <div className="text-center py-8 text-gray-500">
                <AlertCircle className="w-8 h-8 mx-auto mb-2" />
                <p>����ѡ�����ݱ�</p>
              </div>
            ) : (
              <div className="space-y-4 max-h-64 overflow-y-auto">
                {watchedValues.tables.map((tableName, tableIndex) => {
                  const columns = tableColumns[tableIndex] || [];
                  return (
                    <div key={tableName} className="border rounded p-3">
                      <h4 className="font-medium text-gray-700 mb-2">{tableName}</h4>
                      <div className="grid grid-cols-1 gap-1">
                        {columns.map(column => {
                          const isSelected = watchedValues.columns.some(c => 
                            c.tableName === tableName && c.columnName === column.columnName
                          );
                          
                          return (
                            <label key={column.columnName} className="flex items-center space-x-2 text-sm">
                              <input
                                type="checkbox"
                                checked={isSelected}
                                onChange={(e) => {
                                  if (e.target.checked) {
                                    onAddColumn({ tableName, columnName: column.columnName });
                                  } else {
                                    // �Ƴ��е��߼�
                                  }
                                }}
                                className="rounded border-gray-300"
                              />
                              <span>{column.displayName || column.columnName}</span>
                              <span className="text-gray-400">({column.dataType})</span>
                            </label>
                          );
                        })}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>

        {/* �Ҳ���� */}
        <div className="space-y-6">
          {/* �������� */}
          <div className="bg-white border rounded-lg p-4">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center space-x-2">
                <Filter className="w-5 h-5 text-orange-600" />
                <h3 className="text-lg font-medium">��������</h3>
              </div>
              <button
                onClick={onAddFilter}
                className="flex items-center space-x-1 px-3 py-1 bg-orange-600 text-white rounded text-sm hover:bg-orange-700"
              >
                <Plus className="w-4 h-4" />
                <span>���</span>
              </button>
            </div>
            
            <div className="space-y-3">
              {watchedValues.filters.map((filter, index) => (
                <FilterRow
                  key={index}
                  filter={filter}
                  index={index}
                  availableColumns={watchedValues.columns}
                  onUpdate={(updatedFilter) => {
                    const newFilters = [...watchedValues.filters];
                    newFilters[index] = updatedFilter;
                    // setValue('filters', newFilters);
                  }}
                  onRemove={() => {
                    const newFilters = watchedValues.filters.filter((_, i) => i !== index);
                    // setValue('filters', newFilters);
                  }}
                />
              ))}
              
              {watchedValues.filters.length === 0 && (
                <div className="text-center py-4 text-gray-500">
                  <p>���޹�������</p>
                </div>
              )}
            </div>
          </div>

          {/* �������� */}
          {watchedValues.tables.length > 1 && (
            <div className="bg-white border rounded-lg p-4">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-lg font-medium">������</h3>
                <button
                  onClick={onAddJoin}
                  className="flex items-center space-x-1 px-3 py-1 bg-blue-600 text-white rounded text-sm hover:bg-blue-700"
                >
                  <Plus className="w-4 h-4" />
                  <span>�������</span>
                </button>
              </div>
              
              <div className="space-y-3">
                {watchedValues.joins.map((join, index) => (
                  <JoinRow
                    key={index}
                    join={join}
                    index={index}
                    availableTables={watchedValues.tables}
                    tableColumns={tableColumns}
                    onUpdate={(updatedJoin) => {
                      // ���������������߼�
                    }}
                    onRemove={() => {
                      // �Ƴ������������߼�
                    }}
                  />
                ))}
              </div>
            </div>
          )}

          {/* �������� */}
          <div className="bg-white border rounded-lg p-4">
            <div className="flex items-center space-x-2 mb-4">
              <SortAsc className="w-5 h-5 text-purple-600" />
              <h3 className="text-lg font-medium">����</h3>
            </div>
            
            {/* �������� */}
            <div className="space-y-3">
              {/* �����ֶ�ѡ���߼� */}
            </div>
          </div>

          {/* �߼�ѡ�� */}
          <div className="bg-white border rounded-lg p-4">
            <h3 className="text-lg font-medium mb-4">�߼�ѡ��</h3>
            
            <div className="space-y-3">
              <label className="flex items-center space-x-2">
                <Controller
                  name="isDistinct"
                  control={control}
                  render={({ field }) => (
                    <input
                      type="checkbox"
                      checked={field.value}
                      onChange={field.onChange}
                      className="rounded border-gray-300"
                    />
                  )}
                />
                <span>ȥ���ظ���¼ (DISTINCT)</span>
              </label>
              
              <div>
                <label className="block text-sm font-medium mb-1">���Ƽ�¼�� (TOP)</label>
                <Controller
                  name="topCount"
                  control={control}
                  render={({ field }) => (
                    <input
                      type="number"
                      min="1"
                      max="10000"
                      value={field.value || ''}
                      onChange={(e) => field.onChange(e.target.value ? parseInt(e.target.value) : undefined)}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md"
                      placeholder="����: 100"
                    />
                  )}
                />
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

// �������������
interface FilterRowProps {
  filter: FilterCondition;
  index: number;
  availableColumns: { tableName: string; columnName: string }[];
  onUpdate: (filter: FilterCondition) => void;
  onRemove: () => void;
}

const FilterRow: React.FC<FilterRowProps> = ({ filter, availableColumns, onUpdate, onRemove }) => {
  return (
    <div className="flex items-center space-x-2 p-3 border rounded">
      {/* ������������ */}
      <button onClick={onRemove} className="text-red-500 hover:text-red-700">
        <Trash2 className="w-4 h-4" />
      </button>
    </div>
  );
};

// �������������
interface JoinRowProps {
  join: JoinCondition;
  index: number;
  availableTables: string[];
  tableColumns: ColumnInfo[][];
  onUpdate: (join: JoinCondition) => void;
  onRemove: () => void;
}

const JoinRow: React.FC<JoinRowProps> = ({ join, availableTables, onUpdate, onRemove }) => {
  return (
    <div className="flex items-center space-x-2 p-3 border rounded">
      {/* ������������ */}
      <button onClick={onRemove} className="text-red-500 hover:text-red-700">
        <Trash2 className="w-4 h-4" />
      </button>
    </div>
  );
}; 