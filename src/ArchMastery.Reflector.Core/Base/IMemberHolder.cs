using ArchMastery.Reflector.Core.Enums;

// ReSharper disable MemberCanBePrivate.Global

#nullable enable

namespace ArchMastery.Reflector.Core.Base
{
    public interface IMemberHolder
    {
        (Layers layers, string segment, MemberTypes memberType) Segment { get; set; }
    }

    public class MemberHolder<TInfo> : IMemberHolder
        where TInfo : class
    {
        public MemberHolder(TInfo info, (Layers, string, MemberTypes memberType) segment)
        {
            Info = info;
            Segment = segment;
        }

        public TInfo Info { get; }
        public (Layers layers, string segment, MemberTypes memberType) Segment { get; set; }

        public string ToString(Layers layers)
        {
            return layers >= Segment.layers
                       ? Segment.segment
                       : string.Empty;
        }
    }
}
