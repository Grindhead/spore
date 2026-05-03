using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RoobsWorld.Creature.Editor
{
    /// <summary>
    /// In-memory representation of a saved creature entry on disk.
    /// </summary>
    public class CreatureGalleryEntry
    {
        public string FileName;
        public string CreatureName;
        public string CreatureId;
        public CreatureDNA DNA;
    }

    /// <summary>
    /// Manages saving, loading, deleting, and enumerating creature DNA JSON files in a gallery directory.
    /// </summary>
    public class CreatureGallery
    {
        private readonly string _saveDir;
        private readonly List<CreatureGalleryEntry> _entries = new List<CreatureGalleryEntry>();

        public IReadOnlyList<CreatureGalleryEntry> Entries => _entries;
        public string SaveDirectory => _saveDir;

        public CreatureGallery(string baseDir = null)
        {
            _saveDir = baseDir ?? Path.Combine(Application.persistentDataPath, "Creatures");
            Directory.CreateDirectory(_saveDir);
        }

        public void RefreshList()
        {
            _entries.Clear();
            if (!Directory.Exists(_saveDir)) return;

            string[] files = Directory.GetFiles(_saveDir, "*.json");
            foreach (var file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var dna = CreatureDNA.FromJson(json);
                    _entries.Add(new CreatureGalleryEntry
                    {
                        FileName = Path.GetFileNameWithoutExtension(file),
                        CreatureName = dna.CreatureName,
                        CreatureId = dna.CreatureId,
                        DNA = dna
                    });
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CreatureGallery] Failed to load {file}: {e.Message}");
                }
            }
        }

        public bool Save(CreatureDNA dna, string fileName = null)
        {
            if (dna == null) return false;
            fileName ??= dna.CreatureId;
            if (string.IsNullOrEmpty(fileName)) return false;

            string path = Path.Combine(_saveDir, fileName + ".json");
            string json = dna.ToJson();
            File.WriteAllText(path, json);
            return true;
        }

        public CreatureDNA Load(string fileName)
        {
            string path = Path.Combine(_saveDir, fileName + ".json");
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            return CreatureDNA.FromJson(json);
        }

        public bool Delete(string fileName)
        {
            string path = Path.Combine(_saveDir, fileName + ".json");
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }

        public int Count => _entries.Count;
    }
}
