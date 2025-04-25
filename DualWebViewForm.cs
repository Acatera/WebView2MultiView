using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace WebView2MultiView;

public partial class DualWebViewForm : Form
{
    private WebView2 webViewLeft;
    private WebView2 webViewRight;
    private TextBox urlBoxLeft;
    private TextBox urlBoxRight;
    private Button goButtonLeft;
    private Button goButtonRight;
    private SplitContainer splitContainer;

    private Point puckDragStart;
    private bool isDraggingPuck = false;

    public DualWebViewForm()
    {
        InitializeUI();
        Resize += (s, e) =>
        {
            if (splitContainer != null)
            {
                splitContainer.SplitterDistance = this.ClientSize.Width / 2;
            }
        };
    }

    private async void InitializeUI()
    {
        // === Split container ===
        splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = this.ClientSize.Width / 2
        };

        // === Left panel ===
        var leftPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var navPanelLeft = new Panel { Dock = DockStyle.Fill, Height = 30 };
        urlBoxLeft = new TextBox { Dock = DockStyle.Fill, Text = "wikipedia.com" };
        goButtonLeft = new Button { Text = "Go", Dock = DockStyle.Right, Width = 50 };
        goButtonLeft.Click += (s, e) => Navigate(webViewLeft, urlBoxLeft.Text);
        urlBoxLeft.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                Navigate(webViewLeft, urlBoxLeft.Text);
                e.SuppressKeyPress = true;
            }
        };
        navPanelLeft.Controls.Add(urlBoxLeft);
        navPanelLeft.Controls.Add(goButtonLeft);

        var webViewLeftContainer = new Panel
        {
            Dock = DockStyle.Fill
        };

        webViewLeft = new WebView2 { Dock = DockStyle.Fill };
        webViewLeftContainer.Controls.Add(webViewLeft);

        // === Scroll Puck ===
        var scrollPuck = new PictureBox
        {
            Width = 100,
            Height = 100,
            BackColor = Color.FromArgb(100, Color.FromArgb(0, 123, 255)),
            Cursor = Cursors.SizeAll,
            TabStop = true
        };

        // Circular appearance
        var circlePath = new GraphicsPath();
        circlePath.AddEllipse(1, 1, scrollPuck.Width - 2, scrollPuck.Height - 2);
        scrollPuck.Region = new Region(circlePath);
        // Border - dark blue
        var borderPen = new Pen(Color.FromArgb(0, 86, 179), 2);
        var borderPath = new GraphicsPath();
        borderPath.AddEllipse(1, 1, scrollPuck.Width - 2, scrollPuck.Height - 2);
        scrollPuck.Paint += (s, e) =>
        {
            e.Graphics.DrawPath(borderPen, borderPath);
        };

        // Enable wheel scrolling
        scrollPuck.MouseWheel += async (s, e) =>
        {
            int delta = -e.Delta * 2;
            string script = $@"
                window.scrollBy({{
                    top: {delta},
                    left: 0,
                    behavior: 'smooth'
                }});
            ";
            await webViewLeft.ExecuteScriptAsync(script);
            await webViewRight.ExecuteScriptAsync(script);
        };

        scrollPuck.MouseEnter += (s, e) => scrollPuck.Focus();

        // === Drag logic ===
        scrollPuck.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                puckDragStart = e.Location;
                isDraggingPuck = true;
            }
        };

        scrollPuck.MouseMove += (s, e) =>
        {
            if (isDraggingPuck && e.Button == MouseButtons.Left)
            {
                var newLeft = scrollPuck.Left + e.X - puckDragStart.X;
                var newTop = scrollPuck.Top + e.Y - puckDragStart.Y;

                // Clamp to parent bounds
                newLeft = Math.Max(0, Math.Min(scrollPuck.Parent.Width - scrollPuck.Width, newLeft));
                newTop = Math.Max(0, Math.Min(scrollPuck.Parent.Height - scrollPuck.Height, newTop));

                scrollPuck.Left = newLeft;
                scrollPuck.Top = newTop;
            }
        };

        scrollPuck.MouseUp += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                isDraggingPuck = false;
            }
        };

        scrollPuck.Parent = webViewLeftContainer;
        scrollPuck.Location = new Point(20, 20);
        scrollPuck.BringToFront();

        leftPanel.Controls.Add(navPanelLeft, 0, 0);
        leftPanel.Controls.Add(webViewLeftContainer, 0, 1);

        // === Right panel ===
        var rightPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var navPanelRight = new Panel { Dock = DockStyle.Fill, Height = 30 };
        urlBoxRight = new TextBox { Dock = DockStyle.Fill, Text = "wikipedia.com" };
        goButtonRight = new Button { Text = "Go", Dock = DockStyle.Right, Width = 50 };
        goButtonRight.Click += (s, e) => Navigate(webViewRight, urlBoxRight.Text);
        urlBoxRight.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                Navigate(webViewRight, urlBoxRight.Text);
                e.SuppressKeyPress = true;
            }
        };
        navPanelRight.Controls.Add(urlBoxRight);
        navPanelRight.Controls.Add(goButtonRight);

        webViewRight = new WebView2 { Dock = DockStyle.Fill };
        rightPanel.Controls.Add(navPanelRight, 0, 0);
        rightPanel.Controls.Add(webViewRight, 0, 1);

        // === Assemble layout ===
        splitContainer.Panel1.Controls.Add(leftPanel);
        splitContainer.Panel2.Controls.Add(rightPanel);
        Controls.Add(splitContainer);

        // === WebView2 Initialization ===
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyAppWebView2"
        );
        var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

        await webViewLeft.EnsureCoreWebView2Async(env);
        await webViewRight.EnsureCoreWebView2Async(env);

        Navigate(webViewLeft, urlBoxLeft.Text);
        Navigate(webViewRight, urlBoxRight.Text);
    }

    private void Navigate(WebView2 webView, string url)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }
            webView.CoreWebView2.Navigate(url);
        }
    }
}
