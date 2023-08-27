#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Popcron
{
    public static class TypeCache
    {
        private static readonly Dictionary<int, Type> fullNameToType = new Dictionary<int, Type>();
        private static readonly Dictionary<int, Type> aqnToType = new Dictionary<int, Type>();
        private static readonly List<Type> types = new List<Type>();
        private static readonly Dictionary<Assembly, HashSet<Type>> assemblyToTypes = new Dictionary<Assembly, HashSet<Type>>();
        private static readonly Dictionary<Type, HashSet<Type>> typeToSubtypes = new Dictionary<Type, HashSet<Type>>();
        private static readonly Dictionary<Type, HashSet<Type>> typeToImplementing = new Dictionary<Type, HashSet<Type>>();
        private static readonly Dictionary<Type, HashSet<(MethodInfo, Attribute)>> attributeToStaticMethods = new Dictionary<Type, HashSet<(MethodInfo, Attribute)>>();

        public static IReadOnlyList<Type> All => types;

        static TypeCache()
        {
            if (TypeCacheSettings.Singleton is TypeCacheSettings settings)
            {
                foreach (string assemblyName in settings.AssembliesToCache)
                {
                    Assembly asm;
                    try
                    {
                        asm = Assembly.Load(assemblyName);
                    }
                    catch
                    {
                        continue;
                    }

                    LoadAssembly(asm);
                }
            }
            else
            {
                throw new Exception("TypeCache is attempting to initialize without TypeCacheSettings being present.");
            }
        }

        private static void LoadAssembly(Assembly assembly)
        {
            if (assemblyToTypes.ContainsKey(assembly)) return;

            HashSet<Type> types = new HashSet<Type>();
            assemblyToTypes.Add(assembly, types);
            foreach (Type type in assembly.GetTypes())
            {
                int aqnHash = GetHash(type.AssemblyQualifiedName.AsSpan());
                aqnToType.Add(aqnHash, type);

                int fullNameHash = GetHash(type.FullName.AsSpan());
                if (!fullNameToType.ContainsKey(fullNameHash))
                {
                    fullNameToType.Add(fullNameHash, type);
                    typeToSubtypes.Add(type, new HashSet<Type>());
                    typeToImplementing.Add(type, new HashSet<Type>());
                    types.Add(type);
                }
            }

            foreach (Type type in types)
            {
                TypeCache.types.Add(type);
                HashSet<Type> subtypes = typeToSubtypes[type];
                HashSet<Type> implementing = typeToImplementing[type];
                bool isInterface = type.IsInterface;
                foreach (Type otherType in types)
                {
                    if (isInterface)
                    {
                        if (type.IsAssignableFrom(otherType))
                        {
                            implementing.Add(otherType);
                        }
                    }
                    else
                    {
                        if (otherType.IsSubclassOf(type))
                        {
                            subtypes.Add(otherType);
                        }
                    }
                }

                MethodInfo[] staticMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (MethodInfo staticMethod in staticMethods)
                {
                    foreach (Attribute attribute in staticMethod.GetCustomAttributes())
                    {
                        Type attributeType = attribute.GetType();
                        if (!attributeToStaticMethods.TryGetValue(attributeType, out HashSet<(MethodInfo method, Attribute attribute)> staticMethodsList))
                        {
                            staticMethodsList = new HashSet<(MethodInfo, Attribute)>();
                            attributeToStaticMethods.Add(attributeType, staticMethodsList);
                        }

                        staticMethodsList.Add((staticMethod, attribute));
                    }
                }
            }
        }

        public static int GetHash(ReadOnlySpan<char> identifier)
        {
            unchecked
            {
                int hash = 0;
                for (int i = 0; i < identifier.Length; i++)
                {
                    hash = hash * 2147483423 + identifier[i];
                }

                return hash;
            }
        }

        public static Type GetType(string identifier)
        {
            return GetType(identifier.AsSpan());
        }

        public static Type GetType(ReadOnlySpan<char> identifier)
        {
            int hash = GetHash(identifier);
            if (aqnToType.TryGetValue(hash, out Type aqnType))
            {
                return aqnType;
            }
            else if (fullNameToType.TryGetValue(hash, out Type fullNameType))
            {
                return fullNameType;
            }
            else
            {
                if (Type.GetType(identifier.ToString()) is Type foundType)
                {
                    Debug.LogWarningFormat("Found type {0} from assembly {1} but it was not cached", foundType, foundType.Assembly.GetName().Name);
                    return foundType;
                }

                throw new NullReferenceException($"Type with name {identifier.ToString()} cant be found.");
            }
        }

        public static Type GetType(int identifierHash)
        {
            if (TryGetType(identifierHash, out Type type))
            {
                return type;
            }
            else
            {
                throw new NullReferenceException($"Type with name hash {identifierHash} cant be found.");
            }
        }

        public static bool TryGetType(string identifier, out Type type)
        {
            return TryGetType(identifier.AsSpan(), out type);
        }

        public static bool TryGetType(ReadOnlySpan<char> identifier, out Type type)
        {
            int hash = GetHash(identifier);
            return TryGetType(hash, out type);
        }

        public static bool TryGetType(int identifier, out Type type)
        {
            if (aqnToType.TryGetValue(identifier, out type))
            {
                return true;
            }
            else if (fullNameToType.TryGetValue(identifier, out type))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        ///<summary>This type must have its assembly cached.</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>All known types that are derived from <typeparamref name="T"/></returns>.
        public static IReadOnlyCollection<Type> GetSubtypesOf<T>()
        {
            return GetSubtypesOf(typeof(T));
        }

        public static IReadOnlyCollection<Type> GetSubtypesOf(Type type)
        {
            if (typeToSubtypes.TryGetValue(type, out HashSet<Type> subTypes))
            {
#if UNITY_EDITOR
                TypeCacheSettings typeSettings = TypeCacheSettings.Singleton ?? throw new Exception();
                HashSet<Assembly> handledAssemblies = new HashSet<Assembly>();
                foreach (Type cachedType in UnityEditor.TypeCache.GetTypesDerivedFrom(type))
                {
                    if (!subTypes.Contains(cachedType))
                    {
                        Assembly missingAssembly = cachedType.Assembly;
                        if (handledAssemblies.Add(missingAssembly))
                        {
                            AddAssembly(missingAssembly, typeSettings);
                            LoadAssembly(missingAssembly);
                        }
                    }
                }

                if (handledAssemblies.Count > 0)
                {
                    UnityEditor.AssetDatabase.SaveAssetIfDirty(typeSettings);
                    return GetSubtypesOf(type);
                }
#endif
                return subTypes;
            }
            else
            {
                return Array.Empty<Type>();
            }
        }

        public static IReadOnlyCollection<Type> GetTypesThatImplement<T>()
        {
            return GetTypesThatImplement(typeof(T));
        }

        public static IReadOnlyCollection<Type> GetTypesThatImplement(Type type)
        {
            if (typeToImplementing.TryGetValue(type, out HashSet<Type> implementingTypes))
            {
#if UNITY_EDITOR
                TypeCacheSettings typeSettings = TypeCacheSettings.Singleton ?? throw new Exception();
                HashSet<Assembly> handledAssemblies = new HashSet<Assembly>();
                foreach (Type cachedType in UnityEditor.TypeCache.GetTypesDerivedFrom(type))
                {
                    if (!implementingTypes.Contains(cachedType))
                    {
                        Assembly missingAssembly = cachedType.Assembly;
                        if (handledAssemblies.Add(missingAssembly))
                        {
                            AddAssembly(missingAssembly, typeSettings);
                            LoadAssembly(missingAssembly);
                        }
                    }
                }

                if (handledAssemblies.Count > 0)
                {
                    UnityEditor.AssetDatabase.SaveAssetIfDirty(typeSettings);
                    return GetSubtypesOf(type);
                }
#endif
                return implementingTypes;
            }
            else
            {
                return Array.Empty<Type>();
            }
        }

        public static IEnumerable<(MethodInfo method, T attribute)> GetMethodsWithAttribute<T>() where T : Attribute
        {
            foreach ((MethodInfo method, Attribute attribute) entry in GetMethodsWithAttribute(typeof(T)))
            {
                yield return (entry.method, (T)entry.attribute);
            }
        }

        public static IReadOnlyCollection<(MethodInfo method, Attribute attribute)> GetMethodsWithAttribute(Type type)
        {
            IReadOnlyCollection<(MethodInfo method, Attribute attribute)> found;
            if (attributeToStaticMethods.TryGetValue(type, out HashSet<(MethodInfo, Attribute)> methods))
            {
                found = methods;
            }
            else
            {
                found = Array.Empty<(MethodInfo, Attribute)>();
            }

#if UNITY_EDITOR
            TypeCacheSettings typeSettings = TypeCacheSettings.Singleton ?? throw new Exception();
            HashSet<Assembly> handledAssemblies = new HashSet<Assembly>();
            foreach (MethodInfo cachedMethod in UnityEditor.TypeCache.GetMethodsWithAttribute(type))
            {
                bool contains = false;
                foreach (var foundEntry in found)
                {
                    if (foundEntry.method == cachedMethod)
                    {
                        contains = true;
                        break;
                    }
                }

                Assembly missingAssembly = cachedMethod.DeclaringType.Assembly;
                if (!contains && handledAssemblies.Add(missingAssembly))
                {
                    AddAssembly(missingAssembly, typeSettings);
                    LoadAssembly(missingAssembly);
                }
            }

            if (handledAssemblies.Count > 0)
            {
                UnityEditor.AssetDatabase.SaveAssetIfDirty(typeSettings);
                return GetMethodsWithAttribute(type);
            }
#endif
            return found;
        }

#if UNITY_EDITOR
        private static void AddAssembly(Assembly assembly, TypeCacheSettings typeSettings)
        {
            UnityEditor.SerializedObject serializedObject = new UnityEditor.SerializedObject(typeSettings);
            UnityEditor.SerializedProperty array = serializedObject.FindProperty(TypeCacheSettings.AssembliesToCachePropertyName);
            array.arraySize++;
            array.GetArrayElementAtIndex(array.arraySize - 1).stringValue = assembly.GetName().Name;
            serializedObject.ApplyModifiedProperties();
        }
#endif
    }
}