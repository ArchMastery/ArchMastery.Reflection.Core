using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArchMastery.Structurizer.Reflector
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
