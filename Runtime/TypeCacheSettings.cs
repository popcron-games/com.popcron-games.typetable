#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Popcron
{
    public class TypeCacheSettings : ScriptableObject
    {
        public const string AssembliesToCachePropertyName = nameof(assembliesToCache);

        private static TypeCacheSettings? singleton;

        public static TypeCacheSettings Singleton
        {
            get
            {
                if (singleton != null)
                {
                    return singleton;
                }
                else
                {
#if UNITY_EDITOR
                    TypeCacheSettings? settingsFromPreloaded = null;
                    List<Object> preloadedAssets = UnityEditor.PlayerSettings.GetPreloadedAssets().ToList();
                    for (int i = preloadedAssets.Count - 1; i >= 0; i--)
                    {
                        if (preloadedAssets[i] == null)
                        {
                            preloadedAssets.RemoveAt(i);
                        }
                        else if (preloadedAssets[i] is TypeCacheSettings settings)
                        {
                            if (settingsFromPreloaded == null)
                            {
                                settingsFromPreloaded = settings;
                            }
                            else if (settingsFromPreloaded == settings)
                            {
                                Debug.LogErrorFormat("Duplicate TypeCache settings in preloaded assets {0} will be ignored", settings);
                            }
                        }
                    }

                    if (settingsFromPreloaded is null)
                    {
                        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:TypeCacheSettings", new string[] { "Assets" });
                        if (guids.Length > 0)
                        {
                            if (guids.Length == 1)
                            {
                                settingsFromPreloaded = UnityEditor.AssetDatabase.LoadAssetAtPath<TypeCacheSettings>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                                preloadedAssets.Add(settingsFromPreloaded);
                            }
                            else
                            {
                                throw new Exception($"There are {guids.Length} TypeCacheSetting assets instances, 1 is required");
                            }
                        }
                        else
                        {
                            settingsFromPreloaded = ScriptableObject.CreateInstance<TypeCacheSettings>();
                            UnityEditor.AssetDatabase.CreateAsset(settingsFromPreloaded, "Assets/TypeCache Settings.asset");
                            preloadedAssets.Add(settingsFromPreloaded);
                        }
                    }

                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
                    UnityEditor.AssetDatabase.SaveAssets();
                    return settingsFromPreloaded;
#else
                    throw new Exception("TypeCacheSettings not found at runtime");
#endif
                }
            }
        }

        [SerializeField]
        private List<string> assembliesToCache = new List<string>();

        public IReadOnlyCollection<string> AssembliesToCache
        {
            get
            {
                return assembliesToCache;
            }
        }

        private void OnEnable()
        {
            singleton = this;
        }

        private void OnDisable()
        {
            singleton = null;
        }
    }
}