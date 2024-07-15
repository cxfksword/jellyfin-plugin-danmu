using System;

namespace Emby.Plugin.Danmu.Core
{
    using System.IO;
    using System.IO.Compression;

    public class InflaterInputStream : Stream
    {
        private Stream baseStream;
        private DeflateStream deflateStream;

        public InflaterInputStream(Stream compressedStream)
        {
            this.baseStream = compressedStream;

            // 读取和跳过ZLIB头。这里仅仅是一个简单的实现；
            // 在实际使用中，你可能需要更严格地处理和验证头部信息。
            byte[] header = new byte[2];
            int readBytes = baseStream.Read(header, 0, header.Length);
            if (readBytes != 2)
            {
                throw new InvalidDataException("Could not read the ZLIB header.");
            }
        
            // 如果你要检查头部的正确性，这里是进行的地方。
            // 比如，你可以检查压缩方法和进行校验。

            this.deflateStream = new DeflateStream(baseStream, CompressionMode.Decompress);
        }

        // DeflateStream的包装方法
        public override int Read(byte[] buffer, int offset, int count)
        {
            return deflateStream.Read(buffer, offset, count);
        }

        // 省略其他必须重写的Stream成员...
        // 下面的方法都需要根据具体需要实现。

        public override bool CanRead => deflateStream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush() => deflateStream.Flush();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                deflateStream?.Dispose();
                baseStream?.Dispose();
            }
            baseStream = null;
            deflateStream = null;
            base.Dispose(disposing);
        }
    }
}