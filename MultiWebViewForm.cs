using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace WebView2MultiView;

public partial class MultiWebViewForm : Form
{
    private class Account
    {
        public string Url { get; set; } = string.Empty;
        public bool IsActive { get; set; } = false;
        public bool WasVisited { get; set; } = false;
    }

    private class WebViewElement
    {
        public WebView2 WebView { get; set; } = new();
        public Button NavigateButton { get; set; } = new();
        public List<string> Hashes { get; set; } = [];
    }

    private class Cluster
    {
        public string Name { get; set; } = string.Empty;
        public List<Account> Accounts { get; set; } = [];
    }

    private readonly List<WebViewElement> webViewElements = [];
    private readonly List<Account> accounts = [];
    private readonly List<Cluster> clusters = [];

    private TableLayoutPanel mainLayout = new();
    private TableLayoutPanel gridPanel = new();

    public MultiWebViewForm()
    {
        InitializeComponent();
        InitializeComponents();
    }

    private async void InitializeComponents()
    {
        Text = "WebView2 MultiView";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        Width = 1200;
        Height = 600;

        Controls.Clear();
        mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // Button panel
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // WebViews
        Controls.Add(mainLayout);

        AddButtonPanel();

        var rows = 2;
        var cols = 4;
        await PrepareUi(rows, cols);

        await LoadData("source.json");

        // Ctrl + Shift + L to show input form
        KeyPreview = true;
        KeyDown += (s, e) =>
        {
            if (e.Control && e.Shift && e.KeyCode == Keys.L)
            {
                var inputForm = new InputForm();
                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    var clusterIndex = inputForm.ClusterIndex;
                    SelectClusterIndex(clusterIndex);
                }
            }
        };

        // SelectClusterIndex(0); // Default cluster
    }

    private void AddButtonPanel()
    {
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        var selectClusterButton = new Button
        {
            Text = "Select Cluster",
            Size = new Size(100, 30),
        };
        selectClusterButton.Click += (s, e) =>
        {
            var inputForm = new InputForm();
            if (inputForm.ShowDialog() == DialogResult.OK)
            {
                var clusterIndex = inputForm.ClusterIndex;
                SelectClusterIndex(clusterIndex);
            }
        };
        buttonPanel.Controls.Add(selectClusterButton);

        var refreshAllButton = new Button
        {
            Text = "Refresh All",
            Size = new Size(100, 30),
        };
        refreshAllButton.Click += (s, e) =>
        {
            foreach (var element in webViewElements)
            {
                element.WebView.CoreWebView2?.Reload();
            }
        };
        buttonPanel.Controls.Add(refreshAllButton);

        var scrollButton = new Button
        {
            Text = "Scroll All",
            Size = new Size(100, 30),
        };
        scrollButton.Click += (s, e) =>
        {
            foreach (var element in webViewElements)
            {
                element.WebView.CoreWebView2?.ExecuteScriptAsync(@"
                    (function() {
                        const target = document.querySelector('div[data-pagelet=""ProfileTilesFeed_1""]');
                        if (target) {
                            target.scrollIntoView({ behavior: 'smooth', block: 'center' });
                        }
                    })();
                ");
            }
        };
        buttonPanel.Controls.Add(scrollButton);

        mainLayout.Controls.Add(buttonPanel, 0, 0);
    }

    private async Task PrepareUi(int rows, int cols)
    {
        webViewElements.Clear();
        gridPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = cols,
            RowCount = rows,
            Margin = new Padding(5),
        };

        for (int i = 0; i < cols; i++)
            gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / cols));
        for (int i = 0; i < rows; i++)
            gridPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));

        mainLayout.Controls.Add(gridPanel, 0, 1);

        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyAppWebView2"
        );
        var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                var panel = new Panel
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(2),
                    BorderStyle = BorderStyle.FixedSingle
                };

                var webView = new WebView2
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(2),
                    ZoomFactor = 0.75f,
                };

                webView.NavigationCompleted += async (sender, args) =>
                {
                    if (args.IsSuccess)
                    {
                        await webView.CoreWebView2.ExecuteScriptAsync(@"
                            (function() {
                                const target = document.querySelector('div[data-pagelet=""ProfileTilesFeed_1""]');
                                if (target) {
                                    target.scrollIntoView({ behavior: 'smooth', block: 'center' });
                                }
                            })();
                        ");

                        string js = @"
                            (() => {
                                const images = [];
                                const imgElements = document.querySelectorAll('div > div > a > div > img');
                                imgElements.forEach(img => {
                                    images.push(img.src);
                                });
                                return images;
                            })()
                            ";

                        var result = await webView.ExecuteScriptAsync(js);

                        // Remember to deserialize JSON output
                        var rawList = JsonSerializer.Deserialize<List<string>>(result);
                        if (rawList == null) return;
                        var hashes = rawList
                            .Select(url => new Uri(url).AbsolutePath);
                    }
                };

                var button = new Button
                {
                    Text = "Navigate",
                    Dock = DockStyle.Bottom,
                    Size = new Size(100, 30),
                };
                button.Click += (s, e) =>
                {
                    var account = GetRandomAccount();
                    if (account != null)
                    {
                        webView.CoreWebView2?.Navigate(account.Url);
                        if (account.IsActive)
                        {
                            button.BackColor = Color.LightGreen;
                        }
                        else
                        {
                            button.BackColor = Color.LightCoral;
                        }
                    }
                    else
                    {
                        webView.CoreWebView2?.Navigate("about:blank");
                        button.BackColor = SystemColors.Control;
                    }
                };

                panel.Controls.Add(webView);
                panel.Controls.Add(button);
                gridPanel.Controls.Add(panel, col, row);
                webViewElements.Add(new WebViewElement { WebView = webView, NavigateButton = button });

                await webView.EnsureCoreWebView2Async(env);
                webView.CoreWebView2.Settings.IsScriptEnabled = true;
                webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
            }
        }
    }

    private async Task LoadData(string dataFile)
    {
        var contents = await File.ReadAllTextAsync(dataFile);

        var json = JsonSerializer.Deserialize<Dictionary<string, object>>(contents);

        if (json != null && json.TryGetValue("clusters", out var clustersObj) && clustersObj is JsonElement clustersElement && clustersElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var cluster in clustersElement.EnumerateArray())
            {
                var clusterName = cluster.GetProperty("name").GetString();
                if (string.IsNullOrEmpty(clusterName))
                {
                    continue;
                }
                if (cluster.ValueKind == JsonValueKind.Object)
                {
                    clusters.Add(new Cluster
                    {
                        Name = clusterName,
                        Accounts = ExtractAccountsFromCluster(cluster)
                    });
                }
            }
        }
    }

    private static List<Account> ExtractAccountsFromCluster(JsonElement cluster)
    {
        var accounts = new List<Account>();
        if (cluster.TryGetProperty("accounts", out var accountsElement) && accountsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var account in accountsElement.EnumerateObject())
            {
                if (!account.Value.TryGetProperty("profile_url", out var urlElement))
                {
                    continue;
                }
                var accountUrl = urlElement.GetString();
                if (string.IsNullOrEmpty(accountUrl))
                {
                    continue;
                }

                if (!account.Value.TryGetProperty("active", out var activeElement))
                {
                    continue;
                }
                var isActive = activeElement.GetBoolean();

                var accountObj = new Account
                {
                    Url = accountUrl,
                    IsActive = isActive,
                    WasVisited = false
                };
                accounts.Add(accountObj);
            }
        }
        return accounts;
    }

    private Account? GetRandomAccount()
    {
        var unvisitedAccounts = accounts.Where(a => !a.WasVisited).ToList();
        if (unvisitedAccounts.Count == 0) return null;

        var random = new Random();
        var selected = unvisitedAccounts[random.Next(unvisitedAccounts.Count)];
        selected.WasVisited = true;
        return selected;
    }

    private void NavigateAll()
    {
        foreach (var element in webViewElements)
        {
            var account = GetRandomAccount();

            if (account != null)
            {
                element.WebView.CoreWebView2?.Navigate(account.Url);
                if (account.IsActive)
                {
                    element.NavigateButton.BackColor = Color.LightGreen;
                }
                else
                {
                    element.NavigateButton.BackColor = Color.LightCoral;
                }
            }
            else
            {
                element.WebView.CoreWebView2?.Navigate("about:blank");
                element.NavigateButton.BackColor = SystemColors.Control;
            }
        }
    }

    private void SelectClusterIndex(int clusterIndex)
    {
        if (clusterIndex < 0 || clusterIndex >= clusters.Count) return;

        accounts.Clear();
        foreach (var account in clusters[clusterIndex].Accounts)
        {
            accounts.Add(new Account
            {
                Url = account.Url,
                IsActive = account.IsActive,
                WasVisited = false
            });
        }

        Text = $"WebView2 MultiView - {clusters[clusterIndex].Name} ({clusterIndex})";

        NavigateAll();
    }
}
