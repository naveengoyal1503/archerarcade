using System;
using System.IO;
using ArcherArcade.Logic.Save;
using UnityEngine;

namespace ArcherArcade.Save
{
    /// <summary>
    /// Versioned JSON save on the phone (CLAUDE.md): writes save.json.tmp first, then swaps it in and keeps the
    /// previous file as save.json.bak. Loading falls back to the backup, then to a fresh save — a broken file can
    /// never lose progress or crash the game. Changes are marked dirty and written at most once per frame.
    /// </summary>
    public sealed class SaveSystem
    {
        const string FileName = "save.json";
        readonly string _path;
        readonly string _tmp;
        readonly string _bak;
        bool _dirty;

        public SaveSystem(string folder)
        {
            _path = Path.Combine(folder, FileName);
            _tmp = _path + ".tmp";
            _bak = _path + ".bak";
        }

        public SaveData Data { get; private set; }

        /// <summary>True when the last load found no save at all (first launch).</summary>
        public bool IsFirstLaunch { get; private set; }

        public SaveData Load()
        {
            IsFirstLaunch = !File.Exists(_path) && !File.Exists(_bak);
            Data = TryRead(_path) ?? TryRead(_bak) ?? SaveCodec.NewSave();
            return Data;
        }

        static SaveData TryRead(string file)
        {
            try
            {
                if (!File.Exists(file)) return null;
                string text = File.ReadAllText(file);
                if (string.IsNullOrWhiteSpace(text) || text.TrimStart()[0] != '{') return null;
                return SaveCodec.FromJson(text);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Save read failed (" + Path.GetFileName(file) + "): " + e.Message);
                return null;
            }
        }

        /// <summary>Asks for a write; <see cref="FlushIfDirty"/> performs it (once per frame at most).</summary>
        public void MarkDirty() => _dirty = true;

        public void FlushIfDirty()
        {
            if (_dirty) SaveNow();
        }

        public void SaveNow()
        {
            _dirty = false;
            if (Data == null) return;
            try
            {
                string json = SaveCodec.ToJson(Data);
                File.WriteAllText(_tmp, json);
                if (File.Exists(_path))
                {
                    try
                    {
                        File.Replace(_tmp, _path, _bak);
                        return;
                    }
                    catch (Exception)
                    {
                        // Some file systems do not support Replace: fall back to copy + move.
                        File.Copy(_path, _bak, true);
                        File.Delete(_path);
                    }
                }
                File.Move(_tmp, _path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Save write failed: " + e.Message);
            }
        }

        /// <summary>Deletes every save file and starts fresh (Settings → Reset progress).</summary>
        public SaveData Reset()
        {
            foreach (string f in new[] { _path, _tmp, _bak })
            {
                try { if (File.Exists(f)) File.Delete(f); }
                catch (Exception e) { Debug.LogWarning("Save delete failed: " + e.Message); }
            }
            Data = SaveCodec.NewSave();
            SaveNow();
            return Data;
        }
    }
}
