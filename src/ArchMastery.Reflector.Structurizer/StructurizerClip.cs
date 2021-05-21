using ArchMastery.Reflector.Core.Base;
using ArchMastery.Reflector.Core.Enums;

namespace ArchMastery.Reflector.Structurizer
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
