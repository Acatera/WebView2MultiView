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
}