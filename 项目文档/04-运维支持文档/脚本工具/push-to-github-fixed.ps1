# BZK病种库查询系统 - GitHub同步脚本
# 使用说明：运行此脚本将本地提交推送到GitHub远程仓库

Write-Host "=== BZK病种库查询系统 GitHub同步 ===" -ForegroundColor Green
Write-Host "远程仓库: https://github.com/sunxmin/BZK_NEW.git" -ForegroundColor Cyan

# 检查网络连接
Write-Host "`n检查网络连接..." -ForegroundColor Yellow
try {
    $ping = Test-NetConnection -ComputerName "github.com" -Port 443 -WarningAction SilentlyContinue
    if ($ping.TcpTestSucceeded) {
        Write-Host "✓ GitHub连接正常" -ForegroundColor Green
    }
    else {
        Write-Host "✗ 无法连接到GitHub" -ForegroundColor Red
        Write-Host "请检查网络连接或稍后重试" -ForegroundColor Yellow
        exit 1
    }
}
catch {
    Write-Host "✗ 网络检查失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 检查Git状态
Write-Host "`n检查本地Git状态..." -ForegroundColor Yellow
$status = git status --porcelain
if ($status) {
    Write-Host "发现未提交的更改，先提交本地更改..." -ForegroundColor Yellow
    git add .
    $commitMessage = "自动提交 - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    git commit -m $commitMessage
}

# 显示提交历史
Write-Host "`n最近的提交记录:" -ForegroundColor Cyan
git log --oneline -5

# 推送到GitHub
Write-Host "`n开始推送到GitHub..." -ForegroundColor Yellow
$maxRetries = 3
$retry = 0

while ($retry -lt $maxRetries) {
    try {
        Write-Host "尝试推送 ($($retry + 1)/$maxRetries)..." -ForegroundColor Yellow

        # 推送到远程仓库
        git push origin master

        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ 成功推送到GitHub!" -ForegroundColor Green
            Write-Host "远程仓库已更新，包含最新的部署文件和配置" -ForegroundColor Green
            break
        }
        else {
            throw "Git push失败，退出代码: $LASTEXITCODE"
        }

    }
    catch {
        $retry++
        Write-Host "✗ 推送失败: $($_.Exception.Message)" -ForegroundColor Red

        if ($retry -lt $maxRetries) {
            Write-Host "等待10秒后重试..." -ForegroundColor Yellow
            Start-Sleep 10
        }
        else {
            Write-Host "`n推送失败，可能的原因:" -ForegroundColor Red
            Write-Host "1. 网络连接不稳定" -ForegroundColor Yellow
            Write-Host "2. 需要配置GitHub Personal Access Token" -ForegroundColor Yellow
            Write-Host "3. 远程仓库可能有新的提交需要先拉取" -ForegroundColor Yellow
            Write-Host "`n建议操作:" -ForegroundColor Cyan
            Write-Host "git pull origin main --rebase" -ForegroundColor White
            Write-Host "git push origin master" -ForegroundColor White
            exit 1
        }
    }
}

Write-Host "`n=== 同步完成 ===" -ForegroundColor Green
Write-Host "本地代码已成功同步到GitHub仓库" -ForegroundColor Green
Write-Host "仓库地址: https://github.com/sunxmin/BZK_NEW" -ForegroundColor Cyan
