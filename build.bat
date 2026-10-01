@echo off
chcp 65001 >nul
echo [PrivacyShield] 正在偵測編譯環境...

set CSC_EXE=""
if exist "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
    set CSC_EXE="%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
) else if exist "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe" (
    set CSC_EXE="%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

if %CSC_EXE%=="" (
    echo [錯誤] 找不到 .NET Framework 4.0+ csc.exe 編譯器！
    pause
    exit /b 1
)

echo [PrivacyShield] 使用編譯器: %CSC_EXE%
echo [PrivacyShield] 正在編譯 PrivacyShield.exe...

%CSC_EXE% /nologo /target:winexe /optimize+ /out:PrivacyShield.exe PrivacyShield.cs

if %ERRORLEVEL% EQU 0 (
    echo [成功] PrivacyShield.exe 編譯完成！
) else (
    echo [失敗] 編譯過程中發生錯誤 (代碼: %ERRORLEVEL%)。
)

echo.
pause
