using System.IO;
using System.Threading.Tasks;

namespace ArchMastery.Reflector.Core
{
    public static class StreamExtensions
    {
        public static void Write(this Stream stream, byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        public static Task WriteAsync(this Stream stream, byte[] bytes)
        {
            return stream.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
