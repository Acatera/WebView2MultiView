using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebView2MultiView
{
    public partial class PostCommentScraper : Form
    {
        private readonly List<string> urlsToScrape;
        private readonly Dictionary<string, DateTime?> scrapeResults = new();
        private WebView2 webView;
        private Button nextPageButton;
        private Button scrapeCommentsButton;
        private int currentPage = 0;

        public PostCommentScraper(string[] urls)
        {
            WindowState = FormWindowState.Maximized;

            urlsToScrape = [.. urls];

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
            var panel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };
            Controls.Add(panel);
            nextPageButton = new Button
            {
                Text = "Load next page",
                Dock = DockStyle.Left
            };
            nextPageButton.Click += async (_, _) =>
            {
                nextPageButton.Enabled = false;

                if (currentPage >= urlsToScrape.Count)
                {
                    MessageBox.Show("No more pages to load.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var url = urlsToScrape[currentPage];
                currentPage++;
                webView.Source = new Uri(url);

                nextPageButton.Enabled = true;
            };
            panel.Controls.Add(nextPageButton);



            scrapeCommentsButton = new Button
            {
                Text = "Scrape Comments",
                Dock = DockStyle.Right
            };
            scrapeCommentsButton.Click += async (_, _) => await StartScrapingAsync();

            panel.Controls.Add(scrapeCommentsButton);
        }

        public class Comment
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("text")]
            public string Text { get; set; }
        }

        private async Task StartScrapingAsync()
        {
            scrapeCommentsButton.Enabled = false;

            var jsResult = await webView.ExecuteScriptAsync(JSGetCommentsScript());
            Console.WriteLine($"Raw JS result: {jsResult}");

            if (!string.IsNullOrWhiteSpace(jsResult))
            {
                // WebView2 wraps the result in quotes, so remove them
                jsResult = System.Text.Json.JsonSerializer.Deserialize<string>(jsResult);

                // Now deserialize into a C# list
                var comments = JsonSerializer.Deserialize<List<Comment>>(jsResult);

                if (comments != null)
                {
                    foreach (var comment in comments)
                    {
                        Console.WriteLine($"Name: {comment.Name}, Text: {comment.Text}");
                    }
                }
            }

            scrapeCommentsButton.Enabled = true;
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
})();";



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
