using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Ossuary.Core;

namespace Ossuary.Desktop
{
    /// <summary>The persisted run: its seed and every canonical key applied since the start.</summary>
    public sealed class SaveData
    {
        /// <summary>Bumped whenever simulation rules change, since a save is a replay (v2: regeneration, Mp, race traits; v3: spell catalogue and books; v4: perks, abilities, Vigor; v5: item rarity, affixes, new slots, artifacts; v6: gods and piety; v7: surfaces and elemental status; v8: balance pass, potions).</summary>
        public int Version { get; set; } = 8;
        public string Seed { get; set; }
        public List<string> Keys { get; set; } = new List<string>();
        public string Info { get; set; } = "";
        /// <summary>Creation choice. Absent in older saves, which load as the default hero.</summary>
        public string Name { get; set; }
        public string Race { get; set; }
        public string Role { get; set; }
        /// <summary>The run began on the overworld (created through the creation screen) rather than on dungeon level 1.</summary>
        public bool Overworld { get; set; }
    }

    /// <summary>
    /// Files under the per-user data directory (override with OSSUARY_DATA, which the tests use).
    /// A save is a replay: the simulation is deterministic, so seed + keys rebuilds the run exactly.
    /// </summary>
    public static class SaveStore
    {
        static readonly JsonSerializerOptions Json = new JsonSerializerOptions { WriteIndented = false };

        public static string Dir
        {
            get
            {
                string env = Environment.GetEnvironmentVariable("OSSUARY_DATA");
                return !string.IsNullOrEmpty(env) ? env : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ossuary");
            }
        }

        static string SavePath => Path.Combine(Dir, "save.json");
        static string SettingsPath => Path.Combine(Dir, "settings.json");

        static void WriteAtomic(string path, string text)
        {
            Directory.CreateDirectory(Dir);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, text);
            File.Move(tmp, path, true);
        }

        public static void WriteSave(SaveData data) => WriteAtomic(SavePath, JsonSerializer.Serialize(data, Json));

        public static SaveData ReadSave()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                var data = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(SavePath), Json);
                return data != null && data.Version == 8 && ulong.TryParse(data.Seed, out _) ? data : null;
            }
            catch (Exception ex) { Console.Error.WriteLine("Unreadable save: " + ex.Message); return null; }
        }

        public static void DeleteSave()
        {
            try { if (File.Exists(SavePath)) File.Delete(SavePath); }
            catch (Exception ex) { Console.Error.WriteLine("Could not delete save: " + ex.Message); }
        }

        // ----------------------------------------------------------------- settings

        sealed class SettingsData
        {
            public string Binds { get; set; } = "";
            public string Volume { get; set; } = "";
        }

        public static void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return;
                var data = JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(SettingsPath), Json);
                if (data == null) return;
                KeyBindings.Current.Load(data.Binds);
                AudioSettings.Current.Load(data.Volume);
            }
            catch (Exception ex) { Console.Error.WriteLine("Unreadable settings: " + ex.Message); }
        }

        public static void SaveSettings()
        {
            try
            {
                WriteAtomic(SettingsPath, JsonSerializer.Serialize(new SettingsData
                {
                    Binds = KeyBindings.Current.Serialize(),
                    Volume = AudioSettings.Current.Serialize(),
                }, Json));
            }
            catch (Exception ex) { Console.Error.WriteLine("Could not save settings: " + ex.Message); }
        }
    }
}
