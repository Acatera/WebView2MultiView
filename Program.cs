using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Forms.VisualStyles;

namespace WebView2MultiView;

static class Program
{
    private static readonly Dictionary<string, DateTime?> creationDates = [];

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new PostCommentScraper(["https://www.facebook.com/permalink.php?story_fbid=pfbid02XA7bmaZ8GbkyhB7c4GUnRRA8TZRmLoph9PHu4cBWCgGdWfXEg3aJfoMFyCGevysFl&id=100064853043607"]));
        return;
        LoadCreationDates();

        var json = File.ReadAllText("raw.json");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var accounts = root.GetProperty("accounts");

        var accountList = new List<Account>();

        foreach (var account in accounts.EnumerateObject())
        {
            var name = account.Value.GetProperty("name").GetString();
            var profileUrl = account.Value.GetProperty("profile_url").GetString();

            /*
            "stats": {
                "followers": 70000,
                "likes": 831
            },
            */

            var followers = account.Value.GetProperty("stats").TryGetProperty("followers", out var followersElement) ? followersElement.GetInt32() : 0;
            var likes = account.Value.GetProperty("stats").TryGetProperty("likes", out var likesElement) ? likesElement.GetInt32() : 0;

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(profileUrl))
            {
                continue;
            }

            var (postCount, commentCount) = SummarizeComments(account.Value.GetProperty("posts"));

            var accountStats = new Stats(followers, likes, 0);

            var creationDate = creationDates.TryGetValue(profileUrl, out DateTime? value) ? value : null;

            var accountItem = new Account(name, profileUrl, creationDate, accountStats, postCount, commentCount);
            accountList.Add(accountItem);
        }

        var relevantAccounts = accountList
            .Where(x => x.CommentCount > x.PostCount)
            .Where(x => x.CommentCount / (double)x.PostCount > 10)
            // .OrderByDescending(x => x.CommentCount / (double)x.PostCount)
            .OrderByDescending(x => x.CommentCount / (double)x.Stats.Followers)
            ;

        var maxNameLength = relevantAccounts.Max(x => x.Name.Length);

        // Print results as a table
        Console.WriteLine($"{"Name".PadRight(maxNameLength)} | {"Post #",10} | {"Comment #",10} | {"Post %",10} | {"Followers %",10} | {"Likes %",10}");
        Console.WriteLine(new string('-', maxNameLength + 10 * 5 + 4));
        foreach (var account in relevantAccounts)
        {
            var postRatio = account.CommentCount / (double)account.PostCount;
            var followersRatio = account.CommentCount / (double)account.Stats.Followers;
            var likesRatio = account.CommentCount / (double)account.Stats.Likes;

            // Pad name to 60 characters, pad integer values to 10 characters, format double values to 5 whole, 2 decimal places
            Console.WriteLine($"{account.Name.PadRight(maxNameLength)} | {account.Stats.Followers,10} | {account.PostCount,10} | {account.CommentCount,10} | {postRatio,10:F2} | {followersRatio,10:F2} | {likesRatio,10:F2}");
        }

        relevantAccounts = accountList
                    .Where(x => x.CreationDate != null)
                    .Where(x => x.Stats.Followers / ((DateTime.UtcNow - (x.CreationDate ?? DateTime.UtcNow)).TotalDays + 1) > 250)
                    .OrderBy(x => x.Stats.Followers / ((DateTime.UtcNow - (x.CreationDate ?? DateTime.UtcNow)).TotalDays + 1));

        // Print a table of accounts ordered by follower gain per date after creation date
        maxNameLength = relevantAccounts.Max(x => NormalizeFancyText(x.Name).Length);
        Console.WriteLine("\n\n\n");
        Console.WriteLine($"{"Name".PadRight(maxNameLength)} |  Creation  | Days Active | Followers | Follower/Day |");
        Console.WriteLine(new string('-', maxNameLength + 10 * 2 + 4));


        foreach (var account in relevantAccounts)
        {

            if (account.CreationDate == null)
            {
                continue;
            }

            // Ensure only utf8 chars (transform 𝙼𝙾𝙾𝙳   into MOOD, 𝐉𝐮𝐫𝐧𝐚𝐥 into Jurnal)
            var name = NormalizeFancyText(account.Name);
            var daysActive = (DateTime.UtcNow - account.CreationDate.Value).TotalDays;
            var followersPerDay = account.Stats.Followers / daysActive;
            var creationDate = account.CreationDate.Value.ToString("yyyy-MM-dd");
            if (followersPerDay < 250)
            { continue; }

            // Pad name to 60 characters, pad integer values to 10 characters, format double values to 5 whole, 2 decimal places
            Console.WriteLine($"{name.PadRight(maxNameLength)} | {creationDate:yyyy-MM-dd} | {daysActive,6:N0} | {account.Stats.Followers,10:N0} | {followersPerDay,10:F2}");
        }


        // ApplicationConfiguration.Initialize();
        // Application.Run(new DualWebViewForm());
        // Application.Run(new FacebookPageScraperForm([.. accountList.Select(x => x.Item2).ToArray()]));
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