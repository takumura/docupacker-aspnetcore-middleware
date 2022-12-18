
using DocuPacker.JsonIndexPack;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocuPacker.Middleware.Services;

public class DocumentsWatchService : BackgroundService
{
    readonly ILogger<DocumentsWatchService> logger;
    readonly IOptions<DocumentsWatchServiceOptions> options;
    readonly IMarkdownConverterService service;

    public DocumentsWatchService(
        ILogger<DocumentsWatchService> _logger,
        IOptions<DocumentsWatchServiceOptions> _options,
        IMarkdownConverterService _service)
    {
        logger = _logger;
        options = _options;
        service = _service;
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        logger.LogInformation($"Start initial json indexpack conversion.");
        try
        {
            await service.ConvertAsync(options.Value.InputDir, options.Value.OutputDir, options.Value.IndexDir, null, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "");
        }

        while (!token.IsCancellationRequested)
        {
            var now = DateTime.Now;
            logger.LogInformation($"Watch documents updated after {now}");

            if (string.IsNullOrEmpty(options.Value.InputDir))
            {
                throw new ArgumentNullException(nameof(options.Value.InputDir));
            }

            // Not sure why, but getting TaskCompletionSource by method or local function doesn't work.
            // IChangeToken from Watch() method never completed... To be confirmed later.
            // register ChangeToken to detect file or directory changes under InputDir
            var fileProvider = new PhysicalFileProvider(options.Value.InputDir);
            var changeToken = fileProvider.Watch("**");
            TaskCompletionSource tcs = new();
            changeToken.RegisterChangeCallback(state => ((TaskCompletionSource)state!).TrySetResult(), tcs);

            using (token.Register(() =>
            {
                // this callback will be executed when token is cancelled
                logger.LogInformation($"token is cancelled!");
                tcs.TrySetCanceled();
            }))
            {
                // wait until any document change event happens fro file provider
                await tcs.Task.ConfigureAwait(false);
            }

            logger.LogInformation($"File change detected. Start json indexpack conversion for updated file(s).");
            try
            {
                await service.ConvertAsync(options.Value.InputDir, options.Value.OutputDir, options.Value.IndexDir, now, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "");
            }
        }
    }
}
