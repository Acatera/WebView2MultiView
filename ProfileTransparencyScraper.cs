using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Drawing.Design;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WebView2MultiView
{
    public partial class ProfileTransparencyScraper : Form
    {
        private const string folderName = "profile_transparency";
        private readonly List<string> urlsToScrape;
        private WebView2 webView;
        private Button startButton;
        private TaskCompletionSource<bool>? currentScrapeTcs;

        public ProfileTransparencyScraper(string[] urls)
        {
            urlsToScrape = urls.ToList();
            WindowState = FormWindowState.Maximized;
            InitializeWebViewAsync();
            InitializeUI();
        }

        private async void InitializeWebViewAsync()
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MyAppWebView"
            );

            var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            webView = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(webView);
            await webView.EnsureCoreWebView2Async(environment);

            webView.CoreWebView2.WebMessageReceived += WebMessageReceivedHandler;

            // Inject JS after every navigation
            webView.NavigationCompleted += async (_, _) =>
            {
                await webView.ExecuteScriptAsync(JSPressCloseButton());
                await webView.ExecuteScriptAsync(JavascriptScraper());
            };
        }

        private void InitializeUI()
        {
            startButton = new Button
            {
                Text = "Start Scraping",
                Dock = DockStyle.Bottom
            };
            startButton.Click += async (_, _) => await StartScrapingAsync();
            Controls.Add(startButton);
        }

        private async Task StartScrapingAsync()
        {
            startButton.Enabled = false;
            Directory.CreateDirectory(folderName);

            foreach (var url in urlsToScrape)
            {
                if (await NavigateAndScrapeAsync(url))
                {
                    await Task.Delay(5000); // delay between pages
                }
            }

            MessageBox.Show("Scraping complete.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            startButton.Enabled = true;
        }

        private string currentUrl = string.Empty;
        private string currentUrlHash = string.Empty;

        private async Task<bool> NavigateAndScrapeAsync(string url)
        {
            currentScrapeTcs = new TaskCompletionSource<bool>();
            currentUrl = url;
            currentUrlHash = GetHashedUrl(url);

            // Check if profile file already exists
            var fileName = Path.Combine(folderName, $"profile_{currentUrlHash}.json");
            if (File.Exists(fileName))
            {
                Console.WriteLine($"Profile file already exists for {currentUrlHash}. Skipping...");
                currentScrapeTcs.SetResult(true);
                return false;
            }

            var fullUrl = url.Contains("?")
                ? $"{url}&sk=about_profile_transparency"
                : $"{url}?sk=about_profile_transparency";

            webView.Source = new Uri(fullUrl);

            // Wait for result from WebMessageReceived
            await currentScrapeTcs.Task;

            return true;
        }

        private void WebMessageReceivedHandler(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string json = e.WebMessageAsJson;
                Console.WriteLine("✅ Received from JS:\n" + json);

                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                // Append the current URL to the result
                if (result != null)
                {
                    result["url"] = currentUrl;
                }

                // And date 
                if (result != null)
                {
                    result["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd");
                }
                if (result != null)
                {
                    var fileName = Path.Combine(folderName, $"profile_{currentUrlHash}.json");
                    var jsonResult = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                    File.WriteAllText(fileName, jsonResult);
                }

                currentScrapeTcs?.SetResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Failed to parse result: " + ex.Message);
                currentScrapeTcs?.SetResult(true);
            }
        }

        private string GetHashedUrl(string? url = null)
        {
            url ??= webView.Source.ToString();
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)));
            return hash;
        }

        private static string JSPressCloseButton() => @"document.querySelector('div[aria-label=\'Close\'][role=\'button\']').click();";


        private static string JavascriptScraper() => @"
        (() => {
            const delay = (ms, cb) => setTimeout(cb, ms);

            const triggerReactClick = (element) => {
                const reactKey = Object.keys(element).find(key => key.startsWith('__reactProps$'));
                if (reactKey) {
                    const props = element[reactKey];
                    if (typeof props.onClick === 'function') {
                        const mockEvent = {
                            preventDefault: () => {},
                            stopPropagation: () => {},
                            nativeEvent: {
                                preventDefault: () => {},
                                stopImmediatePropagation: () => {}
                            }
                        };
                        props.onClick(mockEvent);
                        return true;
                    }
                }
                return false;
            };

            const result = {
                transparencyStatus: 'Not found',
                seeMoreStatus: 'Not found',
                historyEntries: [],
                adsStatus: [],
                pageManagers: []
            };

            const step1 = () => {
                const transparencyBtn = document.querySelector('[role=\'button\'][aria-label=\'See all transparency information\']');
                if (transparencyBtn) {
                    triggerReactClick(transparencyBtn);
                    result.transparencyStatus = 'Clicked';
                }
                delay(1000, step2);
            };

            const step2 = () => {
                const tryClickMore = () => {
                    const allButtons = document.querySelectorAll('[role=\'button\']');
                    for (const btn of allButtons) {
                        const label = btn.innerText?.trim();
                        if (/^See \d+ More$/.test(label)) {
                            triggerReactClick(btn);
                            result.seeMoreStatus = `Clicked: ${label}`;
                            return delay(500, step3); // proceed
                        }
                    }

                    if (++attempts < maxAttempts) {
                        setTimeout(tryClickMore, 100);
                    } else {
                        result.seeMoreStatus = 'Not found (timeout)';
                        step3();
                    }
                };

                let attempts = 0;
                const maxAttempts = 20;
                tryClickMore();
            };

            const step3 = () => {
                const spanElements = Array.from(document.querySelectorAll('span'));

                // History section
                const historySpan = spanElements.find(el => el.textContent.trim() === 'History');
                if (historySpan) {
                    let container = historySpan;
                    for (let i = 0; i < 5; i++) container = container?.parentElement;
                    const section = container?.nextElementSibling;
                    if (section) {
                        Array.from(section.children).forEach(child => {
                            const text = child.innerText?.trim();
                            if (text &&
                                !/^See less$/i.test(text) &&
                                !/^See more$/i.test(text) &&
                                !/^See \\d+ More$/i.test(text)) {
                                result.historyEntries.push(text);
                            }
                        });
                    }
                }

                // Ads
                const adMessages = ['is not currently running ads', 'has run ads about', 'is currently running ads'];
                const adTextTargets = document.querySelectorAll('body *');
                adTextTargets.forEach(el => {
                    const text = el.textContent?.trim();
                    if (text) {
                        adMessages.forEach(msg => {
                            if (text.startsWith('This') && text.includes(msg) && !result.adsStatus.includes(text)) {
                                result.adsStatus.push(text);
                            }
                        });
                    }
                });

                // Page managers
                const managerSpan = spanElements.find(el =>
                    el.textContent.trim().startsWith('People who manage this ')
                );
                if (managerSpan) {
                    let container = managerSpan;
                    for (let i = 0; i < 5; i++) container = container?.parentElement;
                    const siblings = Array.from(container?.parentElement?.children || []).filter(el => el !== container);
                    siblings.forEach(el => {
                        const text = el.innerText?.trim();
                        if (text) result.pageManagers.push(text);
                    });
                }

                // ✅ Send result to C#
                window.chrome.webview.postMessage(result);
            };

            // Start process
            step1();
        })();
        ";
    }
}
