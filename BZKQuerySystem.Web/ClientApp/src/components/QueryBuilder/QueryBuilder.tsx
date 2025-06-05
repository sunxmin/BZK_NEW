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

  // 获取用户可访问的表
  const { data: availableTables, isLoading: tablesLoading } = useQuery({
    queryKey: ['user-tables', userId],
    queryFn: () => QueryBuilderAPI.getUserTables(userId),
    staleTime: 5 * 60 * 1000, // 5分钟缓存
  });

  // 获取选中表的列信息
  const { data: tableColumns } = useQuery({
    queryKey: ['table-columns', watchedValues.tables],
    queryFn: () => Promise.all(
      watchedValues.tables.map(tableName => 
        QueryBuilderAPI.getTableColumns(tableName)
      )
    ),
    enabled: watchedValues.tables.length > 0,
  });

  // 执行查询
  const executeQueryMutation = useMutation({
    mutationFn: (request: QueryRequest) => QueryBuilderAPI.executeQuery(userId, request),
    onMutate: () => {
      setIsExecuting(true);
      toast.loading('正在执行查询...', { id: 'query-execution' });
    },
    onSuccess: (result) => {
      setQueryResult(result);
      setActiveTab('results');
      toast.success(`查询成功！共返回 ${result.totalCount} 条记录`, { id: 'query-execution' });
    },
    onError: (error: any) => {
      console.error('查询执行失败:', error);
      toast.error(error.response?.data?.message || '查询执行失败', { id: 'query-execution' });
    },
    onSettled: () => {
      setIsExecuting(false);
    }
  });

  // 保存查询
  const saveQueryMutation = useMutation({
    mutationFn: (queryData: { name: string; description: string; request: QueryRequest }) => 
      QueryBuilderAPI.saveQuery(userId, queryData),
    onSuccess: (savedQuery) => {
      toast.success('查询保存成功！');
      onQuerySave?.(savedQuery.id);
      queryClient.invalidateQueries({ queryKey: ['saved-queries', userId] });
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || '保存查询失败');
    }
  });

  // 导出数据
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
      toast.success('导出成功！');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || '导出失败');
    }
  });

  // 添加表
  const handleAddTable = useCallback((tableName: string) => {
    const currentTables = watchedValues.tables;
    if (!currentTables.includes(tableName)) {
      setValue('tables', [...currentTables, tableName]);
    }
  }, [watchedValues.tables, setValue]);

  // 移除表
  const handleRemoveTable = useCallback((tableName: string) => {
    const newTables = watchedValues.tables.filter(t => t !== tableName);
    setValue('tables', newTables);
    
    // 清理相关的列、连接和过滤条件
    const newColumns = watchedValues.columns.filter(c => c.tableName !== tableName);
    const newJoins = watchedValues.joins.filter(j => 
      j.leftTable !== tableName && j.rightTable !== tableName
    );
    const newFilters = watchedValues.filters.filter(f => f.tableName !== tableName);
    
    setValue('columns', newColumns);
    setValue('joins', newJoins);
    setValue('filters', newFilters);
  }, [watchedValues, setValue]);

  // 添加列
  const handleAddColumn = useCallback((column: { tableName: string; columnName: string }) => {
    const currentColumns = watchedValues.columns;
    const exists = currentColumns.some(c => 
      c.tableName === column.tableName && c.columnName === column.columnName
    );
    
    if (!exists) {
      setValue('columns', [...currentColumns, column]);
    }
  }, [watchedValues.columns, setValue]);

  // 添加过滤条件
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

  // 添加连接条件
  const handleAddJoin = useCallback(() => {
    if (watchedValues.tables.length < 2) {
      toast.error('至少需要两个表才能添加连接条件');
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

  // 生成预览SQL
  const previewSQL = useMemo(() => {
    if (watchedValues.tables.length === 0 || watchedValues.columns.length === 0) {
      return '';
    }

    try {
      return QueryBuilderAPI.generatePreviewSQL(watchedValues);
    } catch (error) {
      return '-- SQL生成失败，请检查查询配置';
    }
  }, [watchedValues]);

  // 执行查询
  const handleExecuteQuery = useCallback(() => {
    if (watchedValues.tables.length === 0) {
      toast.error('请至少选择一个表');
      return;
    }
    
    if (watchedValues.columns.length === 0) {
      toast.error('请至少选择一个列');
      return;
    }

    executeQueryMutation.mutate(watchedValues);
  }, [watchedValues, executeQueryMutation]);

  // 重置查询
  const handleResetQuery = useCallback(() => {
    reset();
    setQueryResult(null);
    setActiveTab('builder');
    toast.success('查询已重置');
  }, [reset]);

  if (tablesLoading) {
    return <LoadingSpinner size="lg" text="正在加载表信息..." />;
  }

  return (
    <ErrorBoundary>
      <div className="h-full flex flex-col bg-white">
        {/* 工具栏 */}
        <div className="flex items-center justify-between p-4 border-b bg-gray-50">
          <div className="flex items-center space-x-2">
            <Database className="w-6 h-6 text-blue-600" />
            <h1 className="text-xl font-semibold text-gray-900">查询构建器</h1>
          </div>
          
          <div className="flex items-center space-x-2">
            <button
              onClick={handleExecuteQuery}
              disabled={isExecuting || watchedValues.tables.length === 0}
              className="flex items-center space-x-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <Play className="w-4 h-4" />
              <span>执行查询</span>
            </button>
            
            <button
              onClick={handleResetQuery}
              className="flex items-center space-x-2 px-4 py-2 bg-gray-500 text-white rounded-lg hover:bg-gray-600"
            >
              <RotateCcw className="w-4 h-4" />
              <span>重置</span>
            </button>
            
            {queryResult && (
              <>
                <button
                  onClick={() => exportDataMutation.mutate('excel')}
                  disabled={exportDataMutation.isPending}
                  className="flex items-center space-x-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50"
                >
                  <Download className="w-4 h-4" />
                  <span>导出Excel</span>
                </button>
                
                <button
                  onClick={() => {
                    const queryName = prompt('请输入查询名称:');
                    const queryDesc = prompt('请输入查询描述:');
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
                  <span>保存查询</span>
                </button>
              </>
            )}
          </div>
        </div>

        {/* 标签页 */}
        <div className="flex border-b">
          <button
            onClick={() => setActiveTab('builder')}
            className={`px-4 py-2 font-medium ${
              activeTab === 'builder'
                ? 'text-blue-600 border-b-2 border-blue-600 bg-blue-50'
                : 'text-gray-600 hover:text-gray-900'
            }`}
          >
            查询构建器
          </button>
          <button
            onClick={() => setActiveTab('sql')}
            className={`px-4 py-2 font-medium ${
              activeTab === 'sql'
                ? 'text-blue-600 border-b-2 border-blue-600 bg-blue-50'
                : 'text-gray-600 hover:text-gray-900'
            }`}
          >
            SQL预览
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
              查询结果 ({queryResult.totalCount} 条)
            </button>
          )}
        </div>

        {/* 内容区域 */}
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
                <pre>{previewSQL || '-- 请配置查询条件以生成SQL'}</pre>
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
                  // 处理分页
                }}
              />
            </div>
          )}
        </div>
      </div>
    </ErrorBoundary>
  );
};

// 子组件：查询构建器表单
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
        {/* 左侧面板 */}
        <div className="space-y-6">
          {/* 表选择 */}
          <div className="bg-white border rounded-lg p-4">
            <div className="flex items-center space-x-2 mb-4">
              <Database className="w-5 h-5 text-blue-600" />
              <h3 className="text-lg font-medium">数据表</h3>
            </div>
            
            <Select
              isMulti
              options={tableOptions}
              placeholder="选择数据表..."
              value={watchedValues.tables.map(tableName => ({
                value: tableName,
                label: availableTables.find(t => t.tableName === tableName)?.displayName || tableName
              }))}
              onChange={(selected) => {
                const newTables = selected?.map(option => option.value) || [];
                const currentTables = watchedValues.tables;
                
                // 添加新表
                newTables.forEach(table => {
                  if (!currentTables.includes(table)) {
                    onAddTable(table);
                  }
                });
                
                // 移除删除的表
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

          {/* 列选择 */}
          <div className="bg-white border rounded-lg p-4">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center space-x-2">
                <Eye className="w-5 h-5 text-green-600" />
                <h3 className="text-lg font-medium">选择列</h3>
              </div>
              <span className="text-sm text-gray-500">
                已选择 {watchedValues.columns.length} 列
              </span>
            </div>
            
            {watchedValues.tables.length === 0 ? (
              <div className="text-center py-8 text-gray-500">
                <AlertCircle className="w-8 h-8 mx-auto mb-2" />
                <p>请先选择数据表</p>
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
                                    // 移除列的逻辑
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

        {/* 右侧面板 */}
        <div className="space-y-6">
          {/* 过滤条件 */}
          <div className="bg-white border rounded-lg p-4">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center space-x-2">
                <Filter className="w-5 h-5 text-orange-600" />
                <h3 className="text-lg font-medium">过滤条件</h3>
              </div>
              <button
                onClick={onAddFilter}
                className="flex items-center space-x-1 px-3 py-1 bg-orange-600 text-white rounded text-sm hover:bg-orange-700"
              >
                <Plus className="w-4 h-4" />
                <span>添加</span>
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
                  <p>暂无过滤条件</p>
                </div>
              )}
            </div>
          </div>

          {/* 连接条件 */}
          {watchedValues.tables.length > 1 && (
            <div className="bg-white border rounded-lg p-4">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-lg font-medium">表连接</h3>
                <button
                  onClick={onAddJoin}
                  className="flex items-center space-x-1 px-3 py-1 bg-blue-600 text-white rounded text-sm hover:bg-blue-700"
                >
                  <Plus className="w-4 h-4" />
                  <span>添加连接</span>
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
                      // 更新连接条件的逻辑
                    }}
                    onRemove={() => {
                      // 移除连接条件的逻辑
                    }}
                  />
                ))}
              </div>
            </div>
          )}

          {/* 排序设置 */}
          <div className="bg-white border rounded-lg p-4">
            <div className="flex items-center space-x-2 mb-4">
              <SortAsc className="w-5 h-5 text-purple-600" />
              <h3 className="text-lg font-medium">排序</h3>
            </div>
            
            {/* 排序配置 */}
            <div className="space-y-3">
              {/* 排序字段选择逻辑 */}
            </div>
          </div>

          {/* 高级选项 */}
          <div className="bg-white border rounded-lg p-4">
            <h3 className="text-lg font-medium mb-4">高级选项</h3>
            
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
                <span>去除重复记录 (DISTINCT)</span>
              </label>
              
              <div>
                <label className="block text-sm font-medium mb-1">限制记录数 (TOP)</label>
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
                      placeholder="例如: 100"
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

// 过滤条件行组件
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
      {/* 过滤条件配置 */}
      <button onClick={onRemove} className="text-red-500 hover:text-red-700">
        <Trash2 className="w-4 h-4" />
      </button>
    </div>
  );
};

// 连接条件行组件
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
      {/* 连接条件配置 */}
      <button onClick={onRemove} className="text-red-500 hover:text-red-700">
        <Trash2 className="w-4 h-4" />
      </button>
    </div>
  );
}; 