#nullable enable
using System;
using UnityEngine;

namespace Popcron
{
    /// <summary>
    /// If the field is of type <see cref="string"/>, then a dropdown will appear as an option to select a type.
    /// </summary>
    public class ShowTypeDropdownAttribute : PropertyAttribute
    {
        public Type AssignableFrom { get; }

        public ShowTypeDropdownAttribute()
        {
            AssignableFrom = typeof(object);
        }

        public ShowTypeDropdownAttribute(Type assignableFrom)
        {
            AssignableFrom = assignableFrom;
        }
    }
}