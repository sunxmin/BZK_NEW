# ר����ά�Ȳ�ѯϵͳ - CDN��Դ���ػ��ű�
# ���ڽ���״μ�����������

Write-Host "? ��ʼ����CDN��Դ������..." -ForegroundColor Green

# ����Ŀ¼�ṹ
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
        Write-Host "? ����Ŀ¼: $dir" -ForegroundColor Yellow
    }
}

# ����Ҫ���ص���Դ
$resources = @{
    # Select2 ��Դ
    "https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/css/select2.min.css" = "BZKQuerySystem.Web\wwwroot\lib\select2\css\select2.min.css"
    "https://cdn.jsdelivr.net/npm/select2-bootstrap-5-theme@1.3.0/dist/select2-bootstrap-5-theme.min.css" = "BZKQuerySystem.Web\wwwroot\lib\select2\css\select2-bootstrap-5-theme.min.css"
    "https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/js/select2.min.js" = "BZKQuerySystem.Web\wwwroot\lib\select2\js\select2.min.js"
    
    # SweetAlert2 ��Դ
    "https://cdn.jsdelivr.net/npm/sweetalert2@11/dist/sweetalert2.min.css" = "BZKQuerySystem.Web\wwwroot\lib\sweetalert2\css\sweetalert2.min.css"
    "https://cdn.jsdelivr.net/npm/sweetalert2@11/dist/sweetalert2.min.js" = "BZKQuerySystem.Web\wwwroot\lib\sweetalert2\js\sweetalert2.min.js"
    
    # SignalR ��Դ
    "https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.min.js" = "BZKQuerySystem.Web\wwwroot\lib\signalr\js\signalr.min.js"
    
    # Chart.js ��Դ
    "https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.min.js" = "BZKQuerySystem.Web\wwwroot\lib\chartjs\js\chart.min.js"
}

# ������Դ
$downloadCount = 0
$totalCount = $resources.Count

foreach ($url in $resources.Keys) {
    $localPath = $resources[$url]
    $downloadCount++
    
    try {
        Write-Host "? [$downloadCount/$totalCount] ��������: $(Split-Path $localPath -Leaf)" -ForegroundColor Cyan
        
        # ʹ��WebClient�����ļ�
        $webClient = New-Object System.Net.WebClient
        $webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")
        $webClient.DownloadFile($url, $localPath)
        
        # ����ļ���С
        $fileSize = (Get-Item $localPath).Length
        $fileSizeKB = [math]::Round($fileSize / 1KB, 2)
        
        Write-Host "? �������: $(Split-Path $localPath -Leaf) ($fileSizeKB KB)" -ForegroundColor Green
        
        $webClient.Dispose()
    }
    catch {
        Write-Host "? ����ʧ��: $url" -ForegroundColor Red
        Write-Host "   ������Ϣ: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "? CDN��Դ�������!" -ForegroundColor Green
Write-Host ""
Write-Host "? ��һ������:" -ForegroundColor Yellow
Write-Host "1. �޸� BZKQuerySystem.Web\Views\Shared\_Layout.cshtml" -ForegroundColor White
Write-Host "2. ��CDN�����滻Ϊ�����ļ�����" -ForegroundColor White
Write-Host "3. ���±��벢����ϵͳ" -ForegroundColor White
Write-Host ""
Write-Host "? Ԥ����������: 60-80%" -ForegroundColor Green
Write-Host ""

# ����Layout�ļ��޸�˵��
$layoutModification = @"
<!-- ? _Layout.cshtml �޸�˵�� -->

������CDN���ã�
<link href="https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/css/select2.min.css" rel="stylesheet" />
<link href="https://cdn.jsdelivr.net/npm/select2-bootstrap-5-theme@1.3.0/dist/select2-bootstrap-5-theme.min.css" rel="stylesheet" />
<link href="https://cdn.jsdelivr.net/npm/sweetalert2@11/dist/sweetalert2.min.css" rel="stylesheet" />

<script src="https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/js/select2.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/sweetalert2@11/dist/sweetalert2.min.js"></script>
<script src="https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.min.js"></script>

�滻Ϊ�������ã�
<link rel="stylesheet" href="~/lib/select2/css/select2.min.css" />
<link rel="stylesheet" href="~/lib/select2/css/select2-bootstrap-5-theme.min.css" />
<link rel="stylesheet" href="~/lib/sweetalert2/css/sweetalert2.min.css" />

<script src="~/lib/select2/js/select2.min.js"></script>
<script src="~/lib/sweetalert2/js/sweetalert2.min.js"></script>
<script src="~/lib/signalr/js/signalr.min.js"></script>
<script src="~/lib/chartjs/js/chart.min.js"></script>
"@

$layoutModification | Out-File -FilePath "Layout�޸�˵��.txt" -Encoding UTF8
Write-Host "? ������ 'Layout�޸�˵��.txt' �ļ�" -ForegroundColor Magenta

pause 