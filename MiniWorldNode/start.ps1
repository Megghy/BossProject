# PowerShell脚本 - MiniWorld Node 启动器
# 设置控制台编码为UTF-8

Write-Host "========================================" -ForegroundColor Green
Write-Host "   MiniWorld Node 启动脚本" -ForegroundColor Green
Write-Host "   正在设置UTF-8编码..." -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

try {
    # 设置控制台编码为UTF-8
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    [Console]::InputEncoding = [System.Text.Encoding]::UTF8

    # 对Windows系统设置代码页
    if ($IsWindows -or $env:OS -eq "Windows_NT") {
        chcp 65001 | Out-Null
        Write-Host "✓ 控制台编码已设置为UTF-8" -ForegroundColor Green
    }

    # 设置窗口标题
    if ($Host.UI.RawUI.WindowTitle) {
        $Host.UI.RawUI.WindowTitle = "MiniWorld Node - 迷你世界节点服务器"
    }

    Write-Host "正在启动 MiniWorld Node..." -ForegroundColor Yellow

    # 启动程序
    dotnet run

} catch {
    Write-Host "启动时发生错误: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "详细错误信息: $($_.Exception)" -ForegroundColor Red
} finally {
    Write-Host ""
    Write-Host "程序已退出，按任意键关闭窗口..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}