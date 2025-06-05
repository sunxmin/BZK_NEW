using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.IO.Font;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace BZKQuerySystem.Services
{
    public class PdfExportOptions
    {
        public string Title { get; set; } = "查询报表";
        public string Author { get; set; } = "BZK查询系统";
        public bool IncludeTimestamp { get; set; } = true;
        public bool IncludePageNumbers { get; set; } = true;
        public int MaxRowsPerPage { get; set; } = 50;
        public bool LandscapeOrientation { get; set; } = true; // 默认横向
    }

    public class PdfExportService
    {
        /// <summary>
        /// 异步导出数据表到PDF
        /// </summary>
        /// <param name="data">要导出的数据表</param>
        /// <param name="options">导出选项</param>
        /// <returns>PDF文件的字节数组</returns>
        public async Task<byte[]> ExportToPdfAsync(DataTable data, PdfExportOptions? options = null)
        {
            return await Task.Run(() => ExportToPdf(data, options ?? new PdfExportOptions()));
        }

        /// <summary>
        /// 同步PDF导出
        /// </summary>
        private byte[] ExportToPdf(DataTable data, PdfExportOptions options)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                using var pdfWriter = new PdfWriter(memoryStream);
                using var pdfDocument = new PdfDocument(pdfWriter);
                
                // 设置页面大小和方向
                PageSize pageSize = options.LandscapeOrientation ? PageSize.A4.Rotate() : PageSize.A4;
                using var document = new Document(pdfDocument, pageSize);

                // 设置文档元数据
                var documentInfo = pdfDocument.GetDocumentInfo();
                documentInfo.SetTitle(options.Title);
                documentInfo.SetAuthor(options.Author);
                documentInfo.SetCreator("BZK查询系统");
                documentInfo.SetSubject("数据查询报表");

                // 创建支持中文的字体
                PdfFont font = CreateChineseSupportFont();

                // 添加标题
                if (!string.IsNullOrEmpty(options.Title))
                {
                    var title = new Paragraph(options.Title)
                        .SetFont(font)
                        .SetFontSize(16)
                        .SetBold()
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(20);
                    document.Add(title);
                }

                // 添加时间戳
                if (options.IncludeTimestamp)
                {
                    var timestamp = new Paragraph($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                        .SetFont(font)
                        .SetFontSize(10)
                        .SetTextAlignment(TextAlignment.RIGHT)
                        .SetMarginBottom(15);
                    document.Add(timestamp);
                }

                // 创建表格
                if (data.Rows.Count > 0)
                {
                    var table = CreatePdfTable(data, font, options.LandscapeOrientation);
                    document.Add(table);

                    // 添加统计信息
                    var summary = new Paragraph($"总记录数: {data.Rows.Count}")
                        .SetFont(font)
                        .SetFontSize(10)
                        .SetMarginTop(15)
                        .SetTextAlignment(TextAlignment.RIGHT);
                    document.Add(summary);
                }
                else
                {
                    var noDataMsg = new Paragraph("没有可显示的数据")
                        .SetFont(font)
                        .SetFontSize(12)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginTop(50);
                    document.Add(noDataMsg);
                }

                // 添加页码
                if (options.IncludePageNumbers)
                {
                    AddPageNumbers(pdfDocument, font);
                }

                // 确保文档正确关闭
                document.Close();
                pdfDocument.Close();
                pdfWriter.Close();

                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                // 记录错误并返回简单的错误PDF
                Console.WriteLine($"PDF导出错误: {ex.Message}");
                return CreateErrorPdf(ex.Message);
            }
        }

        /// <summary>
        /// 创建支持中文的字体
        /// </summary>
        private PdfFont CreateChineseSupportFont()
        {
            try
            {
                // 方案1: 尝试使用Windows系统中文字体
                try
                {
                    // 尝试使用系统的宋体字体文件
                    string fontPath = @"C:\Windows\Fonts\simsun.ttc,0"; // 宋体
                    if (System.IO.File.Exists(@"C:\Windows\Fonts\simsun.ttc"))
                    {
                        return PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                    }
                }
                catch { }

                // 方案2: 尝试使用其他系统字体
                try
                {
                    string fontPath = @"C:\Windows\Fonts\msyh.ttc,0"; // 微软雅黑
                    if (System.IO.File.Exists(@"C:\Windows\Fonts\msyh.ttc"))
                    {
                        return PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                    }
                }
                catch { }

                // 方案3: 尝试使用内置字体
                try
                {
                    return PdfFontFactory.CreateFont("STSong-Light", "UniGB-UCS2-H");
                }
                catch { }

                // 方案4: 使用支持基础Unicode的字体
                try
                {
                    return PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN, PdfEncodings.IDENTITY_H);
                }
                catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"字体创建错误: {ex.Message}");
            }

            // 最后的备选方案：使用标准字体
            return PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        }

        /// <summary>
        /// 创建PDF表格
        /// </summary>
        private Table CreatePdfTable(DataTable data, PdfFont font, bool isLandscape)
        {
            var table = new Table(data.Columns.Count);
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // 根据列数调整字体大小
            int fontSize = data.Columns.Count > 10 ? 7 : (data.Columns.Count > 6 ? 8 : 9);
            int headerFontSize = fontSize + 1;

            // 添加表头
            foreach (DataColumn column in data.Columns)
            {
                var headerText = GetDisplayColumnName(column.ColumnName);
                var cell = new Cell()
                    .SetBackgroundColor(new DeviceRgb(220, 220, 220))
                    .SetFont(font)
                    .SetFontSize(headerFontSize)
                    .SetBold()
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetPadding(4)
                    .Add(new Paragraph(headerText));
                
                table.AddHeaderCell(cell);
            }

            // 添加数据行
            for (int row = 0; row < data.Rows.Count; row++)
            {
                for (int col = 0; col < data.Columns.Count; col++)
                {
                    var cellValue = data.Rows[row][col]?.ToString() ?? "";
                    
                    // 截断过长的文本
                    if (cellValue.Length > 50)
                    {
                        cellValue = cellValue.Substring(0, 47) + "...";
                    }

                    var backgroundColor = row % 2 == 0 ? 
                        ColorConstants.WHITE : 
                        new DeviceRgb(248, 248, 248);
                    
                    var cell = new Cell()
                        .SetBackgroundColor(backgroundColor)
                        .SetFont(font)
                        .SetFontSize(fontSize)
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetPadding(3)
                        .Add(new Paragraph(cellValue));
                    
                    table.AddCell(cell);
                }
            }

            return table;
        }

        /// <summary>
        /// 添加页码
        /// </summary>
        private void AddPageNumbers(PdfDocument pdfDocument, PdfFont font)
        {
            int numberOfPages = pdfDocument.GetNumberOfPages();
            
            for (int i = 1; i <= numberOfPages; i++)
            {
                var page = pdfDocument.GetPage(i);
                var pageSize = page.GetPageSize();
                
                var canvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(page);
                canvas.SaveState()
                    .BeginText()
                    .SetFontAndSize(font, 10)
                    .MoveText(pageSize.GetWidth() - 50, 30)
                    .ShowText($"{i}/{numberOfPages}")
                    .EndText()
                    .RestoreState();
            }
        }

        /// <summary>
        /// 获取显示的列名
        /// </summary>
        private string GetDisplayColumnName(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
                return "列";

            // 如果列名包含下划线且长度大于10，则分割并返回最后一个部分
            if (columnName.Contains("_") && columnName.Length > 10)
            {
                var parts = columnName.Split('_');
                if (parts.Length >= 2)
                {
                    return parts[parts.Length - 1];
                }
            }

            return columnName;
        }

        /// <summary>
        /// 创建简单的错误PDF
        /// </summary>
        private byte[] CreateErrorPdf(string errorMessage)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                using var pdfWriter = new PdfWriter(memoryStream);
                using var pdfDocument = new PdfDocument(pdfWriter);
                using var document = new Document(pdfDocument);

                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                // 添加错误信息
                var errorParagraph = new Paragraph("PDF导出错误")
                    .SetFont(font)
                    .SetFontSize(16)
                    .SetBold()
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20);
                document.Add(errorParagraph);

                var messageParagraph = new Paragraph($"错误: {errorMessage}")
                    .SetFont(font)
                    .SetFontSize(12)
                    .SetTextAlignment(TextAlignment.LEFT);
                document.Add(messageParagraph);

                document.Close();
                return memoryStream.ToArray();
            }
            catch
            {
                // 如果连错误PDF都失败，返回空数组
                return new byte[0];
            }
        }

        /// <summary>
        /// 异步导出多个数据表到PDF
        /// </summary>
        public async Task<byte[]> ExportMultipleToPdfAsync(
            Dictionary<string, DataTable> dataTables, 
            string title = "多个数据表报表")
        {
            return await Task.Run(() => ExportMultipleToPdf(dataTables, title));
        }

        /// <summary>
        /// 同步多表导出
        /// </summary>
        private byte[] ExportMultipleToPdf(Dictionary<string, DataTable> dataTables, string title)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                using var pdfWriter = new PdfWriter(memoryStream);
                using var pdfDocument = new PdfDocument(pdfWriter);
                using var document = new Document(pdfDocument, PageSize.A4.Rotate()); // 横向布局

                var font = CreateChineseSupportFont();

                // 添加主标题
                var mainTitle = new Paragraph(title)
                    .SetFont(font)
                    .SetFontSize(18)
                    .SetBold()
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(30);
                document.Add(mainTitle);

                // 添加生成时间
                var timestamp = new Paragraph($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                    .SetFont(font)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(20);
                document.Add(timestamp);

                // 处理每个数据表
                int tableIndex = 0;
                foreach (var kvp in dataTables)
                {
                    if (tableIndex > 0)
                    {
                        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                    }

                    // 添加表标题
                    var tableTitle = new Paragraph(kvp.Key)
                        .SetFont(font)
                        .SetFontSize(14)
                        .SetBold()
                        .SetMarginBottom(15);
                    document.Add(tableTitle);

                    // 添加表格
                    if (kvp.Value.Rows.Count > 0)
                    {
                        var table = CreatePdfTable(kvp.Value, font, true);
                        document.Add(table);

                        var summary = new Paragraph($"记录数: {kvp.Value.Rows.Count}")
                            .SetFont(font)
                            .SetFontSize(10)
                            .SetMarginTop(10)
                            .SetMarginBottom(20);
                        document.Add(summary);
                    }
                    else
                    {
                        var noDataMsg = new Paragraph("没有可显示的数据")
                            .SetFont(font)
                            .SetFontSize(12)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetMarginTop(30)
                            .SetMarginBottom(30);
                        document.Add(noDataMsg);
                    }

                    tableIndex++;
                }

                AddPageNumbers(pdfDocument, font);
                document.Close();

                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"多表PDF导出错误: {ex.Message}");
                return CreateErrorPdf(ex.Message);
            }
        }

        /// <summary>
        /// 保存PDF文件到指定路径
        /// </summary>
        public async Task<string> SavePdfFileAsync(
            DataTable data, 
            string fileName, 
            string filePath, 
            PdfExportOptions? options = null)
        {
            var pdfBytes = await ExportToPdfAsync(data, options);
            var fullPath = System.IO.Path.Combine(filePath, fileName);
            
            await File.WriteAllBytesAsync(fullPath, pdfBytes);
            return fullPath;
        }
    }
} 