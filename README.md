# PrivacyShield for Windows 🛡️

[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D6.svg?logo=windows)](https://github.com/knworker/PrivacyShield-windows)
[![Runtime](https://img.shields.io/badge/.NET%20Framework-4.0%2B-512BD4.svg?logo=dotnet)](https://github.com/knworker/PrivacyShield-windows)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Build and Release](https://github.com/knworker/PrivacyShield-windows/actions/workflows/build-release.yml/badge.svg)](https://github.com/knworker/PrivacyShield-windows/actions/workflows/build-release.yml)

> **極致輕量、即時喚出的 Windows 螢幕隱私遮蔽與防窺工具。**  
> 一鍵快捷鍵切換半透明/全黑遮罩，支援八向縮放、滾輪無段微調透明度，並內建「**恐慌模式**」瞬間黑屏所有螢幕，保護會議、直播與公開場合下的個人敏感隱私！

---

## ✨ 核心特色

- ⚡ **超輕量免安裝**：單一 C# 檔案架構，編譯後體積僅約 **20 KB**，零外部相依套件，啟動瞬間完成。
- 🚨 **恐慌模式（Panic Mode）**：按下 `Ctrl + Shift + F`，立即將**所有螢幕（包含多螢幕虛擬桌面）**以 100% 不透明全黑覆蓋，瞬間阻絕任何窺探。
- ⌨️ **全域快捷鍵**：按下 `Ctrl + Q` 隨時在任何應用程式上方顯示或隱藏遮蔽視窗。
- 🖱️ **滑鼠自由操控**：
  - **任意拖曳**：滑鼠左鍵點擊視窗任意處即可隨處拖曳定位。
  - **八向縮放**：游標移至邊緣或四角自動轉換為調整大小指標。
  - **滾輪調光**：直接在視窗上滾動滑鼠滾輪，即時微調透明度（10% ~ 100%）。
- 📐 **快速螢幕佈局**：右鍵選單內建「螢幕上半」、「螢幕下半」、「螢幕左半」、「螢幕右半」以及「預設置中 (800×500)」，一秒貼齊定位。
- 💾 **自動記憶佈局**：關閉或隱藏時自動將視窗位置、長寬及透明度儲存至 `PrivacyShield.ini`，下次啟動自動精準還原。
- 📌 **系統匣常駐整合**：工作列系統匣（System Tray）圖示常駐，點擊兩下切換視窗，並支援勾選「**開機自動啟動**」。
- 🔒 **單一執行緒保護**：內建 Mutex 機制，防止重複啟動多個實例。

---

## ⌨️ 快捷鍵與操作指南

| 操作 | 作用 | 說明 |
| :--- | :--- | :--- |
| <kbd>Ctrl</kbd> + <kbd>Q</kbd> | **切換遮蔽** | 在桌面任意處切換顯示／隱藏遮蔽視窗 |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>F</kbd> | **恐慌模式 (Panic)** | 瞬間以 100% 全黑覆蓋所有螢幕；再次按下可還原先前尺寸 |
| **滑鼠滾輪** | **調節透明度** | 在遮蔽視窗上滾動滑鼠滾輪（步進值 5%，範圍 10% ~ 100%） |
| **滑鼠左鍵拖曳** | **移動視窗** | 在視窗內任意空白處按住左鍵即可拖曳移動 |
| **滑鼠邊緣拖曳** | **調整大小** | 游標移至 8 向邊緣或 4 角拖曳可任意縮放長寬 |
| **滑鼠右鍵** | **功能選單** | 開啟右鍵選單，包含透明度數值、快速版面分割與恐慌模式 |
| **右上角 ✕ 按鈕** | **隱藏至背景** | 關閉視窗僅會隱藏並儲存設定，程式仍在系統匣常駐 |

---

## 🚀 取得與編譯方式

### 方式一：直接下載（免安裝）

至 [Releases 頁面](https://github.com/knworker/PrivacyShield-windows/releases) 下載最新編譯好的 `PrivacyShield.exe`，雙擊即可立即使用。

### 方式二：使用 `build.bat` 一鍵編譯（推薦開發者）

專案內建 `build.bat` 腳本，會自動搜尋 Windows 內建的 .NET Framework `csc.exe` 編譯器，無需安裝肥大的 Visual Studio：

1. 複製或下載本專案原始碼：
   ```bash
   git clone https://github.com/knworker/PrivacyShield-windows.git
   cd PrivacyShield-windows
   ```
2. 雙擊執行目錄下的 `build.bat`。
3. 編譯完成後，同目錄下將產生 `PrivacyShield.exe`。

### 方式三：使用命令列手動編譯

在 Windows 命令提示字元（CMD）或 PowerShell 中執行以下單行指令：

```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /optimize+ /out:PrivacyShield.exe PrivacyShield.cs
```

*(註：若為 32 位元系統，請將 `Framework64` 改為 `Framework`)*

---

## 📁 檔案結構

```text
PrivacyShield-windows/
├── .github/
│   └── workflows/
│       └── build-release.yml   # GitHub Actions 自動化編譯與發布腳本
├── .gitignore                  # Git 忽略清單 (排除 exe, ini, pdb 等)
├── build.bat                   # Windows 內建 csc 一鍵編譯批次檔
├── LICENSE                     # MIT 開源授權條款
├── PrivacyShield.cs            # 主程式原始碼 (WinForms + Win32 API)
└── README.md                   # 專案中文說明文件
```

---

## 🛠️ 技術實作細節

- **GUI 架構**：採用 C# Windows Forms (`System.Windows.Forms`)，無邊框視窗 (`FormBorderStyle.None`) 搭配自繪關閉按鈕與微弱發光邊框。
- **Win32 API 整合**：
  - `RegisterHotKey` / `UnregisterHotKey`：註冊全域系統熱鍵（`WM_HOTKEY`）。
  - `SendMessage` / `ReleaseCapture`：利用 Win32 `WM_NCLBUTTONDOWN` 與 `HT_CAPTION` 實現視窗非工作區平滑拖曳與邊界調整。
  - `SystemInformation.VirtualScreen`：動態計算多螢幕環境總畫布範圍，實現跨螢幕完整黑屏。
- **組態管理**：不使用複雜的 Registry 或 XML，以簡潔輕快的 INI 格式讀寫 `PrivacyShield.ini`，隨身攜帶、零殘留。

---

## 📄 授權條款

本專案採用 [MIT License](LICENSE) 授權。歡迎自由修改、分發與個人或商業使用。
