using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WebView2MultiView;

public class SearchForComment : Form
{
    private readonly List<string> _urlsToScrape;
    public List<SearchResult> SearchResults { get; } = [];
    private WebView2 _webView;
    private Button _nextPageButton;
    private Button _scrapeCommentsButton;
    private Label _statusLabel;
    private int _currentPage = 0;

    public SearchForComment(string[] urls)
    {
        _urlsToScrape = [.. urls];
        var screenSize = Screen.PrimaryScreen!.Bounds.Size;

        WindowState = FormWindowState.Normal;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(0, 0);
        Size = new Size(screenSize.Width / 2, screenSize.Height - 50);

        InitializeUI();
        _ = InitializeWebViewAsync();
    }

    private async Task InitializeWebViewAsync()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyAppWebView--"
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
        var panel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
        Controls.Add(panel);

        _nextPageButton = new Button { Text = "Load Next Page", Width = 150, Left = 10, Top = 10 };
        _nextPageButton.Click += async (_, _) => await LoadNextPageAsync();
        panel.Controls.Add(_nextPageButton);

        _scrapeCommentsButton = new Button { Text = "Scrape Comments", Width = 150, Left = 170, Top = 10 };
        _scrapeCommentsButton.Click += async (_, _) => await ScrapeCommentsAsync();
        panel.Controls.Add(_scrapeCommentsButton);

        _statusLabel = new Label { Text = "Status: Ready", AutoSize = true, Left = 500, Top = 20 };
        panel.Controls.Add(_statusLabel);
    }

    private async Task LoadNextPageAsync()
    {
        if (_currentPage >= _urlsToScrape.Count)
        {
            await InvokeAsync(() => MessageBox.Show("No more pages to load.", "Info"));
            return;
        }

        var url = _urlsToScrape[_currentPage++];
        var hash = GetHashedUrl(url);
        var fileName = $"comments/comments_{hash}.json";

        if (File.Exists(fileName))
        {
            await InvokeAsync(() => _nextPageButton.PerformClick());
            return;
        }

        await InvokeAsync(async () =>
        {
            _statusLabel.Text = $"Loading page {_currentPage}...";

            // Open DevTools (helps in some bypass scenarios)
            _webView.CoreWebView2.OpenDevToolsWindow();

            // Inject JS to spoof navigator.webdriver before any page loads
            await _webView.ExecuteScriptAsync(@"
                setTimeout(() => { 
                    window.location = 'https://www.facebook.com/www.hotnews.ro/posts/1096044209210790';
                }, 100); "
            );

            async void Handler(object? sender, CoreWebView2NavigationCompletedEventArgs args)
            {
                await ScrapeCommentsAsync();
                _webView.CoreWebView2.NavigationCompleted -= Handler;
            }
            _webView.CoreWebView2.NavigationCompleted += Handler;
        });
    }


    private string GetHashedUrl(string? url = null)
    {
        url ??= _webView.Source.ToString();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)));
        return hash;
    }

    private async Task ScrapeCommentsAsync()
    {
        var hash = GetHashedUrl();
        var fileName = $"comments/comments_{hash}.json";

        if (_webView.CoreWebView2 == null)
        {
            await InvokeAsync(() => MessageBox.Show("WebView not ready.", "Error"));
            return;
        }

        await InvokeAsync(() =>
        {
            _statusLabel.Text = "Scraping comments...";
            _scrapeCommentsButton.Enabled = false;
        });

        var tcs = new TaskCompletionSource<string>();
        void Handler(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            var message = args.TryGetWebMessageAsString();
            if (!string.IsNullOrEmpty(message))
            {
                // var json = JsonSerializer.Deserialize<string>(message);
                _webView.CoreWebView2.WebMessageReceived -= Handler;
                tcs.SetResult(message);
            }
        }
        _webView.CoreWebView2.WebMessageReceived += Handler;

        await _webView.ExecuteScriptAsync(JSExtractAllComments());

        var json = await tcs.Task;
        var searchResult = JsonSerializer.Deserialize<PageSearchResult>(json);

        if (searchResult != null && searchResult.Comments != null)
        {
            var result = JsonSerializer.Serialize(new
            {
                url = _webView.Source.ToString(),
                searchResult.Comments
            }, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            // Append timestamp to the file name
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileNameWithTimestamp = Path.Combine("comments", $"comments_{hash}_{timestamp}.json");

            File.WriteAllText(fileNameWithTimestamp, result);

            var searchResultObj = new SearchResult
            {
                Url = _webView.Source.ToString(),
                FoundAntiDezInfoHashTag = searchResult.FoundAntiDezInfoHashTag,
            };
            if (searchResult.FoundAntiDezInfoHashTag)
            {
                var antiDezInfoComment = searchResult.Comments
                    .FirstOrDefault(c => c.Contains("#antidezinfo-"));
                if (antiDezInfoComment != null)
                {
                    searchResultObj.Comment = antiDezInfoComment;
                }
            }
            SearchResults.Add(searchResultObj);
            await InvokeAsync(() =>
            {
                _statusLabel.Text = $"Scraped {searchResult.Comments.Length} comments.";
                _nextPageButton.PerformClick();
            });
        }

        await InvokeAsync(() => _scrapeCommentsButton.Enabled = true);
    }


    private static string JSExtractAllComments() => @"
        (() => {
            let comments = [];
            let emptyChecks = 0;
            const MAX_EMPTY_CHECKS = 10;
            let foundAntiDezInfoHashTag = false;
            const ANTI_DEZ_INFO_HASH_TAG = '#antidezinfo-\d+';

            function consumeComment(comment) {
                const text = comment.innerText.trim();
                comments.push(text);

                // Check if the comment contains the anti-dezinfo hashtag regex(#antidezinfo-\d+)
                if (text.match(ANTI_DEZ_INFO_HASH_TAG)) {
                    foundAntiDezInfoHashTag = true;
                    console.log('Found anti-dezinfo hashtag:', text);
                }

                const parent = comment.closest('div:not([class])');
                if (parent) parent.remove();
            }

            function getNextComment() {
                return document.querySelector('div[role=""dialog""] div[aria-label^=""Comment""][role=""article""]');
            }

            function processNext()
                {
                    if (foundAntiDezInfoHashTag)
                    {
                        console.log('Found anti-dezinfo hashtag, stopping processing.');
                        window.chrome.webview.postMessage(JSON.stringify({
                            foundAntiDezInfoHashTag,
                        comments
                        }));
                        return;
                    }

                    if (comments.length >= 50)
                    {
                        console.log('Reached 50 comments, stopping processing.');
                        window.chrome.webview.postMessage(JSON.stringify({
                            foundAntiDezInfoHashTag,
                            comments
                        }));
                        return;
                    }

                    const comment = getNextComment();
                    if (!comment)
                    {
                        if (emptyChecks++ < MAX_EMPTY_CHECKS)
                        {
                            setTimeout(processNext, 500);
                        }
                        else
                        {
                            console.log('Finished processing comments.');
                            window.chrome.webview.postMessage(JSON.stringify({
                                foundAntiDezInfoHashTag,
                            comments
                            }));
                        }
                        return;
                    }

                    emptyChecks = 0;

                    const seeMore = Array.from(comment.querySelectorAll('div[role=""button""]'))
                        .find(button =>
                        {
                            const text = button.textContent.trim();
                            return text === 'See more' || text === 'Afișează mai mult';
                        });

                    if (!seeMore)
                    {
                        consumeComment(comment);
                        setTimeout(processNext, 100);
                        return;
                    }

                    const key = Object.keys(seeMore).find(k => k.startsWith('__reactProps$'));
                    if (!key || typeof seeMore[key]?.onClick !== 'function')
                    {
                        console.warn('No React onClick found for See more:', seeMore);
                        consumeComment(comment);
                        setTimeout(processNext, 100);
                        return;
                    }

                    const observer = new MutationObserver(() =>
                    {
                        observer.disconnect();
                        consumeComment(comment);
                        setTimeout(processNext, 100);
                    });

                    observer.observe(comment, { childList: true, subtree: true, characterData: true });

                    seeMore[key].onClick({
                    type: 'click',
                    nativeEvent: new MouseEvent('click'),
                    currentTarget: seeMore,
                    target: seeMore,
                    stopPropagation() { },
                    preventDefault() { }
                    });
                }

                processNext();
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

    public class PageSearchResult
    {
        public string Url { get; set; } = default!;
        [JsonPropertyName("foundAntiDezInfoHashTag")]
        public bool FoundAntiDezInfoHashTag { get; set; }
        [JsonPropertyName("comments")]
        public string[] Comments { get; set; } = [];
    }

    public class SearchResult
    {
        public string Url { get; set; } = default!;
        public bool FoundAntiDezInfoHashTag { get; set; }
        public string? Comment { get; set; }
    }
}
