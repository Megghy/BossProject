@echo off
chcp 65001 > nul
title MiniWorld Node - 迷你世界节点服务器

echo ========================================
echo    MiniWorld Node 启动脚本
echo    正在设置UTF-8编码...
echo ========================================

:: 设置控制台代码页为UTF-8
chcp 65001

:: 启动程序
dotnet MiniWorldNode.dll

:: 程序退出后暂停
echo.
echo 程序已退出，按任意键关闭窗口...
pause > nul