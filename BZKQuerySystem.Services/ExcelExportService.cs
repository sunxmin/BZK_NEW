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
            // 注意：需要在创建项目时验证许可，在Program.cs全局设置
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
                return columnName;
            }

            // 检查列名是否包含下划线的命名规则，如果是表名_字段名格式，需要转换为表名.字段名显示
            if (columnName.Contains('_') && !columnName.Contains('.'))
            {
                // 尝试将下划线格式转换为点号格式来显示
                var parts = columnName.Split('_');
                if (parts.Length >= 2)
                {
                    // 假设第一部分是表名，其余部分是字段名
                    string possibleTableName = parts[0];
                    string possibleColumnName = string.Join("_", parts.Skip(1));
                    return $"{possibleTableName}.{possibleColumnName}";
                }
            }
            
            // 如果已经包含点号，保持原样
            if (columnName.Contains('.'))
            {
                return columnName;
            }
            
            // 其他情况直接返回原列名
            return columnName;
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
            using (var package = new ExcelPackage())
            {
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

                // 返回Excel文件的字节数组
                return package.GetAsByteArray();
            }
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