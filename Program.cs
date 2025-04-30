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
        
        var accountUrls = DataUtils.GetAccountUrls();
        if (accountUrls == null || accountUrls.Length == 0)
        {
            MessageBox.Show("No account URLs found in augmented_accounts.json.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        Application.Run(new ProfileTransparencyScraper(accountUrls));
        return;

        Application.Run(new PostMessageScraper([
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02kqR8Tkx1wyZZ2TjQu8stmYX1VrnXnZa1hZe5WbrhrwLesHtzZKSD7ZHm44vyXZSFl&id=61572584199454",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02g3NHqmPpKQsbrpQTvF5SA73zXmK6Hmi7387waghnr4gv127wGJv3afmAqcABYydpl&id=61572584199454",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid061HbjZhpYQWKQDkhz3Ftb63ihxbrM4zECzzKpRwzxjbPqJLsRvJ6HAL5SXUegx1tl&id=61572584199454",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid011LEscrwA6FyKSVHcpeCfhN3bcnxDVbHdHvixRdBLopaP5EujF9WRfRbhZREyxYl&id=61572584199454",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02g7JKd4ZhjqxVGKGLCPPdhprbiGZBLhbpdXmoHdrtht74tzsnRM2fLWtVLekQTKbXl&id=61572584199454",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0YRcbJS2iLHZq9j5apfKSng4ht6AqHUJh5PFC8CLQsfW6VPnb1w863KV64ubSnQpzl&id=61572584199454",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid06kCwBDKFPbPZqgTH7Yj2XuNfSfx8CSFDHxY7KdVfhdURKQPzUb3AjESBtpB1PUhHl&id=61572584199454",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02yLTpLWGsEMRBK763WWN2F231RdpUChVyLxdjNWhh9F2ZavHttXvgJ2Qaz3Tb8QKAl&id=61570183406826",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0jyWVRbaGxEoeGCUeRcETxM3aiE76gQj3aSsaGaBHfk2pRUq6DAAbJBJV8xgfDttsl&id=61573805564272",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02BcBk7H9B1Wysv5vdp1ndPJpqfCkW5W8WUSmi2RmrBT6EEG73hWiTgCPJXdMNKVsel&id=61573805564272",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02DsEnuiTptkQGewmiBrreZX6uUWYpmA5UsaXv3CHi4bmAEek4gPo86dStG3kepzSdl&id=61573805564272",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0uiZYcxw6VtY22WsCMa168ChfuzqDWjVwnqPkTKZVMzXpkUKAMphRmKvnQqEhEwHUl&id=61573805564272",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02y9ei1Cv5V6L68vuWY8HhKTD3GxPcZ3rmXhSmGJBsXFuQaRAQ9oHATHKQqUZJVFefl&id=61574075790239",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0GQnHgGjJsaQWbMjskMHNxfzAPuVxCfQQXUDjCm17hmXLjkjA7hh3VsaMRML9wzrUl&id=61551125926880",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0JjDhLzSyY6Y2Njj7UvAHqpwUgVUHWEWFMfagG7744uj5k7CCfxmsShpqUN86k2T2l&id=61551125926880",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0hniUZ4gqH7PE7CabpyXMnNu1NGX3NSTxBT9uG7JVZokJKA2R1d7M1yPmHPGoc1F7l&id=61551125926880",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0G33qNQpMogCN426o3aS5q99CrTWua8QmZbmH6ifFWKq5B4LGGF6kjpG2hE4C4Trkl&id=61573384113558",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0XnGGwtGgzUY6gwAX4ceoro53ur3e6ot1xdAjsutwaiaxAU5FZsFY6s7aJP1qKRTZl&id=61573384113558",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02SEK2yCQvj8JnSzVuz5vARBt6WHZtXEVJV1dasA9ST6LxqfYkBX6yvjzXLLG7HZp9l&id=61573384113558",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid028dk4fGR37ZmjStowt65o24UVgP1qVLLTqqKu3ypFkjQPA8PiHrnXP9GDHyy5GVECl&id=61573384113558",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02449KgNu9KND39pySGswb2c8XE3XBDCvv9xHHQrvTem9bxVTE4DfGq6gsQWHS8fycl&id=61573384113558",
            "https://www.facebook.com/permalink.php?story_fbid=122124043904779470&id=61573384113558",
            "https://www.facebook.com/permalink.php?story_fbid=122124036716779470&id=61573384113558",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0orNZBew8dR9FnigLsyvYtm2WpkGz5Vw92Nax14RW6hrgbqo16MARCWPakLccaFVTl&id=61573384113558",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0FRtCfv9q2kFbtpUYev4heCiS3qncQq8SJ6uHz3TFmpcPtfAiJfrrCbRwyXqfQwenl&id=61572403288929",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02UXsk94TJ84YSH5uTHJgmdTsEy9frQWz5KWbVyPAgPLuLxkV1JSKaQ9YmiSWZAniQl&id=100090138815948",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid033BgtnkxCpcbFTogzGMjt8JY9kMZrBdT1aaoCJh3bKFivnK6Vi3AdTyXteZJKe6W5l&id=100089687406770",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02yJVn1d3pgVXmME4y8RJQdU3s9NuqaMiQoGq23pHdriTNrjppVjeGBroLM6LNjnbjl&id=61572998127931",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0xHJhzzt8wGh7s6NbG3QdDwBNfed36oPoWoudoejv7nBBCuWnkvXoCucbdPvzJAHCl&id=61572998127931",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0mhYae1GjW6w3qGfgkDMxeWdXMxU29uA2LfgVMq2hjrT8XGa21dqgE925xYpEx1bAl&id=61572998127931",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0vQRXLwQqMNf5zW1KbHF8gtSLEe65DBXND96JcYt3V3SabDXd7ZMJCcHo8h2JLk32l&id=61572215671821",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0rphuFTuVbNPE7T3BiVmozotjUL5oyS12KPBFR3eMc3fyhbn3kdKAPCWwrUF8SLq8l&id=61572215671821",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0ipZcL8HL2Fn89CHMivXF83Fxy6yMTYgobsMs5HtiDqUqZZedABJQD4FkjTmXGPzSl&id=61555844099582",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0podetCq6P5paFjqPK5jWGtfpVcFgqJab823C6TYx9T8EYnqDt2LbetsX9CxpQrKml&id=61555844099582",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0354MQgQuxYe52axhSWuc3ys1H95M5NpZwXuVKsXvKaDLvZYmK5sbCw5q1KKMBwxQul&id=61555844099582",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0uGTg8XYxcvVeKLm7zGT18tWQxvxk3Hm5HqYPAmvHLmNG5ujVMERcqcyvcnRMN6iWl&id=61555844099582",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02MKP9ESJvcKqUmnuPAF8CqQCQYYxiBXwCApMzXpn6wpvycfe6Hk9ExbkyLXARSQRrl&id=61573549770905",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02FgxG2fQZiUoHiqRAK3g3jrkyonyPDNwa4Czoeuv156FQ5xRbJLtXybjLwpXGVncxl&id=100080252033829",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02WJRa2bnmhn9TTdfrsGJU7EqnaiDhDt8WDvSNCqVkaSCx2bjRq5aV3ie1B1yjMUGTl&id=100093575597433",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02XENf8idSKPRC5eR8VNnaSvZRn1La1X7gkQjTC9xfSVv4dcDDGjZqy5Q1gLrbiq8Ql&id=100093575597433",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0FvPRap91LWD3oDb7wBUG3CwidXMVsfyokhxiL5eM3mDcqCLfvtFxpNMzwF2CKzgsl&id=100093575597433",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02qAvm4W6yz33YVQC1sPFXWZtwAUYZyFUJBo7AiHmouBtEn8y14azLA4778ZVRiKx1l&id=61571064283643",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid07fW1z7dGSJMvjmraCduKjQwftk5pdaLcAgzcuib21qAMSN4J5PMxqkKgECN52WpHl&id=61571064283643",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0w4eWm7D4856M8C626MBbS43PbGfnPTdjtG3iV2pNS8a7iER1rCqNkXfipK8tXfkFl&id=61571064283643",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02bqPd99J3rrU5T7VAWG1tHgyFX4QwEPtQWVyjE2goUnsv99fCpG1K8mNe994ojeRl&id=61574667997684",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0vy5SoY5SwJcc42DPmGFa9HykKbCYccByiKTZndYoNcmR43WzD9fjSzpxBs2wNDQkl&id=61574667997684",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0Y9T7DCgy6WPAVJfKE2TAeUn8jo2yiedqFPNcuiQMuTi7LtLTixw7zrG5jUsUfi4nl&id=61574667997684",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02viYwRMoG2mRjNy1QbxtCfhTkQpJ4jLSpiPNcNBZ3TnXFnoVPSwvMXYTxmCAm8WQzl&id=61574667997684",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0HGN2uX26Bz3tdkuVbCwyTGTXCyxdqFSfVozSqSD6WW1BwKpdQMEQfPbxq5wNdN2Bl&id=100078027680502",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid036NMygEZzrZTJ3F7hACxg4JMjJ39y1sW1afkgDo76Yk1AZdFmZFY9qrt2S3Fm5GdKl&id=61557509000173",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0FZXbU6z422qzaGkYuAyzu8z2fJq9uXG2L7KgS6QaCnjjqksS2Uu2w4yGrtAzubjCl&id=61557509000173",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02erg64HfaNc6LcU2hC1JoX9c2nnGMJEX1WZU77os6y8SLfKeSsh3ihLosECoeNAbkl&id=61557509000173",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02DLSXXKzHFf9zmeKgFFc36DwJB4yHQMP6gRemYYQUtvfih9Zn9Fx7dJ1hHZn248Krl&id=61557509000173",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0PLnTDXYp3Lm3KEikYt8bwE5Pjz42DKXa1tvjb5C7prLkTcxx4f9DpUem14gdwoEfl&id=61557509000173",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0peTajBS2Q36PqKdBD3D69ZLMR95rQmzFeE3DZPUTPyHyWabBPQuQhYjXu8EWpBKal&id=61557509000173",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid022HbVnqZfjoNRs9fDiDKfG4KsBQxBfBkeRoQECWQ285zTntckkhZu921THFhAomBZl&id=61557509000173",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid0f3vYX9fRb4Z5iLQpufJL6dMD2xbbkW591TiR1fe6wFVn4bgZRzQNszkfFmjzCokFl&id=100075882535860",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02of86inwHQs2TMXmkitPQwoM82G3jLUViZmfgRjhQ2PFM5FY6iGJUMPo7S3RE8Gh3l&id=100023686410690",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02ecayg9JecbH4VCMCXNuWL1Z18X83RFs7ZKjjpA1FUbjZ7kkif8SGKbhsZ15LLko5l&id=100023686410690",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02eGAV6FAo1aoTbsepXqfPG82Bx2TYuhYXrsgdDdSSSb2N7ePUCEf779YQVYwQtsDLl&id=100070177842826",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02VwnPF4WGQCkn8FZdJCPXm6PZiZ8FXzDjHrbrGHs36cMEv8nJdMsjq5zFK6k8muJ4l&id=100064677341135",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid034vNzJPBJwGuLocAU2JEWEwW1dQP8N5sD2sdUvDiphxx2NxJSr9PaS6JAp8Z99NGdl&id=100064677341135",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02oQNxcCQcEzPcPiFUq6mk3CxTMz5nrqpas2wDe3fBEY7iy7ij6jBKHbWPbS7Ekqdgl&id=100064677341135",
            "https://www.facebook.com/permalink.php?story_fbid=pfbid02XA7bmaZ8GbkyhB7c4GUnRRA8TZRmLoph9PHu4cBWCgGdWfXEg3aJfoMFyCGevysFl&id=100064853043607",
            "https://www.facebook.com/stirileromanilor.ro/posts/pfbid02SmywyyHZxEjdoMmTLkZn5hQA719yAAyNdMWnTn4XWCKHa8BpyMGB8LTDV16gt5Jcl",
            "https://www.facebook.com/AutovehiculeRomanesti/posts/pfbid02fUVj3zrkiaiEGmQfwtx5h4qsBiFe1uavuR2qMgTGajttzUcE9PMyT1zN6rrDKZoDl",
            "https://www.facebook.com/AutovehiculeRomanesti/posts/pfbid0221YPxHFu7KPzRaYyuWUPX8SnAjQhFooR1WcnrpAypGusDD7zvM68bvzrPghXFTBsl",
            "https://www.facebook.com/DianaSosoacaOficial/posts/pfbid0KBkuNErD8VUfbzsk84D6DcBPAyoor1nahZkswAdAdoXpqBCi2gUdxGDJWDZEkL64l",
            "https://www.facebook.com/DianaSosoacaOficial/posts/pfbid02gqZemVeV267z2XXdwbRZrsfNmYUeHPmz4VQB3bT2aUnRQxRZDMnCCEQ5bXraa2ftl",
            "https://www.facebook.com/DianaSosoacaOficial/posts/pfbid02Y4s1fU6Gg3tRDrT6XnPxx1M5dZDp9ZG2f1Qs8hQGW9aspokpxiP6WU8F3iG2opvml",
            "https://www.facebook.com/DianaSosoacaOficial/posts/pfbid0e3H7PZ1YtpGmqnBwJBHeHnEVdH9NtFKyyuqM4B5ssuTRnX1Q8tvDAtP759hANGazl",
            "https://www.facebook.com/DianaSosoacaOficial/posts/pfbid0yRGaeeP7Wm1UnGFvTzhiKph85CA3ochv4uY8Gk3KZLNaNKWBbNDGqYab8XYAhJBfl",
            "https://www.facebook.com/DianaSosoacaOficial/posts/pfbid09Hy7Yf7RKH9U536UrdncbCWWcnLco4av6JXoanJWT1ZYcdD1dGeV1Bvp8DRmPqsRl",
            "https://www.facebook.com/DianaSosoacaOficial/posts/pfbid01TyrMhQ6QRT3wCu6tmghfNw3kWesB2SNbfbP2KW6FmyCFNq8ZtrbXNfwqsLVc3M4l",
            "https://www.facebook.com/DianaSosoacaOficial/posts/pfbid0SS7BnJpkRcUkF7yVzJGMA9gB5xyTr2hAXokPJmdia63mtcyEgcd3Fwg9DeoPDY3wl",
            "https://www.facebook.com/DianaSosoacaOficial/posts/pfbid02KC5sUrrUKZ8YwDgyWACPHLzm7JP6gtC8Vr6Tya7yeXQXjHaWoWCBHfoLexpto535l",
            "https://www.facebook.com/diana.macovei1/posts/pfbid024p9zx44KT9kLkxW94dYcFpemweSUw7w14eA7BM3nptjSy6SJidMcR7pfdcCLXbTGl",
            "https://www.facebook.com/diana.macovei1/posts/pfbid02UY6QuTxqyomTAfYVn9EaodMAtCaYfXFeU9UetWD6Y8qjapTVmKg5PJsZud6vYqngl",
            "https://www.facebook.com/botosani.sosro.ro/posts/pfbid0tfjRqKBjEi7r7omdBs8zNYqQFX7mRTKVyfDghioQdHjNEBhSjWf5yuc4JCMVCNN4l",
            "https://www.facebook.com/bancuriofficial/posts/pfbid0wpLASwTodUHeaozERvoQAeihGB9XQ4mvNHAB88J2zN7NemiFGZqGASwW5W2GX3mtl",
            "https://www.facebook.com/bancuriofficial/posts/pfbid02UMpzvR22iLfYij6otkxXZqEUtz9PuWRDuvbneyvhHaoVeDEfsHp98LfyP3ZnR8XPl",
            "https://www.facebook.com/bancuriofficial/posts/1160820242756367",
            "https://www.facebook.com/bancuriofficial/posts/1160129289492129",
            "https://www.facebook.com/bancuriofficial/posts/1158171189687939",
            "https://www.facebook.com/bancuriofficial/posts/pfbid0271pfSyLriXNHnCndqiDdqA2jNkVYGjVhCoFpohRhmZLmYXEnK8Xw7gSn6ygnf1Rwl",
            "https://www.facebook.com/bancuriofficial/posts/pfbid0R59a1n9hDK4yfv6fUqJpc2dcpiVkhmcNFeCnX1v9aVEBcsWLtEs32h2BihnzAFqul",
            "https://www.facebook.com/bancuriofficial/posts/pfbid02WmqRgEb6t85Yz5usRb5rpMFb83echLXxSBaNMadtrDzstKaigiepUnaJgoTM9mrMl",
            "https://www.facebook.com/TotulPentruTine.Ro/posts/1202574464768423",
            "https://www.facebook.com/TotulPentruTine.Ro/posts/pfbid0Fi3fKurURtaYkVKYZwzskDi97hyNDpzMgcc4AoG5t7beCRBjg6bREVwWCRpi6PUPl",
            "https://www.facebook.com/TotulPentruTine.Ro/posts/pfbid0r52E8Q1eFLtNfguzabh4jpxpgwt3oZGuRWnc8BAh33gmj3u6JnmVhWz95Tg75AnYl",
            "https://www.facebook.com/TotulPentruTine.Ro/posts/pfbid02LiSEApS9NVXB2sJdoC7Bw9Efb2hbBTDZKfk2fWgRUhk12uKY3EZpcKh5czcfwDcWl",
            "https://www.facebook.com/TotulPentruTine.Ro/posts/pfbid02CuyhDAZs8z5mafp5bUzXQZ7p4p3ogww7uzSkD7tXZ1etNB8WyWJMRFGjQJQAWC8vl",
            "https://www.facebook.com/TotulPentruTine.Ro/posts/pfbid0h7crwk8cm6aQ2mzzsMYsrJYgqfVjYEzs4hWs7X2q5xi9GYX565zK8PvgH2FuR9DSl",
            "https://www.facebook.com/TotulPentruTine.Ro/posts/pfbid0WAZYVzi4T8cY8RSm2YkggSmQGNhTsMMvBd4q5ajtNPJjUYanrTVVACVCQTc22DkPl",
            "https://www.facebook.com/PovesteaMe/posts/pfbid02pWbThNC68FTUpsZYcfizWfzM26H6qmAGdEfXb5mrUG7ygWmA6Rs8CGvMv8dZbikbl",
            "https://www.facebook.com/PovesteaMe/posts/pfbid0aEM2YKYNxpa1n7GGACadbe8Bg94jKr7ToKoTbwxyxYryNc9WbpsNVTF6Cm8eU6MCl",
            "https://www.facebook.com/PovesteaMe/posts/pfbid07Mha2KF6x3sxomemaNh8HBx5Ass8eEP6w6fbEXtjHgr9nB33zBEP5BDWXEBBBW38l",
            "https://www.facebook.com/PovesteaMe/posts/pfbid0218kzx3JhRCKDQAchMBZJMMvtfBemBw2fFMHXUrUvV7zczfe3mjEBcr4DQ65gBzmnl",
            "https://www.facebook.com/PovesteaMe/posts/pfbid02kkim13Vi2V7Uq8aZGLCeX315tk9uzRJvDRgSkXUsSQ7quikFNcRnE7yrpooxndiTl",
            "https://www.facebook.com/PovesteaMe/posts/pfbid023c3TBwX1SMbmq9shf1YNw8k3GN5hQCz8mRMKJSRHkuePaof6Xf8ZmEKfYKcT79Gzl",
            "https://www.facebook.com/stiriromani/posts/pfbid04c2xvnBVqHijsw6ZtcMu3AR79NwZ5PuJoT5engyo4jRYscM8RvBkboVUxKUcSrSwl",
            "https://www.facebook.com/bula1977/posts/pfbid034ZXABg2McwkE4hNThCooqoRJszMKQTKjzsVheubcB1zyqoBDBMXSyTtqzjWC62MAl",
            "https://www.facebook.com/bula1977/posts/1052473536904121",
            "https://www.facebook.com/bula1977/posts/pfbid02bhCewoiCiQZAvxjbRqZ6Cb6LMjvcr7CJhkmnJ2cYsCt3j7JXQ7rrjQqNEfsfxSZxl",
            "https://www.facebook.com/bula1977/posts/pfbid02W8hoXKyjErVRYVteXSShVkLCp98KddkGrsJUtsNECLpeSY7GfcrFiL5w7M3kmbK3l",
            "https://www.facebook.com/bula1977/posts/pfbid05vovUVQAKTwfRg32wFKLj4CgDRTMEc8ypYaMnxzkPGMYEez4CQdqpA4Sicob4mzVl",
            "https://www.facebook.com/bula1977/posts/pfbid0PuSNg8DRdBSfaVd6HSYeVbeYZRX4n5T7szf8fn76NT8ZY29Q2vmM8QxMjJCUdNRNl",
            "https://www.facebook.com/ReverieOriginal/posts/pfbid02aC3yU5S3sfdkgHsxx2v1FGmX2BMbsCTY9jKcKp974NHxo6ZavDuitFQEVDnz7Dq6l",
            "https://www.facebook.com/ReverieOriginal/posts/pfbid0soAgAF6yZcZ4q3w5GKLVKaQqoM32aemMhsyb1hv7sv3sULhwtTyUaiTzbuJNbkjtl",
            "https://www.facebook.com/cosminavram.ro/posts/pfbid08CBu7q4qU26JYo1G2zLszc66XByAwUyQcSMy3HbFmtbQmULavaAQN9BiSM179QXFl",
            "https://www.facebook.com/cosminavram.ro/posts/pfbid02KJREFNzJKxRw17N7Pg2DeGHdtUz9KbQSkW2LQ5nLCuJHFckudQGckUtmJy6wKb9Jl",
            "https://www.facebook.com/Azuroficial/posts/pfbid0wSpL31nCk1d3m89UxkFaKgc4cyWC1kYTovNtSVeQ86GLYWtEhxZGNkwksDy3eE5kl",
            "https://www.facebook.com/Azuroficial/posts/pfbid02d52wmxRTMPDTeVJCN1LPKonyLgHZvvdCqeyHFN7t2BJ3X1uuhiPMwnyoiBwXArbxl",
            "https://www.facebook.com/viata.si.culori/posts/pfbid024D6hdm9ZcBp5tDFGKrhuJ6i4b4h6poGHffbTGo7vLMe48XeZdizKMTiurGZHb4wEl",
            "https://www.facebook.com/viata.si.culori/posts/pfbid02YtUiZqA1SWyZCxSKsARWTnRAhASAAH7wMPusGHekHAGuDLDLJjVkGL2vJdakXNZql",
            "https://www.facebook.com/viata.si.culori/posts/pfbid02JFhSqvzhzenkNmW85npu61y4wBte2jp4rXkq8YiWJiW8EJyExDcbgX3W2oeoqhnEl",
            "https://www.facebook.com/viata.si.culori/posts/pfbid02kdQNMTvopujKEzDPK3uvU3n3piNUpedTyNhqwrqkYUCtUyQdRXF5LDi5vNEeEBVnl",
            "https://www.facebook.com/viata.si.culori/posts/pfbid024y7QLfwqaa48k7VerGuiDJF2SzBwVhb3btv2kxq8mDNdu6EponsmpVdEuDnZVUL4l",
            "https://www.facebook.com/viata.si.culori/posts/pfbid02mw5xduxdBs1funxi6q2bVLBc1GfkvTJZ42LXQbZz9TLLMv32xaMaHLb6YJKsFt2Dl",
            "https://www.facebook.com/viata.si.culori/posts/pfbid0sB24TAHaGc5JT6HSVnUHp2tPKPBqZp9U95hSGM3SYAQH8PyEmGb3eGh9GmYFb1r8l",
            "https://www.facebook.com/FloridedorOriginal/posts/pfbid021UCV67eafjwxjFXUHw4iMdAFnGxM6vyAxgafeM5vxoLxZ3ERTg3nVeELiSoWZceYl",
            "https://www.facebook.com/antihastagrezist/posts/pfbid0bCUoJVUY9FEXmqMbK3SyTbbWv1bywWGJsG2bRrmHFKumPym19GFDgHGR1MpBGtMMl",
            "https://www.facebook.com/antihastagrezist/posts/pfbid0rQgP2ss5MVLyBibRMZLVHh8kMwdSeKcznifymmttymKWFyurhPPT61RFhJUy1hAEl",
            "https://www.facebook.com/antihastagrezist/posts/pfbid0sBM631BztvRFz6V7wiXxNNj73M1sA6zwMdy8SBTLS7BRD2ooARLfM3eJis5yfbKYl",
            "https://www.facebook.com/antihastagrezist/posts/pfbid028rCAJNTLwu2wAZadisMxtgWaRLj6S2hD2fzVuTSJLHafTBYYSeo86yPFbQHq2rbMl",
            "https://www.facebook.com/meleagurileromaniei/posts/pfbid0ADUqgjHtNk2q5JogWNfTm1Doh2ZVsVp8Mqq4k26qC4tqndQ122PfnNseTG7rAZ2Fl",
            "https://www.facebook.com/ciumitarosie/posts/pfbid02RnyWABKDLv45eBj6HdGybGBa2EvCp4n3UH6BTVJnJeV1vRKLzFu1Uvc8q78qxrcdl",
            "https://www.facebook.com/ciumitarosie/posts/1154553883350190",
            "https://www.facebook.com/secretelevietiioficial/posts/pfbid02tTcFSMsUv9vjPC7JrpeyYn67WcwHqMFTwnF2S4HLzuKt2MYMFKRQwNm6DmVbN3ftl",
            "https://www.facebook.com/beznamintii/posts/pfbid02YJd7u1BseZuiA2v5NV5aEcZFfmZ3ovpqAWrgFnLZkrFRPoC9d2iV9FLNhxJvyWfEl",
            "https://www.facebook.com/beznamintii/posts/pfbid02ZbDNMRfwj2Nszfzo7oYJ7LxyfTBMM32oVoTYaXZZ7DWmDpQXizZwBL1uP6mxBqQ6l",
            "https://www.facebook.com/beznamintii/posts/pfbid0C3rK6tmGmdRuPK54VSEDXbQLw9p1QLzkoxWjrdiy6X42hpZ6V9uxBCvrJYTToetTl",
            "https://www.facebook.com/cinematografiavecheofficial/posts/pfbid02nDuRb8BvA383Gvi8EvQq6bpfy1ALjEGw6BABAgok2bVsQScrasQ1ALNKGmrHcXU8l",
            "https://www.facebook.com/naturacufrumusetile/posts/pfbid036wnDP1sw6QkT7whRaxc69P4wg5mcrVuhYexJ2xs9fjmwUWGUvKPsvcRhQsoFE2Rgl",
            "https://www.facebook.com/naturacufrumusetile/posts/pfbid02JZPZRXd6YxfjfWxChax89VCwxPTK4XGsSW8rVPAmoJSv38NJsLBUe1sBN2CyjDwql",
            "https://www.facebook.com/naturacufrumusetile/posts/pfbid0dy25TeaLgihoxQ1nP3Np9pF6sbdB6VSabjGCsof4rC1GUPDzFUx2HMY8PG8LYDe5l",
            "https://www.facebook.com/Trandafirulprietenieii/posts/997723482543328",
            "https://www.facebook.com/Trandafirulprietenieii/posts/pfbid02wMuELfvBXz9PQA1JEfofuyyHWJqmu5M7UKu2bitzTVUQM41Ax9E4i7auwtbMuxnVl",
            "https://www.facebook.com/A.frumoase/posts/pfbid0dMk8pBo8pkQ9SH6Z4F3V1RnnJGpeijYhQYPLr1M5uA7vjApMQj7yPn6gU4E5tomBl",
            "https://www.facebook.com/A.frumoase/posts/pfbid02ZHju6zv1BjKCGdVyKBP2sjs8wRt39shag2bz1fdu6vuxDRhi5vSfVmZBrtkiZys9l",
            "https://www.facebook.com/A.frumoase/posts/pfbid02jpfMwxYNKkSVeams8faDpFTupgqZLReNe46UtArrTYtUwNcTvrGGU7MGaqWXEYczl",
            "https://www.facebook.com/A.frumoase/posts/pfbid0J7j5w8ADpG2wQEao9faw3RZtPxt6Q9mbPje5z47yV4dytidNq1S6s988sy9MPKAKl",
            "https://www.facebook.com/A.frumoase/posts/pfbid0YcRemG7JQPovosnht8QXeQmwKpEr95E2Wg6vLb9Y4wjwVBZCj5LkhPFAzGZMujcnl",
            "https://www.facebook.com/A.frumoase/posts/pfbid07MGEVWE1EeAA7xUzGXfZkRzLYLa5GPUYDTa9TkyogSEcbrRMZvjgpXH8hNaNaRNHl",
            "https://www.facebook.com/A.frumoase/posts/pfbid02Pn1bB7zb4akEon85peR2cjyyEyMow7JjEHDBDF2o4ppEEj6Vv7UJAYVoDf4EeTrPl",
            "https://www.facebook.com/Un.Suflet/posts/pfbid02zXhfbm8symSjGd3Y2C67TsxUX5WRyqwA5HmzvBEtMH18iAXgGvcjj9thgoVRAeQsl",
            "https://www.facebook.com/ZilnicuIsus/posts/pfbid02WzPrhYPBeVL5zxrTMoEZqMYCJRspNkv6RZimKmz7TeRyj7n4TitCU222hGFQLuXul",
            "https://www.facebook.com/MuntiiCarpatiOfficial/posts/pfbid0DKgFK8c2BeXtyJyxy9qc1SGGBEYwA8Qke67NoZokLtDBC2RSeF4j71rPoKNhQvjul",
            "https://www.facebook.com/dragosteameaaaa/posts/pfbid02LBxzNGs2Md2qVKmtyP4GxGeJNqpQJSUniywJQZwN3T2gX4yBqSpA6Fv6D2kyRgdGl",
            "https://www.facebook.com/AdoreAcasa/posts/pfbid0yrPRG2LBCYiBMF53Lnbay71ab17W25UzxxGvvCPDJRRUW89RBZHrNrsRtm6Qk7FJl",
            "https://www.facebook.com/AdoreAcasa/posts/pfbid0Wa9jBmjNyhCCbi4zcEwADp3X6mYcYYToypKukrtt8JGNzBX3tjmkJXrqkRUD8W11l",
            "https://www.facebook.com/AdoreAcasa/posts/pfbid02EeZTLb3CKnS71U4ifxk8JffJkbdwvV1ztTsWk3S2KA7P85RYEDosLUoBj24p99Azl",
            "https://www.facebook.com/AdoreAcasa/posts/pfbid02mMEMy5zVPf8jKyYPR4e9sfqTz9bZijAYz8X2CbqpD6qnWQTQ7DwcrWF3WibNV9xTl",
            "https://www.facebook.com/AdoreAcasa/posts/pfbid0ptSMj83BEKbWCWfBq6yUshrX9NuFsrZo9knrSuQBcWJkj2DMrMc9t4vmGY4GSQgel",
            "https://www.facebook.com/AdoreAcasa/posts/pfbid02VJYRhx97ppvWfut1HZys3u4y9uexqovzh76M5MwcWqHrjCDFSRD3nZQGY1XACVTZl",
            "https://www.facebook.com/votrediverssemantt/posts/pfbid095WzUKS1e8UAUx2GpBYUbPFaqXetHmT7ekZHpXbCPsJxWdF7LxniYPUYaWGGwi3wl",
            "https://www.facebook.com/viralnews.romania/posts/pfbid0ZH5vqmeRCw6ZhFjqPw4v96y4i1tPsdXQ1q8hVNTyhW78cTUAeMt8g5n5UwXpDNZql",
            "https://www.facebook.com/viralnews.romania/posts/pfbid0WTALgoRoZbLthVhugh7Xj6pctgCh2Hm5rxGocE2uZsyG4N6Zk7gEV2UVaGgyPEVnl",
            "https://www.facebook.com/ISUSuceava/posts/pfbid0f1JYmXBw9Rk2dQE1JGVctNsx86YPF9nyeECM8GSo9MtLdqjkCDmR8xoi5dy7bxK4l",
            "https://www.facebook.com/ministeruldeinterne/posts/pfbid0di4J1tBDVyX5VCZpoztts2Jn2NcPsCkoexuKujFtipKdv8crcFmBBNs52CerFrxbl",
            "https://www.facebook.com/ministeruldeinterne/posts/pfbid022K1sCi1qedPd7aiRg62Mc936tvgGnENxSNApqFHjhbcqgUd1ka5q9fe1WZmMx9rEl",
            "https://www.facebook.com/ministeruldeinterne/posts/pfbid05hA8BvRES3Lqzb98cni4bZqXKMQjwcvcLBFhWAYsupKHs8GfpwNWMFRsQczdToVzl",
            "https://www.facebook.com/ministeruldeinterne/posts/pfbid02hTbw6Ni7NxbucLN5BaDcqQRaj23A29qv6sHVNiemPjbH4FV5TyE6nDA58UUtFnMGl",
            "https://www.facebook.com/ministeruldeinterne/posts/pfbid02EynGiFsXibHrZdHH5Ut42nZgyYJtpSp3AzPC1ZgvrYvRmUot5x3i9t1rwm98Bb2hl",
            "https://www.facebook.com/ministeruldeinterne/posts/pfbid02QLLWV7PsrjmtH535QQpqN83RtmNMFDomPRwQ3T9pZ1WZ8KMbK7vE7e7CV7zdYjxil",
            "https://www.facebook.com/ministeruldeinterne/posts/pfbid02UFPHLTVKwaRGPgihoYvMeZBCgd3EuamC6N5iSEnvQqoRDaKEsQKD1kicz7Dov3S4l",
            "https://www.facebook.com/ministeruldeinterne/posts/pfbid0v7iPmvwKK5EEcHzJikqvGJGFqtsiXncKFNbXEiR7EUAQYBRwtBUeshtubD3VFamZl",
            "https://www.facebook.com/ministeruldeinterne/posts/pfbid0rfCrP4tCfZpXKJMx7dF1iq1zSxADssSwLKSf7xHT9hxrJSdJ4WyNDqypyXBSJvGwl",
            "https://www.facebook.com/ministeruldeinterne/posts/pfbid02L9te6RXJigooJuw9k3WkHSByFPJhuL99MZSsgaZbKLFxUQ8oHcT73VxFPnFbefmtl",
            "https://www.facebook.com/isuhunedoara/posts/pfbid02Th4N2U639Bb8YgoUUFrZThcV7mUtvLhniwaM82VgVQx1n526BUzpBPmfpdodjsPTl",
            ]));
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