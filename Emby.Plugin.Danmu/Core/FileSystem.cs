using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Emby.Plugin.Danmu.Core
{
    public class FileSystem:IFileSystem
    {
        public static IFileSystem instant = new FileSystem();
        
        public async Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default)
        {
            // 使用FileStream创建文件以进行异步写操作
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                // 异步写入字节数据
                await fs.WriteAsync(bytes, 0, bytes.Length);
            }
            // return File.WriteAllBytesAsync(path, bytes, cancellationToken);
        }

        public async Task WriteAllTextAsync(string path, string? contents, Encoding encoding, CancellationToken cancellationToken = default)
        {
            // 使用StreamWriter进行异步写入
            using (var writer = new StreamWriter(path, false, encoding))
            {
                await writer.WriteAsync(contents);
            }
            // return File.WriteAllTextAsync(path, contents, cancellationToken);
        }

        public DateTime GetLastWriteTime(string path)
        {
            return File.GetLastWriteTime(path);
        }

        public bool Exists(string? path)
        {
            return File.Exists(path);
        }
    }
}