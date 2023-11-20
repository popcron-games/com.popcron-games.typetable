#nullable enable
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using System.Text;
using System.Linq;

namespace Popcron
{
    [CustomPropertyDrawer(typeof(ShowTypeDropdownAttribute))]
    public class ShowTypeDropdownAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ShowTypeDropdownAttribute attribute = (ShowTypeDropdownAttribute)base.attribute;
            float width = Mathf.Max(100f, position.width * 0.35f);
            float gap = 3f;
            Rect idField = new Rect(position.x, position.y, position.width - width - gap, position.height);
            Rect valueField = new Rect(position.max.x - width, position.y, width, position.height);

            int newValue = EditorGUI.IntField(idField, label, property.intValue);
            if (newValue != property.intValue)
            {
                property.intValue = newValue;
            }

            GUIContent dropdownLabel;
            if (TypeTable.TryGetType((ushort)property.intValue, out Type foundType))
            {
                dropdownLabel = new GUIContent(foundType.Name);
            }
            else
            {
                dropdownLabel = new GUIContent("?");
            }

            if (EditorGUI.DropdownButton(valueField, dropdownLabel, FocusType.Keyboard))
            {
                string title;
                if (attribute.assignableFrom != typeof(object))
                {
                    title = attribute.assignableFrom.Name;
                }
                else
                {
                    title = "Types";
                }

                HashSet<SearchableDropdown.Option> options = new HashSet<SearchableDropdown.Option>();
                foreach (Type type in TypeTable.Types)
                {
                    if (attribute.ignoreInterfaces && type.IsInterface)
                    {
                        continue;
                    }

                    if (!attribute.assignableFrom.IsAssignableFrom(type))
                    {
                        continue;
                    }

                    ushort typeId = TypeTable.GetID(type);
                    if (type.BaseType != null && TypeTable.Contains(type.BaseType))
                    {
                        ushort parentTypeId = TypeTable.GetID(type.BaseType);
                        options.Add(new SearchableDropdown.Option(typeId, type.Name, parentTypeId));
                    }
                    else
                    {
                        options.Add(new SearchableDropdown.Option(typeId, type.Name));
                    }
                }

                SearchableDropdown dropdown = new SearchableDropdown(title, options.ToArray(), (option) =>
                {
                    property.intValue = option.typeId;
                    property.serializedObject.ApplyModifiedProperties();
                });

                dropdown.Show(new Rect(valueField.position, valueField.size * 2));
            }
        }

        /// <summary>
        /// A searchable enum drawer.
        /// </summary>
        internal sealed class SearchableDropdown : AdvancedDropdown
        {
            private readonly string title;
            private readonly IReadOnlyList<Option> options;
            private readonly Action<Option> onSelected;

            public SearchableDropdown(string title, IReadOnlyList<Option> options, Action<Option> onSelected) : base(null)
            {
                this.title = title;
                this.options = options;
                this.onSelected = onSelected;
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                AdvancedDropdownItem root = new AdvancedDropdownItem(title);

                //convert the options list
                //into a tree structure
                Stack<AdvancedDropdownItem> stack = new Stack<AdvancedDropdownItem>();
                stack.Push(root);

                foreach (Option option in options)
                {
                    if (option.parentTypeId != null)
                    {
                        //find the parent
                        AdvancedDropdownItem? parent = null;
                        foreach (AdvancedDropdownItem item in stack)
                        {
                            if (item.id == option.parentTypeId)
                            {
                                parent = item;
                                break;
                            }
                        }

                        if (parent is null)
                        {
                            parent = root;
                        }

                        AdvancedDropdownItem child = new AdvancedDropdownItem(option.label);
                        child.id = option.typeId;
                        parent.AddChild(child);
                        stack.Push(child);
                    }
                    else
                    {
                        AdvancedDropdownItem child = new AdvancedDropdownItem(option.label);
                        child.id = option.typeId;
                        root.AddChild(child);
                        stack.Push(child);
                    }
                }

                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                foreach (Option o in options)
                {
                    if (o.typeId == item.id)
                    {
                        onSelected(o);
                        break;
                    }
                }
            }

            public readonly struct Option
            {
                private static readonly StringBuilder sb = new StringBuilder();

                public readonly ushort typeId;
                public readonly string label;
                public readonly ushort? parentTypeId;

                public Option(ushort typeId, string label, ushort parentTypeId)
                {
                    this.typeId = typeId;
                    this.label = label;
                    this.parentTypeId = parentTypeId;
                }

                public Option(ushort typeId, string label)
                {
                    this.typeId = typeId;
                    this.label = label;
                    this.parentTypeId = null;
                }

                public override string ToString()
                {
                    sb.Clear(); 
                    sb.Append(label);
                    sb.Append(" (");
                    sb.Append(typeId);
                    sb.Append(")");
                    return sb.ToString();
                }
            }
        }
    }
}