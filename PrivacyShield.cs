// PrivacyShield — 隱私遮蔽工具 v2.0
// 全域快捷鍵 Ctrl+Q 切換遮蔽視窗 ｜ Ctrl+Shift+F 恐慌模式全螢幕遮蔽
// 編譯: csc /target:winexe /out:PrivacyShield.exe PrivacyShield.cs

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// ════════════════════════════════════════════════════════════════
//  程式進入點
// ════════════════════════════════════════════════════════════════
static class Program
{
    [STAThread]
    static void Main()
    {
        bool createdNew;
        using (var mutex = new System.Threading.Mutex(true, "PrivacyShield_SingleInstance_Mutex", out createdNew))
        {
            if (!createdNew)
            {
                MessageBox.Show("PrivacyShield 已在執行中。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayContext());
        }
    }
}

// ════════════════════════════════════════════════════════════════
//  設定管理（INI 風格純文字）
// ════════════════════════════════════════════════════════════════
static class Settings
{
    static readonly string FilePath = Path.Combine(
        Path.GetDirectoryName(Application.ExecutablePath),
        "PrivacyShield.ini");

    static Dictionary<string, string> data = new Dictionary<string, string>();

    static Settings()
    {
        Load();
    }

    public static void Load()
    {
        data.Clear();
        if (!File.Exists(FilePath)) return;
        try
        {
            foreach (var line in File.ReadAllLines(FilePath))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("#")) continue;
                int eq = trimmed.IndexOf('=');
                if (eq < 0) continue;
                string key = trimmed.Substring(0, eq).Trim();
                string val = trimmed.Substring(eq + 1).Trim();
                data[key] = val;
            }
        }
        catch { }
    }

    public static void Save()
    {
        try
        {
            var lines = new List<string>();
            lines.Add("# PrivacyShield 設定檔");
            foreach (var kv in data)
                lines.Add(kv.Key + "=" + kv.Value);
            File.WriteAllLines(FilePath, lines.ToArray());
        }
        catch { }
    }

    public static string Get(string key, string def)
    {
        string val;
        if (data.TryGetValue(key, out val)) return val;
        return def;
    }
    public static int GetInt(string key, int def)
    {
        string s;
        if (!data.TryGetValue(key, out s)) return def;
        int v;
        if (int.TryParse(s, out v)) return v;
        return def;
    }
    public static double GetDouble(string key, double def)
    {
        string s;
        if (!data.TryGetValue(key, out s)) return def;
        double v;
        if (double.TryParse(s, out v)) return v;
        return def;
    }
    public static bool GetBool(string key, bool def)
    {
        string s;
        if (!data.TryGetValue(key, out s)) return def;
        if (s == "1" || s.ToLower() == "true") return true;
        if (s == "0" || s.ToLower() == "false") return false;
        return def;
    }

    public static void Set(string key, string val) { data[key] = val; }
    public static void Set(string key, int val) { data[key] = val.ToString(); }
    public static void Set(string key, double val) { data[key] = val.ToString("F2"); }
    public static void Set(string key, bool val) { data[key] = val ? "1" : "0"; }
}

// ════════════════════════════════════════════════════════════════
//  系統匣管理 + 全域快捷鍵
// ════════════════════════════════════════════════════════════════
class TrayContext : ApplicationContext
{
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    const int HOTKEY_TOGGLE = 0x1001;   // Ctrl+Q
    const int HOTKEY_PANIC  = 0x1002;   // Ctrl+Shift+F
    const uint MOD_CTRL     = 0x0002;
    const uint MOD_SHIFT    = 0x0004;
    const uint VK_Q         = 0x51;
    const uint VK_F         = 0x46;

    readonly NotifyIcon  trayIcon;
    readonly ShieldForm  shield;
    readonly HotkeyWnd   hkWnd;

    // 開機自啟用的 Registry 鍵
    const string REG_RUN_KEY  = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    const string REG_APP_NAME = "PrivacyShield";

    public TrayContext()
    {
        // ── 系統匣圖示 ──
        trayIcon = new NotifyIcon
        {
            Icon    = SystemIcons.Shield,
            Text    = "PrivacyShield — Ctrl+Q 遮蔽 ｜ Ctrl+Shift+F 恐慌模式",
            Visible = true,
            ContextMenuStrip = MakeMenu()
        };
        trayIcon.DoubleClick += (s, e) => Toggle();

        // ── 遮蔽視窗（載入佈局） ──
        shield = new ShieldForm();
        shield.LoadLayout();

        // ── 隱藏視窗接收 WM_HOTKEY ──
        hkWnd = new HotkeyWnd();
        hkWnd.HotkeyPressed += OnHotkey;

        // 註冊快捷鍵
        if (!RegisterHotKey(hkWnd.Handle, HOTKEY_TOGGLE, MOD_CTRL, VK_Q))
        {
            MessageBox.Show(
                "無法註冊全域快捷鍵 Ctrl+Q，\n可能已被其他程式佔用。",
                "PrivacyShield", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        if (!RegisterHotKey(hkWnd.Handle, HOTKEY_PANIC, MOD_CTRL | MOD_SHIFT, VK_F))
        {
            MessageBox.Show(
                "無法註冊全域快捷鍵 Ctrl+Shift+F，\n可能已被其他程式佔用。",
                "PrivacyShield", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    void OnHotkey(object sender, HotkeyEventArgs e)
    {
        if (e.Id == HOTKEY_TOGGLE)
            Toggle();
        else if (e.Id == HOTKEY_PANIC)
            PanicToggle();
    }

    ContextMenuStrip MakeMenu()
    {
        var m = new ContextMenuStrip();
        m.Items.Add("切換遮蔽  (Ctrl+Q)", null, (s, e) => Toggle());
        m.Items.Add("恐慌模式  (Ctrl+Shift+F)", null, (s, e) => PanicToggle());
        m.Items.Add(new ToolStripSeparator());

        var autoStartItem = new ToolStripMenuItem("開機自動啟動");
        autoStartItem.Checked = IsAutoStartEnabled();
        autoStartItem.Click += (s, e) =>
        {
            bool newState = !autoStartItem.Checked;
            SetAutoStart(newState);
            autoStartItem.Checked = newState;
        };
        m.Items.Add(autoStartItem);

        m.Items.Add(new ToolStripSeparator());
        m.Items.Add("結束程式", null, (s, e) => Quit());
        return m;
    }

    void Toggle()
    {
        if (shield.Visible)
        {
            shield.SaveLayout();
            shield.Hide();
        }
        else
        {
            shield.ExitPanicMode();
            shield.Show();
            shield.BringToFront();
            shield.Activate();
        }
    }

    void PanicToggle()
    {
        if (shield.Visible && shield.IsPanicMode)
        {
            // 退出恐慌模式 → 還原之前的佈局
            shield.SaveLayout();
            shield.Hide();
        }
        else
        {
            // 進入恐慌模式 → 先儲存當前佈局再全螢幕
            if (shield.Visible)
                shield.SaveLayout();
            shield.EnterPanicMode();
        }
    }

    // ── 開機自啟動 ──
    bool IsAutoStartEnabled()
    {
        try
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REG_RUN_KEY, false))
            {
                if (key == null) return false;
                var val = key.GetValue(REG_APP_NAME);
                return val != null;
            }
        }
        catch { return false; }
    }

    void SetAutoStart(bool enable)
    {
        try
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REG_RUN_KEY, true))
            {
                if (key == null) return;
                if (enable)
                    key.SetValue(REG_APP_NAME, "\"" + Application.ExecutablePath + "\"");
                else
                    key.DeleteValue(REG_APP_NAME, false);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("無法設定開機自動啟動：\n" + ex.Message,
                "PrivacyShield", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    void Quit()
    {
        if (shield.Visible) shield.SaveLayout();
        UnregisterHotKey(hkWnd.Handle, HOTKEY_TOGGLE);
        UnregisterHotKey(hkWnd.Handle, HOTKEY_PANIC);
        trayIcon.Visible = false;
        trayIcon.Dispose();
        hkWnd.DestroyHandle();
        shield.RealClose();
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UnregisterHotKey(hkWnd.Handle, HOTKEY_TOGGLE);
            UnregisterHotKey(hkWnd.Handle, HOTKEY_PANIC);
            trayIcon.Dispose();
            shield.Dispose();
        }
        base.Dispose(disposing);
    }

    // ── 隱藏 NativeWindow 接收快捷鍵 ──
    class HotkeyWnd : NativeWindow
    {
        const int WM_HOTKEY = 0x0312;
        public event EventHandler<HotkeyEventArgs> HotkeyPressed;
        public HotkeyWnd() { CreateHandle(new CreateParams()); }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && HotkeyPressed != null)
                HotkeyPressed(this, new HotkeyEventArgs((int)m.WParam));
            base.WndProc(ref m);
        }
    }
}

// ── 快捷鍵事件引數 ──
class HotkeyEventArgs : EventArgs
{
    public int Id { get; private set; }
    public HotkeyEventArgs(int id) { Id = id; }
}

// ════════════════════════════════════════════════════════════════
//  全黑遮蔽視窗
//  ▸ 置頂、無邊框
//  ▸ 拖曳移動、邊緣調整大小
//  ▸ 滑鼠滾輪調整透明度
//  ▸ 右上角關閉按鈕（僅隱藏）
//  ▸ 右鍵選單（透明度、佈局、恐慌模式）
//  ▸ 恐慌模式（全螢幕遮蔽）
//  ▸ 佈局記憶（儲存/載入位置大小透明度）
// ════════════════════════════════════════════════════════════════
class ShieldForm : Form
{
    [DllImport("user32.dll")] static extern int  SendMessage(IntPtr h, int m, int w, int l);
    [DllImport("user32.dll")] static extern bool ReleaseCapture();

    const int WM_NCLBUTTONDOWN = 0xA1;
    const int HT_CAPTION       = 0x2;
    const int GRIP              = 8;        // 邊緣感應寬度 (px)

    bool closeBtnHover;
    bool allowClose;

    // 恐慌模式狀態
    bool panicMode;
    Rectangle prePanicBounds;  // 進入恐慌模式前的位置大小

    // 提示文字淡出用
    Label hintLabel;
    System.Windows.Forms.Timer hintTimer;
    System.Windows.Forms.Timer fadeTimer;

    // 右鍵選單
    ContextMenuStrip shieldMenu;

    public bool IsPanicMode { get { return panicMode; } }

    public ShieldForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        BackColor       = Color.Black;
        Opacity         = 0.92;
        TopMost         = true;
        ShowInTaskbar   = false;
        StartPosition   = FormStartPosition.CenterScreen;
        Size            = new Size(800, 500);
        MinimumSize     = new Size(160, 100);
        DoubleBuffered  = true;

        // ── 提示標籤 ──
        hintLabel = new Label
        {
            Text      = "拖曳移動 ｜ 邊緣調整 ｜ Ctrl+Q 隱藏 ｜ Ctrl+Shift+F 全螢幕 ｜ 滾輪透明度 ｜ 右鍵選單",
            ForeColor = Color.FromArgb(140, 255, 255, 255),
            BackColor = Color.Transparent,
            Font      = new Font("Microsoft JhengHei UI", 9.5f, FontStyle.Regular),
            AutoSize  = true
        };
        Controls.Add(hintLabel);

        // 2.5 秒後開始淡出提示
        hintTimer = new System.Windows.Forms.Timer { Interval = 2500 };
        hintTimer.Tick += (s, e) =>
        {
            hintTimer.Stop();
            StartFadeHint();
        };

        VisibleChanged += (s, e) =>
        {
            if (Visible)
            {
                hintLabel.Visible   = true;
                hintLabel.ForeColor = Color.FromArgb(140, 255, 255, 255);
                CenterHint();
                hintTimer.Start();
            }
            else
            {
                hintTimer.Stop();
                if (fadeTimer != null) fadeTimer.Stop();
            }
        };

        // ── 右鍵選單 ──
        shieldMenu = BuildShieldMenu();

        MouseDown  += OnMouseDown;
        MouseMove  += OnMouseMove;
        MouseLeave += (s, e) => { closeBtnHover = false; Invalidate(); };
        MouseWheel += OnMouseWheel;
    }

    ContextMenuStrip BuildShieldMenu()
    {
        var m = new ContextMenuStrip();

        // 透明度子選單
        var opacityMenu = new ToolStripMenuItem("透明度");
        int[] levels = { 100, 90, 80, 70, 60, 50, 40, 30, 20, 10 };
        foreach (int lvl in levels)
        {
            int captured = lvl; // closure capture
            var item = new ToolStripMenuItem(lvl + "%");
            item.Click += (s, e) => { Opacity = captured / 100.0; Invalidate(); };
            opacityMenu.DropDownItems.Add(item);
        }
        m.Items.Add(opacityMenu);

        m.Items.Add(new ToolStripSeparator());

        // 快速佈局
        var layoutMenu = new ToolStripMenuItem("快速佈局");

        var screenBounds = Screen.FromControl(this).Bounds;

        layoutMenu.DropDownItems.Add("螢幕上半", null, (s, e) => {
            var sb = Screen.FromControl(this).Bounds;
            SetBounds(sb.X, sb.Y, sb.Width, sb.Height / 2);
        });
        layoutMenu.DropDownItems.Add("螢幕下半", null, (s, e) => {
            var sb = Screen.FromControl(this).Bounds;
            SetBounds(sb.X, sb.Y + sb.Height / 2, sb.Width, sb.Height / 2);
        });
        layoutMenu.DropDownItems.Add("螢幕左半", null, (s, e) => {
            var sb = Screen.FromControl(this).Bounds;
            SetBounds(sb.X, sb.Y, sb.Width / 2, sb.Height);
        });
        layoutMenu.DropDownItems.Add("螢幕右半", null, (s, e) => {
            var sb = Screen.FromControl(this).Bounds;
            SetBounds(sb.X + sb.Width / 2, sb.Y, sb.Width / 2, sb.Height);
        });
        layoutMenu.DropDownItems.Add(new ToolStripSeparator());
        layoutMenu.DropDownItems.Add("置中 (800×500)", null, (s, e) => {
            Size = new Size(800, 500);
            CenterToScreen();
        });
        m.Items.Add(layoutMenu);

        m.Items.Add(new ToolStripSeparator());

        // 恐慌模式
        m.Items.Add("恐慌模式 — 全螢幕  (Ctrl+Shift+F)", null, (s, e) => EnterPanicMode());

        m.Items.Add(new ToolStripSeparator());

        // 隱藏
        m.Items.Add("隱藏遮蔽  (Ctrl+Q)", null, (s, e) => { SaveLayout(); Hide(); });

        return m;
    }

    // ── 恐慌模式 ──
    public void EnterPanicMode()
    {
        if (!panicMode)
            prePanicBounds = Bounds;

        panicMode = true;

        // 覆蓋整個虛擬桌面（所有螢幕）
        Rectangle total = SystemInformation.VirtualScreen;
        SetBounds(total.X, total.Y, total.Width, total.Height);
        Opacity = 1.0;

        if (!Visible)
        {
            Show();
            BringToFront();
            Activate();
        }
        else
        {
            BringToFront();
        }

        Invalidate();
    }

    public void ExitPanicMode()
    {
        if (!panicMode) return;
        panicMode = false;
        SetBounds(prePanicBounds.X, prePanicBounds.Y,
                  prePanicBounds.Width, prePanicBounds.Height);
        Opacity = Settings.GetDouble("opacity", 0.92);
        Invalidate();
    }

    // ── 佈局記憶 ──
    public void SaveLayout()
    {
        // 不在恐慌模式時才儲存佈局
        if (!panicMode)
        {
            Settings.Set("x", Left);
            Settings.Set("y", Top);
            Settings.Set("w", Width);
            Settings.Set("h", Height);
            Settings.Set("opacity", Opacity);
        }
        Settings.Save();
    }

    public void LoadLayout()
    {
        int x = Settings.GetInt("x", -9999);
        int y = Settings.GetInt("y", -9999);
        int w = Settings.GetInt("w", 800);
        int h = Settings.GetInt("h", 500);
        double op = Settings.GetDouble("opacity", 0.92);

        if (x != -9999 && y != -9999)
        {
            StartPosition = FormStartPosition.Manual;
            SetBounds(x, y, w, h);
        }
        else
        {
            Size = new Size(w, h);
        }
        Opacity = Math.Max(0.10, Math.Min(1.0, op));
    }

    // ── 提示動畫 ──
    void StartFadeHint()
    {
        int alpha = 140;
        fadeTimer = new System.Windows.Forms.Timer { Interval = 30 };
        fadeTimer.Tick += (s, e) =>
        {
            alpha -= 5;
            if (alpha <= 0)
            {
                hintLabel.Visible = false;
                fadeTimer.Stop();
                fadeTimer.Dispose();
                fadeTimer = null;
                return;
            }
            hintLabel.ForeColor = Color.FromArgb(alpha, 255, 255, 255);
        };
        fadeTimer.Start();
    }

    void CenterHint()
    {
        if (hintLabel == null) return;
        hintLabel.Location = new Point(
            Math.Max(0, (ClientSize.Width  - hintLabel.Width)  / 2),
            Math.Max(0, (ClientSize.Height - hintLabel.Height) / 2));
    }

    // ── 繪製 ──
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int w = ClientSize.Width, h = ClientSize.Height;

        // 微弱邊框
        using (var pen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f))
            g.DrawRectangle(pen, 0, 0, w - 1, h - 1);

        // 四角圓點提示可調大小
        if (!panicMode)
        {
            int dotR = 3;
            using (var brush = new SolidBrush(Color.FromArgb(50, 255, 255, 255)))
            {
                g.FillEllipse(brush, 2, 2, dotR * 2, dotR * 2);
                g.FillEllipse(brush, w - dotR * 2 - 3, 2, dotR * 2, dotR * 2);
                g.FillEllipse(brush, 2, h - dotR * 2 - 3, dotR * 2, dotR * 2);
                g.FillEllipse(brush, w - dotR * 2 - 3, h - dotR * 2 - 3, dotR * 2, dotR * 2);
            }
        }

        // 關閉按鈕 ✕
        var btnRect = CloseBtnRect();
        var xClr = closeBtnHover
            ? Color.FromArgb(230, 255, 70, 70)
            : Color.FromArgb(80, 255, 255, 255);

        if (closeBtnHover)
        {
            using (var bg = new SolidBrush(Color.FromArgb(40, 255, 80, 80)))
                g.FillRectangle(bg, btnRect);
        }

        using (var pen = new Pen(xClr, 2f))
        {
            int m = 10;
            g.DrawLine(pen, btnRect.X + m, btnRect.Y + m,
                            btnRect.Right - m, btnRect.Bottom - m);
            g.DrawLine(pen, btnRect.Right - m, btnRect.Y + m,
                            btnRect.X + m, btnRect.Bottom - m);
        }

        // 右下角狀態指示
        string status = ((int)(Opacity * 100)).ToString() + "%";
        if (panicMode)
            status = "🔒 PANIC ｜ " + status;

        using (var f = new Font("Consolas", 8f))
        using (var b = new SolidBrush(Color.FromArgb(60, 255, 255, 255)))
        {
            var sz = g.MeasureString(status, f);
            g.DrawString(status, f, b, w - sz.Width - 8, h - sz.Height - 6);
        }
    }

    Rectangle CloseBtnRect()
    {
        return new Rectangle(ClientSize.Width - 40, 0, 40, 36);
    }

    // ── 滑鼠事件 ──
    void OnMouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            shieldMenu.Show(this, e.Location);
            return;
        }

        if (e.Button != MouseButtons.Left) return;

        // 關閉按鈕
        if (CloseBtnRect().Contains(e.Location)) { SaveLayout(); Hide(); return; }

        // 恐慌模式下不允許拖曳和縮放
        if (panicMode) return;

        // 邊緣調整大小
        int dir = ResizeDir(e.Location);
        if (dir != 0)
        {
            ReleaseCapture();
            SendMessage(Handle, 0x0112 /* WM_SYSCOMMAND */, dir, 0);
            return;
        }

        // 拖曳
        ReleaseCapture();
        SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
    }

    void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (panicMode)
        {
            Cursor = CloseBtnRect().Contains(e.Location) ? Cursors.Hand : Cursors.Default;
        }
        else
        {
            int dir = ResizeDir(e.Location);
            switch (dir)
            {
                case 0xF001: case 0xF002: Cursor = Cursors.SizeWE;   break;
                case 0xF003: case 0xF006: Cursor = Cursors.SizeNS;   break;
                case 0xF004: case 0xF008: Cursor = Cursors.SizeNWSE; break;
                case 0xF005: case 0xF007: Cursor = Cursors.SizeNESW; break;
                default:
                    Cursor = CloseBtnRect().Contains(e.Location)
                        ? Cursors.Hand : Cursors.SizeAll;
                    break;
            }
        }

        bool hover = CloseBtnRect().Contains(e.Location);
        if (hover != closeBtnHover)
        {
            closeBtnHover = hover;
            Invalidate(CloseBtnRect());
        }
    }

    void OnMouseWheel(object sender, MouseEventArgs e)
    {
        double d = e.Delta > 0 ? 0.05 : -0.05;
        Opacity = Math.Max(0.10, Math.Min(1.0, Opacity + d));
        Invalidate();
    }

    int ResizeDir(Point p)
    {
        bool l = p.X < GRIP;
        bool r = p.X > ClientSize.Width  - GRIP;
        bool t = p.Y < GRIP;
        bool b = p.Y > ClientSize.Height - GRIP;

        if (t && l) return 0xF004;
        if (t && r) return 0xF005;
        if (b && l) return 0xF007;
        if (b && r) return 0xF008;
        if (l)      return 0xF001;
        if (r)      return 0xF002;
        if (t)      return 0xF003;
        if (b)      return 0xF006;
        return 0;
    }

    // 攔截 ALT+F4 → 僅隱藏
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!allowClose && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            SaveLayout();
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }

    /// <summary>真正關閉視窗（結束程式時呼叫）</summary>
    public void RealClose()
    {
        allowClose = true;
        Close();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        CenterHint();
        Invalidate();
    }
}
