using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
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
        // === Top scroll button panel ===
        var scrollControlPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 35,
            Padding = new Padding(5),
            AutoSize = true
        };

        void AddScrollButton(string text, int delta)
        {
            var btn = new Button
            {
                Text = text,
                Width = 120,
                Height = 30,
                Margin = new Padding(5)
            };
            btn.Click += async (s, e) =>
            {
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
            scrollControlPanel.Controls.Add(btn);
        }

        AddScrollButton("Scroll by -100", -100);
        AddScrollButton("Scroll by -50", -50);


        // === Mouse scroll panel ===
        var scrollPanel = new Panel
        {
            Width = 100,
            Height = 30,
            BackColor = Color.LightGray,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(10)
        };
        scrollPanel.MouseWheel += async (s, e) =>
        {
            int delta = -e.Delta / 3; // Invert and scale (e.Delta is usually in steps of 120)
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

        // Important: enable the panel to receive focus and mouse wheel input
        scrollPanel.TabStop = true;
        scrollPanel.MouseEnter += (s, e) => scrollPanel.Focus();

        // === Add to top panel ===
        scrollControlPanel.Controls.Add(new Label
        {
            Text = "Scroll Pad",
            AutoSize = true,
            Padding = new Padding(10, 15, 0, 0)
        });
        scrollControlPanel.Controls.Add(scrollPanel);

        AddScrollButton("Scroll by 50", 50);
        AddScrollButton("Scroll by 100", 100);

        // === Split container with two panels ===
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
        urlBoxLeft.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                Navigate(webViewLeft, urlBoxLeft.Text);
                e.SuppressKeyPress = true;
            }
        };
        goButtonLeft = new Button { Text = "Go", Dock = DockStyle.Right, Width = 50 };
        goButtonLeft.Click += (s, e) => Navigate(webViewLeft, urlBoxLeft.Text);
        navPanelLeft.Controls.Add(urlBoxLeft);
        navPanelLeft.Controls.Add(goButtonLeft);

        webViewLeft = new WebView2 { Dock = DockStyle.Fill };
        leftPanel.Controls.Add(navPanelLeft, 0, 0);
        leftPanel.Controls.Add(webViewLeft, 0, 1);
        // leftPanel.Controls.Add(scrollPanel); // Add scroll panel to left panel

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
        urlBoxRight.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                Navigate(webViewRight, urlBoxRight.Text);
                e.SuppressKeyPress = true;
            }
        };
        goButtonRight = new Button { Text = "Go", Dock = DockStyle.Right, Width = 50 };
        goButtonRight.Click += (s, e) => Navigate(webViewRight, urlBoxRight.Text);
        navPanelRight.Controls.Add(urlBoxRight);
        navPanelRight.Controls.Add(goButtonRight);

        webViewRight = new WebView2 { Dock = DockStyle.Fill };
        rightPanel.Controls.Add(navPanelRight, 0, 0);
        rightPanel.Controls.Add(webViewRight, 0, 1);

        // === Assemble layout ===
        splitContainer.Panel1.Controls.Add(leftPanel);
        splitContainer.Panel2.Controls.Add(rightPanel);
        Controls.Add(splitContainer);
        Controls.Add(scrollControlPanel); // <-- Top panel added last so it appears at the top

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
