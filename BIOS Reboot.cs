using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

static class BiosReboot
{
    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();

    [STAThread]
    static void Main()
    {
        // Without this, Windows renders at 96 DPI and stretches the bitmap, which looks blurry on scaled displays.
        // Must run before any window is created.
        SetProcessDPIAware();

        // shutdown /fw needs admin rights, so relaunch elevated (UAC prompt) if we don't have them.
        if (!IsAdmin())
        {
            try
            {
                ProcessStartInfo elevate = new ProcessStartInfo(Application.ExecutablePath);
                elevate.UseShellExecute = true;
                elevate.Verb = "runas";
                Process.Start(elevate);
            }
            catch (Win32Exception)
            {
                // UAC prompt was declined.
            }
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (AskDialog.Show("Reboot into BIOS/UEFI setup now?", "BIOS Reboot") != DialogResult.Yes)
            return;

        // Second confirmation, since this reboots immediately.
        if (AskDialog.Show("Are you sure? Unsaved work will be lost.", "BIOS Reboot") != DialogResult.Yes)
            return;

        // Send the reboot command silently. Exit on success; otherwise retry every second,
        // up to 30 attempts, then give up and exit.
        for (int attempt = 1; attempt <= 30; attempt++)
        {
            if (TryReboot())
                return;
            if (attempt < 30)
                Thread.Sleep(1000);
        }
    }

    static bool TryReboot()
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo("shutdown.exe", "/r /fw /t 0");
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            using (Process p = Process.Start(psi))
            {
                p.WaitForExit();
                return p.ExitCode == 0;
            }
        }
        catch
        {
            return false;
        }
    }

    static bool IsAdmin()
    {
        using (WindowsIdentity id = WindowsIdentity.GetCurrent())
            return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
    }
}

// Yes/No prompt like MessageBox, but a real window so the EXE icon shows in the title bar and taskbar.
class AskDialog : Form
{
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    // Windows 10 20H1+ and Windows 11 use 20; older Windows 10 builds used 19.
    const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

    static readonly Color DarkBack = Color.FromArgb(32, 32, 32);
    static readonly Color DarkButton = Color.FromArgb(51, 51, 51);
    static readonly Color DarkButtonHover = Color.FromArgb(69, 69, 69);
    static readonly Color DarkBorder = Color.FromArgb(96, 96, 96);

    // Ratio of the system DPI to 96, used to scale the hard-coded pixel sizes below.
    readonly float scale;

    // Follows Settings > Personalization > Colors > "Choose your app mode".
    readonly bool dark = IsDarkMode();

    static bool IsDarkMode()
    {
        try
        {
            object v = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme", 1);
            return v is int && (int)v == 0;
        }
        catch
        {
            return false;
        }
    }

    void StyleButton(Button b)
    {
        if (!dark)
            return;
        b.FlatStyle = FlatStyle.Flat;
        b.BackColor = DarkButton;
        b.ForeColor = Color.White;
        b.FlatAppearance.BorderColor = DarkBorder;
        b.FlatAppearance.MouseOverBackColor = DarkButtonHover;
        b.FlatAppearance.MouseDownBackColor = DarkBorder;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (!dark)
            return;
        // Dark title bar. Try the current attribute, then the pre-20H1 one.
        int on = 1;
        if (DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref on, sizeof(int)) != 0)
            DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref on, sizeof(int));
    }

    int S(int px)
    {
        return (int)Math.Round(px * scale);
    }

    AskDialog(string text, string caption)
    {
        using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            scale = g.DpiX / 96f;

        Text = caption;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(S(20));
        Font = SystemFonts.MessageBoxFont;
        Opacity = 0.99;

        // The label and layout panels inherit these colors from the form.
        if (dark)
        {
            BackColor = DarkBack;
            ForeColor = Color.White;
        }

        Label label = new Label();
        label.Text = text;
        label.AutoSize = true;
        label.MaximumSize = new Size(S(400), 0);
        label.Margin = new Padding(0);
        label.TextAlign = ContentAlignment.MiddleCenter;
        label.Anchor = AnchorStyles.None;

        // AutoSize so the buttons grow to fit their text at any font size or DPI.
        Button yes = new Button();
        yes.Text = "&Yes";
        yes.DialogResult = DialogResult.Yes;
        yes.AutoSize = true;
        yes.Padding = new Padding(S(12), S(2), S(12), S(2));

        Button no = new Button();
        no.Text = "&No";
        no.DialogResult = DialogResult.No;
        no.AutoSize = true;
        no.Padding = new Padding(S(12), S(2), S(12), S(2));

        FlowLayoutPanel buttons = new FlowLayoutPanel();
        buttons.FlowDirection = FlowDirection.LeftToRight;
        buttons.AutoSize = true;
        buttons.Anchor = AnchorStyles.None;
        buttons.Margin = new Padding(0, S(16), 0, 0);
        StyleButton(yes);
        StyleButton(no);
        buttons.Controls.Add(yes);
        buttons.Controls.Add(no);

        TableLayoutPanel layout = new TableLayoutPanel();
        layout.ColumnCount = 1;
        layout.AutoSize = true;
        layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        // Form Padding only affects docked controls, so place the layout inside it by hand.
        layout.Location = new Point(Padding.Left, Padding.Top);
        layout.Margin = new Padding(0);
        layout.Controls.Add(label, 0, 0);
        layout.Controls.Add(buttons, 0, 1);
        Controls.Add(layout);

        // Default to No (and give it focus) so a stray Enter doesn't reboot.
        AcceptButton = no;
        ActiveControl = no;
        CancelButton = no;
    }

    public static DialogResult Show(string text, string caption)
    {
        using (AskDialog dialog = new AskDialog(text, caption))
            return dialog.ShowDialog();
    }
}
