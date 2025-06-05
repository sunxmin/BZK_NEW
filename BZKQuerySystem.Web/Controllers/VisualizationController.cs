using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BZKQuerySystem.Services;
using System;
using System.Threading.Tasks;
using System.Data;

namespace BZKQuerySystem.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VisualizationController : ControllerBase
    {
        private readonly IAdvancedVisualizationService _visualizationService;
        private readonly QueryBuilderService _queryBuilderService;
        private readonly ILogger<VisualizationController> _logger;

        public VisualizationController(
            IAdvancedVisualizationService visualizationService,
            QueryBuilderService queryBuilderService,
            ILogger<VisualizationController> logger)
        {
            _visualizationService = visualizationService;
            _queryBuilderService = queryBuilderService;
            _logger = logger;
        }

        /// <summary>
        /// 创建单个图表
        /// </summary>
        [HttpPost("create-chart")]
        public async Task<IActionResult> CreateChart([FromBody] CreateChartRequest request)
        {
            try
            {
                // 执行查询获取数据
                var data = await _queryBuilderService.ExecuteQueryAsync(
                    request.Tables,
                    request.Columns,
                    request.JoinConditions ?? new List<string>(),
                    request.WhereConditions ?? new List<string>(),
                    request.OrderBy ?? new List<string>(),
                    request.Parameters ?? new Dictionary<string, object>()
                );

                // 创建图表
                var chart = await _visualizationService.CreateChartAsync(data, request.ChartConfig);

                _logger.LogInformation("创建图表成功: {ChartId}, 用户: {UserId}",
                    chart.Id, User.Identity?.Name);

                return Ok(chart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建图表时发生错误");
                return StatusCode(500, new { error = "创建图表失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 创建联动图表组
        /// </summary>
        [HttpPost("create-linked-charts")]
        public async Task<IActionResult> CreateLinkedCharts([FromBody] CreateLinkedChartsRequest request)
        {
            try
            {
                // 执行查询获取数据
                var data = await _queryBuilderService.ExecuteQueryAsync(
                    request.Tables,
                    request.Columns,
                    request.JoinConditions ?? new List<string>(),
                    request.WhereConditions ?? new List<string>(),
                    request.OrderBy ?? new List<string>(),
                    request.Parameters ?? new Dictionary<string, object>()
                );

                // 创建联动图表组
                var charts = await _visualizationService.CreateLinkedChartsAsync(data, request.ChartConfigs);

                _logger.LogInformation("创建联动图表组成功: {ChartCount} 个图表, 用户: {UserId}",
                    charts.Count, User.Identity?.Name);

                return Ok(charts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建联动图表时发生错误");
                return StatusCode(500, new { error = "创建联动图表失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 执行数据钻取
        /// </summary>
        [HttpPost("drill-down")]
        public async Task<IActionResult> DrillDown([FromBody] DrillDownRequest request)
        {
            try
            {
                var result = await _visualizationService.ExecuteDrillDownAsync(
                    request.ChartId, request.DrillValue, request.DrillDownConfig);

                _logger.LogInformation("执行数据钻取: 图表={ChartId}, 钻取值={DrillValue}, 结果行数={RowCount}",
                    request.ChartId, request.DrillValue, result.DetailData.Rows.Count);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行数据钻取时发生错误: {ChartId}", request.ChartId);
                return StatusCode(500, new { error = "数据钻取失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 获取图表联动数据
        /// </summary>
        [HttpGet("chart-link-data/{chartId}")]
        public async Task<IActionResult> GetChartLinkData(string chartId, [FromQuery] string filterValue)
        {
            try
            {
                var data = await _visualizationService.GetChartDataForLinkingAsync(chartId, filterValue);

                _logger.LogInformation("获取图表联动数据: {ChartId}, 过滤值: {FilterValue}", chartId, filterValue);

                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取图表联动数据时发生错误: {ChartId}", chartId);
                return StatusCode(500, new { error = "获取联动数据失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 获取图表推荐
        /// </summary>
        [HttpPost("recommend-charts")]
        public async Task<IActionResult> RecommendCharts([FromBody] RecommendChartsRequest request)
        {
            try
            {
                // 执行查询获取数据
                var data = await _queryBuilderService.ExecuteQueryAsync(
                    request.Tables,
                    request.Columns,
                    request.JoinConditions ?? new List<string>(),
                    request.WhereConditions ?? new List<string>(),
                    request.OrderBy ?? new List<string>(),
                    request.Parameters ?? new Dictionary<string, object>()
                );

                // 获取图表推荐
                var recommendations = await _visualizationService.GetRecommendedChartsAsync(data);

                _logger.LogInformation("生成图表推荐: 数据行数={RowCount}, 推荐数量={RecommendationCount}",
                    data.Rows.Count, recommendations.Count);

                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取图表推荐时发生错误");
                return StatusCode(500, new { error = "获取图表推荐失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 创建仪表板
        /// </summary>
        [HttpPost("create-dashboard")]
        public async Task<IActionResult> CreateDashboard([FromBody] CreateDashboardRequest request)
        {
            try
            {
                // 这里假设图表已经创建，实际应用中可能需要先创建图表
                var charts = new List<InteractiveChart>(); // 需要从缓存或数据库获取

                var dashboard = await _visualizationService.CreateDashboardAsync(
                    request.Name, charts, request.ChartLinkages);

                _logger.LogInformation("创建仪表板成功: {DashboardId}, 名称: {Name}, 用户: {UserId}",
                    dashboard.Id, request.Name, User.Identity?.Name);

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建仪表板时发生错误");
                return StatusCode(500, new { error = "创建仪表板失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 导出仪表板
        /// </summary>
        [HttpPost("export-dashboard/{dashboardId}")]
        public async Task<IActionResult> ExportDashboard(string dashboardId, [FromBody] ExportDashboardRequest request)
        {
            try
            {
                var exportData = await _visualizationService.ExportDashboardAsync(dashboardId, request.Format);

                _logger.LogInformation("导出仪表板: {DashboardId}, 格式: {Format}, 用户: {UserId}",
                    dashboardId, request.Format, User.Identity?.Name);

                return Ok(exportData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出仪表板时发生错误: {DashboardId}", dashboardId);
                return StatusCode(500, new { error = "导出仪表板失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 获取图表数据摘要
        /// </summary>
        [HttpPost("data-summary")]
        public async Task<IActionResult> GetDataSummary([FromBody] DataSummaryRequest request)
        {
            try
            {
                // 执行查询获取数据
                var data = await _queryBuilderService.ExecuteQueryAsync(
                    request.Tables,
                    request.Columns,
                    request.JoinConditions ?? new List<string>(),
                    request.WhereConditions ?? new List<string>(),
                    request.OrderBy ?? new List<string>(),
                    request.Parameters ?? new Dictionary<string, object>()
                );

                // 生成数据摘要
                var summary = GenerateDataSummary(data);

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取数据摘要时发生错误");
                return StatusCode(500, new { error = "获取数据摘要失败", details = ex.Message });
            }
        }

        #region 私有方法

        private object GenerateDataSummary(DataTable data)
        {
            var numericColumns = data.Columns.Cast<DataColumn>()
                .Where(col => IsNumericType(col.DataType))
                .ToList();

            var summary = new
            {
                totalRows = data.Rows.Count,
                totalColumns = data.Columns.Count,
                numericColumns = numericColumns.Count,
                categoricalColumns = data.Columns.Count - numericColumns.Count,
                columnInfo = data.Columns.Cast<DataColumn>().Select(col => new
                {
                    name = col.ColumnName,
                    type = col.DataType.Name,
                    isNumeric = IsNumericType(col.DataType),
                    hasNulls = data.AsEnumerable().Any(row => row[col] == DBNull.Value),
                    uniqueValues = IsNumericType(col.DataType) ? -1 :
                        data.AsEnumerable().Select(row => row[col]?.ToString()).Distinct().Count()
                }).ToList(),
                numericSummary = numericColumns.Any() ? numericColumns.ToDictionary(
                    col => col.ColumnName,
                    col => GetNumericColumnSummary(data, col.ColumnName)
                ) : new Dictionary<string, object>()
            };

            return summary;
        }

        private object GetNumericColumnSummary(DataTable data, string columnName)
        {
            var values = data.AsEnumerable()
                .Where(row => row[columnName] != DBNull.Value)
                .Select(row => Convert.ToDouble(row[columnName]))
                .ToList();

            if (!values.Any())
                return new { count = 0 };

            return new
            {
                count = values.Count,
                min = values.Min(),
                max = values.Max(),
                average = Math.Round(values.Average(), 2),
                sum = values.Sum(),
                standardDeviation = Math.Round(CalculateStandardDeviation(values), 2)
            };
        }

        private double CalculateStandardDeviation(List<double> values)
        {
            if (values.Count <= 1) return 0;

            var average = values.Average();
            var sumOfSquaresOfDifferences = values.Select(val => (val - average) * (val - average)).Sum();
            return Math.Sqrt(sumOfSquaresOfDifferences / values.Count);
        }

        private bool IsNumericType(Type type)
        {
            return type == typeof(int) || type == typeof(long) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }

        #endregion
    }

    #region 请求模型

    public class CreateChartRequest
    {
        public List<string> Tables { get; set; } = new();
        public List<string> Columns { get; set; } = new();
        public List<string>? JoinConditions { get; set; }
        public List<string>? WhereConditions { get; set; }
        public List<string>? OrderBy { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
        public ChartConfig ChartConfig { get; set; } = new();
    }

    public class CreateLinkedChartsRequest
    {
        public List<string> Tables { get; set; } = new();
        public List<string> Columns { get; set; } = new();
        public List<string>? JoinConditions { get; set; }
        public List<string>? WhereConditions { get; set; }
        public List<string>? OrderBy { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
        public List<ChartConfig> ChartConfigs { get; set; } = new();
    }

    public class DrillDownRequest
    {
        public string ChartId { get; set; } = "";
        public string DrillValue { get; set; } = "";
        public DrillDownConfig DrillDownConfig { get; set; } = new();
    }

    public class RecommendChartsRequest
    {
        public List<string> Tables { get; set; } = new();
        public List<string> Columns { get; set; } = new();
        public List<string>? JoinConditions { get; set; }
        public List<string>? WhereConditions { get; set; }
        public List<string>? OrderBy { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
    }

    public class CreateDashboardRequest
    {
        public string Name { get; set; } = "";
        public List<string> ChartIds { get; set; } = new();
        public Dictionary<string, List<string>> ChartLinkages { get; set; } = new();
    }

    public class ExportDashboardRequest
    {
        public string Format { get; set; } = "json"; // json, pdf, excel
    }

    public class DataSummaryRequest
    {
        public List<string> Tables { get; set; } = new();
        public List<string> Columns { get; set; } = new();
        public List<string>? JoinConditions { get; set; }
        public List<string>? WhereConditions { get; set; }
        public List<string>? OrderBy { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
    }

    #endregion
}
