#nullable enable
using System;
using UnityEngine;

namespace Popcron
{
    /// <summary>
    /// Displays a dropdown for <see cref="int"/> and <see cref="ushort"/> fields.
    /// </summary>
    public class ShowTypeDropdownAttribute : PropertyAttribute
    {
        public readonly Type assignableFrom;
        public readonly bool ignoreInterfaces;

        public ShowTypeDropdownAttribute()
        {
            assignableFrom = typeof(object);
            ignoreInterfaces = false;
        }

        public ShowTypeDropdownAttribute(Type assignableFrom, bool ignoreInterfaces = false)
        {
            this.assignableFrom = assignableFrom;
            this.ignoreInterfaces = ignoreInterfaces;
        }
    }
}