@echo off
chcp 65001 > nul
title Cai dat plugin ky so KSTS

rem Bọc script PowerShell bằng .cmd vì Windows không chạy .ps1 khi bấm đúp, và chính sách thực thi mặc
rem định cũng chặn script chưa ký. Bypass chỉ áp cho đúng lần chạy này, không đổi cấu hình của máy.
powershell.exe -ExecutionPolicy Bypass -NoProfile -File "%~dp0cai-dat.ps1"

echo.
pause
