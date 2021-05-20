using System.Reflection;
using ArchMastery.Structurizer.Reflector.Common.Base;
using ArchMastery.Structurizer.Reflector.Common.Enums;

namespace ArchMastery.Structurizer.Reflector
{
    public class StructurizerClip : ClipBase
    {
        /// <inheritdoc />
        public override string AsOutput(Layers layers, params object[] options)
        {
            return "Structurizer Output Goes Here!";
        }
    }
}
