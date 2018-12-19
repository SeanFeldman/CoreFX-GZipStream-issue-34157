namespace Repro
{
    using System.IO;
    using System.IO.Compression;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public class Repro
    {
        private readonly ITestOutputHelper output;

        public Repro(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async Task Should_compress()
        {
            var payload = new string('A', 1500);
            var bytes = Encoding.UTF8.GetBytes(payload);
            var compressed = await GzipCompressor(bytes);
            var hex = GetHexaRepresentation(compressed);

#if NETFRAMEWORK
            Assert.Equal("1f8b080000000000040073741c05a360148c825130dc00004d6f6cebdc050000", hex);
#else
            Assert.Equal("1f8b080000000000000b73741c05a321301a02a321301a028ec30c00004d6f6cebdc050000", hex);
#endif

            output.WriteLine(hex);
        }

        private string GetHexaRepresentation(byte[] result)
        {
            var builder = new StringBuilder(result.Length);
            foreach (var b in result)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }

        static async Task<byte[]> GzipCompressor(byte[] bytes)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                {
                    await gzipStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                }

                return memoryStream.ToArray();
            }
        }

        static async Task<byte[]> GzipDecompressor(byte[] bytes)
        {
            using (var gzipStream = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress))
            {
                const int size = 4096;
                var buffer = new byte[size];
                using (var memory = new MemoryStream())
                {
                    int count;
                    do
                    {
                        count = await gzipStream.ReadAsync(buffer, 0, size).ConfigureAwait(false);
                        if (count > 0)
                        {
                            await memory.WriteAsync(buffer, 0, count).ConfigureAwait(false);
                        }
                    } while (count > 0);

                    return memory.ToArray();
                }
            }
        }
    }
}
