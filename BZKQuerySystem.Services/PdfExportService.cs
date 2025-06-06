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
        public string Title { get; set; } = "查询结果";
        public string Author { get; set; } = "BZK查询系统";
        public bool IncludeTimestamp { get; set; } = true;
        public bool IncludePageNumbers { get; set; } = true;
        public int MaxRowsPerPage { get; set; } = 50;
        public bool LandscapeOrientation { get; set; } = true; // 默认竖屏
    }

    public class PdfExportService
    {
        /// <summary>
        /// 同步PDF生成
        /// </summary>
        /// <param name="data">要生成PDF的数据表</param>
        /// <param name="options">可选参数</param>
        /// <returns>PDF文件的字节数组</returns>
        public async Task<byte[]> ExportToPdfAsync(DataTable data, PdfExportOptions? options = null)
        {
            return await Task.Run(() => ExportToPdf(data, options ?? new PdfExportOptions()));
        }

        /// <summary>
        /// 同步PDF生成
        /// </summary>
        private byte[] ExportToPdf(DataTable data, PdfExportOptions options)
        {
            try
            {
                Console.WriteLine($"开始生成PDF: 表数量: {data?.Rows?.Count ?? 0}, 列数量: {data?.Columns?.Count ?? 0}");

                // 验证数据
                if (data == null)
                {
                    Console.WriteLine("PDF导出错误: 数据表为空");
                    return CreateErrorPdf("数据表为空");
                }

                if (data.Columns.Count == 0)
                {
                    Console.WriteLine("PDF导出错误: 数据表没有列定义");
                    return CreateErrorPdf("数据表没有列定义");
                }

                using var memoryStream = new MemoryStream();
                using var pdfWriter = new PdfWriter(memoryStream);
                using var pdfDocument = new PdfDocument(pdfWriter);

                // 设置页面大小和方向
                PageSize pageSize = options.LandscapeOrientation ? PageSize.A4.Rotate() : PageSize.A4;
                using var document = new Document(pdfDocument, pageSize);

                // 设置文档元数据
                try
                {
                    var documentInfo = pdfDocument.GetDocumentInfo();
                    documentInfo.SetTitle(options.Title ?? "查询结果");
                    documentInfo.SetAuthor(options.Author ?? "BZK查询系统");
                    documentInfo.SetCreator("BZK查询系统");
                    documentInfo.SetSubject("数据查询报告");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"设置PDF元数据时出错: {ex.Message}");
                }

                // 创建支持中文的字体
                PdfFont font = CreateChineseSupportFont();
                if (font == null)
                {
                    Console.WriteLine("PDF导出错误: 无法创建字体");
                    return CreateErrorPdf("无法创建字体");
                }

                // 添加标题
                if (!string.IsNullOrEmpty(options.Title))
                {
                    try
                    {
                        var title = new Paragraph(options.Title)
                            .SetFont(font)
                            .SetFontSize(16)
                            .SetBold()
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetMarginBottom(20);
                        document.Add(title);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"添加PDF标题时出错: {ex.Message}");
                    }
                }

                // 添加时间戳
                if (options.IncludeTimestamp)
                {
                    try
                    {
                        var timestamp = new Paragraph($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                            .SetFont(font)
                            .SetFontSize(10)
                            .SetTextAlignment(TextAlignment.RIGHT)
                            .SetMarginBottom(15);
                        document.Add(timestamp);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"添加PDF时间戳时出错: {ex.Message}");
                    }
                }

                // 添加数据表
                if (data.Rows.Count > 0 && data.Columns.Count > 0)
                {
                    try
                    {
                        var table = CreatePdfTable(data, font, options.LandscapeOrientation);
                        if (table != null)
                        {
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
                            Console.WriteLine("PDF导出警告: 无法创建数据表");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"创建PDF数据表时出错: {ex.Message}");
                        // 添加错误信息而不是完全失败
                        var errorMsg = new Paragraph($"数据表生成失败: {ex.Message}")
                            .SetFont(font)
                            .SetFontSize(12)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetMarginTop(50);
                        document.Add(errorMsg);
                    }
                }
                else
                {
                    try
                    {
                        var noDataMsg = new Paragraph("没有可以显示的数据")
                            .SetFont(font)
                            .SetFontSize(12)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetMarginTop(50);
                        document.Add(noDataMsg);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"添加无数据消息时出错: {ex.Message}");
                    }
                }

                // 添加页码
                if (options.IncludePageNumbers)
                {
                    try
                    {
                        AddPageNumbers(pdfDocument, font);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"添加PDF页码时出错: {ex.Message}");
                    }
                }

                // 确保文档正确关闭
                try
                {
                    document.Close();
                    pdfDocument.Close();
                    pdfWriter.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"关闭PDF文档时出错: {ex.Message}");
                }

                var result = memoryStream.ToArray();
                Console.WriteLine($"PDF生成成功，文件大小: {result.Length} 字节");
                return result;
            }
            catch (Exception ex)
            {
                // 记录错误并返回简单的错误PDF
                Console.WriteLine($"PDF生成出错: {ex.Message}");
                Console.WriteLine($"错误堆栈: {ex.StackTrace}");
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
                // 方案1: 尝试使用Windows系统宋体字体
                try
                {
                    string fontPath = @"C:\Windows\Fonts\simsun.ttc,0"; // 宋体
                    if (System.IO.File.Exists(@"C:\Windows\Fonts\simsun.ttc"))
                    {
                        Console.WriteLine("使用系统宋体字体");
                        return PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"加载宋体字体失败: {ex.Message}");
                }

                // 方案2: 尝试使用微软雅黑字体
                try
                {
                    string fontPath = @"C:\Windows\Fonts\msyh.ttc,0"; // 微软雅黑
                    if (System.IO.File.Exists(@"C:\Windows\Fonts\msyh.ttc"))
                    {
                        Console.WriteLine("使用微软雅黑字体");
                        return PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"加载微软雅黑字体失败: {ex.Message}");
                }

                // 方案3: 尝试使用其他中文字体
                try
                {
                    Console.WriteLine("尝试使用STSong-Light字体");
                    return PdfFontFactory.CreateFont("STSong-Light", "UniGB-UCS2-H");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"加载STSong-Light字体失败: {ex.Message}");
                }

                // 方案4: 使用支持基础Unicode的字体
                try
                {
                    Console.WriteLine("使用Times Roman Unicode字体");
                    return PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN, PdfEncodings.IDENTITY_H);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"加载Times Roman Unicode字体失败: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"字体创建过程出错: {ex.Message}");
            }

            // 最后备选方案：使用标准字体
            try
            {
                Console.WriteLine("使用Helvetica标准字体");
                return PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建标准字体失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 创建PDF数据表
        /// </summary>
        private Table CreatePdfTable(DataTable data, PdfFont font, bool isLandscape)
        {
            try
            {
                if (data == null || data.Columns.Count == 0)
                {
                    Console.WriteLine("CreatePdfTable错误: 数据表为空或没有列");
                    return null;
                }

                Console.WriteLine($"创建PDF表格: {data.Columns.Count}列, {data.Rows.Count}行");

                var table = new Table(data.Columns.Count);
                table.SetWidth(UnitValue.CreatePercentValue(100));

                // 根据列数调整字体大小
                int fontSize = data.Columns.Count > 10 ? 7 : (data.Columns.Count > 6 ? 8 : 9);
                int headerFontSize = fontSize + 1;

                // 添加表头
                foreach (DataColumn column in data.Columns)
                {
                    try
                    {
                        var headerText = GetDisplayColumnName(column.ColumnName);
                        var cell = new Cell()
                            .SetBackgroundColor(new DeviceRgb(220, 220, 220))
                            .SetFont(font)
                            .SetFontSize(headerFontSize)
                            .SetBold()
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetPadding(4)
                            .Add(new Paragraph(headerText ?? "未知"));

                        table.AddHeaderCell(cell);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"添加表头单元格时出错: {ex.Message}");
                        // 添加一个简单的错误单元格
                        var errorCell = new Cell()
                            .SetBackgroundColor(new DeviceRgb(220, 220, 220))
                            .SetFont(font)
                            .SetFontSize(headerFontSize)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetPadding(4)
                            .Add(new Paragraph("错误"));
                        table.AddHeaderCell(errorCell);
                    }
                }

                // 添加数据行
                for (int row = 0; row < data.Rows.Count; row++)
                {
                    for (int col = 0; col < data.Columns.Count; col++)
                    {
                        try
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
                        catch (Exception ex)
                        {
                            Console.WriteLine($"添加数据单元格时出错(行{row},列{col}): {ex.Message}");
                            // 添加一个简单的错误单元格
                            var errorCell = new Cell()
                                .SetFont(font)
                                .SetFontSize(fontSize)
                                .SetTextAlignment(TextAlignment.LEFT)
                                .SetPadding(3)
                                .Add(new Paragraph("错误"));
                            table.AddCell(errorCell);
                        }
                    }
                }

                Console.WriteLine("PDF表格创建成功");
                return table;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreatePdfTable整体出错: {ex.Message}");
                Console.WriteLine($"错误堆栈: {ex.StackTrace}");
                return null;
            }
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
        /// 获取显示列名
        /// </summary>
        private string GetDisplayColumnName(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
                return "未知";

            // 如果列名包含下划线且长度大于10，则拆分并返回最后一个部分
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
                var errorParagraph = new Paragraph("PDF生成错误")
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
                // 如果创建PDF失败，返回空字节数组
                return new byte[0];
            }
        }

        /// <summary>
        /// 同步生成多个数据表的PDF
        /// </summary>
        public async Task<byte[]> ExportMultipleToPdfAsync(
            Dictionary<string, DataTable> dataTables,
            string title = "多个数据表")
        {
            return await Task.Run(() => ExportMultipleToPdf(dataTables, title));
        }

        /// <summary>
        /// 同步生成多个数据表的PDF
        /// </summary>
        private byte[] ExportMultipleToPdf(Dictionary<string, DataTable> dataTables, string title)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                using var pdfWriter = new PdfWriter(memoryStream);
                using var pdfDocument = new PdfDocument(pdfWriter);
                using var document = new Document(pdfDocument, PageSize.A4.Rotate()); // 多页

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

                // 添加每个数据表
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

                    // 添加数据表
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
                        var noDataMsg = new Paragraph("没有可以显示的数据")
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
                Console.WriteLine($"生成PDF时出错: {ex.Message}");
                return CreateErrorPdf(ex.Message);
            }
        }

        /// <summary>
        /// 同步保存PDF文件到指定路径
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
