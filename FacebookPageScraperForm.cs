using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebView2MultiView
{
    public partial class FacebookPageScraperForm : Form
    {
        private readonly List<string> urlsToScrape;
        private WebView2 webView;
        private Button startButton;
        private readonly Dictionary<string, string?> addresses = [];

        public FacebookPageScraperForm(string[] urls)
        {
            urlsToScrape = urls.ToList();

            Size = new System.Drawing.Size(800, 600);

            InitializeWebViewAsync();
            InitializeUI();
        }

        private async void InitializeWebViewAsync()
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MyAppWebView2"
            );

            var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };

            Controls.Add(webView);
            await webView.EnsureCoreWebView2Async(environment);
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

            // If a previous scraping result file exists, load it
            var resultsFile = "output/addresses.json";
            if (File.Exists(resultsFile))
            {
                var previousResultsJson = File.ReadAllText(resultsFile);
                addresses.Clear();
                var previousResults = JsonSerializer.Deserialize<Dictionary<string, string?>>(previousResultsJson);
                if (previousResults != null)
                {
                    foreach (var kv in previousResults)
                    {
                        addresses[kv.Key] = kv.Value;
                    }

                    Console.WriteLine("Loaded previous results from file.");
                }
            }

            foreach (var url in urlsToScrape)
            {
                // if (addresses.TryGetValue(url, out DateTime? value) && value != null)
                // {
                //     continue; // Skip if already scraped
                // }

                await NavigateAndScrapeAsync(url);

                var json = JsonSerializer.Serialize(addresses, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                File.WriteAllText(resultsFile, json);
            }

            MessageBox.Show("Scraping complete.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            startButton.Enabled = true;

            // Save results to JSON file
            // var json = JsonSerializer.Serialize(addresses, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            // File.WriteAllText(resultsFile, json);

            // Output to console
            foreach (var kv in addresses)
                Console.WriteLine($"{kv.Key} => {kv.Value}");

            // Output to JSON file
            // var json = JsonSerializer.Serialize(addresses, new JsonSerializerOptions { WriteIndented = true });
            // File.WriteAllText(resultsFile, json);
            Console.WriteLine($"Results saved to {resultsFile}");
        }

        private async Task NavigateAndScrapeAsync(string url)
        {
            var tcs = new TaskCompletionSource<bool>();

            async void NavigationCompletedHandler(object sender, CoreWebView2NavigationCompletedEventArgs e)
            {
                webView.NavigationCompleted -= NavigationCompletedHandler;

                try
                {
                    await webView.ExecuteScriptAsync(JSPressCloseButton());

                    var addressJsResult = await webView.ExecuteScriptAsync(JSAddressScript());
                    string addressText = addressJsResult?.Trim('"');
                    Console.WriteLine($"Address for {url}: {addressText}");
                    addresses[url] = addressText;
                }
                catch
                {
                    addresses[url] = null;
                }

                tcs.SetResult(true);
            }

            webView.NavigationCompleted += NavigationCompletedHandler;

            if (!url.Contains("about"))
            {
                webView.Source = new Uri(url + (url.Contains('?') ? "&" : "?") + "sk=about_profile_transparency");
            }
            else
            {
                webView.Source = new Uri(url);
            }

            await tcs.Task;
        }

        private static string JSPressCloseButton() => @"document.querySelector('div[aria-label=\'Close\'][role=\'button\']').click();";

        private static string JavascriptScraper() => @"
(() => {
    try {
        const spans = document.querySelectorAll('span');
        for (const span of spans) {
            if (span.innerText.trim() === 'Creation date') {
                const container = span.closest('div');
                if (!container) continue;
                const dateSpan = container.parentElement?.previousElementSibling?.querySelector('span');
                if (dateSpan && dateSpan.innerText.trim()) {
                    return dateSpan.innerText.trim();
                }
            }
        }
        return null;
    } catch (err) {
        return 'Error: ' + err.message;
    }
})();";

        private static string JSAddressScript() => @"
(() => {
    // Step 1: find the span with label text `Address`
    const label = Array.from(document.querySelectorAll('span'))
        .find(el => el.textContent.trim() === 'Address');

    if (!label) return null;

    // Step 2: go up and find the nearest preceding <span dir=`auto`>
    const addressSpan = label.closest('div')?.parentElement?.previousElementSibling?.querySelector('span[dir=""auto""]');
    const address = addressSpan ? addressSpan.textContent.trim() : null;

    console.log(address);
    return address;
})();
";

        private static DateTime? ParseFacebookCreationDate(string rawDate)
        {
            if (string.IsNullOrWhiteSpace(rawDate))
                return null;

            var formats = new[] { "MMMM d, yyyy", "MMMM d" };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(rawDate, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                {
                    if (format == "MMMM d")
                        return new DateTime(DateTime.Now.Year, parsed.Month, parsed.Day);
                    return parsed;
                }
            }

            return null;
        }
    }
}
