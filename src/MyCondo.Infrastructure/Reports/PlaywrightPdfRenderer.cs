using Microsoft.Playwright;

namespace MyCondo.Infrastructure.Reports;

/// <summary>Owns the headless Chromium browser instance used to render report PDFs. Registered as a
/// singleton: launching a browser is expensive (hundreds of ms), so one instance is launched lazily
/// on first use and kept alive for the app's lifetime; each export opens and closes its own page
/// against the shared browser, which is Playwright's documented safe-for-concurrent-use pattern.
/// Requires the Playwright browser binaries to be installed on the host/image
/// (`playwright.ps1 install chromium` from the build output directory) — this is a one-time
/// per-environment setup step, not something this class can do for itself.</summary>
public sealed class PlaywrightPdfRenderer : IAsyncDisposable
{
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async Task<byte[]> RenderPdfAsync(string html, CancellationToken cancellationToken)
    {
        IBrowser browser = await GetBrowserAsync(cancellationToken);
        await using IPage page = await browser.NewPageAsync();
        await page.SetContentAsync(html, new PageSetContentOptions { WaitUntil = WaitUntilState.NetworkIdle });
        return await page.PdfAsync(new PagePdfOptions
        {
            Format = "A4",
            PrintBackground = true,
            Margin = new Margin { Top = "16mm", Bottom = "16mm", Left = "12mm", Right = "12mm" },
        });
    }

    private async Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken)
    {
        if (_browser is not null)
        {
            return _browser;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_browser is not null)
            {
                return _browser;
            }

            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            return _browser;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();
        _initLock.Dispose();
    }
}
