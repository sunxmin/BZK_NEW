using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BZKQuerySystem.Services.Quality
{
    /// <summary>
    /// 代码质量检查器
    /// </summary>
    public class CodeQualityChecker
    {
        private readonly ILogger<CodeQualityChecker> _logger;
        private readonly string _projectPath;

        public CodeQualityChecker(ILogger<CodeQualityChecker> logger, string projectPath)
        {
            _logger = logger;
            _projectPath = projectPath;
        }

        /// <summary>
        /// 执行完整的代码质量检查
        /// </summary>
        public async Task<CodeQualityReport> RunFullAnalysisAsync()
        {
            _logger.LogInformation("开始执行代码质量检查");

            var report = new CodeQualityReport
            {
                AnalysisDate = DateTime.UtcNow,
                ProjectPath = _projectPath
            };

            try
            {
                // 检查Console.WriteLine使用
                var consoleIssues = await CheckConsoleWriteLineUsageAsync();
                report.Issues.AddRange(consoleIssues);

                // 检查异常处理
                var exceptionIssues = await CheckExceptionHandlingAsync();
                report.Issues.AddRange(exceptionIssues);

                // 检查代码注释
                var commentIssues = await CheckCodeCommentsAsync();
                report.Issues.AddRange(commentIssues);

                // 检查命名规范
                var namingIssues = await CheckNamingConventionsAsync();
                report.Issues.AddRange(namingIssues);

                // 统计信息
                report.TotalFiles = await CountCSharpFilesAsync();
                report.TotalLines = await CountTotalLinesAsync();
                report.IssueCount = report.Issues.Count;

                // 计算质量分数
                report.QualityScore = CalculateQualityScore(report);

                _logger.LogInformation("代码质量检查完成，发现 {IssueCount} 个问题", report.IssueCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "代码质量检查过程中发生错误");
                report.HasErrors = true;
                report.ErrorMessage = ex.Message;
            }

            return report;
        }

        /// <summary>
        /// 检查Console.WriteLine的使用
        /// </summary>
        private async Task<List<CodeQualityIssue>> CheckConsoleWriteLineUsageAsync()
        {
            var issues = new List<CodeQualityIssue>();
            var pattern = @"Console\.WriteLine\s*\(";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);

            var csFiles = Directory.GetFiles(_projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .ToList();

            foreach (var file in csFiles)
            {
                try
                {
                    var lines = await File.ReadAllLinesAsync(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (regex.IsMatch(lines[i]))
                        {
                            issues.Add(new CodeQualityIssue
                            {
                                File = Path.GetRelativePath(_projectPath, file),
                                Line = i + 1,
                                Severity = "Warning",
                                Category = "Logging",
                                Message = "使用Console.WriteLine而非统一的日志系统",
                                Suggestion = "建议使用ILogger或统一的日志服务"
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "无法读取文件 {File}", file);
                }
            }

            return issues;
        }

        /// <summary>
        /// 检查异常处理
        /// </summary>
        private async Task<List<CodeQualityIssue>> CheckExceptionHandlingAsync()
        {
            var issues = new List<CodeQualityIssue>();
            var emptyCatchPattern = @"catch\s*\([^)]*\)\s*\{\s*\}";
            var genericCatchPattern = @"catch\s*\(\s*Exception\s+\w+\s*\)";
            
            var regex1 = new Regex(emptyCatchPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var regex2 = new Regex(genericCatchPattern, RegexOptions.IgnoreCase);

            var csFiles = Directory.GetFiles(_projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .ToList();

            foreach (var file in csFiles)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    var lines = content.Split('\n');

                    // 检查空的catch块
                    var emptyMatches = regex1.Matches(content);
                    foreach (Match match in emptyMatches)
                    {
                        var lineNumber = content.Substring(0, match.Index).Count(c => c == '\n') + 1;
                        issues.Add(new CodeQualityIssue
                        {
                            File = Path.GetRelativePath(_projectPath, file),
                            Line = lineNumber,
                            Severity = "Error",
                            Category = "Exception Handling",
                            Message = "空的catch块",
                            Suggestion = "应该记录异常或进行适当的错误处理"
                        });
                    }

                    // 检查通用异常捕获
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (regex2.IsMatch(lines[i]) && !lines[i].Contains("_logger") && !lines[i].Contains("Log"))
                        {
                            issues.Add(new CodeQualityIssue
                            {
                                File = Path.GetRelativePath(_projectPath, file),
                                Line = i + 1,
                                Severity = "Warning",
                                Category = "Exception Handling",
                                Message = "捕获通用异常但未记录日志",
                                Suggestion = "建议记录异常详细信息"
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "无法分析文件 {File}", file);
                }
            }

            return issues;
        }

        /// <summary>
        /// 检查代码注释
        /// </summary>
        private async Task<List<CodeQualityIssue>> CheckCodeCommentsAsync()
        {
            var issues = new List<CodeQualityIssue>();
            var publicMethodPattern = @"public\s+(?:async\s+)?(?:static\s+)?[\w<>]+\s+\w+\s*\(";
            var xmlCommentPattern = @"///\s*<summary>";
            
            var methodRegex = new Regex(publicMethodPattern, RegexOptions.IgnoreCase);
            var commentRegex = new Regex(xmlCommentPattern, RegexOptions.IgnoreCase);

            var csFiles = Directory.GetFiles(_projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .ToList();

            foreach (var file in csFiles)
            {
                try
                {
                    var lines = await File.ReadAllLinesAsync(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (methodRegex.IsMatch(lines[i]))
                        {
                            // 检查前面几行是否有XML注释
                            bool hasComment = false;
                            for (int j = Math.Max(0, i - 5); j < i; j++)
                            {
                                if (commentRegex.IsMatch(lines[j]))
                                {
                                    hasComment = true;
                                    break;
                                }
                            }

                            if (!hasComment)
                            {
                                issues.Add(new CodeQualityIssue
                                {
                                    File = Path.GetRelativePath(_projectPath, file),
                                    Line = i + 1,
                                    Severity = "Info",
                                    Category = "Documentation",
                                    Message = "公共方法缺少XML文档注释",
                                    Suggestion = "建议为公共方法添加XML文档注释"
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "无法检查注释 {File}", file);
                }
            }

            return issues;
        }

        /// <summary>
        /// 检查命名规范
        /// </summary>
        private async Task<List<CodeQualityIssue>> CheckNamingConventionsAsync()
        {
            var issues = new List<CodeQualityIssue>();
            var classPattern = @"public\s+class\s+([a-z]\w*)";
            var methodPattern = @"public\s+(?:async\s+)?(?:static\s+)?[\w<>]+\s+([a-z]\w*)\s*\(";
            
            var classRegex = new Regex(classPattern, RegexOptions.IgnoreCase);
            var methodRegex = new Regex(methodPattern, RegexOptions.IgnoreCase);

            var csFiles = Directory.GetFiles(_projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .ToList();

            foreach (var file in csFiles)
            {
                try
                {
                    var lines = await File.ReadAllLinesAsync(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        // 检查类名
                        var classMatch = classRegex.Match(lines[i]);
                        if (classMatch.Success)
                        {
                            var className = classMatch.Groups[1].Value;
                            if (char.IsLower(className[0]))
                            {
                                issues.Add(new CodeQualityIssue
                                {
                                    File = Path.GetRelativePath(_projectPath, file),
                                    Line = i + 1,
                                    Severity = "Warning",
                                    Category = "Naming",
                                    Message = $"类名 '{className}' 应该以大写字母开头",
                                    Suggestion = "使用PascalCase命名规范"
                                });
                            }
                        }

                        // 检查方法名
                        var methodMatch = methodRegex.Match(lines[i]);
                        if (methodMatch.Success)
                        {
                            var methodName = methodMatch.Groups[1].Value;
                            if (char.IsLower(methodName[0]))
                            {
                                issues.Add(new CodeQualityIssue
                                {
                                    File = Path.GetRelativePath(_projectPath, file),
                                    Line = i + 1,
                                    Severity = "Warning",
                                    Category = "Naming",
                                    Message = $"方法名 '{methodName}' 应该以大写字母开头",
                                    Suggestion = "使用PascalCase命名规范"
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "无法检查命名规范 {File}", file);
                }
            }

            return issues;
        }

        /// <summary>
        /// 统计C#文件数量
        /// </summary>
        private async Task<int> CountCSharpFilesAsync()
        {
            return await Task.Run(() =>
            {
                return Directory.GetFiles(_projectPath, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                    .Count();
            });
        }

        /// <summary>
        /// 统计总行数
        /// </summary>
        private async Task<int> CountTotalLinesAsync()
        {
            var totalLines = 0;
            var csFiles = Directory.GetFiles(_projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .ToList();

            foreach (var file in csFiles)
            {
                try
                {
                    var lines = await File.ReadAllLinesAsync(file);
                    totalLines += lines.Length;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "无法统计文件行数 {File}", file);
                }
            }

            return totalLines;
        }

        /// <summary>
        /// 计算质量分数
        /// </summary>
        private int CalculateQualityScore(CodeQualityReport report)
        {
            var baseScore = 100;
            var errorPenalty = report.Issues.Count(i => i.Severity == "Error") * 10;
            var warningPenalty = report.Issues.Count(i => i.Severity == "Warning") * 5;
            var infoPenalty = report.Issues.Count(i => i.Severity == "Info") * 1;

            var score = baseScore - errorPenalty - warningPenalty - infoPenalty;
            return Math.Max(0, Math.Min(100, score));
        }
    }

    /// <summary>
    /// 代码质量报告
    /// </summary>
    public class CodeQualityReport
    {
        public DateTime AnalysisDate { get; set; }
        public string ProjectPath { get; set; } = string.Empty;
        public int TotalFiles { get; set; }
        public int TotalLines { get; set; }
        public int IssueCount { get; set; }
        public int QualityScore { get; set; }
        public bool HasErrors { get; set; }
        public string? ErrorMessage { get; set; }
        public List<CodeQualityIssue> Issues { get; set; } = new();

        /// <summary>
        /// 按严重程度分组的问题统计
        /// </summary>
        public Dictionary<string, int> IssuesBySeverity => Issues
            .GroupBy(i => i.Severity)
            .ToDictionary(g => g.Key, g => g.Count());

        /// <summary>
        /// 按类别分组的问题统计
        /// </summary>
        public Dictionary<string, int> IssuesByCategory => Issues
            .GroupBy(i => i.Category)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// 代码质量问题
    /// </summary>
    public class CodeQualityIssue
    {
        public string File { get; set; } = string.Empty;
        public int Line { get; set; }
        public string Severity { get; set; } = string.Empty; // Error, Warning, Info
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
    }
} 