using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Core;

public interface IFileSystem
{
    bool Exists(string? path);
    DateTime GetLastWriteTime(string path);
    Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken);

    Task WriteAllTextAsync(string path, string? contents, Encoding encoding, CancellationToken cancellationToken = default);
}
