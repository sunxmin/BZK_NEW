using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace BZKQuerySystem.Services
{
    public class ChartConfig
    {
        public string Type { get; set; } = ""; // bar, line, pie, scatter, heatmap
        public string Title { get; set; } = "";
        public string XColumn { get; set; } = "";
        public string YColumn { get; set; } = "";
        public List<string> SeriesColumns { get; set; } = new();
        public Dictionary<string, object> Options { get; set; } = new();
        public bool EnableDrillDown { get; set; } = false;
        public DrillDownConfig? DrillDownConfig { get; set; }
    }

    public class DrillDownConfig
    {
        public string TargetTable { get; set; } = "";
        public string LinkColumn { get; set; } = "";
        public List<string> DisplayColumns { get; set; } = new();
        public Dictionary<string, object> FilterConditions { get; set; } = new();
    }

    public class InteractiveChart
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = "";
        public string Title { get; set; } = "";
        public object Data { get; set; } = new();
        public object Config { get; set; } = new();
        public List<string> LinkedCharts { get; set; } = new();
        public bool HasDrillDown { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class DashboardLayout
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<ChartPosition> Charts { get; set; } = new();
        public Dictionary<string, List<string>> ChartLinkages { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = "";
    }

    public class ChartPosition
    {
        public string ChartId { get; set; } = "";
        public int Row { get; set; }
        public int Column { get; set; }
        public int Width { get; set; } = 1;
        public int Height { get; set; } = 1;
    }

    public class DrillDownResult
    {
        public string SourceChartId { get; set; } = "";
        public string DrillValue { get; set; } = "";
        public DataTable DetailData { get; set; } = new();
        public string DetailTitle { get; set; } = "";
        public bool CanDrillFurther { get; set; }
    }

    public interface IAdvancedVisualizationService
    {
        Task<InteractiveChart> CreateChartAsync(DataTable data, ChartConfig config);
        Task<List<InteractiveChart>> CreateLinkedChartsAsync(DataTable data, List<ChartConfig> configs);
        Task<DrillDownResult> ExecuteDrillDownAsync(string chartId, string drillValue, DrillDownConfig config);
        Task<DashboardLayout> CreateDashboardAsync(string name, List<InteractiveChart> charts, Dictionary<string, List<string>> linkages);
        Task<object> GetChartDataForLinkingAsync(string chartId, string filterValue);
        Task<List<object>> GetRecommendedChartsAsync(DataTable data);
        Task<object> ExportDashboardAsync(string dashboardId, string format);
    }

    public class AdvancedVisualizationService : IAdvancedVisualizationService
    {
        private readonly ILogger<AdvancedVisualizationService> _logger;
        private readonly QueryBuilderService _queryBuilderService;
        private readonly Dictionary<string, InteractiveChart> _chartCache = new();
        private readonly Dictionary<string, DashboardLayout> _dashboardCache = new();

        public AdvancedVisualizationService(
            ILogger<AdvancedVisualizationService> logger,
            QueryBuilderService queryBuilderService)
        {
            _logger = logger;
            _queryBuilderService = queryBuilderService;
        }

        public async Task<InteractiveChart> CreateChartAsync(DataTable data, ChartConfig config)
        {
            try
            {
                var chart = new InteractiveChart
                {
                    Type = config.Type,
                    Title = config.Title,
                    HasDrillDown = config.EnableDrillDown,
                    Metadata = new Dictionary<string, object>
                    {
                        ["dataSource"] = "query",
                        ["rowCount"] = data.Rows.Count,
                        ["columnCount"] = data.Columns.Count,
                        ["createdAt"] = DateTime.UtcNow
                    }
                };

                // 根据图表类型生成数据
                chart.Data = config.Type.ToLower() switch
                {
                    "bar" => GenerateBarChartData(data, config),
                    "line" => GenerateLineChartData(data, config),
                    "pie" => GeneratePieChartData(data, config),
                    "scatter" => GenerateScatterChartData(data, config),
                    "heatmap" => GenerateHeatmapData(data, config),
                    _ => GenerateDefaultChartData(data, config)
                };

                // 生成图表配置
                chart.Config = GenerateChartConfig(config);

                // 缓存图表
                _chartCache[chart.Id] = chart;

                _logger.LogInformation("创建图表成功: {ChartId}, 类型: {Type}, 数据行数: {RowCount}",
                    chart.Id, config.Type, data.Rows.Count);

                await Task.CompletedTask;
                return chart;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建图表时发生错误: {ChartType}", config.Type);
                throw;
            }
        }

        public async Task<List<InteractiveChart>> CreateLinkedChartsAsync(DataTable data, List<ChartConfig> configs)
        {
            try
            {
                var charts = new List<InteractiveChart>();

                foreach (var config in configs)
                {
                    var chart = await CreateChartAsync(data, config);
                    charts.Add(chart);
                }

                // 设置图表联动关系
                foreach (var chart in charts)
                {
                    chart.LinkedCharts = charts
                        .Where(c => c.Id != chart.Id)
                        .Select(c => c.Id)
                        .ToList();
                }

                _logger.LogInformation("创建联动图表组成功，包含 {ChartCount} 个图表", charts.Count);

                return charts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建联动图表时发生错误");
                throw;
            }
        }

        public async Task<DrillDownResult> ExecuteDrillDownAsync(string chartId, string drillValue, DrillDownConfig config)
        {
            try
            {
                if (!_chartCache.TryGetValue(chartId, out var sourceChart))
                {
                    throw new ArgumentException($"图表 {chartId} 不存在");
                }

                // 构建钻取查询
                var whereConditions = new List<string>
                {
                    $"{config.LinkColumn} = '{drillValue}'"
                };

                // 添加额外的过滤条件
                foreach (var condition in config.FilterConditions)
                {
                    whereConditions.Add($"{condition.Key} = '{condition.Value}'");
                }

                // 执行钻取查询
                var detailData = await _queryBuilderService.ExecuteQueryAsync(
                    new List<string> { config.TargetTable },
                    config.DisplayColumns,
                    new List<string>(),
                    whereConditions,
                    new List<string>(),
                    new Dictionary<string, object>()
                );

                var result = new DrillDownResult
                {
                    SourceChartId = chartId,
                    DrillValue = drillValue,
                    DetailData = detailData,
                    DetailTitle = $"{sourceChart.Title} - {drillValue} 详细信息",
                    CanDrillFurther = detailData.Rows.Count > 0
                };

                _logger.LogInformation("执行数据钻取: 图表={ChartId}, 钻取值={DrillValue}, 结果行数={RowCount}",
                    chartId, drillValue, detailData.Rows.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行数据钻取时发生错误: 图表={ChartId}, 钻取值={DrillValue}", chartId, drillValue);
                throw;
            }
        }

        public async Task<DashboardLayout> CreateDashboardAsync(string name, List<InteractiveChart> charts, Dictionary<string, List<string>> linkages)
        {
            try
            {
                var dashboard = new DashboardLayout
                {
                    Name = name,
                    Description = $"包含 {charts.Count} 个图表的交互式仪表板",
                    ChartLinkages = linkages
                };

                // 自动布局图表
                for (int i = 0; i < charts.Count; i++)
                {
                    var position = new ChartPosition
                    {
                        ChartId = charts[i].Id,
                        Row = i / 2,
                        Column = i % 2,
                        Width = 1,
                        Height = 1
                    };
                    dashboard.Charts.Add(position);
                }

                // 缓存仪表板
                _dashboardCache[dashboard.Id] = dashboard;

                _logger.LogInformation("创建仪表板成功: {DashboardId}, 图表数量: {ChartCount}",
                    dashboard.Id, charts.Count);

                await Task.CompletedTask;
                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建仪表板时发生错误");
                throw;
            }
        }

        public async Task<object> GetChartDataForLinkingAsync(string chartId, string filterValue)
        {
            try
            {
                if (!_chartCache.TryGetValue(chartId, out var chart))
                {
                    throw new ArgumentException($"图表 {chartId} 不存在");
                }

                // 根据过滤值重新生成图表数据
                // 这里需要重新查询数据库或处理现有数据
                var filteredData = ApplyFilterToChartData(chart.Data, filterValue);

                _logger.LogInformation("获取联动图表数据: {ChartId}, 过滤值: {FilterValue}", chartId, filterValue);

                await Task.CompletedTask;
                return filteredData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取联动图表数据时发生错误: {ChartId}", chartId);
                throw;
            }
        }

        public async Task<List<object>> GetRecommendedChartsAsync(DataTable data)
        {
            try
            {
                var recommendations = new List<object>();

                // 分析数据结构
                var numericColumns = GetNumericColumns(data);
                var categoricalColumns = GetCategoricalColumns(data);
                var dateColumns = GetDateColumns(data);

                // 基于数据特征推荐图表类型
                if (categoricalColumns.Any() && numericColumns.Any())
                {
                    recommendations.Add(new
                    {
                        type = "bar",
                        title = "分类数据统计",
                        confidence = 0.9,
                        reason = "检测到分类字段和数值字段，适合柱状图展示",
                        suggestedConfig = new
                        {
                            xColumn = categoricalColumns.First(),
                            yColumn = numericColumns.First()
                        }
                    });
                }

                if (dateColumns.Any() && numericColumns.Any())
                {
                    recommendations.Add(new
                    {
                        type = "line",
                        title = "时间趋势分析",
                        confidence = 0.85,
                        reason = "检测到时间字段和数值字段，适合线图展示趋势",
                        suggestedConfig = new
                        {
                            xColumn = dateColumns.First(),
                            yColumn = numericColumns.First()
                        }
                    });
                }

                if (numericColumns.Count >= 2)
                {
                    recommendations.Add(new
                    {
                        type = "scatter",
                        title = "相关性分析",
                        confidence = 0.75,
                        reason = "检测到多个数值字段，适合散点图分析相关性",
                        suggestedConfig = new
                        {
                            xColumn = numericColumns[0],
                            yColumn = numericColumns[1]
                        }
                    });
                }

                if (categoricalColumns.Any())
                {
                    var categoryColumn = categoricalColumns.First();
                    var uniqueValues = data.AsEnumerable()
                        .Select(row => row[categoryColumn]?.ToString())
                        .Distinct()
                        .Count();

                    if (uniqueValues <= 10)
                    {
                        recommendations.Add(new
                        {
                            type = "pie",
                            title = "分布比例分析",
                            confidence = 0.8,
                            reason = "检测到少量分类值，适合饼图展示分布",
                            suggestedConfig = new
                            {
                                categoryColumn = categoryColumn
                            }
                        });
                    }
                }

                _logger.LogInformation("生成图表推荐: 数据行数={RowCount}, 推荐数量={RecommendationCount}",
                    data.Rows.Count, recommendations.Count);

                await Task.CompletedTask;
                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成图表推荐时发生错误");
                throw;
            }
        }

        public async Task<object> ExportDashboardAsync(string dashboardId, string format)
        {
            try
            {
                if (!_dashboardCache.TryGetValue(dashboardId, out var dashboard))
                {
                    throw new ArgumentException($"仪表板 {dashboardId} 不存在");
                }

                var exportData = new
                {
                    dashboard.Id,
                    dashboard.Name,
                    dashboard.Description,
                    ExportedAt = DateTime.UtcNow,
                    Format = format,
                    Charts = dashboard.Charts.Select(pos =>
                    {
                        var chart = _chartCache.GetValueOrDefault(pos.ChartId);
                        return new
                        {
                            pos.ChartId,
                            pos.Row,
                            pos.Column,
                            pos.Width,
                            pos.Height,
                            ChartData = chart?.Data,
                            ChartConfig = chart?.Config
                        };
                    }).ToList(),
                    Linkages = dashboard.ChartLinkages
                };

                _logger.LogInformation("导出仪表板: {DashboardId}, 格式: {Format}", dashboardId, format);

                await Task.CompletedTask;
                return exportData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出仪表板时发生错误: {DashboardId}", dashboardId);
                throw;
            }
        }

        #region 私有方法

        private object GenerateBarChartData(DataTable data, ChartConfig config)
        {
            var categories = new List<string>();
            var series = new List<object>();

            if (string.IsNullOrEmpty(config.XColumn) || string.IsNullOrEmpty(config.YColumn))
                return new { categories, series };

            // 按X轴分组统计
            var groups = data.AsEnumerable()
                .GroupBy(row => row[config.XColumn]?.ToString() ?? "")
                .ToList();

            categories = groups.Select(g => g.Key).ToList();

            if (config.SeriesColumns.Any())
            {
                foreach (var seriesCol in config.SeriesColumns)
                {
                    var seriesData = groups.Select(g =>
                        g.Sum(row => Convert.ToDouble(row[seriesCol] ?? 0))).ToList();

                    series.Add(new
                    {
                        name = seriesCol,
                        data = seriesData
                    });
                }
            }
            else
            {
                var seriesData = groups.Select(g =>
                    g.Sum(row => Convert.ToDouble(row[config.YColumn] ?? 0))).ToList();

                series.Add(new
                {
                    name = config.YColumn,
                    data = seriesData
                });
            }

            return new { categories, series };
        }

        private object GenerateLineChartData(DataTable data, ChartConfig config)
        {
            var xValues = new List<object>();
            var series = new List<object>();

            if (string.IsNullOrEmpty(config.XColumn) || string.IsNullOrEmpty(config.YColumn))
                return new { xValues, series };

            var sortedData = data.AsEnumerable()
                .OrderBy(row => row[config.XColumn])
                .ToList();

            xValues = sortedData.Select(row => row[config.XColumn]).ToList();

            var yValues = sortedData.Select(row =>
                Convert.ToDouble(row[config.YColumn] ?? 0)).ToList();

            series.Add(new
            {
                name = config.YColumn,
                data = yValues
            });

            return new { xValues, series };
        }

        private object GeneratePieChartData(DataTable data, ChartConfig config)
        {
            if (string.IsNullOrEmpty(config.XColumn))
                return new { series = new List<object>() };

            var groups = data.AsEnumerable()
                .GroupBy(row => row[config.XColumn]?.ToString() ?? "")
                .Select(g => new
                {
                    name = g.Key,
                    value = g.Count()
                })
                .ToList();

            return new { series = groups };
        }

        private object GenerateScatterChartData(DataTable data, ChartConfig config)
        {
            if (string.IsNullOrEmpty(config.XColumn) || string.IsNullOrEmpty(config.YColumn))
                return new { series = new List<object>() };

            var points = data.AsEnumerable()
                .Select(row => new
                {
                    x = Convert.ToDouble(row[config.XColumn] ?? 0),
                    y = Convert.ToDouble(row[config.YColumn] ?? 0)
                })
                .ToList();

            return new { series = new[] { new { name = "数据点", data = points } } };
        }

        private object GenerateHeatmapData(DataTable data, ChartConfig config)
        {
            // 简化的热力图数据生成
            var heatmapData = new List<object>();

            // 这里需要根据实际需求实现热力图数据转换逻辑
            return new { data = heatmapData };
        }

        private object GenerateDefaultChartData(DataTable data, ChartConfig config)
        {
            return new
            {
                message = "Unsupported chart type",
                rowCount = data.Rows.Count,
                columnCount = data.Columns.Count
            };
        }

        private object GenerateChartConfig(ChartConfig config)
        {
            return new
            {
                responsive = true,
                maintainAspectRatio = false,
                plugins = new
                {
                    title = new
                    {
                        display = !string.IsNullOrEmpty(config.Title),
                        text = config.Title
                    },
                    legend = new
                    {
                        display = true,
                        position = "top"
                    }
                },
                scales = config.Type.ToLower() switch
                {
                    "pie" => null,
                    _ => new
                    {
                        x = new { title = new { display = true, text = config.XColumn } },
                        y = new { title = new { display = true, text = config.YColumn } }
                    }
                },
                interaction = new
                {
                    intersect = false,
                    mode = "index"
                },
                onClick = config.EnableDrillDown ? "handleChartClick" : null
            };
        }

        private object ApplyFilterToChartData(object chartData, string filterValue)
        {
            // 这里需要实现数据过滤逻辑
            // 具体实现取决于图表数据结构
            return chartData;
        }

        private List<string> GetNumericColumns(DataTable data)
        {
            return data.Columns.Cast<DataColumn>()
                .Where(col => IsNumericType(col.DataType))
                .Select(col => col.ColumnName)
                .ToList();
        }

        private List<string> GetCategoricalColumns(DataTable data)
        {
            return data.Columns.Cast<DataColumn>()
                .Where(col => col.DataType == typeof(string))
                .Select(col => col.ColumnName)
                .ToList();
        }

        private List<string> GetDateColumns(DataTable data)
        {
            return data.Columns.Cast<DataColumn>()
                .Where(col => col.DataType == typeof(DateTime))
                .Select(col => col.ColumnName)
                .ToList();
        }

        private bool IsNumericType(Type type)
        {
            return type == typeof(int) || type == typeof(long) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }

        #endregion
    }
}
