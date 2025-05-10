using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Forms.VisualStyles;

namespace WebView2MultiView;

static class Program
{
    private static readonly Dictionary<string, DateTime?> creationDates = [];

    private static readonly bool scrapeProfileTransparency = false;
    private static readonly bool scrapePostWithComments = false;

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        if (scrapeProfileTransparency)
        {
            var accountUrls = DataUtils.GetAccountUrls();
            if (accountUrls == null || accountUrls.Length == 0)
            {
                MessageBox.Show("No account URLs found in augmented_accounts.json.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            ScrapeProfileTransparency(accountUrls);
        }

        if (scrapePostWithComments)
        {
            var engagingPosts = DataUtils.GetPostUrls();
            if (engagingPosts == null || engagingPosts.Length == 0)
            {
                MessageBox.Show("No post URLs found in engaging_posts.json.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Application.Run(new PostCommentScraper(engagingPosts));
            // Application.Run(new PostMessageScraper(engagingPosts));
            return;
        }

        if (false)
        {
            var accountUrls = DataUtils.GetAccountUrls();
            if (accountUrls == null || accountUrls.Length == 0)
            {
                MessageBox.Show("No account URLs found in augmented_accounts.json.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var aboutUrls = accountUrls.Select(url =>
            {
                return url.Contains('?') ? url + "&sk=about" : url + "/about";
            }).ToArray();

            Application.Run(new FacebookPageScraperForm(aboutUrls));
        }

        if (true)
        {
            var accountUrls = new string[] { "https://www.facebook.com/www.hotnews.ro/posts/1096044209210790" };
            if (accountUrls == null || accountUrls.Length == 0)
            {
                MessageBox.Show("No account URLs found in augmented_accounts.json.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var aboutUrls = accountUrls.Select(url =>
            {
                return url.Contains('?') ? url + "&sk=about" : url + "/about";
            }).ToArray();

            var form = new SearchForComment(aboutUrls);

            Application.Run(form);

            // Persist form.SearchResults to a file
            var filename = "search_results.json";
            var json = JsonSerializer.Serialize(form.SearchResults, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            File.WriteAllText(filename, json);
        }
    }

    private static void ScrapeProfileTransparency(string[] accountUrls)
    {
        // Split account URLs into chunks of 100
        var chunkSize = 100;
        var chunks = new List<string[]>();
        for (int i = 0; i < accountUrls.Length; i += chunkSize)
        {
            var chunk = accountUrls.Skip(i).Take(chunkSize).ToArray();
            chunks.Add(chunk);
        }

        var screenSize = Screen.PrimaryScreen!.Bounds.Size;

        // Create 8 WebView2 windows in a grid layout
        var rows = 2;
        var columns = 4;
        var width = screenSize.Width / columns;
        var height = (screenSize.Height - 50) / rows; // Adjust height to fit the taskbar

        var forms = new List<ProfileTransparencyScraper>();
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                var form = new ProfileTransparencyScraper(chunks[i * columns + j])
                {
                    Size = new Size(width, height),
                    Location = new Point(j * width, i * height),
                    StartPosition = FormStartPosition.Manual,
                    Text = $"WebView2 - {i * columns + j + 1}",
                    WindowState = FormWindowState.Normal,
                };
                forms.Add(form);
                form.Show();
            }
        }

        // Wait for all forms to close before exiting the application
        while (forms.Any(f => !f.IsDisposed))
        {
            Application.DoEvents();
            Thread.Sleep(100); // Sleep for a short duration to avoid busy waiting
        }
    }

    public static string NormalizeFancyText(string input)
    {
        var output = new StringBuilder(input.Length);

        foreach (var rune in input.EnumerateRunes())
        {
            if (rune.Value >= 0x1D400 && rune.Value <= 0x1D419) // Mathematical Bold Capital Letters
            {
                output.Append((char)(rune.Value - 0x1D400 + 'A'));
            }
            else if (rune.Value >= 0x1D41A && rune.Value <= 0x1D433) // Mathematical Bold Small Letters
            {
                output.Append((char)(rune.Value - 0x1D41A + 'a'));
            }
            else if (rune.Value >= 0x1D63C && rune.Value <= 0x1D655) // Sans-Serif Bold Capital Letters
            {
                output.Append((char)(rune.Value - 0x1D63C + 'A'));
            }
            else
            {
                output.Append(rune.ToString()); // Leave unchanged
            }
        }

        return output.ToString();
    }

    public static void LoadCreationDates()
    {
        var filename = "creation_dates.json";

        if (File.Exists(filename))
        {
            var json = File.ReadAllText(filename);
            var dates = JsonSerializer.Deserialize<Dictionary<string, DateTime?>>(json);
            if (dates != null)
            {
                foreach (var kv in dates)
                {
                    creationDates[kv.Key] = kv.Value;
                }
            }
        }
    }

    public record Account(string Name, string ProfileUrl, DateTime? CreationDate, Stats Stats, int PostCount, int CommentCount);
    public record Stats(int Followers, int Likes, int Comments);

    static (int postCount, int commentCount) SummarizeComments(JsonElement posts)
    {
        // `posts` is an array of posts
        // Each post has a `stats` object with a `comments` property. Its value is an integer.
        // Return the sum of all comments across all posts.
        // If `posts` is empty, return 0.

        var minDate = new DateTime(2025, 4, 1);
        var maxDate = new DateTime(2100, 1, 1);

        int sum = 0;
        int postCount = 0;
        foreach (var post in posts.EnumerateArray())
        {
            if (post.TryGetProperty("stats", out var stats))
            {
                // "post_timestamp": "2025-04-18T16:24:27+00:00",
                if (post.TryGetProperty("post_timestamp", out var postTimestamp))
                {
                    var postDate = DateTime.Parse(postTimestamp.GetString()!);

                    if (postDate < minDate || postDate > maxDate)
                    {
                        continue;
                    }

                    if (stats.TryGetProperty("comments", out var comments))
                    {
                        sum += comments.GetInt32();
                    }
                    postCount++;
                }
            }
        }

        return (postCount, sum);
    }
}