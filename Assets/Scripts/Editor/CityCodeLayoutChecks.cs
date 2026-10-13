using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HarborCity
{
    public static class CityCodeLayoutChecks
    {
        [Serializable] sealed class Manifest { public Entry[] files; }
        [Serializable] sealed class Entry { public string oldPath, newPath, sha256, guid; }
        // Run in a disposable workspace copy; never open/replace the user's active city.
        public static void Run()
        {
            if (!Application.isBatchMode) throw new Exception("Use an isolated batch project");
            string manifestPath = Environment.GetEnvironmentVariable("HARBOR_CODE_LAYOUT_MANIFEST");
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            foreach (var entry in manifest.files)
            {
                if (AssetDatabase.GUIDToAssetPath(entry.guid) != entry.newPath)
                    throw new Exception("GUID/path mismatch: " + entry.newPath);
                using (var sha = SHA256.Create())
                    if (BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(entry.newPath))).Replace("-", "") != entry.sha256)
                        throw new Exception("Source changed: " + entry.newPath);
                if (File.Exists(entry.oldPath)) throw new Exception("Old source duplicate: " + entry.oldPath);
            }
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            int components = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                        throw new Exception("Missing scene script: " + transform.name);
                    components += transform.GetComponents<MonoBehaviour>().Length;
                }
            foreach (var type in new[] { typeof(HarborCityGame), typeof(CityLandscape), typeof(CityRoadView), typeof(HarborCityStandaloneValidation) })
            {
                bool found = false;
                foreach (string guid in AssetDatabase.FindAssets(type.Name + " t:MonoScript"))
                    if (AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid)).GetClass() == type) found = true;
                if (!found) throw new Exception("MonoScript class missing: " + type.Name);
            }
            string output = Environment.GetEnvironmentVariable("HARBOR_CODE_LAYOUT_RESULT");
            File.WriteAllText(output, "PASS: " + manifest.files.Length + " unchanged scripts/GUID mappings; scene has no missing scripts; " + components + " behaviours; runtime MonoScript types resolve.");
        }
    }
}
