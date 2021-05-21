#nullable enable
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using ArchMastery.Reflector.Core.Enums;

namespace ArchMastery.Reflector.Core.Base
{
    public interface IClip
    {
        ObservableCollection<IMemberHolder> Segments { get; set; }
        int Version { get; }
        string TypeName { get; }
        string? Namespace { get; }
        Assembly? Assembly { get; }
        void SegmentsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e);

        string AsOutput(Layers layers, params object[] options);
    }
}
