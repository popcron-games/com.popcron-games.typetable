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
        private static readonly HashSet<Type> types = new HashSet<Type>();
        private static readonly HashSet<Assembly> assemblies = new HashSet<Assembly>();
        private static readonly Dictionary<Assembly, HashSet<Type>> assemblyToTypes = new Dictionary<Assembly, HashSet<Type>>();
        private static readonly Dictionary<Type, HashSet<Type>> typeToSubtypes = new Dictionary<Type, HashSet<Type>>();
        private static readonly Dictionary<Type, HashSet<Type>> typeToImplementing = new Dictionary<Type, HashSet<Type>>();
        private static readonly Dictionary<Type, HashSet<(MemberInfo, Attribute)>> attributeToMembers = new Dictionary<Type, HashSet<(MemberInfo, Attribute)>>();
        private static readonly Dictionary<Type, HashSet<(Type, Attribute)>> attributeToTypes = new Dictionary<Type, HashSet<(Type, Attribute)>>();

        public static IReadOnlyCollection<Type> Types => types;
        public static IReadOnlyCollection<Assembly> Assemblies => assemblies;

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
            if (assemblies.Contains(assembly)) return;

            HashSet<Type> types = new HashSet<Type>();
            assemblyToTypes.Add(assembly, types);
            assemblies.Add(assembly);
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

                //type attributes
                foreach (Attribute attribute in type.GetCustomAttributes())
                {
                    Type attributeType = attribute.GetType();
                    if (!attributeToTypes.TryGetValue(attributeType, out HashSet<(Type type, Attribute attribute)> typeList))
                    {
                        typeList = new HashSet<(Type, Attribute)>();
                        attributeToTypes.Add(attributeType, typeList);
                    }

                    typeList.Add((type, attribute));
                }

                //members with attributes
                MemberInfo[] members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (MemberInfo member in members)
                {
                    foreach (Attribute attribute in member.GetCustomAttributes())
                    {
                        Type attributeType = attribute.GetType();
                        if (!attributeToMembers.TryGetValue(attributeType, out HashSet<(MemberInfo method, Attribute attribute)> memberList))
                        {
                            memberList = new HashSet<(MemberInfo, Attribute)>();
                            attributeToMembers.Add(attributeType, memberList);
                        }

                        memberList.Add((member, attribute));
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

        /// <summary>
        /// All registered <see cref="Type"/>s that can be assigned from <typeparamref name="T"/>
        /// </summary>
        public static IReadOnlyCollection<Type> GetTypesAssignableFrom<T>()
        {
            return GetTypesAssignableFrom(typeof(T));
        }

        public static IReadOnlyCollection<Type> GetSubtypesOf(Type type)
        {
            if (!typeToSubtypes.TryGetValue(type, out HashSet<Type> subTypes))
            {
                subTypes = new HashSet<Type>();
                typeToSubtypes.Add(type, subTypes);
            }

#if UNITY_EDITOR
            TypeCacheSettings typeSettings = TypeCacheSettings.Singleton;
            HashSet<Assembly> addedMissingAssemblies = new HashSet<Assembly>();
            foreach (Type cachedType in UnityEditor.TypeCache.GetTypesDerivedFrom(type))
            {
                if (subTypes.Add(cachedType))
                {
                    Assembly missingAssembly = cachedType.Assembly;
                    if (addedMissingAssemblies.Add(missingAssembly))
                    {
                        AddAssembly(missingAssembly, typeSettings);
                        LoadAssembly(missingAssembly);
                    }
                    else
                    {
                        //type is missing but its assembly is fully loaded, shouldnt be possible
                    }
                }
            }

            if (addedMissingAssemblies.Count > 0)
            {
                UnityEditor.AssetDatabase.SaveAssetIfDirty(typeSettings);
                return GetSubtypesOf(type);
            }
#endif
            return subTypes;
        }

        public static IReadOnlyCollection<Type> GetTypesAssignableFrom(Type type)
        {
            if (!typeToImplementing.TryGetValue(type, out HashSet<Type> implementingTypes))
            {
                implementingTypes = new HashSet<Type>();
                typeToImplementing.Add(type, implementingTypes);
            }

#if UNITY_EDITOR
            TypeCacheSettings typeSettings = TypeCacheSettings.Singleton;
            HashSet<Assembly> addedMissingAssemblies = new HashSet<Assembly>();
            foreach (Type cachedType in UnityEditor.TypeCache.GetTypesDerivedFrom(type))
            {
                if (implementingTypes.Add(cachedType))
                {
                    Assembly missingAssembly = cachedType.Assembly;
                    if (!assemblies.Contains(missingAssembly) && addedMissingAssemblies.Add(missingAssembly))
                    {
                        AddAssembly(missingAssembly, typeSettings);
                        LoadAssembly(missingAssembly);
                    }
                    else
                    {
                        //type is missing but its assembly is fully loaded, shouldnt be possible
                    }
                }
            }

            if (addedMissingAssemblies.Count > 0)
            {
                UnityEditor.AssetDatabase.SaveAssetIfDirty(typeSettings);
                return GetSubtypesOf(type);
            }
#endif
            return implementingTypes;
        }

        /// <summary>
        /// All members (fields, properties, methods) that have the attribute <typeparamref name="T"/>
        /// </summary>
        public static IEnumerable<(MemberInfo member, T attribute)> GetMembersWithAttribute<T>() where T : Attribute
        {
            foreach ((MemberInfo member, Attribute attribute) entry in GetMembersWithAttribute(typeof(T)))
            {
                yield return (entry.member, (T)entry.attribute);
            }
        }

        public static IReadOnlyCollection<(MemberInfo member, Attribute attribute)> GetMembersWithAttribute(Type type)
        {
            IReadOnlyCollection<(MemberInfo member, Attribute attribute)> found;
            if (attributeToMembers.TryGetValue(type, out HashSet<(MemberInfo, Attribute)> methods))
            {
                found = methods;
            }
            else
            {
                found = Array.Empty<(MemberInfo, Attribute)>();
            }

#if UNITY_EDITOR
            TypeCacheSettings typeSettings = TypeCacheSettings.Singleton;
            HashSet<Assembly> handledAssemblies = new HashSet<Assembly>();

            //unity editor cached methods
            foreach (MethodInfo cachedMethod in UnityEditor.TypeCache.GetMethodsWithAttribute(type))
            {
                bool contains = false;
                foreach (var foundEntry in found)
                {
                    if (foundEntry.member == cachedMethod)
                    {
                        contains = true;
                        break;
                    }
                }

                Assembly missingAssembly = cachedMethod.DeclaringType.Assembly;
                if (!contains && !assemblies.Contains(missingAssembly) && handledAssemblies.Add(missingAssembly))
                {
                    AddAssembly(missingAssembly, typeSettings);
                    LoadAssembly(missingAssembly);
                }
            }

            //unity editor cached fields
            foreach (FieldInfo cachedField in UnityEditor.TypeCache.GetFieldsWithAttribute(type))
            {
                bool contains = false;
                foreach (var foundEntry in found)
                {
                    if (foundEntry.member == cachedField)
                    {
                        contains = true;
                        break;
                    }
                }

                Assembly missingAssembly = cachedField.DeclaringType.Assembly;
                if (!contains && !assemblies.Contains(missingAssembly) && handledAssemblies.Add(missingAssembly))
                {
                    AddAssembly(missingAssembly, typeSettings);
                    LoadAssembly(missingAssembly);
                }
            }

            if (handledAssemblies.Count > 0)
            {
                UnityEditor.AssetDatabase.SaveAssetIfDirty(typeSettings);
                return GetMembersWithAttribute(type);
            }
#endif
            return found;
        }

        public static IEnumerable<(Type type, T attribute)> GetTypesWithAttribute<T>() where T : Attribute
        {
            foreach ((Type type, Attribute attribute) entry in GetTypesWithAttribute(typeof(T)))
            {
                yield return (entry.type, (T)entry.attribute);
            }
        }

        public static IReadOnlyCollection<(Type type, Attribute attribute)> GetTypesWithAttribute(Type type)
        {
            IReadOnlyCollection<(Type type, Attribute attribute)> found;
            if (attributeToTypes.TryGetValue(type, out HashSet<(Type, Attribute)> types))
            {
                found = types;
            }
            else
            {
                found = Array.Empty<(Type, Attribute)>();
            }

#if UNITY_EDITOR
            TypeCacheSettings typeSettings = TypeCacheSettings.Singleton;
            HashSet<Assembly> handledAssemblies = new HashSet<Assembly>();

            //unity editor cached types
            foreach (Type cachedType in UnityEditor.TypeCache.GetTypesWithAttribute(type))
            {
                bool contains = false;
                foreach (var foundEntry in found)
                {
                    if (foundEntry.type == cachedType)
                    {
                        contains = true;
                        break;
                    }
                }

                Assembly missingAssembly = cachedType.Assembly;
                if (!contains && !assemblies.Contains(missingAssembly) && handledAssemblies.Add(missingAssembly))
                {
                    AddAssembly(missingAssembly, typeSettings);
                    LoadAssembly(missingAssembly);
                }
            }

            if (handledAssemblies.Count > 0)
            {
                UnityEditor.AssetDatabase.SaveAssetIfDirty(typeSettings);
                return GetTypesWithAttribute(type);
            }
#endif

            return found;
        }

#if UNITY_EDITOR
        private static void AddAssembly(Assembly assembly, TypeCacheSettings typeSettings)
        {
            UnityEditor.SerializedObject serializedObject = new UnityEditor.SerializedObject(typeSettings);
            UnityEditor.SerializedProperty array = serializedObject.FindProperty(TypeCacheSettings.AssembliesToCachePropertyName);
            int arraySize = array.arraySize;
            string assemblyName = assembly.GetName().Name;
            for (int i = 0; i < arraySize; i++)
            {
                string element = array.GetArrayElementAtIndex(i).stringValue;
                if (element == assemblyName)
                {
                    //already contained
                    return;
                }
            }

            array.arraySize++;
            array.GetArrayElementAtIndex(array.arraySize - 1).stringValue = assemblyName;
            serializedObject.ApplyModifiedProperties();
            serializedObject.Dispose();
            array.Dispose();
        }
#endif
    }
}