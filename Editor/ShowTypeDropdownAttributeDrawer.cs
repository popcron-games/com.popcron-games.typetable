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

            string newValue = EditorGUI.TextField(idField, label, property.stringValue);
            if (newValue != property.stringValue)
            {
                property.stringValue = newValue;
            }

            GUIContent dropdownLabel;
            if (TypeCache.TryGetType(property.stringValue, out Type foundType))
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
                if (attribute.AssignableFrom != typeof(object))
                {
                    title = attribute.AssignableFrom.Name;
                }
                else
                {
                    title = "Types";
                }

                HashSet<SearchableDropdown.Option> options = new HashSet<SearchableDropdown.Option>();
                foreach (Type type in TypeCache.Types)
                {
                    if (type.IsAbstract)
                    {
                        continue;
                    }

                    if (!attribute.AssignableFrom.IsAssignableFrom(type))
                    {
                        continue;
                    }

                    if (type.BaseType != null)
                    {
                        options.Add(new SearchableDropdown.Option(type.FullName, type.Name, type.BaseType.FullName));
                    }
                    else
                    {
                        options.Add(new SearchableDropdown.Option(type.FullName, type.Name));
                    }
                }

                SearchableDropdown dropdown = new SearchableDropdown(title, options.ToArray(), (option) =>
                {
                    property.stringValue = option.value;
                    property.serializedObject.ApplyModifiedProperties();
                });

                dropdown.Show(valueField);
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
                    if (option.parentValue != null)
                    {
                        //find the parent
                        AdvancedDropdownItem? parent = null;
                        foreach (AdvancedDropdownItem item in stack)
                        {
                            if (item.id == GetHash(option.parentValue))
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
                        child.id = GetHash(option.value);
                        parent.AddChild(child);
                        stack.Push(child);
                    }
                    else
                    {
                        AdvancedDropdownItem child = new AdvancedDropdownItem(option.label);
                        child.id = GetHash(option.value);
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
                    if (GetHash(o.value) == item.id)
                    {
                        onSelected(o);
                        break;
                    }
                }
            }

            public static int GetHash(string input)
            {
                unchecked
                {
                    int h = 0;
                    for (int i = 0; i < input.Length; i++)
                    {
                        h = (h << 5) - h + input[i];
                    }

                    return h;
                }
            }

            public readonly struct Option
            {
                private static readonly StringBuilder sb = new StringBuilder();

                public readonly string value;
                public readonly string label;
                public readonly string? parentValue;

                public Option(string value, string label, string parentValue)
                {
                    this.value = value;
                    this.label = label;
                    this.parentValue = parentValue;
                }

                public Option(string value, string label)
                {
                    this.value = value;
                    this.label = label;
                    this.parentValue = null;
                }

                public override string ToString()
                {
                    sb.Clear(); 
                    sb.Append(label);
                    sb.Append(" (");
                    sb.Append(value);
                    sb.Append(")");
                    return sb.ToString();
                }
            }
        }
    }
}