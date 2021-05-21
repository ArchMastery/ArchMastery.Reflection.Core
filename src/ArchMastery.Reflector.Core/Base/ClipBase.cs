#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using ArchMastery.Reflector.Core.Enums;

namespace ArchMastery.Reflector.Core.Base
{
    public abstract class ClipBase : IClip
    {
        private static readonly object Padlock = new();
        private string? _cached;
        private int _rendered = -1;
        private TypeInfo? _objectTypeInfo;

        protected ClipBase()
        {
            Segments.CollectionChanged += SegmentsOnCollectionChanged;
        }

        public ObservableCollection<IMemberHolder> Segments { get; set; } = new();
        public int Version { get; private set; } = -1;

        public TypeInfo? ObjectTypeInfo
        {
            get => _objectTypeInfo ??= Type.GetType(TypeName)?.GetTypeInfo();
            set => _objectTypeInfo = value;
        }

        public string TypeName { get; init; } = "";
        public string? Namespace { get; init; } = "";
        public Assembly? Assembly { get; init; }

        public void SegmentsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Version++;
        }

        public virtual string ToString(Layers layers)
        {
            if (_rendered > -1 && Version > -1 && _rendered == Version) return _cached ?? string.Empty;

            var sb = new StringBuilder();
            lock (Padlock)
            {
                _cached = null;

                var toRender = (layers switch
                                {
                                    Layers.All => Segments.GroupBy(member => member.Segment.layers,
                                                                   member => member.Segment.segment),
                                    _ => Segments.Where(member => member.Segment.layers <= layers)
                                                 .GroupBy(member => member.Segment.layers,
                                                          member => member.Segment.segment)
                                }).OrderBy(g => (int) g.Key);

                foreach (var item in toRender)
                    item.ToList().ForEach(value =>
                                          {
                                              if (!string.IsNullOrWhiteSpace(value)) sb.AppendLine(value);
                                          });

                _rendered = Version;
                _cached = Environment.NewLine + sb.ToString().Trim();
            }

            return _cached;
        }

        public override string ToString()
        {
            return ToString(Layers.All);
        }

        public abstract string AsOutput(Layers layers, params object[] options);
    }
}
