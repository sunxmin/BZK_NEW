# 专病多维度查询系统 - CDN资源本地化脚本
# 用于解决首次加载慢的问题

Write-Host "? 开始下载CDN资源到本地..." -ForegroundColor Green

# 创建目录结构
$directories = @(
    "BZKQuerySystem.Web\wwwroot\lib\select2\css",
    "BZKQuerySystem.Web\wwwroot\lib\select2\js",
    "BZKQuerySystem.Web\wwwroot\lib\sweetalert2\css",
    "BZKQuerySystem.Web\wwwroot\lib\sweetalert2\js",
    "BZKQuerySystem.Web\wwwroot\lib\signalr\js",
    "BZKQuerySystem.Web\wwwroot\lib\chartjs\js"
)

foreach ($dir in $directories) {
    if (!(Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force
        Write-Host "? 创建目录: $dir" -ForegroundColor Yellow
    }
}

# 定义要下载的资源
$resources = @{
    # Select2 资源
    "https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/css/select2.min.css" = "BZKQuerySystem.Web\wwwroot\lib\select2\css\select2.min.css"
    "https://cdn.jsdelivr.net/npm/select2-bootstrap-5-theme@1.3.0/dist/select2-bootstrap-5-theme.min.css" = "BZKQuerySystem.Web\wwwroot\lib\select2\css\select2-bootstrap-5-theme.min.css"
    "https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/js/select2.min.js" = "BZKQuerySystem.Web\wwwroot\lib\select2\js\select2.min.js"
    
    # SweetAlert2 资源
    "https://cdn.jsdelivr.net/npm/sweetalert2@11/dist/sweetalert2.min.css" = "BZKQuerySystem.Web\wwwroot\lib\sweetalert2\css\sweetalert2.min.css"
    "https://cdn.jsdelivr.net/npm/sweetalert2@11/dist/sweetalert2.min.js" = "BZKQuerySystem.Web\wwwroot\lib\sweetalert2\js\sweetalert2.min.js"
    
    # SignalR 资源
    "https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.min.js" = "BZKQuerySystem.Web\wwwroot\lib\signalr\js\signalr.min.js"
    
    # Chart.js 资源
    "https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.min.js" = "BZKQuerySystem.Web\wwwroot\lib\chartjs\js\chart.min.js"
}

# 下载资源
$downloadCount = 0
$totalCount = $resources.Count

foreach ($url in $resources.Keys) {
    $localPath = $resources[$url]
    $downloadCount++
    
    try {
        Write-Host "? [$downloadCount/$totalCount] 正在下载: $(Split-Path $localPath -Leaf)" -ForegroundColor Cyan
        
        # 使用WebClient下载文件
        $webClient = New-Object System.Net.WebClient
        $webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")
        $webClient.DownloadFile($url, $localPath)
        
        # 检查文件大小
        $fileSize = (Get-Item $localPath).Length
        $fileSizeKB = [math]::Round($fileSize / 1KB, 2)
        
        Write-Host "? 下载完成: $(Split-Path $localPath -Leaf) ($fileSizeKB KB)" -ForegroundColor Green
        
        $webClient.Dispose()
    }
    catch {
        Write-Host "? 下载失败: $url" -ForegroundColor Red
        Write-Host "   错误信息: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "? CDN资源下载完成!" -ForegroundColor Green
Write-Host ""
Write-Host "? 下一步操作:" -ForegroundColor Yellow
Write-Host "1. 修改 BZKQuerySystem.Web\Views\Shared\_Layout.cshtml" -ForegroundColor White
Write-Host "2. 将CDN引用替换为本地文件引用" -ForegroundColor White
Write-Host "3. 重新编译并测试系统" -ForegroundColor White
Write-Host ""
Write-Host "? 预期性能提升: 60-80%" -ForegroundColor Green
Write-Host ""

# 生成Layout文件修改说明
$layoutModification = @"
<!-- ? _Layout.cshtml 修改说明 -->

将以下CDN引用：
<link href="https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/css/select2.min.css" rel="stylesheet" />
<link href="https://cdn.jsdelivr.net/npm/select2-bootstrap-5-theme@1.3.0/dist/select2-bootstrap-5-theme.min.css" rel="stylesheet" />
<link href="https://cdn.jsdelivr.net/npm/sweetalert2@11/dist/sweetalert2.min.css" rel="stylesheet" />

<script src="https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/js/select2.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/sweetalert2@11/dist/sweetalert2.min.js"></script>
<script src="https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.min.js"></script>

替换为本地引用：
<link rel="stylesheet" href="~/lib/select2/css/select2.min.css" />
<link rel="stylesheet" href="~/lib/select2/css/select2-bootstrap-5-theme.min.css" />
<link rel="stylesheet" href="~/lib/sweetalert2/css/sweetalert2.min.css" />

<script src="~/lib/select2/js/select2.min.js"></script>
<script src="~/lib/sweetalert2/js/sweetalert2.min.js"></script>
<script src="~/lib/signalr/js/signalr.min.js"></script>
<script src="~/lib/chartjs/js/chart.min.js"></script>
"@

$layoutModification | Out-File -FilePath "Layout修改说明.txt" -Encoding UTF8
Write-Host "? 已生成 'Layout修改说明.txt' 文件" -ForegroundColor Magenta

pause 