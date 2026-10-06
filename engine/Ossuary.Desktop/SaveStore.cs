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
        /// <summary>Bumped whenever simulation rules change, since a save is a replay (v2: regeneration, Mp, race traits; v3: spell catalogue and books; v4: perks, abilities, Vigor; v5: item rarity, affixes, new slots, artifacts; v6: gods and piety; v7: surfaces and elemental status; v8: balance pass, potions; v9: stealth and noise; v10: corruption, companions, bosses, factions, reputation and road events; v11: 260 spells, spell-backed wands, scrolls and potions, new items and uniques, animations).</summary>
        public int Version { get; set; } = 15;
        public string Seed { get; set; }
        public List<string> Keys { get; set; } = new List<string>();
        public string Info { get; set; } = "";
        /// <summary>Creation choice. Absent in older saves, which load as the default hero.</summary>
        public string Name { get; set; }
        public string Race { get; set; }
        public string Role { get; set; }
        /// <summary>The run began on the overworld (created through the creation screen) rather than on dungeon level 1.</summary>
        public bool Overworld { get; set; }
        /// <summary>"Normal", "Classic" or "Hardcore". Absent in older saves, which are Normal.</summary>
        public string Difficulty { get; set; } = "Normal";
        /// <summary>The daily challenge date when this is a daily run.</summary>
        public string Daily { get; set; } = "";
        /// <summary>The dead heroes this run could meet, as they were when it began. A replay must see the same ones.</summary>
        public List<Bones> Bones { get; set; } = new List<Bones>();
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
                return data != null && data.Version == 15 && ulong.TryParse(data.Seed, out _) ? data : null;
            }
            catch (Exception ex) { Console.Error.WriteLine("Unreadable save: " + ex.Message); return null; }
        }

        public static void DeleteSave()
        {
            try { if (File.Exists(SavePath)) File.Delete(SavePath); }
            catch (Exception ex) { Console.Error.WriteLine("Could not delete save: " + ex.Message); }
        }

        // ----------------------------------------------------------------- history and morgue

        const int HistoryCap = 200;
        static string HistoryPath => Path.Combine(Dir, "history.json");
        public static string MorgueDir => Path.Combine(Dir, "morgue");

        public static List<RunRecord> ReadHistory()
        {
            try
            {
                if (!File.Exists(HistoryPath)) return new List<RunRecord>();
                return JsonSerializer.Deserialize<List<RunRecord>>(File.ReadAllText(HistoryPath), Json) ?? new List<RunRecord>();
            }
            catch (Exception ex) { Console.Error.WriteLine("Unreadable history: " + ex.Message); return new List<RunRecord>(); }
        }

        /// <summary>Appends a finished run to the history (newest last, capped) and writes its morgue file. Never throws.</summary>
        public static string WriteRun(RunRecord record, string morgueText)
        {
            try
            {
                var list = ReadHistory();
                list.Add(record);
                if (list.Count > HistoryCap) list.RemoveRange(0, list.Count - HistoryCap);
                WriteAtomic(HistoryPath, JsonSerializer.Serialize(list, Json));
                Directory.CreateDirectory(MorgueDir);
                string path = Path.Combine(MorgueDir, Morgue.FileStem(record) + ".txt");
                File.WriteAllText(path, morgueText);
                return path;
            }
            catch (Exception ex) { Console.Error.WriteLine("Could not write the morgue: " + ex.Message); return null; }
        }

        // ----------------------------------------------------------------- achievements

        static string AchievementsPath => Path.Combine(Dir, "achievements.json");

        /// <summary>Achievement id to the date it was first unlocked.</summary>
        public static Dictionary<string, string> ReadAchievements()
        {
            try
            {
                if (!File.Exists(AchievementsPath)) return new Dictionary<string, string>();
                return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(AchievementsPath), Json) ?? new Dictionary<string, string>();
            }
            catch (Exception ex) { Console.Error.WriteLine("Unreadable achievements: " + ex.Message); return new Dictionary<string, string>(); }
        }

        public static void WriteAchievements(Dictionary<string, string> unlocked)
        {
            try { WriteAtomic(AchievementsPath, JsonSerializer.Serialize(unlocked, Json)); }
            catch (Exception ex) { Console.Error.WriteLine("Could not write achievements: " + ex.Message); }
        }

        // ----------------------------------------------------------------- bones

        const int BonesCap = 100;
        static string BonesPath => Path.Combine(Dir, "bones.json");

        public static List<Bones> ReadBones()
        {
            try
            {
                if (!File.Exists(BonesPath)) return new List<Bones>();
                return JsonSerializer.Deserialize<List<Bones>>(File.ReadAllText(BonesPath), Json) ?? new List<Bones>();
            }
            catch (Exception ex) { Console.Error.WriteLine("Unreadable bones: " + ex.Message); return new List<Bones>(); }
        }

        /// <summary>One hero per level: a newer death on the same level replaces the older one.</summary>
        public static void WriteBones(Bones bones)
        {
            try
            {
                var list = ReadBones();
                list.RemoveAll(b => b.Key == bones.Key);
                list.Add(bones);
                if (list.Count > BonesCap) list.RemoveRange(0, list.Count - BonesCap);
                WriteAtomic(BonesPath, JsonSerializer.Serialize(list, Json));
            }
            catch (Exception ex) { Console.Error.WriteLine("Could not write bones: " + ex.Message); }
        }

        public static void RemoveBones(string key)
        {
            try
            {
                var list = ReadBones();
                if (list.RemoveAll(b => b.Key == key) > 0) WriteAtomic(BonesPath, JsonSerializer.Serialize(list, Json));
            }
            catch (Exception ex) { Console.Error.WriteLine("Could not update bones: " + ex.Message); }
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
