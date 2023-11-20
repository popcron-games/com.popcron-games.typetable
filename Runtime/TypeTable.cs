#nullable enable
using System;
using System.Collections.Generic;

namespace Popcron
{
    public static class TypeTable
    {
        private static readonly Dictionary<ushort, Type> idToType = new Dictionary<ushort, Type>();
        private static readonly Dictionary<Type, ushort> typeToId = new Dictionary<Type, ushort>();
        private static readonly HashSet<Type> types = new HashSet<Type>();
        private static readonly Dictionary<ushort, HashSet<ushort>> assignableTypeIds = new Dictionary<ushort, HashSet<ushort>>();
        private static readonly Dictionary<Type, HashSet<Type>> assignableTypes = new Dictionary<Type, HashSet<Type>>();

        public static IReadOnlyCollection<Type> Types => types;

        public static void Register(Type type, ushort typeId)
        {
            if (types.Contains(type)) return;
            if (idToType.ContainsKey(typeId))
            {
                throw new Exception($"Type ID {typeId} has already been registered by {idToType[typeId]}");
            }

            idToType[typeId] = type;
            typeToId[type] = typeId;
            types.Add(type);
        }

        public static void Register(string name, ushort typeId)
        {
            if (Type.GetType(name) is Type type)
            {
                Register(type, typeId);
            }
            else
            {
                throw new Exception($"Type {name} could not be found to register");
            }
        }

        public static void FindAssignableTypes()
        {
            foreach (Type type in types)
            {
                HashSet<ushort> typeIds = new HashSet<ushort>();
                HashSet<Type> subTypes = new HashSet<Type>();
                Stack<Type> stack = new Stack<Type>();
                stack.Push(type);
                while (stack.Count > 0)
                {
                    Type current = stack.Pop();
                    if (typeToId.TryGetValue(current, out ushort typeId))
                    {
                        typeIds.Add(typeId);
                        subTypes.Add(current);
                    }

                    foreach (Type assignable in types)
                    {
                        if (current.IsAssignableFrom(assignable))
                        {
                            if (assignable == current) continue;
                            stack.Push(assignable);
                        }
                    }
                }

                assignableTypeIds[typeToId[type]] = typeIds;
                assignableTypes[type] = subTypes;
            }
        }

        public static void Clear()
        {
            idToType.Clear();
            typeToId.Clear();
            types.Clear();
        }

        public static bool TryGetID(Type type, out ushort typeId)
        {
            return typeToId.TryGetValue(type, out typeId);
        }

        public static bool TryGetType(ushort typeId, out Type type)
        {
            return idToType.TryGetValue(typeId, out type);
        }

        public static bool TryGetID<T>(out ushort typeId)
        {
            return TryGetID(typeof(T), out typeId);
        }

        public static ushort GetID<T>()
        {
            if (TryGetID<T>(out ushort typeId))
            {
                return typeId;
            }

            throw new Exception($"Type {typeof(T)} has not been registered");
        }

        public static ushort GetID(Type type)
        {
            if (TryGetID(type, out ushort typeId))
            {
                return typeId;
            }

            throw new Exception($"Type {type} has not been registered");
        }

        public static Type GetType(ushort typeId)
        {
            if (TryGetType(typeId, out Type type))
            {
                return type;
            }

            throw new Exception($"Type ID {typeId} has not been registered");
        }

        public static bool Contains(Type type)
        {
            return types.Contains(type);
        }

        public static bool Contains<T>()
        {
            return Contains(typeof(T));
        }

        public static bool Contains(ushort typeId)
        {
            return idToType.ContainsKey(typeId);
        }

        public static IReadOnlyCollection<ushort> GetTypesAssignableFrom(ushort typeId)
        {
            if (assignableTypeIds.TryGetValue(typeId, out HashSet<ushort> typeIds))
            {
                return typeIds;
            }

            return Array.Empty<ushort>();
        }

        public static IReadOnlyCollection<Type> GetTypesAssignableFrom<T>()
        {
            if (assignableTypes.TryGetValue(typeof(T), out HashSet<Type> types))
            {
                return types;
            }

            return Array.Empty<Type>();
        }

        public static IReadOnlyCollection<Type> GetTypesAssignableFrom(Type type)
        {
            if (assignableTypes.TryGetValue(type, out HashSet<Type> types))
            {
                return types;
            }

            return Array.Empty<Type>();
        }
    }
}