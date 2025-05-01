using System.Text.Json;

namespace WebView2MultiView;

public static class DataUtils
{
    public static string[]? GetAccountUrls()
    {
        // Load augmented_accounts.json from output dir
        string path = "output/augmented_accounts.json";

        if (!File.Exists(path))
        {
            Console.WriteLine($"augmented_accounts.json not found at {path}");
            return null;
        }

        try
        {
           var json = File.ReadAllText(path);
            var jsonObject = JsonDocument.Parse(json);
            var accounts = jsonObject.RootElement.GetProperty("accounts").EnumerateArray();
            var urls = new List<string>();

            foreach (var account in accounts)
            {
                if (account.TryGetProperty("profile_url", out var urlElement))
                {
                    urls.Add(urlElement.GetString() ?? string.Empty);
                }
            }

            return [.. urls];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading augmented_accounts.json: {ex.Message}");
            return null;
        }
    }

    public static string[]? GetPostUrls(){
        // Load engaging_posts.json from output dir
        string path = "output/engaging_posts.json";

        if (!File.Exists(path))
        {
            Console.WriteLine($"engaging_posts.json not found at {path}");
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            var jsonObject = JsonDocument.Parse(json);
            var posts = jsonObject.RootElement.GetProperty("posts").EnumerateArray();
            var urls = new List<string>();

            foreach (var post in posts)
            {
                if (post.TryGetProperty("url", out var urlElement))
                {
                    var url = urlElement.GetString();
                    if (!url!.Contains("permalink") && !url.Contains("posts"))
                    {
                        continue;
                    }
                    urls.Add(urlElement.GetString() ?? string.Empty);
                }
            }

            return [.. urls];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading engaging_posts.json: {ex.Message}");
            return null;
        }
    }
}