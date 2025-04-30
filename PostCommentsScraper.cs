using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace WebView2MultiView;

public partial class PostCommentScraper : Form
{
    private readonly List<string> _urlsToScrape;
    private WebView2 _webView;
    private Button _nextPageButton;
    private Button _scrapeCommentsButton;
    private Label _statusLabel;
    private int _currentPage = 0;

    public PostCommentScraper(string[] urls)
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

        _scrapeCommentsButton = new Button
        {
            Text = "📝 Scrape Comments",
            Width = 150,
            Left = 170,
            Top = 10
        };
        _scrapeCommentsButton.Click += async (_, _) => await ScrapeCommentsAsync();
        panel.Controls.Add(_scrapeCommentsButton);

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

        await InvokeAsync(() =>
        {
            _statusLabel.Text = $"Loading page {_currentPage} of {_urlsToScrape.Count}...";
            _webView.Source = new Uri(url);
        });

        // Wait briefly to allow page rendering to start
        await Task.Delay(2000);

        await InvokeAsync(() =>
        {
            _statusLabel.Text = "Page loaded.";
            // Move focus to Scrape Comments button
            _scrapeCommentsButton.Focus();
            // Perform click action on the button
            _scrapeCommentsButton.PerformClick();
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
        // Check if file exists
        var hash = GetHashedUrl();
        var fileName = $"comments/comments_{hash}.json";
        var mustScrapeComments = true;
        if (File.Exists(fileName))
        {
            mustScrapeComments = false;
            // await InvokeAsync(() =>
            // {
            //     // Update status label
            //     _statusLabel.Text = $"✅ Comments already scraped and saved to {fileName}";
            //     // Move focus to Load Next Page button
            //     _nextPageButton.Focus();
            //     // Perform click action on the button
            //     _nextPageButton.PerformClick();
            // });
            // return;
        }
        else
        {
            // Wait 5 seconds before scraping
            await Task.Delay(2000);
        }

        if (_webView.CoreWebView2 == null)
        {
            await InvokeAsync(() =>
            {
                MessageBox.Show("⚠️ WebView not ready yet.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            });
            return;
        }

        await InvokeAsync(() =>
        {
            _statusLabel.Text = "Scrolling to load comments...";
            _scrapeCommentsButton.Enabled = false;
        });

        try
        {
            var tcs = new TaskCompletionSource();

            void Handler(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
            {
                if (args.TryGetWebMessageAsString() == "scrolling_done")
                {
                    _webView.CoreWebView2.WebMessageReceived -= Handler;
                    tcs.SetResult();
                }
            }

            _webView.CoreWebView2.WebMessageReceived += Handler;
            await Task.Delay(2000);

            var postMessage = await _webView.ExecuteScriptAsync(@"
                document.querySelectorAll('div[role=""dialog""] div[data-ad-rendering-role=""story_message""] > div[data-ad-preview=""message""]').forEach(div => {
                    return div.innerText;
                });
            ");

            if (mustScrapeComments)
            {
                // Inject the scrolling script
                await _webView.ExecuteScriptAsync(@"
                    (function () {
                        let containerSelector = '[role=""dialog""] > div > div > div > div > div > div > div';

                            function delay(ms) {
                            return new Promise(resolve => setTimeout(resolve, ms));
                        }

                        function waitForContainer(callback)
                        {
                            const interval = setInterval(() =>
                            {
                                let el = document.querySelector(containerSelector)?.children[1];
                                if (!el)
                                {
                                    containerSelector += ' > div';
                                }
                                if (el)
                                {
                                    clearInterval(interval);
                                    callback(el);
                                }
                            }, 500);
                        }

                        waitForContainer(async function(container) {
                            console.log(""📦 Comment container found: "", container);

                            let lastHeight = 0;
                            let stableCount = 0;
                            const maxStableChecks = 5;

                            const scrollInterval = setInterval(async () =>
                            {
                            container.scrollTop += 1000;

                            const newHeight = container.scrollHeight;

                            if (newHeight === lastHeight)
                            {
                                stableCount++;
                                console.log(`⏳ No new comments... (${ stableCount } /${ maxStableChecks})`);
                        } else
                        {
                            stableCount = 0;
                            lastHeight = newHeight;
                            console.log(""⬇️ Scrolling more..."");
                        }

                        if (stableCount >= maxStableChecks)
                        {
                            console.log(""✅ Reached the bottom — all comments loaded."");
                            clearInterval(scrollInterval);
                            window.chrome.webview.postMessage('scrolling_done');
                        }
                    }, 1000);
                });
                    })();
                ");

                // Wait for scrolling to complete
                await tcs.Task;

                // Now scrolling is done, start scraping
                await InvokeAsync(() =>
                {
                    _statusLabel.Text = "Scraping comments...";
                });

                var jsResult = await _webView.ExecuteScriptAsync(JSGetCommentsScript()).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(jsResult))
                {
                    var cleanJson = JsonSerializer.Deserialize<string>(jsResult);
                    var comments = JsonSerializer.Deserialize<List<Comment>>(cleanJson);

                    if (comments != null)
                    {
                        await InvokeAsync(() =>
                        {
                            _statusLabel.Text = $"✅ Scraped {comments.Count} comments. Current page: {_currentPage} of {_urlsToScrape.Count}";

                            // Save to file
                            var url = _webView.Source.ToString();
                            var hash = GetHashedUrl();
                            var fileName = $"comments_{hash}.json";

                            var json = JsonSerializer.Serialize(new { url, comments }, new JsonSerializerOptions { WriteIndented = true });
                            File.WriteAllText(fileName, json);

                            // Move focus to Load Next Page button
                            _nextPageButton.Focus();
                            // Perform click action on the button
                            _nextPageButton.PerformClick();

                            // MessageBox.Show($"✅ Saved {comments.Count} comments to {fileName}", "Save Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        });
                    }
                    else
                    {
                        await InvokeAsync(() => _statusLabel.Text = "No comments found.");
                    }
                }
            }
            else
            {
                // Load comments from file and append the post message

                var json = File.ReadAllText(fileName);
                var root = JsonSerializer.Deserialize<JsonObject>(json);
                // Append the post message to root object
                root!["message"] = postMessage;
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
            await InvokeAsync(() => _scrapeCommentsButton.Enabled = true);
        }
    }


    private static string JSGetCommentsScript() => @"
(() => {
    const results = [];

    document.querySelectorAll('[aria-label^=""Comment by""]').forEach(comment => {
        const ariaLabel = comment.getAttribute(""aria-label"");
        let name = '';
        if (ariaLabel) {
            let temp = ariaLabel.replace(/^Comment by /, '');
            const lastSpaceIndex = temp.lastIndexOf(' ');
            if (lastSpaceIndex > 0) {
                name = temp.substring(0, lastSpaceIndex).trim();
            }
        }

        const allSpans = comment.querySelectorAll('[dir=""auto""]');
        let text = '';

        for (const span of allSpans) {
            const parentTag = span.parentElement ? span.parentElement.tagName : null;
            const isInsideLink = span.closest('a');
            const looksLikeText = span.innerText && span.innerText.length > 0;

            if (!isInsideLink && looksLikeText && parentTag !== 'A') {
                text = span.innerText.trim();
                break;
            }
        }

        if (name && text) {
            results.push({ name, text });
        }
    });

    return JSON.stringify(results);
})();
";

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

static class ControlExtensions
{
    public static Task InvokeAsync(this Control control, Action action)
    {
        if (control.InvokeRequired)
        {
            var tcs = new TaskCompletionSource<object?>();

            control.BeginInvoke(new MethodInvoker(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));

            return tcs.Task;
        }
        else
        {
            action();
            return Task.CompletedTask;
        }
    }
}
