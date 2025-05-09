using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace WebView2MultiView;

public class PostCommentScraper : Form
{
    private readonly List<string> _urlsToScrape;
    private WebView2 _webView;
    private Button _nextPageButton;
    private Button _scrapeCommentsButton;
    private Label _statusLabel;
    private int _currentPage = 0;
    private const bool shouldShowAllComments = false; // Set to false to skip showing all comments

    public PostCommentScraper(string[] urls)
    {
        _urlsToScrape = [.. urls];

        var screenSize = Screen.PrimaryScreen!.Bounds.Size;

        WindowState = FormWindowState.Normal;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(0, 0);
        // Half the screen size, left pane
        Size = new Size(screenSize.Width / 2, screenSize.Height - 50);

        InitializeUI();
        _ = InitializeWebViewAsync(); // fire and forget
    }

    private async Task InitializeWebViewAsync()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyAppWebView2-WithExtension"
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
        // Add Tampermonkey extension
        // var extensionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Extensions", "Tampermonkey.crx");
            // await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(TMShowAllComments()).ConfigureAwait(false);


    }

    private static string TMShowAllComments() => @"
            (function() {
            'use strict';
        
            // CHANGE THIS VALUE TO 'newest' IF YOU WANT NEWEST COMMENTS INSTEAD
            const sortPreference = 'all'; // Options: 'newest', 'all'
        
            const processedUrls = new Set();
            const processedButtons = new WeakSet();
        
            const sortButtonTexts = {
                newest: [
                    'newest', 'terbaru', 'most recent', 'recent',
                    'más recientes', 'reciente',
                    'plus récents', 'récent',
                    'neueste', 'aktuellste',
                    'mais recentes', 'recente',
                    'più recenti', 'recente',
                    'nieuwste', 'recent',
                    'новейшие', 'недавние',
                    '最新', '新的',
                    '最新', '新しい',
                    'الأحدث', 'حديث',
                    'नवीनतम', 'हाल का'
                ],
                all: [
                    'all comments', 'semua komentar', 'all',
                    'todos los comentarios', 'todos',
                    'tous les commentaires', 'tous',
                    'alle kommentare', 'alle',
                    'todos os comentários', 'todos',
                    'tutti i commenti', 'tutti',
                    'alle reacties', 'alle',
                    'все комментарии', 'все',
                    '所有评论', '全部',
                    'すべてのコメント', 'すべて',
                    'كل التعليقات', 'الكل',
                    'सभी टिप्पणियां', 'सभी'
                ],
                default: [
                    'most relevant', 'paling relevan', 'relevan', 'most popular', 'komentar teratas', 'oldest',
                    'más relevantes', 'relevante', 'más populares',
                    'plus pertinents', 'pertinent', 'plus populaires',
                    'relevanteste', 'beliebteste',
                    'mais relevantes', 'relevante', 'mais populares',
                    'più rilevanti', 'rilevante', 'più popolari',
                    'meest relevant', 'relevant', 'populairste',
                    'наиболее релевантные', 'популярные',
                    '最相关', '最热门',
                    '最も関連性の高い', '人気',
                    'الأكثر صلة', 'الأكثر شعبية',
                    'सबसे उपयुक्त', 'सबसे लोकप्रिय'
                ]
            };
        
            const blockListTexts = [
                'post filters',
                'filter posts'
            ];
        
            function shouldSkipButton(button) {
                if (!button || !button.textContent) return true;
        
                const text = button.textContent.toLowerCase().trim();
        
                if (blockListTexts.some(blockText => text === blockText)) {
                    return true;
                }
        
                const parentDialog = button.closest('[role=\'dialog\']');
                if (parentDialog && parentDialog.textContent &&
                    parentDialog.textContent.toLowerCase().includes('post filter')) {
                    return true;
                }
        
                let parent = button.parentElement;
                for (let i = 0; i < 3 && parent; i++) {
                    if (parent.getAttribute && parent.getAttribute('aria-label') === 'Filters') {
                        return true;
                    }
                    parent = parent.parentElement;
                }
        
                return false;
            }
        
            function findAndClickSortButtons() {
                const potentialButtons = document.querySelectorAll('div[role=\'button\'], span[role=\'button\']');
        
                for (const button of potentialButtons) {
                    if (!button || processedButtons.has(button)) continue;
        
                    if (shouldSkipButton(button)) {
                        processedButtons.add(button);
                        continue;
                    }
        
                    const text = button.textContent.toLowerCase().trim();
        
                    if (sortButtonTexts.default.some(sortText => text.includes(sortText))) {
                        try {
                            processedButtons.add(button);
                            button.click();
        
                            setTimeout(() => {
                                const menuItems = document.querySelectorAll('[role=\'menuitem\'], [role=\'menuitemradio\'], [role=\'radio\']');
                                const targetTexts = sortPreference === 'newest' ? sortButtonTexts.newest : sortButtonTexts.all;
        
                                if (menuItems.length === 0) {
                                    processedButtons.delete(button);
                                    return;
                                }
        
                                let found = false;
                                let targetItem = null;
        
                                for (const item of menuItems) {
                                    if (!item.textContent) continue;
                                    const itemText = item.textContent.toLowerCase().trim();
                                    if (targetTexts.some(target => itemText === target)) {
                                        targetItem = item;
                                        found = true;
                                        break;
                                    }
                                }
        
                                if (!found) {
                                    for (const item of menuItems) {
                                        if (!item.textContent) continue;
                                        const itemText = item.textContent.toLowerCase().trim();
                                        if (targetTexts.some(target => itemText.includes(target))) {
                                            targetItem = item;
                                            found = true;
                                            break;
                                        }
                                    }
                                }
        
                                if (!found) {
                                    if (sortPreference === 'newest' && menuItems.length >= 2) {
                                        targetItem = menuItems[1];
                                        found = true;
                                    } else if (sortPreference === 'all' && menuItems.length >= 3) {
                                        targetItem = menuItems[2];
                                        found = true;
                                    } else if (menuItems.length >= 1) {
                                        targetItem = menuItems[menuItems.length - 1];
                                        found = true;
                                    }
                                }
        
                                if (found && targetItem) {
                                    targetItem.click();
                                } else {
                                    processedButtons.delete(button);
                                }
                            }, 500);
                        } catch (error) {
                            processedButtons.delete(button);
                        }
                    }
                }
            }
        
            function setupRequestIntercepts() {
                const paramMappings = {
                    'newest': {
                        'feedback_filter': 'stream',
                        'order_by': 'time',
                        'comment_order': 'chronological',
                        'filter': 'stream',
                        'comment_filter': 'stream'
                    },
                    'all': {
                        'feedback_filter': 'all',
                        'order_by': 'ranked',
                        'comment_order': 'ranked_threaded',
                        'filter': 'all',
                        'comment_filter': 'all'
                    }
                };
                const params = paramMappings[sortPreference];
        
                const originalOpen = XMLHttpRequest.prototype.open;
                XMLHttpRequest.prototype.open = function(method, url) {
                    if (typeof url === 'string' && !processedUrls.has(url)) {
                        if ((url.includes('/api/graphql/') || url.includes('feedback')) &&
                            (url.includes('comment') || url.includes('Comment'))) {
                            let modifiedUrl = url;
                            for (const [key, value] of Object.entries(params)) {
                                if (modifiedUrl.includes(`${key}=`)) {
                                    modifiedUrl = modifiedUrl.replace(new RegExp(`${key}=([^&]*)`, 'g'), `${key}=${value}`);
                                } else {
                                    modifiedUrl += (modifiedUrl.includes('?') ? '&' : '?') + `${key}=${value}`;
                                }
                            }
                            processedUrls.add(modifiedUrl);
                            return originalOpen.apply(this, [method, modifiedUrl]);
                        }
                    }
                    return originalOpen.apply(this, arguments);
                };
        
                if (window.fetch) {
                    const originalFetch = window.fetch;
                    window.fetch = function(resource, init) {
                        if (resource && typeof resource === 'string' && !processedUrls.has(resource)) {
                            if ((resource.includes('/api/graphql/') || resource.includes('feedback')) &&
                                (resource.includes('comment') || resource.includes('Comment'))) {
                                let modifiedUrl = resource;
                                for (const [key, value] of Object.entries(params)) {
                                    if (modifiedUrl.includes(`${key}=`)) {
                                        modifiedUrl = modifiedUrl.replace(new RegExp(`${key}=([^&]*)`, 'g'), `${key}=${value}`);
                                    } else {
                                        modifiedUrl += (modifiedUrl.includes('?') ? '&' : '?') + `${key}=${value}`;
                                    }
                                }
                                processedUrls.add(modifiedUrl);
                                return originalFetch.call(this, modifiedUrl, init);
                            }
                        }
                        return originalFetch.apply(this, arguments);
                    };
                }
            }
        
            function initialize() {
                setupRequestIntercepts();
        
                setTimeout(findAndClickSortButtons, 2000);
        
                setInterval(findAndClickSortButtons, 5000);
        
                let lastUrl = location.href;
                new MutationObserver(() => {
                    if (location.href !== lastUrl) {
                        lastUrl = location.href;
                        setTimeout(findAndClickSortButtons, 2000);
                        processedUrls.clear();
                    }
                }).observe(document, {subtree: true, childList: true});
            }
        
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', initialize);
            } else {
                initialize();
            }
        })();";

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
            Text = "Load Next Page",
            Width = 150,
            Left = 10,
            Top = 10
        };
        _nextPageButton.Click += async (_, _) => await LoadNextPageAsync();
        panel.Controls.Add(_nextPageButton);

        _scrapeCommentsButton = new Button
        {
            Text = "Scrape Comments",
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
                MessageBox.Show("No more pages to load.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            return;
        }

        var url = _urlsToScrape[_currentPage++];

        // Check if the URL is already hashed and exists in the file system
        var hash = GetHashedUrl(url);
        var fileName = $"comments/comments_{hash}.json";
        if (File.Exists(fileName))
        {
            await InvokeAsync(() =>
            {
                _nextPageButton.Focus();
                _nextPageButton.PerformClick();
            });
            return;
        }

        await InvokeAsync(() =>
        {
            _statusLabel.Text = $"Loading page {_currentPage} of {_urlsToScrape.Count}...";
            _webView.Source = new Uri(url);
            _webView.CoreWebView2.OpenDevToolsWindow();
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
        var mustScrapeComments = true;

        if (File.Exists(fileName))
        {
            mustScrapeComments = false;
            // Skip this block to always rescrape if needed
        }
        else
        {
            await Task.Delay(2000);
        }

        if (_webView.CoreWebView2 == null)
        {
            await InvokeAsync(() =>
            {
                MessageBox.Show("WebView not ready yet.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            bool isContentUnavailable = false;
            var tries = 3;
            while (tries-- > 0)
            {
                var script = @"
                (function() {
                    return Array.from(document.querySelectorAll('*'))
                        .some(el => el.innerText === 'This content isn\'t available right now' ||
                                    el.innerText === 'This page isn\'t available right now');
                })();
            ";
                var result = await _webView.ExecuteScriptAsync(script);

                isContentUnavailable = bool.Parse(result);

                if (isContentUnavailable)
                {
                    break;
                }

                await Task.Delay(500);
            }

            if (isContentUnavailable)
            {
                await InvokeAsync(() =>
                {
                    // Write a placeholder JSON file (`{}`)
                    File.WriteAllText(fileName, "{}");

                    _nextPageButton.Focus();
                    _nextPageButton.PerformClick();
                });
                return;
            }


            var postText = string.Empty;
            var triesLeft = 30;

            while (triesLeft-- > 0)
            {
                postText = await _webView.ExecuteScriptAsync(@"
                Array.from(document.querySelectorAll('div[role=\'dialog\'] div[data-ad-rendering-role=\'story_message\'] > div[data-ad-preview=\'message\']'))
                    .map(div => div.innerText);
            ");

                if (!string.IsNullOrWhiteSpace(postText))
                    break;

                await Task.Delay(100);
            }

            if (mustScrapeComments)
            {
                var tcs = new TaskCompletionSource<string>();

                if (shouldShowAllComments)
                {
                    void ShowAllHandler(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
                    {
                        var message = args.TryGetWebMessageAsString();
                        if (message == "selected_all")
                        {
                            _webView.CoreWebView2.WebMessageReceived -= ShowAllHandler;
                            tcs.SetResult(message);
                        }
                    }

                    _webView.CoreWebView2.WebMessageReceived += ShowAllHandler;

                    await _webView.ExecuteScriptAsync(JSShowAllComments());

                    await tcs.Task;

                    await Task.Delay(2000);

                    tcs = new TaskCompletionSource<string>();
                }

                void Handler(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
                {
                    var message = args.TryGetWebMessageAsString();
                    if (!string.IsNullOrEmpty(message) && message.StartsWith("[{"))
                    {
                        _webView.CoreWebView2.WebMessageReceived -= Handler;
                        tcs.SetResult(message);
                    }
                }

                _webView.CoreWebView2.WebMessageReceived += Handler;

                await _webView.ExecuteScriptAsync(JSScrollExpandReadComments());

                var json = await tcs.Task;

                var comments = JsonSerializer.Deserialize<List<Comment>>(json);

                if (comments != null)
                {
                    await InvokeAsync(() =>
                    {
                        _statusLabel.Text = $"Scraped {comments.Count} comments. Current page: {_currentPage} of {_urlsToScrape.Count}";

                        var url = _webView.Source.ToString();
                        var jsonOut = JsonSerializer.Serialize(new { url, comments }, new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        });

                        File.WriteAllText(fileName, jsonOut);

                        _nextPageButton.Focus();
                        _nextPageButton.PerformClick();
                    });
                }
                else
                {
                    await InvokeAsync(() => _statusLabel.Text = "No comments found.");
                }
            }
            else
            {
                var json = File.ReadAllText(fileName);
                var root = JsonSerializer.Deserialize<JsonObject>(json);
                root!["message"] = postText;

                var updatedJson = JsonSerializer.Serialize(root, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                File.WriteAllText(fileName, updatedJson);

                await InvokeAsync(() =>
                {
                    _statusLabel.Text = $"✅ Appended post message to {fileName}";
                    _nextPageButton.Focus();
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


    private static string JSPressCloseButton() => @"document.querySelector('div[aria-label=\'Close\'][role=\'button\']').click();";

    private static string JSShowAllComments() => @"
        (async function () {
            function clickReactByTextSequence(items, delay = 500) {
                return new Promise((resolve) => {
                    let i = 0;
                    let success = true;

                    function clickNext() {
                        if (i >= items.length) return resolve(success);

                        const [selector, text] = items[i++];
                        const el = Array.from(document.querySelectorAll(selector))
                            .find(e => e.textContent.includes(text));

                        if (!el) {
                            console.warn(`Element '${text}' not found`);
                            success = false;
                            return resolve(false);
                        }

                        // Scroll to the element
                        el.scrollIntoView({ behavior: 'smooth', block: 'center' });

                        const key = Object.keys(el).find(k => k.startsWith('__reactProps$'));
                        if (!key || !el[key]?.onClick) {
                            console.warn(`React onClick for '${text}' not found`);
                            success = false;
                            return resolve(false);
                        }

                        el[key].onClick({
                            type: 'click',
                            nativeEvent: new MouseEvent('click'),
                            currentTarget: el,
                            target: el,
                            stopPropagation() {},
                            preventDefault() {}
                        });

                        setTimeout(clickNext, delay);
                    }

                    clickNext();
                });
            }

            return await clickReactByTextSequence([
                ['div[aria-haspopup=\'menu\'][role=\'button\']', 'Most relevant'],
                ['[role=\'menuitem\']', 'All comments']
            ], 800).then(() => {
                window.chrome.webview.postMessage('selected_all');
            });
        })()
        ";



    private static string JSScrollAndExpandReplied() => @"
        (function () {
            let containerSelector = '[role=\'dialog\'] > div > div > div > div > div > div > div';

            function delay(ms) {
                return new Promise(resolve => setTimeout(resolve, ms));
            }

            function clickReplyButtons() {
                const buttons = Array.from(document.querySelectorAll('div[role=\'button\']')).filter(e =>
                    (e.textContent.includes('Replies') ||
                    e.textContent.includes('replied') ||
                    e.textContent.includes('View all') ||
                    e.textContent.includes('View 1 reply')) &&
                    !e.hasAttribute('aria-disabled')
                );
            
                buttons.forEach((el, i) => {
                    setTimeout(() => {
                        const key = Object.keys(el).find(k => k.startsWith('__reactProps$'));
            
                        if (key && el[key]?.onClick) {
                            // Use React-style onClick
                            el[key].onClick({
                                type: 'click',
                                nativeEvent: new MouseEvent('click'),
                                currentTarget: el,
                                target: el,
                                stopPropagation() {},
                                preventDefault() {}
                            });
                        } else {
                            // Fallback to native DOM click
                            console.warn('No React onClick found, falling back to native click:', el);
                            try {
                                el.click();
                            } catch (err) {
                                console.error('Failed to click element:', err);
                            }
                        }
                    }, i * 150);
                });
            }
            

            function waitForContainer(callback) {
                const interval = setInterval(() => {
                    let el = document.querySelector(containerSelector)?.children[1];
                    if (!el) {
                        containerSelector += ' > div';
                    }
                    if (el) {
                        clearInterval(interval);
                        callback(el);
                    }
                }, 500);
            }

            waitForContainer(async function (container) {
                console.log('Comment container found:', container);

                let lastHeight = 0;
                let stableCount = 0;
                const maxStableChecks = 5;

                const scrollInterval = setInterval(async () => {
                    container.scrollTop += 500;
                    clickReplyButtons(); // Call click logic after each scroll

                    const newHeight = container.scrollHeight;

                    if (newHeight === lastHeight) {
                        stableCount++;
                        console.log(`No new comments... (${stableCount}/${maxStableChecks})`);
                    } else {
                        stableCount = 0;
                        lastHeight = newHeight;
                        console.log('Scrolling more...');
                    }

                    if (stableCount >= maxStableChecks) {
                        console.log('Reached the bottom — all comments loaded.');
                        clearInterval(scrollInterval);
                        window.chrome.webview.postMessage('scrolling_done');
                    }
                }, 1000);
            });
        })();
    ";

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

    private static string JSScrollExpandReadComments() => @"
        (function () {
            let containerSelector = '[role=\'dialog\'] > div > div > div > div > div > div > div';

            function delay(ms) {
                return new Promise(resolve => setTimeout(resolve, ms));
            }

            function clickReplyButtons() {
                const buttons = Array.from(document.querySelectorAll('div[role=\'button\']')).filter(e =>
                    (e.textContent.includes('Replies') ||
                    e.textContent.includes('replied') ||
                    e.textContent.includes('View all') ||
                    e.textContent.includes('View 1 reply')) &&
                    !e.hasAttribute('aria-disabled')
                );

                buttons.forEach((el, i) => {
                    setTimeout(() => {
                        const key = Object.keys(el).find(k => k.startsWith('__reactProps$'));
                        if (key && el[key]?.onClick) {
                            el[key].onClick({
                                type: 'click',
                                nativeEvent: new MouseEvent('click'),
                                currentTarget: el,
                                target: el,
                                stopPropagation() {},
                                preventDefault() {}
                            });
                        } else {
                            try {
                                el.click();
                            } catch (err) {
                                console.warn('Native click failed:', err);
                            }
                        }
                    }, i * 150);
                });
            }

            function extractComments() {
                const results = [];

                document.querySelectorAll('[aria-label^=\'Comment by\'], [aria-label^=\'Reply by\']').forEach(comment => {
                    const ariaLabel = comment.getAttribute('aria-label');
                    let name = '';
                    if (ariaLabel) {
                        let temp = ariaLabel.replace(/^Comment by /, '');
                        const lastSpaceIndex = temp.lastIndexOf(' ');
                        if (lastSpaceIndex > 0) {
                            name = temp.substring(0, lastSpaceIndex).trim();
                        }
                    }

                    const allSpans = comment.querySelectorAll('[dir=\'auto\']');
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

                return results;
            }

            function waitForContainer(callback) {
                const interval = setInterval(() => {
                    let el = document.querySelector(containerSelector)?.children[1];
                    if (!el) {
                        containerSelector += ' > div';
                    }
                    if (el) {
                        clearInterval(interval);
                        callback(el);
                    }
                }, 500);
            }

            waitForContainer(async function (container) {
                console.log('Comment container found:', container);

                let lastHeight = 0;
                let stableCount = 0;
                const maxStableChecks = 5;
                const allResults = [];

                const scrollInterval = setInterval(async () => {
                    container.scrollTop += 1000;
                    clickReplyButtons();

                    await delay(500); // Give time for replies to load

                    const newComments = extractComments();
                    for (const comment of newComments) {
                        if (!allResults.some(c => c.name === comment.name && c.text === comment.text)) {
                            allResults.push(comment);
                        }
                    }

                    const newHeight = container.scrollHeight;

                    if (newHeight === lastHeight) {
                        stableCount++;
                        console.log('No new comments... (' + stableCount + '/' + maxStableChecks + ')');
                    } else {
                        stableCount = 0;
                        lastHeight = newHeight;
                        console.log('Scrolling more...');
                    }

                    console.log('New comments:', newComments.length, 'Total:', allResults.length);

                    if (stableCount >= maxStableChecks) {
                        console.log('Reached the bottom — all comments loaded.');
                        clearInterval(scrollInterval);
                        window.chrome.webview.postMessage(JSON.stringify(allResults));
                    }
                }, 1500);
            });
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
