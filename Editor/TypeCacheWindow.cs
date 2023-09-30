#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Popcron
{
    public class TypeCacheWindow : EditorWindow
    {
        public const string WindowName = nameof(TypeCache);

        private Vector2 scrollPosition;
        private string? search;
        private int resultsFound;
        private readonly ConcurrentQueue<Type> searchResult = new ConcurrentQueue<Type>();
        private CancellationTokenSource? cts;

        private void OnEnable()
        {
            StartAsyncSearch("");
        }

        private void OnDisable()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        [MenuItem("Window/Popcron/" + WindowName)]
        public static void OpenWindow()
        {
            TypeCacheWindow window = GetWindow<TypeCacheWindow>(WindowName);
            window.titleContent = new GUIContent(WindowName);
            window.Show();
        }

        private static async Task<HashSet<Type>> Search(string search, CancellationToken cancellationToken)
        {
            HashSet<Type> searchResult = new HashSet<Type>();
            DateTime lastTime = DateTime.Now;
            const double MaxThreadBlockTime = 0.1;
            foreach (Type type in TypeCache.Types)
            {
                bool found = false;
                if (!string.IsNullOrEmpty(search))
                {
                    if (type.FullName.Contains(search, StringComparison.OrdinalIgnoreCase))
                    {
                        //found it
                        found = true;
                    }
                    else
                    {
                        Type? current = type;
                        while (current != null)
                        {
                            if (current.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                            {
                                //found it
                                found = true;
                                break;
                            }
                            else
                            {
                                foreach (Type interfaceType in type.GetInterfaces())
                                {
                                    if (interfaceType.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                                    {
                                        //found it
                                        found = true;
                                        break;
                                    }
                                }

                                if (found)
                                {
                                    //found it
                                    break;
                                }
                            }

                            current = current.BaseType;
                        }
                    }
                }
                else
                {
                    found = true;
                }

                if (found)
                {
                    searchResult.Add(type);
                }

                TimeSpan timeElapsedSince = DateTime.Now - lastTime;
                if (timeElapsedSince.TotalSeconds > MaxThreadBlockTime)
                {
                    try
                    {
                        await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                    }
                    catch { }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return searchResult;
                    }

                    lastTime = DateTime.Now;
                }
            }

            return searchResult;
        }

        private void OnGUI()
        {
            TypeCacheSettings settings = TypeCacheSettings.Singleton;
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Settings", settings, typeof(TypeCacheSettings), false);
            GUI.enabled = true;

            IReadOnlyCollection<Type> types = TypeCache.Types;
            if (types.Count == 0)
            {
                EditorGUILayout.HelpBox("No types found", MessageType.Info);
                return;
            }

            string newSearch = EditorGUILayout.TextField("Search", search);
            if (newSearch != search)
            {
                search = newSearch;
                StartAsyncSearch(newSearch);
            }

            EditorGUILayout.LabelField("Results", resultsFound.ToString());
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            {
                resultsFound = 0;
                foreach (Type type in searchResult)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(type.FullName);
                        if (GUILayout.Button("Copy", GUILayout.Width(40)))
                        {
                            EditorGUIUtility.systemCopyBuffer = type.FullName;
                        }

                        resultsFound++;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void StartAsyncSearch(string search)
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();
            Search(search, cts.Token).ContinueWith((finished) =>
            {
                searchResult.Clear();
                foreach (Type type in finished.Result)
                {
                    searchResult.Enqueue(type);
                }
            });
        }
    }
}