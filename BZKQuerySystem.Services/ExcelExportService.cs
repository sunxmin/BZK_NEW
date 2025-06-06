using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BZKQuerySystem.Services
{
    public class ExcelExportService
    {
        public ExcelExportService()
        {
            // 设置EPPlus的编码许可证上下文
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// 智能获取列显示名称，处理表名.字段名的表命名格式
        /// </summary>
        /// <param name="columnName">当前列名</param>
        /// <param name="allColumnNames">所有列名列表</param>
        /// <returns>格式化显示名称</returns>
        private string GetSmartColumnDisplayName(string columnName, List<string> allColumnNames)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                return "未知列";
            }

            try
            {
                // 确保字符串是正确的UTF-8编码
                byte[] bytes = Encoding.UTF8.GetBytes(columnName);
                string cleanColumnName = Encoding.UTF8.GetString(bytes);

                // 检查列名是否包含下划线的命名规则，如果是表名_字段名格式，需要转换为表名.字段名显示
                if (cleanColumnName.Contains('_') && !cleanColumnName.Contains('.'))
                {
                    // 尝试将下划线格式转换为点号格式来显示
                    var parts = cleanColumnName.Split('_');
                    if (parts.Length >= 2)
                    {
                        // 假设第一部分是表名，其余部分是字段名
                        string possibleTableName = parts[0];
                        string possibleColumnName = string.Join("_", parts.Skip(1));
                        return $"{possibleTableName}.{possibleColumnName}";
                    }
                }

                // 如果已经包含点号，保持原样
                if (cleanColumnName.Contains('.'))
                {
                    return cleanColumnName;
                }

                // 其他情况直接返回原列名
                return cleanColumnName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理列名时出错: {columnName}, 错误: {ex.Message}");
                return columnName;
            }
        }

        /// <summary>
        /// 安全地设置Excel单元格值，确保中文字符正确显示
        /// </summary>
        /// <param name="cell">Excel单元格</param>
        /// <param name="value">要设置的值</param>
        private void SetCellValueSafely(ExcelRange cell, object? value)
        {
            try
            {
                if (value == null)
                {
                    cell.Value = "";
                    return;
                }

                string stringValue = value.ToString() ?? "";

                // 确保字符串是正确的UTF-8编码
                byte[] bytes = Encoding.UTF8.GetBytes(stringValue);
                string cleanValue = Encoding.UTF8.GetString(bytes);

                cell.Value = cleanValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置单元格值时出错: {value}, 错误: {ex.Message}");
                cell.Value = value?.ToString() ?? "";
            }
        }

        /// <summary>
        /// 将DataTable导出为Excel文件
        /// </summary>
        /// <param name="data">数据表</param>
        /// <param name="sheetName">工作表名称</param>
        /// <param name="title">Excel标题</param>
        /// <returns>Excel文件的字节数组</returns>
        public byte[] ExportToExcel(DataTable data, string sheetName = "Sheet1", string? title = null)
        {
            try
            {
                Console.WriteLine($"开始生成Excel: 表数量: {data?.Rows?.Count ?? 0}, 列数量: {data?.Columns?.Count ?? 0}");

                if (data == null || data.Columns.Count == 0)
                {
                    Console.WriteLine("Excel导出错误: 数据表为空或没有列");
                    throw new ArgumentException("数据表为空或没有列");
                }

                using (var package = new ExcelPackage())
                {
                    // 确保工作表名称安全
                    string safeSheetName = GetSafeSheetName(sheetName);
                    var worksheet = package.Workbook.Worksheets.Add(safeSheetName);

                    // 设置工作表字体，确保支持中文
                    worksheet.Cells.Style.Font.Name = "宋体";

                    // 如果有标题，添加标题行
                    int startRow = 1;
                    if (!string.IsNullOrEmpty(title))
                    {
                        Console.WriteLine($"添加Excel标题: {title}");
                        SetCellValueSafely(worksheet.Cells[1, 1], title);
                        worksheet.Cells[1, 1, 1, data.Columns.Count].Merge = true;
                        worksheet.Cells[1, 1, 1, data.Columns.Count].Style.Font.Bold = true;
                        worksheet.Cells[1, 1, 1, data.Columns.Count].Style.Font.Size = 14;
                        worksheet.Cells[1, 1, 1, data.Columns.Count].Style.Font.Name = "宋体";
                        worksheet.Cells[1, 1, 1, data.Columns.Count].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        startRow = 2;
                    }

                    Console.WriteLine($"添加列标题，共{data.Columns.Count}列");
                    // 添加列标题
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        try
                        {
                            string columnName = data.Columns[i].ColumnName;
                            string displayName = GetSmartColumnDisplayName(columnName, data.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList());

                            Console.WriteLine($"列{i + 1}: {columnName} -> {displayName}");
                            SetCellValueSafely(worksheet.Cells[startRow, i + 1], displayName);
                            worksheet.Cells[startRow, i + 1].Style.Font.Bold = true;
                            worksheet.Cells[startRow, i + 1].Style.Font.Name = "宋体";
                            worksheet.Cells[startRow, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[startRow, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                            worksheet.Cells[startRow, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"添加列标题时出错(列{i + 1}): {ex.Message}");
                            worksheet.Cells[startRow, i + 1].Value = $"列{i + 1}";
                        }
                    }

                    Console.WriteLine($"添加数据行，共{data.Rows.Count}行");
                    // 添加数据行
                    for (int row = 0; row < data.Rows.Count; row++)
                    {
                        for (int col = 0; col < data.Columns.Count; col++)
                        {
                            try
                            {
                                SetCellValueSafely(worksheet.Cells[row + startRow + 1, col + 1], data.Rows[row][col]);
                                worksheet.Cells[row + startRow + 1, col + 1].Style.Font.Name = "宋体";
                                worksheet.Cells[row + startRow + 1, col + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"添加数据单元格时出错(行{row + 1},列{col + 1}): {ex.Message}");
                                worksheet.Cells[row + startRow + 1, col + 1].Value = "错误";
                            }
                        }
                    }

                    // 自动调整列宽
                    try
                    {
                        worksheet.Cells.AutoFitColumns();
                        // 限制最大列宽，防止过宽
                        for (int col = 1; col <= data.Columns.Count; col++)
                        {
                            if (worksheet.Column(col).Width > 50)
                            {
                                worksheet.Column(col).Width = 50;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"自动调整列宽时出错: {ex.Message}");
                    }

                    // 隔行改变颜色
                    try
                    {
                        for (int row = 0; row < data.Rows.Count; row++)
                        {
                            if (row % 2 == 1)
                            {
                                worksheet.Cells[row + startRow + 1, 1, row + startRow + 1, data.Columns.Count].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                worksheet.Cells[row + startRow + 1, 1, row + startRow + 1, data.Columns.Count].Style.Fill.BackgroundColor.SetColor(Color.AliceBlue);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"设置隔行颜色时出错: {ex.Message}");
                    }

                    // 返回Excel文件的字节数组
                    var result = package.GetAsByteArray();
                    Console.WriteLine($"Excel生成成功，文件大小: {result.Length} 字节");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excel生成出错: {ex.Message}");
                Console.WriteLine($"错误堆栈: {ex.StackTrace}");
                throw new Exception($"Excel导出失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取安全的工作表名称
        /// </summary>
        /// <param name="sheetName">原始工作表名称</param>
        /// <returns>安全的工作表名称</returns>
        private string GetSafeSheetName(string sheetName)
        {
            if (string.IsNullOrEmpty(sheetName))
            {
                return "Sheet1";
            }

            // Excel工作表名称限制：不能超过31个字符，不能包含某些特殊字符
            string safeName = sheetName;

            // 移除或替换非法字符
            char[] illegalChars = { '\\', '/', '*', '?', ':', '[', ']' };
            foreach (char c in illegalChars)
            {
                safeName = safeName.Replace(c, '_');
            }

            // 限制长度
            if (safeName.Length > 31)
            {
                safeName = safeName.Substring(0, 31);
            }

            return safeName;
        }

        /// <summary>
        /// 将查询结果导出为Excel文件并保存到本地
        /// </summary>
        /// <param name="data">数据表</param>
        /// <param name="fileName">输出文件名(不含路径)</param>
        /// <param name="filePath">保存路径</param>
        /// <param name="sheetName">工作表名称</param>
        /// <param name="title">Excel标题</param>
        public string SaveExcelFile(DataTable data, string fileName, string filePath, string sheetName = "Sheet1", string? title = null)
        {
            // 确保输出目录存在
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            // 确保文件名唯一
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            string extension = ".xlsx";
            string fullFileName = Path.Combine(filePath, fileNameWithoutExt + extension);

            int index = 1;
            while (File.Exists(fullFileName))
            {
                fullFileName = Path.Combine(filePath, $"{fileNameWithoutExt}({index}){extension}");
                index++;
            }

            // 生成Excel文件
            byte[] excelData = ExportToExcel(data, sheetName, title);
            File.WriteAllBytes(fullFileName, excelData);

            return fullFileName;
        }

        /// <summary>
        /// 将多个DataTable导出为Excel文件的不同工作表
        /// </summary>
        /// <param name="dataTables">数据表集合，键为工作表名称</param>
        /// <param name="title">Excel标题</param>
        /// <returns>Excel文件的字节数组</returns>
        public byte[] ExportMultipleToExcel(Dictionary<string, DataTable> dataTables, string? title = null)
        {
            if (dataTables == null || dataTables.Count == 0)
                throw new ArgumentException("需要至少一个数据表");

            using (var package = new ExcelPackage())
            {
                foreach (var dataTable in dataTables)
                {
                    var sheetName = dataTable.Key;
                    var data = dataTable.Value;

                    var worksheet = package.Workbook.Worksheets.Add(sheetName);

                    // 如果有标题，添加标题行
                    int startRow = 1;
                    if (!string.IsNullOrEmpty(title))
                    {
                        worksheet.Cells[1, 1].Value = title;
                        worksheet.Cells[1, 1, 1, data.Columns.Count].Merge = true;
                        worksheet.Cells[1, 1, 1, data.Columns.Count].Style.Font.Bold = true;
                        worksheet.Cells[1, 1, 1, data.Columns.Count].Style.Font.Size = 14;
                        worksheet.Cells[1, 1, 1, data.Columns.Count].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        startRow = 2;
                    }

                    // 添加列标题
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        string columnName = data.Columns[i].ColumnName;
                        string displayName = GetSmartColumnDisplayName(columnName, data.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList());

                        worksheet.Cells[startRow, i + 1].Value = displayName;
                        worksheet.Cells[startRow, i + 1].Style.Font.Bold = true;
                        worksheet.Cells[startRow, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[startRow, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
                        worksheet.Cells[startRow, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    // 添加数据行
                    for (int row = 0; row < data.Rows.Count; row++)
                    {
                        for (int col = 0; col < data.Columns.Count; col++)
                        {
                            worksheet.Cells[row + startRow + 1, col + 1].Value = data.Rows[row][col]?.ToString() ?? string.Empty;
                            worksheet.Cells[row + startRow + 1, col + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }
                    }

                    // 自动调整列宽
                    worksheet.Cells.AutoFitColumns();

                    // 隔行改变颜色
                    for (int row = 0; row < data.Rows.Count; row++)
                    {
                        if (row % 2 == 1)
                        {
                            worksheet.Cells[row + startRow + 1, 1, row + startRow + 1, data.Columns.Count].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[row + startRow + 1, 1, row + startRow + 1, data.Columns.Count].Style.Fill.BackgroundColor.SetColor(Color.AliceBlue);
                        }
                    }
                }

                // 返回Excel文件的字节数组
                return package.GetAsByteArray();
            }
        }
    }
}
