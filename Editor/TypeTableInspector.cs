#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Popcron
{
    public class TypeTableInspector : EditorWindow
    {
        public const string WindowName = nameof(TypeTable) + " Inspector";
        private static readonly Queue<(Type result, Type? hit)> searchResult = new Queue<(Type, Type?)>();

        private Vector2 scrollPosition;
        private string? search;
        private int resultsFound;
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

        [MenuItem("Window/" + nameof(Popcron) + "/" + nameof(TypeTable) + "/Inspector")]
        public static void OpenWindow()
        {
            TypeTableInspector window = GetWindow<TypeTableInspector>(WindowName);
            window.titleContent = new GUIContent(WindowName);
            window.Show();
        }

        private static async Task Search(string search, CancellationToken cancellationToken)
        {
            DateTime lastTime = DateTime.Now;
            const double MaxThreadBlockTime = 0.1;
            foreach (Type type in TypeTable.Types)
            {
                bool found = false;
                Type? hit = null;
                if (!string.IsNullOrEmpty(search))
                {
                    if (type.FullName.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        //found it
                        found = true;
                    }
                    else
                    {
                        Type? current = type;
                        while (current != null)
                        {
                            if (current.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                //found it
                                found = true;
                                hit = current;
                                break;
                            }
                            else
                            {
                                foreach (Type interfaceType in type.GetInterfaces())
                                {
                                    if (interfaceType.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1)
                                    {
                                        //found it
                                        found = true;
                                        hit = interfaceType;
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
                    searchResult.Enqueue((type, hit));
                }

                TimeSpan timeElapsedSince = DateTime.Now - lastTime;
                if (timeElapsedSince.TotalSeconds > MaxThreadBlockTime)
                {
                    try
                    {
                        await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                    }
                    catch { }

                    if (cancellationToken.IsCancellationRequested) return;
                    lastTime = DateTime.Now;
                }
            }
        }

        private void OnGUI()
        {
            IReadOnlyCollection<Type> types = TypeTable.Types;
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
                foreach ((Type result, Type? hit) in searchResult)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        if (hit != null)
                        {
                            EditorGUILayout.LabelField(TypeTable.GetID(hit) + ">" + TypeTable.GetID(result), GUILayout.Width(60));
                            EditorGUILayout.LabelField(hit.FullName + ">" + result.FullName);
                        }
                        else
                        {
                            EditorGUILayout.LabelField(TypeTable.GetID(result).ToString(), GUILayout.Width(60));
                            EditorGUILayout.LabelField(result.FullName);
                        }

                        if (GUILayout.Button("Copy", GUILayout.Width(40)))
                        {
                            EditorGUIUtility.systemCopyBuffer = result.AssemblyQualifiedName;
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
            searchResult.Clear();
            _ = Search(search, cts.Token);
        }
    }
}