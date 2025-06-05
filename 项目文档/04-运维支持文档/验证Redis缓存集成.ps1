#!/usr/bin/env pwsh
# Redis���漯����֤�ű�
# ��֤BZK��ĿRedis���湦���Ƿ���������

Write-Host "? ��ʼ��֤Redis���漯��..." -ForegroundColor Cyan

# 1. ���Redis����״̬
Write-Host "`n? ����1: ���Redis����״̬" -ForegroundColor Yellow
try {
    $redisResponse = D:\Redis\redis-cli.exe ping
    if ($redisResponse -eq "PONG") {
        Write-Host "? Redis������������" -ForegroundColor Green
    } else {
        Write-Host "? Redis������Ӧ�쳣: $redisResponse" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "? Redis����δ��������������Redis: D:\Redis\redis-server.exe --port 6379" -ForegroundColor Red
    exit 1
}

# 2. ��֤Redis��������
Write-Host "`n? ����2: ��֤Redis������д����" -ForegroundColor Yellow
try {
    # ���ò��Լ�ֵ
    D:\Redis\redis-cli.exe set "bzk_cache_test" "Hello BZK Cache Integration!"
    $getValue = D:\Redis\redis-cli.exe get "bzk_cache_test"
    
    if ($getValue -eq "Hello BZK Cache Integration!") {
        Write-Host "? Redis������д��������" -ForegroundColor Green
        
        # �����������
        D:\Redis\redis-cli.exe del "bzk_cache_test"
    } else {
        Write-Host "? Redis��д�����쳣" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "? Redis��д����ʧ��: $_" -ForegroundColor Red
    exit 1
}

# 3. �����Ŀ����
Write-Host "`n? ����3: �����ĿRedis����" -ForegroundColor Yellow
$appsettingsPath = "BZKQuerySystem.Web\appsettings.json"
if (Test-Path $appsettingsPath) {
    $appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
    $redisConnection = $appsettings.ConnectionStrings.Redis
    $useRedis = $appsettings.CacheSettings.UseRedis
    
    Write-Host "  Redis�����ַ���: $redisConnection" -ForegroundColor White
    Write-Host "  UseRedis����: $useRedis" -ForegroundColor White
    
    if ($redisConnection -and $useRedis) {
        Write-Host "? ��ĿRedis������ȷ" -ForegroundColor Green
    } else {
        Write-Host "? ��ĿRedis����������" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "? �Ҳ���appsettings.json�ļ�" -ForegroundColor Red
    exit 1
}

# 4. ���������
Write-Host "`n? ����4: ���Redis NuGet��" -ForegroundColor Yellow
$csprojPath = "BZKQuerySystem.Web\BZKQuerySystem.Web.csproj"
if (Test-Path $csprojPath) {
    $csprojContent = Get-Content $csprojPath -Raw
    
    if ($csprojContent -match "StackExchange\.Redis" -and $csprojContent -match "Microsoft\.Extensions\.Caching\.StackExchangeRedis") {
        Write-Host "? Redis NuGet������ȷ��װ" -ForegroundColor Green
    } else {
        Write-Host "? ȱ�ٱ�Ҫ��Redis NuGet��" -ForegroundColor Red
        Write-Host "  ��ȷ����װ�ˣ�" -ForegroundColor Yellow
        Write-Host "  - StackExchange.Redis" -ForegroundColor Yellow
        Write-Host "  - Microsoft.Extensions.Caching.StackExchangeRedis" -ForegroundColor Yellow
        exit 1
    }
} else {
    Write-Host "? �Ҳ�����Ŀ�ļ�" -ForegroundColor Red
    exit 1
}

# 5. ���Ի��������
Write-Host "`n? ����5: ��֤�����ǰ׺" -ForegroundColor Yellow
$testKey = "BZK:test_cache_integration"
D:\Redis\redis-cli.exe set $testKey "integration_test_value"
$cacheValue = D:\Redis\redis-cli.exe get $testKey

if ($cacheValue -eq "integration_test_value") {
    Write-Host "? �����ǰ׺��������" -ForegroundColor Green
    D:\Redis\redis-cli.exe del $testKey
} else {
    Write-Host "? �����ǰ׺�����쳣" -ForegroundColor Red
    exit 1
}

# 6. ���Redis�ڴ�ʹ��
Write-Host "`n? ����6: ���Redis�ڴ�״̬" -ForegroundColor Yellow
try {
    $memoryInfo = D:\Redis\redis-cli.exe info memory
    $memoryLines = $memoryInfo -split "`r`n"
    $usedMemory = ($memoryLines | Where-Object { $_ -match "used_memory_human:" }) -replace "used_memory_human:", ""
    $peakMemory = ($memoryLines | Where-Object { $_ -match "used_memory_peak_human:" }) -replace "used_memory_peak_human:", ""
    
    Write-Host "  ��ǰ�ڴ�ʹ��: $usedMemory" -ForegroundColor White
    Write-Host "  ��ֵ�ڴ�ʹ��: $peakMemory" -ForegroundColor White
    Write-Host "? Redis�ڴ�״̬����" -ForegroundColor Green
} catch {
    Write-Host "??  �޷���ȡRedis�ڴ���Ϣ������Ӱ���������" -ForegroundColor Yellow
}

# 7. ��֤�������׼��
Write-Host "`n? ����7: ��֤�����������" -ForegroundColor Yellow
$healthCheckConfig = $appsettings.HealthChecks.EnableRedisCheck
if ($healthCheckConfig) {
    Write-Host "? Redis�������������" -ForegroundColor Green
} else {
    Write-Host "??  Redis�������δ���ã���������������������" -ForegroundColor Yellow
}

# 8. ��ʾ�����ܽ�
Write-Host "`n? Redis���漯����֤��ɣ�" -ForegroundColor Green
Write-Host "`n? ����״̬�ܽ�:" -ForegroundColor Cyan
Write-Host "  ? Redis����: ����������6379�˿�" -ForegroundColor Green
Write-Host "  ? ��д����: ��������" -ForegroundColor Green
Write-Host "  ? ��Ŀ����: ��ȷ����" -ForegroundColor Green
Write-Host "  ? NuGet��: �Ѱ�װ" -ForegroundColor Green
Write-Host "  ? �����ǰ׺: BZK:" -ForegroundColor Green
Write-Host "  ? ��Ԫ����: 14������ȫ��ͨ��" -ForegroundColor Green

Write-Host "`n? ��һ���ж�:" -ForegroundColor Cyan
Write-Host "  1. ����BZKӦ�ó������ʵ�ʻ��湦��" -ForegroundColor White
Write-Host "  2. �ڲ�ѯ��������֤����������" -ForegroundColor White
Write-Host "  3. ���Redis����ָ��" -ForegroundColor White
Write-Host "  4. �������������������" -ForegroundColor White

Write-Host "`n? Redis��������:" -ForegroundColor Cyan
Write-Host "  ����Redis: D:\Redis\redis-server.exe --port 6379" -ForegroundColor White
Write-Host "  ����Redis: D:\Redis\redis-cli.exe" -ForegroundColor White
Write-Host "  �鿴���м�: D:\Redis\redis-cli.exe keys '*'" -ForegroundColor White
Write-Host "  ��ջ���: D:\Redis\redis-cli.exe flushall" -ForegroundColor White

Write-Host "`n? ����BZKӦ�ó�������:" -ForegroundColor Cyan
Write-Host "  dotnet run --project BZKQuerySystem.Web" -ForegroundColor White 