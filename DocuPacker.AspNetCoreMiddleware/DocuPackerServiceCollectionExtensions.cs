
using DocuPacker.JsonIndexPack;
using DocuPacker.JsonIndexPack.Utils;
using DocuPacker.Middleware.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace DocuPacker.Middleware;

/// <summary>
/// Extensions for configuring DocuPacker
/// </summary>
public static class DocuPackerServiceCollectionExtensions
{
    /// <summary>
    /// Configures DocuPacker
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    public static IServiceCollection AddDocuPacker(this IServiceCollection services, Action<DocumentsWatchServiceOptions> configureOption)
    {
        services.TryAddSingleton<IMarkdownConverterService, MarkdownConverterService>();
        services.TryAddSingleton<IPollyRetryPolicy, PollyRetryPolicy>();

        services.Configure(configureOption);

        services.TryAddSingleton<IHostedService, DocumentsWatchService>();

        return services;
    }
}
