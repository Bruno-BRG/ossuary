using System;
using System.IO;
using System.Text;
using Ossuary.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ossuary.EditorTools
{
    /// <summary>
    /// Command-line entry points for building and inspecting the game.
    ///
    /// Everything here is reachable with
    ///   Unity.exe -batchmode -nographics -quit -projectPath &lt;p&gt;
    ///            -executeMethod Ossuary.EditorTools.OssuaryCli.&lt;Method&gt;
    ///
    /// DumpFrame is the important one: it renders real game state to plain text in
    /// the log, which is the only way to verify the UI headlessly.
    /// </summary>
    public static class OssuaryCli
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        /// <summary>Creates Assets/Scenes/Main.unity with the bootstrap object in it.</summary>
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("Ossuary");
            root.AddComponent<Ossuary.Gameplay.GameApp>();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[cli] scene created at " + ScenePath);
        }

        // ------------------------------------------------------------- builds

        public static void BuildWindows64() => Build("Windows", "Ossuary.exe", BuildTarget.StandaloneWindows64, "StandaloneWindows64");

        public static void BuildLinux64() => Build("Linux", "Ossuary.x86_64", BuildTarget.StandaloneLinux64, "StandaloneLinux64");

        static void Build(string label, string exe, BuildTarget target, string subdir)
        {
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                Debug.Log("[cli] switching build target to " + target);
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(target))
                {
                    Debug.LogError("[cli] could not switch build target; the module may not be installed");
                    Environment.Exit(3);
                }
            }

            string dir = Path.GetFullPath(Path.Combine("Builds", subdir));
            Directory.CreateDirectory(dir);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(dir, exe),
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary s = report.summary;
            Debug.Log($"[cli] {label} build: {s.result} ({s.totalSize / 1024} KiB, {s.totalErrors} errors, {s.totalWarnings} warnings)");
            if (s.result != BuildResult.Succeeded) Environment.Exit(3);
        }

        // ----------------------------------------------------------- inspection

        /// <summary>
        /// Composes one real frame and writes it to the log as plain text.
        /// This is how the ASCII interface gets reviewed without a display.
        /// </summary>
        public static void DumpFrame()
        {
            var game = new Game(12345);
            Run(game, 40);

            var hud = new GameHud(game);
            hud.Ui.Resize(100, 34);
            string art = hud.Draw().ToAscii();

            Debug.Log("\n" + Box("FRAME: dungeon, turn " + game.Turn + ", depth " + game.Depth));
            Debug.Log("\n" + art);
            Debug.Log("\n" + Box("END FRAME"));
        }

        /// <summary>Same, but on the overworld, so the world map gets reviewed too.</summary>
        public static void DumpOverworld()
        {
            var game = new Game(777);
            game.LeaveToOverworld();

            var hud = new GameHud(game);
            hud.Ui.Resize(100, 34);
            Debug.Log("\n" + Box("FRAME: overworld"));
            Debug.Log("\n" + hud.Draw().ToAscii());
            Debug.Log("\n" + Box("END FRAME"));
        }

        /// <summary>Dumps a fresh dungeon level straight from the generator.</summary>
        public static void DumpLevel()
        {
            var rng = new Rng(31337);
            var opts = new Ossuary.Core.Gen.GenOptions
            {
                Width = 79, Height = 25, Style = Ossuary.Core.Gen.LevelStyle.Rooms,
                WallStyle = 0, MaxRooms = 10, AllowStairsUp = true, AllowStairsDown = true,
            };
            var map = Ossuary.Core.Gen.DungeonGen.Generate(opts, rng,
                out var specials, out var starts);

            Debug.Log("\n" + Box("GENERATED LEVEL: " + map.W + "x" + map.H + ", " + map.CountWalkable() + " walkable"));
            Debug.Log("\n" + map.ToAscii(false, -1, -1));
            Debug.Log("\n" + Box("END LEVEL"));
        }

        static void Run(Game game, int turns)
        {
            var cmd = new Commands(game);
            var rng = game.Rng;
            // Real Commands.Execute labels (verified against Commands.cs): g=pick up,
            // s=search, i=inventory, l=look, f=fire, k=kick.
            string[] acts = { "move-n", "move-s", "move-e", "move-w", "move-se", "g", "i", "s" };

            for (int i = 0; i < turns && game.Mode != GameMode.GameOver; i++)
            {
                try
                {
                    cmd.Execute(acts[rng.Range(0, acts.Length)]);
                    if (game.UiState.IsTargeting) game.ResolveTargeting(game.Player.X, game.Player.Y);
                    if (game.PendingChoice.Active && game.PendingChoice.Items.Count > 0)
                        game.PendingChoice.Clear();
                    if (game.UiState.Active != Panel.None) game.UiState.Active = Panel.None;
                }
                catch (Exception e)
                {
                    Debug.LogError("[cli] action threw: " + e);
                    Environment.Exit(4);
                }
            }
        }

        static string Box(string title)
        {
            var sb = new StringBuilder();
            sb.Append("+---- ").Append(title).Append(" ----+");
            return sb.ToString();
        }

        /// <summary>
        /// Dumps the glyph atlas itself and reports what the renderer actually built.
        /// When text renders as nothing but the background quads do, the question is
        /// always the same: did the font load, and does the atlas contain coverage?
        /// </summary>
        public static void RenderDiagnostics()
        {
            var font = Resources.Load<Font>("Fonts/CascadiaMono");
            Debug.Log("[diag] Resources font: " + (font == null ? "NULL" : font.name + " dynamic=" + font.dynamic));

            if (font == null)
            {
                font = Resources.Load<Font>("Fonts/Consolas");
                Debug.Log("[diag] fallback font: " + (font == null ? "NULL" : font.name));
            }

            var shader = Shader.Find("Ossuary/Terminal");
            Debug.Log("[diag] shader Ossuary/Terminal: " + (shader == null ? "NULL" : shader.name));
            var crt = Shader.Find("Ossuary/Crt");
            Debug.Log("[diag] shader Ossuary/Crt: " + (crt == null ? "NULL" : crt.name));

            var root = new GameObject("Diag");
            try
            {
                var r = root.AddComponent<Ossuary.Render.TerminalRenderer>();
                r.EnsureInitialized();
                Debug.Log("[diag] ScreenAscii length: " + (r.ToAsciiSnapshot(null) == null ? -1 : 0));

                var atlasField = typeof(Ossuary.Render.TerminalRenderer)
                    .GetField("_atlas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var atlas = atlasField?.GetValue(r) as Ossuary.Render.GlyphAtlas;
                if (atlas == null) { Debug.Log("[diag] atlas: NULL"); return; }

                int slotsTotal = Ossuary.Render.GlyphAtlas.Charset.Length;
                int withGlyph = 0;
                foreach (var c in Ossuary.Render.GlyphAtlas.Charset) if (atlas.Has(c)) withGlyph++;

                // atlas.Texture can be null when no font texture could be resolved, which
                // is itself the finding worth reporting rather than a crash.
                string texInfo = atlas.Texture == null ? "NULL" :
                    atlas.Texture.width + "x" + atlas.Texture.height;
                Debug.Log("[diag] atlas: " + texInfo +
                    " cell " + atlas.CellW + "x" + atlas.CellH +
                    " baseline " + atlas.Baseline +
                    " glyphs " + withGlyph + "/" + slotsTotal);

                // What the material actually samples. If this is not the font texture,
                // the shader has been handed a default white surface and every glyph
                // renders as a solid block — which looks exactly like a coverage bug.
                var glyphMatField = typeof(Ossuary.Render.TerminalRenderer)
                    .GetField("_glyphMat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var gm = glyphMatField?.GetValue(r) as Material;
                if (gm != null)
                {
                    var bound = gm.HasProperty("_MainTex") ? gm.GetTexture("_MainTex") : null;
                    var boundAtlas = gm.HasProperty("_Atlas") ? gm.GetTexture("_Atlas") : null;
                    Debug.Log("[diag] glyph material " + gm.shader.name +
                        " _MainTex=" + (bound == null ? "null" : bound.name + " " + bound.width + "x" + bound.height) +
                        " _Atlas=" + (boundAtlas == null ? "null" : boundAtlas.name + " " + boundAtlas.width + "x" + boundAtlas.height) +
                        " sameAsAtlas=" + (atlas.Texture != null && ReferenceEquals(bound, atlas.Texture)));
                }

                string dir = Path.GetFullPath(Path.Combine("Builds", "shots"));
                Directory.CreateDirectory(dir);

                // Dump the raw font texture as well. Comparing the two is what tells us
                // whether a wrong atlas is our copy step or the readback itself.
                if (atlas.Font != null && atlas.Font.material != null)
                {
                    var fontTex = atlas.Font.material.mainTexture;
                    if (fontTex is Texture2D f2d)
                    {
                        var raw = RenderTexture.GetTemporary(f2d.width, f2d.height, 0, RenderTextureFormat.ARGB32);
                        Graphics.Blit(f2d, raw);
                        var readback = new Texture2D(f2d.width, f2d.height, TextureFormat.RGBA32, false);
                        RenderTexture.active = raw;
                        readback.ReadPixels(new Rect(0, 0, f2d.width, f2d.height), 0, 0, false);
                        readback.Apply();
                        RenderTexture.active = null;
                        RenderTexture.ReleaseTemporary(raw);

                        var rawPx = readback.GetPixels32();
                        byte amin = 255, amax = 0;
                        for (int i = 0; i < rawPx.Length; i++)
                        {
                            if (rawPx[i].a < amin) amin = rawPx[i].a;
                            if (rawPx[i].a > amax) amax = rawPx[i].a;
                        }
                        Debug.Log($"[diag] font texture {f2d.width}x{f2d.height} fmt={f2d.format} alphaRange={amin}..{amax}");

                        var rdump = new Texture2D(f2d.width, f2d.height, TextureFormat.RGBA32, false);
                        var rflat = new Color32[rawPx.Length];
                        for (int i = 0; i < rawPx.Length; i++)
                        {
                            byte v = rawPx[i].a;
                            rflat[i] = new Color32(v, v, v, 255);
                        }
                        rdump.SetPixels32(rflat);
                        rdump.Apply();
                        File.WriteAllBytes(Path.Combine(dir, "font-raw.png"), rdump.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(rdump);
                        UnityEngine.Object.DestroyImmediate(readback);
                        Debug.Log("[diag] raw font dump written");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Renders real frames to PNG through an offscreen camera.
        ///
        /// This is the only way to actually SEE the terminal renderer in CI: the
        /// glyph atlas orientation, the UV corner order and the palette are all
        /// verified by looking at the image, not by any assertion.
        /// Must be run WITHOUT -nographics so a graphics device exists.
        /// </summary>
        public static void CaptureFrames()
        {
            int w = 1600, h = 900;
            string dir = Path.GetFullPath(Path.Combine("Builds", "shots"));
            Directory.CreateDirectory(dir);

            Capture(w, h, dir, "01-dungeon", g => { }, 0);
            Capture(w, h, dir, "02-dungeon-explored", g => Play(g, 140), 0);
            Capture(w, h, dir, "03-inventory", g => Play(g, 60), Panel.Inventory);
            Capture(w, h, dir, "04-character", g => Play(g, 60), Panel.Character);
            Capture(w, h, dir, "05-overworld", g => { g.LeaveToOverworld(); }, 0);
            Debug.Log("[cli] frames written to " + dir);
        }

        static void Play(Game game, int turns)
        {
            var cmd = new Commands(game);
            var rng = game.Rng;
            string[] acts = { "move-n", "move-s", "move-e", "move-w", "move-se", "g", "s", "l" };
            for (int i = 0; i < turns && game.Mode != GameMode.GameOver; i++)
            {
                try
                {
                    cmd.Execute(acts[rng.Range(0, acts.Length)]);
                    if (game.UiState.IsTargeting) game.ResolveTargeting(game.Player.X, game.Player.Y);
                    if (game.PendingChoice.Active && game.PendingChoice.Items.Count > 0) game.PendingChoice.Clear();
                }
                catch (Exception e) { Debug.LogWarning("[cli] action skipped: " + e.Message); }
            }
        }

        /// <summary>
        /// Renders a known alphabet/paragraph through the real renderer mesh and
        /// shaders to PNG, and logs the slot + UV rect of probe glyphs. If the
        /// atlas is good but this image is wrong, the bug is in mesh UVs, camera
        /// fit or the shader — not in extraction.
        /// Must be run WITHOUT -nographics so a graphics device exists.
        /// </summary>
        public static void DumpTestPattern()
        {
            int w = 1600, h = 900;
            string dir = Path.GetFullPath(Path.Combine("Builds", "shots"));
            Directory.CreateDirectory(dir);

            var root = new GameObject("TestPattern");
            try
            {
                var camObj = new GameObject("Cam");
                camObj.transform.SetParent(root.transform, false);
                var cam = camObj.AddComponent<Camera>();
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 1f);
                cam.nearClipPlane = -50f;
                cam.farClipPlane = 50f;

                var renderer = root.AddComponent<Ossuary.Render.TerminalRenderer>();
                renderer.EnsureInitialized();

                var tb = new TextBuilder(40, 12);
                tb.Clear();
                var fg = Rgb.FromHex(0xE8E0D0);
                tb.Write(1, 1, "ABCDEFGHIJKLMNOPQRSTUVWXYZ", fg, true);
                tb.Write(1, 2, "abcdefghijklmnopqrstuvwxyz", fg, true);
                tb.Write(1, 3, "0123456789!@#$%^&*()+-=", fg, true);
                tb.Write(1, 4, "#.+,<>^\"'_I pillars", fg, true);
                tb.Write(1, 5, "THE QUICK BROWN FOX JUMPS", fg, true);
                tb.Write(1, 6, "over the lazy dog 0123456789", fg, true);

                var atlas = renderer.Atlas;
                Debug.Log("[pattern] cellH=" + renderer.CellHeight);
                foreach (char c in new[] { 'A', '#', '@', '?', '█', '─', '▶', 'd', 'b', 'w', '=', '^', '>', '"', '\'', ',', '.', '_', '-' })
                {
                    Vector4 uv = atlas.UvOf(c);
                    Vector4 ink = atlas.InkOf(c);
                    Debug.Log($"[pattern] '{c}' uv=({uv.x:F4},{uv.y:F4})+({uv.z:F4},{uv.w:F4}) ink=({ink.x},{ink.y})+({ink.z}x{ink.w})");
                }

                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;

                renderer.Present(tb);
                renderer.FitCameraPixels(cam, tb.Width, tb.Height, w, h);

                // Kill the glow pass for this shot: its shader shrinks UVs toward
                // the texture centre, which was a halo trick for full-cell UVs and
                // is garbage sampling for tight glyph rects. If the letters snap
                // into place with glow off, the glyph pass was right all along.
                var glowField = typeof(Ossuary.Render.TerminalRenderer).GetField("_glowRenderer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var glowR = glowField?.GetValue(renderer) as MeshRenderer;
                if (glowR != null) glowR.enabled = false;

                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();

                var png = new Texture2D(w, h, TextureFormat.RGB24, false);
                png.LoadRawTextureData(tex.GetRawTextureData());
                png.Apply();
                File.WriteAllBytes(Path.Combine(dir, "testpattern.png"), png.EncodeToPNG());

                // Second shot: V-flipped UV cache. The font's uvBottomLeft.y sits
                // ABOVE uvTopRight.y (flipped names), so the stored rect top may be
                // sampled as the bottom. Flipping (x,y,w,h)->(x,y-h,w,h) with the
                // renderer untouched puts world-bottom on the true glyph bottom.
                // If THIS shot reads correctly, the convention is confirmed.
                var cacheField = typeof(Ossuary.Render.TerminalRenderer).GetField("_uvCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var cache = cacheField?.GetValue(renderer) as UnityEngine.Vector4[];
                if (cache != null)
                {
                    for (int i = 0; i < cache.Length; i++)
                    {
                        var v = cache[i];
                        if (v.w > 0f) cache[i] = new UnityEngine.Vector4(v.x, v.y - v.w, v.z, v.w);
                    }
                    renderer.Present(tb);
                    cam.Render();
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                    tex.Apply();
                    var png2 = new Texture2D(w, h, TextureFormat.RGB24, false);
                    png2.LoadRawTextureData(tex.GetRawTextureData());
                    png2.Apply();
                    File.WriteAllBytes(Path.Combine(dir, "testpattern_flipped.png"), png2.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(png2);
                    Debug.Log("[pattern] flipped shot written");
                }

                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(tex);
                UnityEngine.Object.DestroyImmediate(png);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                Debug.Log("[pattern] written to " + Path.Combine(dir, "testpattern.png"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        static void Capture(int w, int h, string dir, string name, Action<Game> setup, Panel panel)
        {            var game = new Game(20260930);
            setup(game);

            var root = new GameObject("Shot_" + name);
            try
            {
                var camObj = new GameObject("Cam");
                camObj.transform.SetParent(root.transform, false);
                var cam = camObj.AddComponent<Camera>();
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 1f);
                cam.nearClipPlane = -50f;
                cam.farClipPlane = 50f;

                var renderer = root.AddComponent<Ossuary.Render.TerminalRenderer>();
                renderer.EnsureInitialized();   // Awake does not run in edit mode

                var hud = new GameHud(game);
                game.UiState.Active = panel;

                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;

                // Draw once so the mesh and atlas exist, then size and render.
                // Choose a grid that matches the capture's aspect ratio, so the terminal fills
                // the frame instead of being scaled down to fit a mismatched height
                // and leaving most of the screen empty.
                float cellAspect = renderer.CellHeight;   // world height of one cell, one unit wide
                int rows = 44;
                int cols = Mathf.RoundToInt(rows * cellAspect * (w / (float)h));
                cols = Mathf.Clamp(cols, 40, 400);

                hud.Ui.Resize(cols, rows);
                renderer.Present(hud.Draw());
                renderer.FitCameraPixels(cam, cols, rows, w, h);

                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();

                var png = new Texture2D(w, h, TextureFormat.RGB24, false);
                png.LoadRawTextureData(tex.GetRawTextureData());
                png.Apply();
                File.WriteAllBytes(Path.Combine(dir, name + ".png"), png.EncodeToPNG());

                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(tex);
                UnityEngine.Object.DestroyImmediate(png);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}