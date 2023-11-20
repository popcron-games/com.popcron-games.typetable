#nullable enable
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Popcron
{
    [InitializeOnLoad]
    public static class TypeTableLoader
    {
        private const string MenuName = "Window/" + nameof(Popcron) + "/" + nameof(TypeTable) + "/Use Reflection (Editor)";

        private static bool initialized;
        private static bool useReflection;

        static TypeTableLoader()
        {
            useReflection = EditorPrefs.GetBool("useReflectionInEditor", true);
            if (useReflection)
            {
                Load();
            }
        }

        [MenuItem(MenuName)]
        private static void ToggleUseReflection()
        {
            useReflection = !useReflection;
            EditorPrefs.SetBool("useReflectionInEditor", useReflection);

            TypeTable.Clear();
            if (useReflection)
            {
                Load();
            }
        }

        [MenuItem(MenuName, true)]
        private static bool ToggleUseReflectionValidate()
        {
            Menu.SetChecked(MenuName, useReflection);
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            if (initialized) return;
            initialized = true;

            foreach (Type type in TypeCache.GetTypesWithAttribute<TypeIDAttribute>())
            {
                ushort typeId = type.GetCustomAttribute<TypeIDAttribute>().typeId;
                TypeTable.Register(type, typeId);
            }

            TypeTable.FindAssignableTypes();
        }
    }
}