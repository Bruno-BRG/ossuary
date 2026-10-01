using System.Collections.Generic;
using Ossuary.Core;
using UnityEngine;

namespace Ossuary.Render
{
    /// <summary>
    /// Draws a <see cref="TextBuilder"/> as a lit, CRT-flavoured screen of glyph quads.
    ///
    /// Structure (all children of this transform, mesh space == world space so keep this
    /// GameObject at the origin with unit scale):
    ///
    ///   Background   one quad per horizontal run of equal bg colour, solid colour write.
    ///   Glow         re-draws the *glyph* mesh with an additive material (bloom halo).
    ///   Glyphs       the real glyph mesh, alpha blended over the background.
    ///   CRT          a full-screen quad in front of everything, scanlines + vignette.
    ///
    /// Draw order is pinned twice over (distinct material render queues *and*
    /// Renderer.sortingOrder) because in the Built-in pipeline sortingOrder is only
    /// honoured inside the transparent range, and distance sorting must never decide
    /// the order of our passes.
    ///
    /// Shader.Find only resolves for shaders in Resources or in Always Included Shaders
    /// (or in the Editor). If Ossuary/Terminal is missing everything is disabled silently;
    /// a missing *font* is the only case that logs, because that is a content bug.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TerminalRenderer : MonoBehaviour
    {
        // ---------------------------------------------------------------- shaders

        public const string TerminalShaderName = "Ossuary/Terminal";
        public const string CrtShaderName = "Ossuary/Crt";

        // Property ids resolved once: string-keyed lookups are what actually cost time
        // when you set them every frame in LateUpdate.
        static readonly int PropAtlas = Shader.PropertyToID("_Atlas");
        static readonly int PropMainTex = Shader.PropertyToID("_MainTex");
        static readonly int PropMode = Shader.PropertyToID("_Mode");
        static readonly int PropGlowExpand = Shader.PropertyToID("_GlowExpand");
        static readonly int PropBoost = Shader.PropertyToID("_Boost");
        static readonly int PropScanline = Shader.PropertyToID("_Scanline");
        static readonly int PropVignette = Shader.PropertyToID("_Vignette");
        static readonly int PropTime = Shader.PropertyToID("_Time");

        // Queue / sortingOrder pairs. Ascending == drawn ascending == drawn underneath.
        const int QueueBackground = 3000, OrderBackground = 0;
        const int QueueGlow = 3010, OrderGlow = 10;
        const int QueueGlyph = 3020, OrderGlyph = 20;
        const int QueueCrt = 3030, OrderCrt = 30;

        /// <summary>Pixel reach of the additive halo, in atlas cells.</summary>
        const float GlowExpand = 0.35f;

        /// <summary>Intensity of the additive halo.</summary>
        const float GlowBoost = 0.55f;

        /// <summary>
        /// Vertical orientation of the atlas texture.
        ///
        /// GlyphAtlas fills its Color32[] bottom-up (row 0 is the glyph's bottom
        /// row: ReadPixels and SetPixels32 are both bottom-up, and the copy maps
        /// uvBottomLeft straight onto a row index with no flip). UvOf() therefore
        /// hands back rects that sample straight. When this was true the quads
        /// sampled each padded cell upside down: visible, but mirrored.
        /// </summary>
        const bool AtlasIsVFlipped = false;

        // ------------------------------------------------------------------ setup

        /// <summary>Optional font override. Null falls back to Resources, then gives up once.</summary>
        public Font Font;

        /// <summary>Rasterisation size handed to <see cref="GlyphAtlas"/>. Changing it rebuilds the atlas.</summary>
        public int CellSize = 28;

        /// <summary>Camera used by the automatic fit; assign one and the grid re-centres on resize.</summary>
        public Camera Cam;

        /// <summary>How far in front of the grid the fitted camera sits. Keeps the mesh inside the default clip range.</summary>
        public float CameraDistance = 10f;

        /// <summary>Round the computed scale to whole device pixels per cell so glyphs stay crisp.</summary>
        public bool SnapToWholePixels = true;

        [SerializeField]
        [Tooltip("Cells painted in this colour are assumed to be covered by the camera clear, so no background quad is emitted for them.")]
        Color _screenBackground = new Color32(0x0A, 0x0A, 0x0C, 0xFF);

        /// <summary>
        /// The colour the camera clears to. Background quads are only emitted where a cell
        /// differs from this, which keeps the background mesh empty on a plain screen.
        /// Stored as a Color because Core.Rgb is not [Serializable] and would not persist.
        /// </summary>
        public Rgb ScreenBackground
        {
            get { return new Rgb((byte)(_screenBackground.r * 255f + 0.5f), (byte)(_screenBackground.g * 255f + 0.5f), (byte)(_screenBackground.b * 255f + 0.5f)); }
            set { _screenBackground = new Color(value.R / 255f, value.G / 255f, value.B / 255f, 1f); }
        }

        /// <summary>World height of one character cell; 0 until the atlas exists.</summary>
        public float CellHeight { get { return _cellH; } }

        /// <summary>Columns of the last presented screen.</summary>
        public int Columns { get { return _cols; } }

        /// <summary>Rows of the last presented screen.</summary>
        public int Rows { get { return _rows; } }

        /// <summary>The atlas backing the current mesh, or null if no font could be loaded.</summary>
        public GlyphAtlas Atlas { get { return _atlas; } }

        // -------------------------------------------------------------- internals

        const int ForceRebuild = int.MinValue;

        GlyphAtlas _atlas;
        Font _atlasFont;
        int _atlasSize;
        float _cellH;

        // Per-slot (u0, v0, du, dv). A flat array indexed by atlas slot keeps the per-cell
        // path free of dictionary lookups and of any per-call allocation.
        Vector4[] _uvCache;

        Mesh _glyphMesh, _bgMesh, _crtMesh;
        MeshFilter _glyphFilter, _bgFilter, _crtFilter;
        MeshRenderer _glyphRenderer, _bgRenderer, _crtRenderer, _glowRenderer;
        Material _glyphMat, _glowMat, _bgMat, _crtMat;
        bool _built;

        // Allocated once, at construction, and only ever Clear()ed afterwards, so Present()
        // allocates nothing. Capacities cover a typical 80x50 grid with headroom; growth
        // past them just resizes the backing array.
        readonly List<Vector3> _glyphVerts = new List<Vector3>(4096);
        readonly List<Vector2> _glyphUvs = new List<Vector2>(4096);
        readonly List<Color> _glyphCols = new List<Color>(4096);
        readonly List<int> _glyphTris = new List<int>(6144);

        readonly List<Vector3> _bgVerts = new List<Vector3>(1024);
        readonly List<Color> _bgCols = new List<Color>(1024);
        readonly List<int> _bgTris = new List<int>(1536);

        int _cols, _rows;
        int _lastVersion = ForceRebuild;
        bool _glyphWasDrawn, _bgWasDrawn, _warnedFont;

        // ------------------------------------------------------------- lifecycle

        void Awake()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// Builds the renderer graph if it does not exist yet.
        ///
        /// Awake does not run in edit mode, so editor tooling that adds this
        /// component and wants to render a frame has to call this explicitly;
        /// otherwise there are no materials, no meshes, and the camera renders a
        /// blank frame that looks like a content bug rather than a lifecycle one.
        /// </summary>
        public void EnsureInitialized()
        {
            if (_built) return;
            _built = true;
            BuildChildren();
            BuildAtlas();
        }

        void OnDestroy()
        {
            DestroyRef(_glyphMesh); DestroyRef(_bgMesh); DestroyRef(_crtMesh);
            DestroyRef(_glyphMat); DestroyRef(_glowMat); DestroyRef(_bgMat); DestroyRef(_crtMat);
            // The atlas texture belongs to the Font, not to us, so it must not be destroyed.
        }

        /// <summary>
        /// Creates the four child objects, their meshes and their materials.
        /// Materials come from Shader.Find and may legitimately be null (shader not
        /// included in the build); those renderers are simply left disabled.
        /// </summary>
        void BuildChildren()
        {
            Shader terminal = Shader.Find(TerminalShaderName);
            Shader crt = Shader.Find(CrtShaderName);

            // --- background ---------------------------------------------------
            _bgMat = NewMaterial(terminal, "TerminalBg", QueueBackground, OrderBackground);
            // Solid mode: the background pass paints flat cell colours and must not
            // sample the glyph atlas at all. Relying on _GlowExpand == 0 for this
            // would silently break the moment the glyph pass stops expanding.
            SetFloatIfPresent(_bgMat, PropMode, 1f);
            _bgRenderer = NewChild("Background", out _bgFilter, _bgMat, OrderBackground);
            _bgMesh = NewMesh("TerminalBgMesh");
            _bgMesh.MarkDynamic();
            _bgFilter.sharedMesh = _bgMesh;

            // --- glow ---------------------------------------------------------
            // Same mesh instance as the glyph pass, so the halo can never drift out of
            // sync with the text: there is only one copy of the geometry.
            _glowMat = NewMaterial(terminal, "TerminalGlow", QueueGlow, OrderGlow);
            _glowRenderer = NewChild("Glow", out MeshFilter glowFilter, _glowMat, OrderGlow);
            SetFloatIfPresent(_glowMat, PropMode, 0f);
            SetFloatIfPresent(_glowMat, PropGlowExpand, GlowExpand);
            SetFloatIfPresent(_glowMat, PropBoost, GlowBoost);
            _glowRenderer.enabled = false;

            // --- glyphs -------------------------------------------------------
            _glyphMat = NewMaterial(terminal, "TerminalGlyph", QueueGlyph, OrderGlyph);
            _glyphRenderer = NewChild("Glyphs", out _glyphFilter, _glyphMat, OrderGlyph);
            SetFloatIfPresent(_glyphMat, PropMode, 0f);
            SetFloatIfPresent(_glyphMat, PropGlowExpand, 0f);
            SetFloatIfPresent(_glyphMat, PropBoost, 1f);
            _glyphMesh = NewMesh("TerminalGlyphMesh");
            _glyphMesh.MarkDynamic();
            _glyphFilter.sharedMesh = _glyphMesh;

            // The glow pass shares the glyph mesh; assigned after both exist.
            glowFilter.sharedMesh = _glyphMesh;

            // --- CRT ----------------------------------------------------------
            // A 2x2 quad centred on the origin, nudged in front so it depth-tests over
            // the text if the terminal shader ever turns ZTest back on.
            _crtMat = NewMaterial(crt, "TerminalCrt", QueueCrt, OrderCrt);
            _crtRenderer = NewChild("CRT", out _crtFilter, _crtMat, OrderCrt);
            _crtRenderer.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            _crtMesh = BuildCrtQuad();
            _crtFilter.sharedMesh = _crtMesh;
            _crtRenderer.enabled = _crtMat != null;

            _bgRenderer.enabled = _bgMat != null && _glyphMat != null;
            _glyphRenderer.enabled = _glyphMat != null;
        }

        MeshRenderer NewChild(string name, out MeshFilter filter, Material mat, int order = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            filter = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sortingOrder = order;
            // sharedMaterial, not material: material() clones per-renderer and we only
            // ever need one instance of each of these.
            if (mat != null) mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return mr;
        }

        static Material NewMaterial(Shader shader, string name, int queue, int order)
        {
            if (shader == null) return null;
            // Material has no sortingOrder; draw order comes from the render queue
            // plus Renderer.sortingOrder, which NewChild sets.
            return new Material(shader)
            {
                name = name,
                hideFlags = HideFlags.DontSave,
                renderQueue = queue,
            };
        }

        static Mesh NewMesh(string name)
        {
            // Nothing in this file ever reads normals, tangents or lightmap UVs, and a
            // fresh Mesh has those channels empty already.
            return new Mesh { name = name, hideFlags = HideFlags.DontSave };
        }

        /// <summary>
        /// The CRT overlay: 2x2 world units centred on the origin, UVs 0..1 so the shader
        /// can address the whole screen with them.
        /// </summary>
        static Mesh BuildCrtQuad()
        {
            var m = new Mesh { name = "TerminalCrtQuad", hideFlags = HideFlags.DontSave };

            // Same BL, TL, TR, BR ordering as the cell quads (see BuildGlyphMesh).
            m.vertices = new[]
            {
                new Vector3(-1f, -1f, 0f),
                new Vector3(-1f, 1f, 0f),
                new Vector3(1f, 1f, 0f),
                new Vector3(1f, -1f, 0f),
            };
            m.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f),
            };
            m.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            m.bounds = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(2f, 2f, 0.1f));
            return m;
        }

        // ----------------------------------------------------------------- atlas

        /// <summary>
        /// Resolves the font and rasterises the atlas, reusing it unless the font or the
        /// requested size actually changed. A missing font disables rendering and warns
        /// exactly once.
        /// </summary>
        void BuildAtlas()
        {
            Font font = Font;
            if (font == null) font = Resources.Load<Font>("Fonts/CascadiaMono");
            if (font == null) font = Resources.Load<Font>("Fonts/Consolas");

            int size = Mathf.Clamp(CellSize, 8, 512);

            if (_atlas != null && _atlasFont == font && _atlasSize == size) return;

            if (font == null)
            {
                if (!_warnedFont)
                {
                    _warnedFont = true;
                    Debug.LogWarning("[TerminalRenderer] no font available: assign Font, or drop one at Resources/Fonts/CascadiaMono. Text will not render.", this);
                }
                DisableRendering();
                return;
            }

            // The atlas now borrows the font's own texture rather than owning a copy, so there
            // is nothing to release here and destroying it would break the font.
            _atlas = new GlyphAtlas(font, size, null);
            _atlasFont = font;
            _atlasSize = size;
            CacheUvs();

            // Cell height in world units = the font's own cell aspect. One cell is 1.0
            // world unit wide, so keeping the atlas' pixel aspect here is what stops
            // glyphs from being stretched: a font whose cells are taller than they are
            // wide stays taller than wide on screen.
            _cellH = _atlas.CellH / (float)_atlas.CellW;

            ApplyAtlasToMaterials();

            

            // No terminal shader means nothing can be drawn at all. BuildChildren has
            // already parked the renderers; Present() checks the material directly.
        }

        /// <summary>
        /// Bakes each atlas slot's UV rect into a flat array so the per-cell loop is one
        /// array index and four adds. UvOf() hands back the *padded* pitch-sized rect,
        /// which is what we want: sampling the padded cell gives the glow room to expand
        /// while never crossing into the neighbouring cell.
        /// </summary>
        void CacheUvs()
        {
            int slots = GlyphAtlas.Charset.Length;
            if (_uvCache == null || _uvCache.Length != slots) _uvCache = new Vector4[slots];

            for (int i = 0; i < slots; i++)
            {
                // GlyphAtlas keys its rects by character now — they come straight from
                // the font engine's own layout — so the cache is indexed by codepoint.
                _uvCache[i] = _atlas.UvOf(GlyphAtlas.Charset[i]);
            }
        }

        void ApplyAtlasToMaterials()
        {
            if (_atlas == null) return;
            Texture tex = _atlas.Texture;

            // The shader author picks the property name; support both spellings and skip
            // whichever this shader does not declare.
            SetTextureIfPresent(_glyphMat, PropAtlas, tex);
            SetTextureIfPresent(_glyphMat, PropMainTex, tex);
            SetTextureIfPresent(_glowMat, PropAtlas, tex);
            SetTextureIfPresent(_glowMat, PropMainTex, tex);
            // The background pass is a solid colour write; the atlas must not tint it.
            SetTextureIfPresent(_bgMat, PropAtlas, tex);
            SetTextureIfPresent(_bgMat, PropMainTex, tex);
        }

        // --------------------------------------------------------------- present

        /// <summary>
        /// Rebuilds the meshes from <paramref name="screen"/> and re-fits the camera.
        /// The game is turn-based, so a full rebuild per call is cheap; the lists backing
        /// it are allocated once, so this path allocates nothing.
        /// </summary>
        public void Present(TextBuilder screen)
        {
            Present(screen, ForceRebuild);
        }

        /// <summary>
        /// Dirty-checked variant: pass the version counter your map bumps (GameMap.Version)
        /// and an unchanged screen costs two integer compares. ForceRebuild always rebuilds.
        /// </summary>
        public void Present(TextBuilder screen, int version)
        {
            if (screen == null) return;

            // Cheap enough to check every call, so a Font assigned at runtime still works.
            BuildAtlas();
            if (_atlas == null || _glyphMat == null) return;

            int cols = screen.Width, rows = screen.Height;
            bool resized = cols != _cols || rows != _rows;
            if (resized)
            {
                _cols = cols;
                _rows = rows;
                _lastVersion = ForceRebuild; // a different grid is always a full rebuild
            }

            if (!resized && version != ForceRebuild && version == _lastVersion) return;
            _lastVersion = version;

            BuildGlyphMesh(screen, cols, rows);
            BuildBackgroundMesh(screen, cols, rows);

            if (resized && Cam != null) FitCamera(Cam, cols, rows);
        }

        /// <summary>
        /// One quad per non-blank cell.
        ///
        /// Corner order is BL, TL, TR, BR with triangles (0,1,2) and (0,2,3). That is
        /// clockwise as seen from +Z, which is Unity's front-face winding, so the quads
        /// survive a Cull Back shader; the terminal shader should still be Cull Off.
        ///
        /// Cell (x, y) with y = 0 at the top is centred at (x + 0.5, -(y + 0.5) * cellH),
        /// so the grid spans x in [0, cols] and y in [-rows * cellH, 0].
        /// </summary>
        void BuildGlyphMesh(TextBuilder screen, int cols, int rows)
        {
            _glyphVerts.Clear();
            _glyphUvs.Clear();
            _glyphCols.Clear();
            _glyphTris.Clear();

            // One cell is one world unit wide and _cellH tall; the atlas's pixel metrics are
            // converted into that space so the quad covers the glyph's own ink box.
            float cellW = 1f;
            float cellH = _cellH;
            float pxToWorldW = cellW / Mathf.Max(1, _atlas.CellW);
            float pxToWorldH = cellH / Mathf.Max(1, _atlas.CellH);

            for (int y = 0; y < rows; y++)
            {
                float rowTop = -y * cellH;

                for (int x = 0; x < cols; x++)
                {
                    char ch = screen.CharAt(x, y);
                    if (ch == ' ' || ch == '\0') continue;   // blank cell: no glyph quad

                    Rgb fg = screen.ColorAt(x, y);
                    if (screen.BoldAt(x, y)) fg = fg.Brighten(0.25f);
                    // Rgb has no alpha channel, so the glyph quads are fully opaque and
                    // the shader's coverage term supplies the transparency.
                    Color col = new Color(fg.R / 255f, fg.G / 255f, fg.B / 255f, 1f);

                    // GlyphAtlas keys rects by character now, so the cache is indexed by charset
                    // position. A lookup is required rather than an arithmetic offset:
                    // the charset is contiguous only across printable ASCII and then
                    // jumps to scattered codepoints (box drawing, arrows, braille).
                    int chIndex = _atlas.IndexOf(ch);
                    if (chIndex < 0 || chIndex >= _uvCache.Length) continue;
                    Vector4 uvr = _uvCache[chIndex];
                    if (uvr.z <= 0f || uvr.w <= 0f) continue;   // font has no glyph for this

                    // Draw the glyph's ink box, not the whole cell: the UV rect describes
                    // exactly the letterform, so stretching it over the full advance would
                    // distort every character and let box drawing overlap its neighbours.
                    Vector4 ink = _atlas.InkOf(ch);
                    float x0 = x * cellW + ink.x * pxToWorldW;
                    float x1 = x0 + ink.z * pxToWorldW;

                    // ink.y is measured up from the cell floor, while world y runs downward.
                    float yBot = rowTop - ink.y * pxToWorldH;
                    float yTop = yBot - ink.w * pxToWorldH;

                    float u0 = uvr.x, du = uvr.z;
                    // uv.y is the rect's bottom edge, uv.w its height.
                    float vHigh = AtlasIsVFlipped ? uvr.y + uvr.w : uvr.y;   // world bottom
                    float vLow = AtlasIsVFlipped ? uvr.y : uvr.y + uvr.w;     // world top

                    // Unity pads the packed glyph box, and that padding overlaps the
                    // texels of whatever is packed next to it. Sampling the full padded
                    // rect therefore drags a sliver of the neighbouring letter into this
                    // cell. Inset the sample by half a texel on each side: enough to drop
                    // the padding, small enough to keep the letterform's own edge.
                    float texelU = 0.5f / Mathf.Max(1, _atlas.Texture.width);
                    float texelV = 0.5f / Mathf.Max(1, _atlas.Texture.height);
                    u0 += texelU;
                    du = Mathf.Max(0f, du - texelU * 2f);
                    vHigh += texelV;
                    vLow = Mathf.Max(vHigh, vLow - texelV);

                    _glyphVerts.Add(new Vector3(x0, yBot, 0f));
                    _glyphVerts.Add(new Vector3(x0, yTop, 0f));
                    _glyphVerts.Add(new Vector3(x1, yTop, 0f));
                    _glyphVerts.Add(new Vector3(x1, yBot, 0f));

                    _glyphUvs.Add(new Vector2(u0, vHigh));
                    _glyphUvs.Add(new Vector2(u0, vLow));
                    _glyphUvs.Add(new Vector2(u0 + du, vLow));
                    _glyphUvs.Add(new Vector2(u0 + du, vHigh));

                    _glyphCols.Add(col);
                    _glyphCols.Add(col);
                    _glyphCols.Add(col);
                    _glyphCols.Add(col);
                }
            }
            int quads = _glyphVerts.Count / 4;
            FillTriangles(_glyphTris, quads);

            if (quads > 0 || _glyphWasDrawn)
            {
                _glyphMesh.SetVertices(_glyphVerts);
                _glyphMesh.SetUVs(0, _glyphUvs);
                _glyphMesh.SetColors(_glyphCols);
                _glyphMesh.SetTriangles(_glyphTris, 0, true);
                // Bounds are never recalculated from vertices, so they must be set by
                // hand or Unity culls the renderer as soon as the grid leaves the default
                // zero-sized bounds. The margin covers the glow halo.
                _glyphMesh.bounds = GridBounds(cols, rows, _cellH, 0.5f);
            }
            _glyphWasDrawn = quads > 0;

            // Glow stays off: its shader shrinks UVs toward the texture centre, a halo
            // trick that only works for full-cell UVs. With tight ink-box rects it
            // samples unrelated glyphs (verified: ghost doubling on the test pattern).
            if (_glowRenderer != null) _glowRenderer.enabled = false;
            if (_glyphMat != null) _glyphRenderer.enabled = quads > 0;
        }

        /// <summary>
        /// Background quads as a separate solid-colour mesh drawn under the glyphs.
        ///
        /// Adjacent equal-colour cells are merged into one quad per horizontal run: a full
        /// screen of one colour collapses to rows instead of cells, which is the difference
        /// between a few dozen and a few thousand quads. Runs matching the screen
        /// background are skipped entirely, leaving the mesh empty on a plain screen.
        /// </summary>
        void BuildBackgroundMesh(TextBuilder screen, int cols, int rows)
        {
            _bgVerts.Clear();
            _bgCols.Clear();
            _bgTris.Clear();

            Rgb screenBg = ScreenBackground;
            float cellW = 1f, cellH = _cellH;

            for (int y = 0; y < rows; y++)
            {
                float yTop = -y * cellH;
                float yBot = -(y + 1) * cellH;

                int x = 0;
                while (x < cols)
                {
                    Rgb bg = screen.BgAt(x, y);
                    int run = 1;
                    while (x + run < cols && screen.BgAt(x + run, y).Equals(bg)) run++;

                    int runStart = x;
                    x += run;

                    if (bg.Equals(screenBg)) continue;

                    float x0 = runStart * cellW;
                    float x1 = x * cellW;
                    Color col = new Color(bg.R / 255f, bg.G / 255f, bg.B / 255f, 1f);

                    _bgVerts.Add(new Vector3(x0, yBot, 0f));
                    _bgVerts.Add(new Vector3(x0, yTop, 0f));
                    _bgVerts.Add(new Vector3(x1, yTop, 0f));
                    _bgVerts.Add(new Vector3(x1, yBot, 0f));

                    _bgCols.Add(col);
                    _bgCols.Add(col);
                    _bgCols.Add(col);
                    _bgCols.Add(col);
                }
            }

            int quads = _bgVerts.Count / 4;
            FillTriangles(_bgTris, quads);

            if (quads > 0 || _bgWasDrawn)
            {
                _bgMesh.SetVertices(_bgVerts);
                _bgMesh.SetColors(_bgCols);
                // No UVs: this mesh is a pure colour write, the atlas is not sampled.
                _bgMesh.SetTriangles(_bgTris, 0, true);
                _bgMesh.bounds = GridBounds(cols, rows, _cellH, 0f);
            }
            _bgWasDrawn = quads > 0;

            if (_bgMat != null) _bgRenderer.enabled = quads > 0;
        }

        /// <summary>
        /// Two triangles per quad, matching the BL, TL, TR, BR corner order. Skips the
        /// refill when the quad count is unchanged, since the buffer is a pure function
        /// of the count.
        /// </summary>
        static void FillTriangles(List<int> tris, int quads)
        {
            if (tris.Count == quads * 6) return;
            tris.Clear();
            for (int i = 0; i < quads; i++)
            {
                int b = i * 4;
                tris.Add(b);
                tris.Add(b + 1);
                tris.Add(b + 2);
                tris.Add(b);
                tris.Add(b + 2);
                tris.Add(b + 3);
            }
        }

        /// <summary>
        /// Culling volume for a grid mesh. Unity never derives bounds from the vertices we
        /// assign, so without this both renderers vanish the moment the grid leaves the
        /// default zero-sized bounds. The margin keeps the glow halo inside it.
        /// </summary>
        static Bounds GridBounds(int cols, int rows, float cellH, float margin)
        {
            float w = cols + margin * 2f;
            float h = rows * cellH + margin * 2f;
            return new Bounds(
                new Vector3(cols * 0.5f, -rows * cellH * 0.5f, 0f),
                new Vector3(w, h, 0.1f));
        }

        // ---------------------------------------------------------------- camera

        /// <summary>
        /// Fits the grid to the viewport with whole device pixels per cell.
        ///
        /// pxPerUnit is device pixels per world unit; one cell is exactly one world unit
        /// wide, so it is also pixels per cell. The tighter of the two axes wins, and
        /// orthographicSize then converts that scale back into world units vertically:
        /// a world unit covers pxPerUnit pixels, so the visible height is
        /// Screen.height / pxPerUnit and half of that is the ortho size. Snapping pxPerUnit
        /// to an integer keeps every cell boundary on a device pixel, which is what stops
        /// the atlas from resampling into mush. The camera then looks at the grid centre,
        /// x = cols / 2, y = -rows * cellH / 2, so the screen is centred and fills the view.
        /// </summary>
        public void FitCamera(Camera cam, int cols, int rows)
        {
            FitCameraPixels(cam, cols, rows, Screen.width, Screen.height);
        }

        /// <summary>
        /// Same fit against an explicit pixel size. Screen.width/height lie in
        /// batchmode (and in any offscreen capture), so editor tooling that renders
        /// into a RenderTexture must pass the target size here instead.
        /// </summary>
        public void FitCameraPixels(Camera cam, int cols, int rows, int pxW, int pxH)
        {
            if (cam == null) return;
            if (cols <= 0 || rows <= 0 || _cellH <= 0f) return;
            // Zero before the first frame or in a headless run.
            if (pxW <= 0 || pxH <= 0) return;

            float pxPerUnit = Mathf.Min(
                pxW / (float)cols,
                pxH / (rows * _cellH));

            if (SnapToWholePixels) pxPerUnit = Mathf.Max(1f, Mathf.RoundToInt(pxPerUnit));

            cam.orthographic = true;
            cam.orthographicSize = (pxH / pxPerUnit) * 0.5f;

            float centreX = cols * 0.5f;
            float centreY = -rows * _cellH * 0.5f;
            // In front of the grid: the camera looks along +Z with identity
            // rotation (see EnsureCamera), so it must sit at NEGATIVE z. A
            // positive z parks the whole grid behind the camera and the screen
            // goes black — exactly what happened here before.
            cam.transform.position = new Vector3(centreX, centreY, -CameraDistance);
        }

        /// <summary>Call after a window resize, or when the grid size changes under the camera.</summary>
        public void Refit()
        {
            if (Cam != null && _cols > 0) FitCamera(Cam, _cols, _rows);
        }

        // ------------------------------------------------------------------- CRT

        /// <summary>Scanline strength and vignette falloff for the CRT overlay. Zero disables each.</summary>
        public void SetCrt(float scanline, float vignette)
        {
            SetFloatIfPresent(_crtMat, PropScanline, scanline);
            SetFloatIfPresent(_crtMat, PropVignette, vignette);
        }

        /// <summary>
        /// Only the CRT clock is pushed per frame. The meshes are rebuilt from Present(),
        /// never from here, because the game is turn-based and the screen only changes
        /// when something happens.
        /// </summary>
        void LateUpdate()
        {
            SetFloatIfPresent(_crtMat, PropTime, Time.time);
        }

        // ------------------------------------------------------------------ util

        /// <summary>
        /// Debug/verification helper: the plain-text form of a screen. Exists so a headless
        /// harness can assert on layout without a GPU, and so a screenshot bug can be
        /// compared against what the game actually meant to draw. Not used at runtime.
        /// </summary>
        public string ToAsciiSnapshot(TextBuilder screen)
        {
            return screen == null ? string.Empty : screen.ToAscii();
        }

        void DisableRendering()
        {
            if (_bgRenderer != null) _bgRenderer.enabled = false;
            if (_glyphRenderer != null) _glyphRenderer.enabled = false;
            if (_crtRenderer != null) _crtRenderer.enabled = false;
        }

        static void SetFloatIfPresent(Material mat, int prop, float value)
        {
            if (mat != null && mat.HasProperty(prop)) mat.SetFloat(prop, value);
        }

        static void SetTextureIfPresent(Material mat, int prop, Texture value)
        {
            if (mat != null && mat.HasProperty(prop)) mat.SetTexture(prop, value);
        }

        static void DestroyRef(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }
    }
}
