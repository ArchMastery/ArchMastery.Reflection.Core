using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ArchMastery.Structurizer.Reflector.Common.Base;
using ArchMastery.Structurizer.Reflector.Common.Enums;

namespace ArchMastery.Structurizer.Reflector
{
    public class StructuredWriter<TClip, TGenerator>
    where TClip : ClipBase, new()
    where TGenerator : GeneratorBase<TClip>, new()
    {
        private readonly TGenerator _generator;

        public StructuredWriter()
        {
            _generator = new TGenerator();
        }

        public Encoding Encoding { get; } = Encoding.UTF8;

        public FileStream WriteFile(string path, IEnumerable<(TClip clip, Layers layer)> clips)
        {
            var stream = new FileStream(path, FileMode.Append | FileMode.OpenOrCreate);

            _generator.BuildDocument(stream, clips);

            stream.Flush();

            return stream;
        }

        public async Task<FileStream> WriteFileAsync(string path,
                                                     IEnumerable<(TClip clip, Layers layer)> clips)
        {
            var stream = new FileStream(path, FileMode.Append | FileMode.OpenOrCreate);

            _generator.BuildDocument(stream, clips);

            await stream.FlushAsync();

            return stream;
        }

        public Stream WriteStream(Stream stream, IEnumerable<(TClip clip, Layers layer)> clips)
        {
            _generator.BuildDocument(stream, clips);

            stream.Flush();

            return stream;
        }

        public async Task<Stream> WriteStreamAsync(Stream stream,
                                                   IEnumerable<(TClip clip, Layers layer)> clips)
        {
            await _generator.BuildDocumentAsync(stream, clips);

            await stream.FlushAsync();

            return stream;
        }


    }
}
