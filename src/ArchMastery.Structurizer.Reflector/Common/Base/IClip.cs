#nullable enable
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using ArchMastery.Structurizer.Reflector.Common.Enums;

namespace ArchMastery.Structurizer.Reflector.Common.Base
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
