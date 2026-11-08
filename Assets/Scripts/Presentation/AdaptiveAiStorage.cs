using System;
using System.Collections.Generic;
using System.IO;
using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// Local-only learning profiles. No cloud telemetry, personal data, or
    /// recorded card hands. Profiles are deliberately small and versioned.
    /// </summary>
    public sealed class AdaptiveAiStorage
    {
        [Serializable]
        private sealed class SaveFile
        {
            public int schema = 1;
            public List<AiLearningState> profiles = new List<AiLearningState>();
        }

        private readonly string path;
        private readonly Dictionary<string, AiLearningState> profiles =
            new Dictionary<string, AiLearningState>(StringComparer.Ordinal);

        public string FilePath => path;

        public AdaptiveAiStorage()
        {
            path = Path.Combine(Application.persistentDataPath, "Reborn",
                "adaptive-ai-v1.json");
            Load();
        }

        public AiLearningState Get(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity) || identity.Length > 128)
                throw new ArgumentException("Invalid opponent identity", nameof(identity));
            if (!profiles.TryGetValue(identity, out var found))
            {
                found = new AiLearningState(identity);
                profiles.Add(identity, found);
            }
            found.Sanitize();
            return found;
        }

        private void Load()
        {
            if (!File.Exists(path)) return;
            try
            {
                var saved = JsonUtility.FromJson<SaveFile>(File.ReadAllText(path));
                if (saved == null || saved.schema != 1 || saved.profiles == null)
                    throw new InvalidDataException("Unsupported learning schema");
                foreach (var item in saved.profiles)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.key) ||
                        item.key.Length > 128 || profiles.ContainsKey(item.key))
                        continue;
                    item.Sanitize();
                    profiles.Add(item.key, item);
                    if (profiles.Count >= 512) break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("REBORN: aprendizagem local invalida; usando perfil novo. " + ex.Message);
                profiles.Clear();
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var saved = new SaveFile();
                foreach (var pair in profiles)
                    saved.profiles.Add(pair.Value);
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(saved, true));
                if (File.Exists(path))
                {
                    // Atomic replacement where supported; fallback to overwrite.
                    try { File.Replace(temp, path, null); }
                    catch (IOException)
                    {
                        File.Copy(temp, path, true);
                        File.Delete(temp);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(temp, path, true);
                        File.Delete(temp);
                    }
                }
                else File.Move(temp, path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("REBORN: nao foi possivel guardar aprendizagem local. " + ex.Message);
            }
        }
    }
}
