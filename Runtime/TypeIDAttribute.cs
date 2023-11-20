#nullable enable
using UnityEngine.Scripting;

namespace Popcron
{
    /// <summary>
    /// Makes this type available through <see cref="TypeTable"/>.
    /// </summary>
    public class TypeIDAttribute : PreserveAttribute
    {
        public readonly ushort typeId;

        public TypeIDAttribute(ushort typeId)
        {
            this.typeId = typeId;
        }
    }
}