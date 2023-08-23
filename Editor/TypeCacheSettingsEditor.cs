#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace Popcron
{
    [CustomEditor(typeof(TypeCacheSettings))]
    public class TypeCacheSettingsEditor : Editor
    {
        private static readonly HashSet<Assembly> knownAssemblies = new HashSet<Assembly>();
        private static readonly Dictionary<string, Assembly> assemblyNameToAssembly = new Dictionary<string, Assembly>();
        private static bool initialized;

        private SerializedProperty array = null!;

        private void OnEnable()
        {
            array = serializedObject.FindProperty(TypeCacheSettings.AssembliesToCachePropertyName);
        }

        public override void OnInspectorGUI()
        {
            if (!initialized)
            {
                Initialize();
                initialized = true;
            }

            serializedObject.Update();
            EditorGUILayout.PropertyField(array, true);
            for (int i = 0; i < array.arraySize; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                string assemblyName = element.stringValue;
                if (!assemblyNameToAssembly.ContainsKey(assemblyName))
                {
                    EditorGUILayout.HelpBox($"Assembly {assemblyName} is was not found", MessageType.Error);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void Initialize()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                knownAssemblies.Add(assembly);
                assemblyNameToAssembly.Add(assembly.GetName().Name, assembly);
            }
        }
    }
}