using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace WebView2MultiView;

public partial class PostMessageScraper : Form
{
    private readonly List<string> _urlsToScrape;
    private WebView2 _webView;
    private Button _nextPageButton;
    private Label _statusLabel;
    private int _currentPage = 0;

    public PostMessageScraper(string[] urls)
    {
        _urlsToScrape = [.. urls];
        WindowState = FormWindowState.Maximized;
        InitializeUI();
        _ = InitializeWebViewAsync(); // fire and forget
    }

    private async Task InitializeWebViewAsync()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyAppWebView2"
        );

        var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder).ConfigureAwait(false);

        await InvokeAsync(() =>
        {
            _webView = new WebView2
            {
                Dock = DockStyle.Fill,
                ZoomFactor = 0.75
            };
            Controls.Add(_webView);
        });

        await _webView.EnsureCoreWebView2Async(environment).ConfigureAwait(false);
    }

    private void InitializeUI()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 40
        };
        Controls.Add(panel);

        _nextPageButton = new Button
        {
            Text = "➡️ Load Next Page",
            Width = 150,
            Left = 10,
            Top = 10
        };
        _nextPageButton.Click += async (_, _) => await LoadNextPageAsync();
        panel.Controls.Add(_nextPageButton);

        _statusLabel = new Label
        {
            Text = "Status: Ready",
            AutoSize = true,
            Left = 500,
            Top = 20
        };
        panel.Controls.Add(_statusLabel);
    }

    private async Task LoadNextPageAsync()
    {
        if (_currentPage >= _urlsToScrape.Count)
        {
            await InvokeAsync(() =>
            {
                MessageBox.Show("✅ No more pages to load.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            return;
        }

        var url = _urlsToScrape[_currentPage++];

        await InvokeAsync(async () =>
        {
            _statusLabel.Text = $"Loading page {_currentPage} of {_urlsToScrape.Count}...";
            _webView.Source = new Uri(url);
            await Task.Delay(5000); // Wait for the page to load
            await ScrapeCommentsAsync();
        });
    }

    private string GetHashedUrl()
    {
        var url = _webView.Source.ToString();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)));
        return hash;
    }

    private async Task ScrapeCommentsAsync()
    {
        if (_webView.CoreWebView2 == null)
        {
            await InvokeAsync(() =>
            {
                MessageBox.Show("⚠️ WebView not ready yet.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            });
            return;
        }

        try
        {
            var tcs = new TaskCompletionSource();

            var postMessageJson = await _webView.ExecuteScriptAsync(@"
                Array.from(document.querySelectorAll('div[role=\'dialog\'] div[data-ad-rendering-role=\'story_message\'] > div[data-ad-preview=\'message\']'))
                    .map(div => div.innerText);
            ");

            // Deserialize it
            var postMessages = JsonSerializer.Deserialize<List<string>>(postMessageJson);

            // (Optional) pick the first message, or all
            var firstPostMessage = postMessages?.FirstOrDefault();


            // Load comments from file and append the post message
            var hash = GetHashedUrl();
            var fileName = $"comments/comments_{hash}.json";
            var json = File.ReadAllText(fileName);
            var root = JsonSerializer.Deserialize<JsonObject>(json);
            // Append the post message to root object
            root!["message"] = firstPostMessage;
            var updatedJson = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(fileName, updatedJson);

            await InvokeAsync(() =>
            {
                _statusLabel.Text = $"✅ Appended post message to {fileName}";
                // Move focus to Load Next Page button
                _nextPageButton.Focus();
                // Perform click action on the button
                _nextPageButton.PerformClick();
            });
        }
        catch (Exception ex)
        {
            await InvokeAsync(() =>
            {
                MessageBox.Show($"Failed to parse comments:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            });
        }
        finally
        {
        }
    }

    private static async Task InvokeAsync(Action action)
    {
        if (Application.OpenForms.Count > 0 && Application.OpenForms[0].InvokeRequired)
        {
            await Application.OpenForms[0].InvokeAsync(action);
        }
        else
        {
            action();
        }
    }

    public class Comment
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}


