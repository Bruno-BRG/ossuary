using System;
using System.Text;
using System.Text.Json;
using Ossuary.Core;

namespace Ossuary.Desktop
{
    public sealed class Request
    {
        public string Op { get; set; }
        public string Seed { get; set; }
        public string Code { get; set; }
        public string Key { get; set; }
        public bool Shift { get; set; }
        public bool Ctrl { get; set; }
        /// <summary>The key is held down (OS autorepeat). Only calm walking honours it.</summary>
        public bool Repeat { get; set; }
        public int Cols { get; set; } = 110;
        public int Rows { get; set; } = 36;
        public int Theme { get; set; }
        public int Crt { get; set; } = 1;
        public int Scale { get; set; }
        /// <summary>1 draws map tiles two columns wide (square on screen).</summary>
        public int Square { get; set; }
        public bool Create { get; set; }
        /// <summary>With op "new": start today's daily challenge (fixed seed and hero) instead of a normal run.</summary>
        public bool Daily { get; set; }
        public string Lang { get; set; }
    }

    public static class Program
    {
        static readonly JsonSerializerOptions Json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };

        public static int Main(string[] args)
        {
            Console.InputEncoding = new UTF8Encoding(false);
            Console.OutputEncoding = new UTF8Encoding(false);
            SaveStore.LoadSettings();
            int bindVersion = KeyBindings.Current.Version, audioVersion = AudioSettings.Current.Version;
            var session = new Session();
            string line;
            while ((line = Console.ReadLine()) != null)
            {
                try
                {
                    if (line.Length > 8192) throw new ArgumentException("Request too large.");
                    var r = JsonSerializer.Deserialize<Request>(line, Json) ?? throw new ArgumentException("Missing request.");
                    switch (r.Op)
                    {
                        case "new":
                            ulong? seed = null;
                            if (r.Seed != null) seed = ulong.Parse(r.Seed);
                            ApplySettings(r);
                            if (r.Daily) session.NewDaily(DateTime.UtcNow); else session.New(seed, r.Create); session.Resize(r.Cols, r.Rows); break;
                        case "load":
                            ApplySettings(r);
                            session.New(); session.Resize(r.Cols, r.Rows); session.Load(); break;
                        case "title": RequireGame(session); session.SetTitle(true); break;
                        case "play": RequireGame(session); session.SetTitle(false); break;
                        case "key": RequireGame(session); if (r.Repeat) session.KeyRepeat(r.Code, r.Key, r.Shift, r.Ctrl); else session.Key(r.Code, r.Key, r.Shift, r.Ctrl); break;
                        case "resize": RequireGame(session); session.Resize(r.Cols, r.Rows); break;
                        case "display": RequireGame(session); ApplySettings(r); break;
                        case "frame": RequireGame(session); break;
                        default: throw new ArgumentException("Unknown operation: " + r.Op);
                    }
                    if (bindVersion != KeyBindings.Current.Version || audioVersion != AudioSettings.Current.Version)
                    {
                        bindVersion = KeyBindings.Current.Version; audioVersion = AudioSettings.Current.Version;
                        SaveStore.SaveSettings();
                    }
                    Console.WriteLine(JsonSerializer.Serialize(new { ok = true, frame = session.Draw() }, Json));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                    Console.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }, Json));
                }
            }
            return 0;
        }

        static void RequireGame(Session s)
        {
            if (s.Game == null) throw new InvalidOperationException("Start a run first.");
        }
        static void ApplySettings(Request r)
        {
            var s = DisplaySettings.Current;
            s.Apply((ThemePreset)Math.Clamp(r.Theme, 0, 3), (CrtLevel)Math.Clamp(r.Crt, 0, 2));
            s.Scale = Math.Clamp(r.Scale, 0, 3);
            s.Square = r.Square != 0;
            if (r.Lang != null) s.SetLanguage(r.Lang == "en" ? Lang.En : Lang.Pt);
        }
    }
}
