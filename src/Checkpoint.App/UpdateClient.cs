// Descarga instaladores y comprueba tamaño, host y SHA-256 antes de devolver el archivo.
// Las descargas incompletas se descartan y no se convierten en instaladores utilizables.

using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Checkpoint.Core;

namespace Checkpoint.App;

internal sealed class UpdateClient : IDisposable
{
    private readonly HttpClient http;

    internal UpdateClient(HttpMessageHandler? handler = null)
    {
        http = new(handler ?? new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromMinutes(10),
        };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Checkpoint-updater/0.8");
    }

    private async Task<HttpResponseMessage> Get(Uri uri, CancellationToken cancellation)
    {
        for (int redirect = 0; redirect < 5; redirect++)
        {
            if (
                uri.Scheme != "https"
                || uri.UserInfo.Length != 0
                || uri.Host
                    is not (
                        "api.github.com"
                        or "github.com"
                        or "release-assets.githubusercontent.com"
                        or "objects.githubusercontent.com"
                    )
            )
                throw new InvalidDataException(
                    I18n.T("La dirección de actualización no es válida.")
                );
            var response = await http.GetAsync(
                uri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellation
            );
            if ((int)response.StatusCode is >= 300 and < 400)
            {
                var next = response.Headers.Location;
                response.Dispose();
                if (next is null)
                    break;
                uri = new Uri(uri, next);
                continue;
            }
            if (!response.IsSuccessStatusCode)
            {
                response.Dispose();
                throw new InvalidOperationException(
                    I18n.T(
                        "No se pudieron consultar las actualizaciones. Puedes seguir usando esta versión."
                    )
                );
            }
            return response;
        }
        throw new InvalidDataException(I18n.T("La dirección de actualización no es válida."));
    }

    private async Task<string> Text(Uri uri, int maximum, CancellationToken cancellation)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        cancellation = timeout.Token;
        using var response = await Get(uri, cancellation);
        using var stream = await response.Content.ReadAsStreamAsync(cancellation);
        using var buffer = new MemoryStream();
        byte[] bytes = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(bytes, cancellation)) > 0)
        {
            if (buffer.Length + count > maximum)
                throw new InvalidDataException(
                    I18n.T("La respuesta de actualización no es válida.")
                );
            await buffer.WriteAsync(bytes.AsMemory(0, count), cancellation);
        }
        return Encoding.UTF8.GetString(buffer.ToArray()).TrimStart('\uFEFF');
    }

    internal async Task<AppUpdate?> Check(
        Version current,
        string runtime,
        CancellationToken cancellation = default
    )
    {
        return AppUpdates.Select(
            await Text(new(AppUpdates.Releases), 4_000_000, cancellation),
            current,
            runtime
        );
    }

    internal async Task<string> Download(
        AppUpdate update,
        string folder,
        IProgress<int>? progress = null,
        CancellationToken cancellation = default
    )
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(TimeSpan.FromMinutes(15));
        cancellation = timeout.Token;
        AppUpdates.Validate(update);
        AppUpdates.Checksum(await Text(update.Checksum, 4096, cancellation), update);
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, update.Name),
            temporary = file + ".partial";
        try
        {
            using (var response = await Get(update.Installer, cancellation))
            using (var input = await response.Content.ReadAsStreamAsync(cancellation))
            await using (
                var output = new FileStream(
                    temporary,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    true
                )
            )
            {
                byte[] bytes = new byte[81920];
                long received = 0;
                int count;
                while ((count = await input.ReadAsync(bytes, cancellation)) > 0)
                {
                    received += count;
                    if (received > update.Size)
                        throw new InvalidDataException(
                            I18n.T(
                                "La actualización no supera la comprobación de integridad. No se instalará."
                            )
                        );
                    await output.WriteAsync(bytes.AsMemory(0, count), cancellation);
                    progress?.Report((int)(received * 100 / update.Size));
                }
                if (received != update.Size)
                    throw new InvalidDataException(
                        I18n.T(
                            "La actualización no supera la comprobación de integridad. No se instalará."
                        )
                    );
            }
            using (var stream = File.OpenRead(temporary))
                if (
                    !Convert
                        .ToHexString(await SHA256.HashDataAsync(stream, cancellation))
                        .Equals(update.Digest, StringComparison.OrdinalIgnoreCase)
                )
                    throw new InvalidDataException(
                        I18n.T(
                            "La actualización no supera la comprobación de integridad. No se instalará."
                        )
                    );
            File.Move(temporary, file, true);
            await File.WriteAllTextAsync(
                file + ":Zone.Identifier",
                "[ZoneTransfer]\r\nZoneId=3\r\nHostUrl=" + update.Installer.AbsoluteUri + "\r\n",
                cancellation
            );
            return file;
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    public void Dispose() => http.Dispose();
}
