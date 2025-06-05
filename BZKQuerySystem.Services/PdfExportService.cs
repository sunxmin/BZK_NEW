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
        public string Title { get; set; } = "��ѯ����";
        public string Author { get; set; } = "BZK��ѯϵͳ";
        public bool IncludeTimestamp { get; set; } = true;
        public bool IncludePageNumbers { get; set; } = true;
        public int MaxRowsPerPage { get; set; } = 50;
        public bool LandscapeOrientation { get; set; } = true; // Ĭ�Ϻ���
    }

    public class PdfExportService
    {
        /// <summary>
        /// �첽�������ݱ�PDF
        /// </summary>
        /// <param name="data">Ҫ���������ݱ�</param>
        /// <param name="options">����ѡ��</param>
        /// <returns>PDF�ļ����ֽ�����</returns>
        public async Task<byte[]> ExportToPdfAsync(DataTable data, PdfExportOptions? options = null)
        {
            return await Task.Run(() => ExportToPdf(data, options ?? new PdfExportOptions()));
        }

        /// <summary>
        /// ͬ��PDF����
        /// </summary>
        private byte[] ExportToPdf(DataTable data, PdfExportOptions options)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                using var pdfWriter = new PdfWriter(memoryStream);
                using var pdfDocument = new PdfDocument(pdfWriter);
                
                // ����ҳ���С�ͷ���
                PageSize pageSize = options.LandscapeOrientation ? PageSize.A4.Rotate() : PageSize.A4;
                using var document = new Document(pdfDocument, pageSize);

                // �����ĵ�Ԫ����
                var documentInfo = pdfDocument.GetDocumentInfo();
                documentInfo.SetTitle(options.Title);
                documentInfo.SetAuthor(options.Author);
                documentInfo.SetCreator("BZK��ѯϵͳ");
                documentInfo.SetSubject("���ݲ�ѯ����");

                // ����֧�����ĵ�����
                PdfFont font = CreateChineseSupportFont();

                // ��ӱ���
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

                // ���ʱ���
                if (options.IncludeTimestamp)
                {
                    var timestamp = new Paragraph($"����ʱ��: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                        .SetFont(font)
                        .SetFontSize(10)
                        .SetTextAlignment(TextAlignment.RIGHT)
                        .SetMarginBottom(15);
                    document.Add(timestamp);
                }

                // �������
                if (data.Rows.Count > 0)
                {
                    var table = CreatePdfTable(data, font, options.LandscapeOrientation);
                    document.Add(table);

                    // ���ͳ����Ϣ
                    var summary = new Paragraph($"�ܼ�¼��: {data.Rows.Count}")
                        .SetFont(font)
                        .SetFontSize(10)
                        .SetMarginTop(15)
                        .SetTextAlignment(TextAlignment.RIGHT);
                    document.Add(summary);
                }
                else
                {
                    var noDataMsg = new Paragraph("û�п���ʾ������")
                        .SetFont(font)
                        .SetFontSize(12)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginTop(50);
                    document.Add(noDataMsg);
                }

                // ���ҳ��
                if (options.IncludePageNumbers)
                {
                    AddPageNumbers(pdfDocument, font);
                }

                // ȷ���ĵ���ȷ�ر�
                document.Close();
                pdfDocument.Close();
                pdfWriter.Close();

                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                // ��¼���󲢷��ؼ򵥵Ĵ���PDF
                Console.WriteLine($"PDF��������: {ex.Message}");
                return CreateErrorPdf(ex.Message);
            }
        }

        /// <summary>
        /// ����֧�����ĵ�����
        /// </summary>
        private PdfFont CreateChineseSupportFont()
        {
            try
            {
                // ����1: ����ʹ��Windowsϵͳ��������
                try
                {
                    // ����ʹ��ϵͳ�����������ļ�
                    string fontPath = @"C:\Windows\Fonts\simsun.ttc,0"; // ����
                    if (System.IO.File.Exists(@"C:\Windows\Fonts\simsun.ttc"))
                    {
                        return PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                    }
                }
                catch { }

                // ����2: ����ʹ������ϵͳ����
                try
                {
                    string fontPath = @"C:\Windows\Fonts\msyh.ttc,0"; // ΢���ź�
                    if (System.IO.File.Exists(@"C:\Windows\Fonts\msyh.ttc"))
                    {
                        return PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                    }
                }
                catch { }

                // ����3: ����ʹ����������
                try
                {
                    return PdfFontFactory.CreateFont("STSong-Light", "UniGB-UCS2-H");
                }
                catch { }

                // ����4: ʹ��֧�ֻ���Unicode������
                try
                {
                    return PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN, PdfEncodings.IDENTITY_H);
                }
                catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"���崴������: {ex.Message}");
            }

            // ���ı�ѡ������ʹ�ñ�׼����
            return PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        }

        /// <summary>
        /// ����PDF���
        /// </summary>
        private Table CreatePdfTable(DataTable data, PdfFont font, bool isLandscape)
        {
            var table = new Table(data.Columns.Count);
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // �����������������С
            int fontSize = data.Columns.Count > 10 ? 7 : (data.Columns.Count > 6 ? 8 : 9);
            int headerFontSize = fontSize + 1;

            // ��ӱ�ͷ
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

            // ���������
            for (int row = 0; row < data.Rows.Count; row++)
            {
                for (int col = 0; col < data.Columns.Count; col++)
                {
                    var cellValue = data.Rows[row][col]?.ToString() ?? "";
                    
                    // �ضϹ������ı�
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
        /// ���ҳ��
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
        /// ��ȡ��ʾ������
        /// </summary>
        private string GetDisplayColumnName(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
                return "��";

            // ������������»����ҳ��ȴ���10����ָ�������һ������
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
        /// �����򵥵Ĵ���PDF
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

                // ��Ӵ�����Ϣ
                var errorParagraph = new Paragraph("PDF��������")
                    .SetFont(font)
                    .SetFontSize(16)
                    .SetBold()
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20);
                document.Add(errorParagraph);

                var messageParagraph = new Paragraph($"����: {errorMessage}")
                    .SetFont(font)
                    .SetFontSize(12)
                    .SetTextAlignment(TextAlignment.LEFT);
                document.Add(messageParagraph);

                document.Close();
                return memoryStream.ToArray();
            }
            catch
            {
                // ���������PDF��ʧ�ܣ����ؿ�����
                return new byte[0];
            }
        }

        /// <summary>
        /// �첽����������ݱ�PDF
        /// </summary>
        public async Task<byte[]> ExportMultipleToPdfAsync(
            Dictionary<string, DataTable> dataTables, 
            string title = "������ݱ���")
        {
            return await Task.Run(() => ExportMultipleToPdf(dataTables, title));
        }

        /// <summary>
        /// ͬ�������
        /// </summary>
        private byte[] ExportMultipleToPdf(Dictionary<string, DataTable> dataTables, string title)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                using var pdfWriter = new PdfWriter(memoryStream);
                using var pdfDocument = new PdfDocument(pdfWriter);
                using var document = new Document(pdfDocument, PageSize.A4.Rotate()); // ���򲼾�

                var font = CreateChineseSupportFont();

                // ���������
                var mainTitle = new Paragraph(title)
                    .SetFont(font)
                    .SetFontSize(18)
                    .SetBold()
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(30);
                document.Add(mainTitle);

                // �������ʱ��
                var timestamp = new Paragraph($"����ʱ��: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                    .SetFont(font)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(20);
                document.Add(timestamp);

                // ����ÿ�����ݱ�
                int tableIndex = 0;
                foreach (var kvp in dataTables)
                {
                    if (tableIndex > 0)
                    {
                        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                    }

                    // ��ӱ����
                    var tableTitle = new Paragraph(kvp.Key)
                        .SetFont(font)
                        .SetFontSize(14)
                        .SetBold()
                        .SetMarginBottom(15);
                    document.Add(tableTitle);

                    // ��ӱ��
                    if (kvp.Value.Rows.Count > 0)
                    {
                        var table = CreatePdfTable(kvp.Value, font, true);
                        document.Add(table);

                        var summary = new Paragraph($"��¼��: {kvp.Value.Rows.Count}")
                            .SetFont(font)
                            .SetFontSize(10)
                            .SetMarginTop(10)
                            .SetMarginBottom(20);
                        document.Add(summary);
                    }
                    else
                    {
                        var noDataMsg = new Paragraph("û�п���ʾ������")
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
                Console.WriteLine($"���PDF��������: {ex.Message}");
                return CreateErrorPdf(ex.Message);
            }
        }

        /// <summary>
        /// ����PDF�ļ���ָ��·��
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