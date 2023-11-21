using System.IO;
using UnityEditor;

namespace Popcron
{
    [InitializeOnLoad]
    public static class TypeTableLoadChecker
    {
        public const string DefaultLoaderPath = "Assets/" + TypeTableLoaderGenerator.TypeName + ".cs";

        static TypeTableLoadChecker()
        {
            EditorApplication.delayCall += () => Check();
        }

        [MenuItem("Window/" + nameof(Popcron) + "/" + nameof(TypeTable) + "/Generate Loader Script")]
        private static void DoIt()
        {
            string script = TypeTableLoaderGenerator.GenerateScript();
            File.WriteAllText(DefaultLoaderPath, script);
            EditorPrefs.SetString("lastGeneratedTypeTableLoader", script);
            AssetDatabase.Refresh();
        }

        private static void Check()
        {
            string last = EditorPrefs.GetString("lastGeneratedTypeTableLoader", string.Empty);
            string current = TypeTableLoaderGenerator.GenerateScript();
            if (last != current)
            {
                EditorPrefs.SetString("lastGeneratedTypeTableLoader", current);
                File.WriteAllText(DefaultLoaderPath, current);
                AssetDatabase.Refresh();
            }
        }
    }
}