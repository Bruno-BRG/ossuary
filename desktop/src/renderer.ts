import type { Frame } from './protocol';
import { cssColor } from './protocol';
import type { Layout } from './font';
import { CRT_FRAGMENT, CRT_VERTEX, crtParams, type CrtParams } from './crt';

// Draws the engine's cell grid. The glyph/colour grid is rasterised once per frame from the
// 8x16 bitmap font; the CRT look (curvature, scanlines, phosphor mask, bloom, vignette) is a
// fragment shader over that texture. Canvas 2D is the fallback when WebGL2 is unavailable.
export class TerminalRenderer {
  private base = document.createElement('canvas');
  private glow = document.createElement('canvas');
  private context: CanvasRenderingContext2D | null = null;
  private gl: WebGL2RenderingContext | null = null;
  private program: WebGLProgram | null = null;
  private textures: { base: WebGLTexture; emit: WebGLTexture } | null = null;
  private uniforms = new Map<string, WebGLUniformLocation | null>();
  private source = { w: 0, h: 0 };
  private view = { width: 0, height: 0, scale: 1, dpr: 1 };
  private params: CrtParams = crtParams(0);
  private voidColor = [0, 0, 0];
  private frameHandle = 0;
  private lastPaint = 0;
  private reducedMotion = matchMedia('(prefers-reduced-motion: reduce)').matches;
  /** True when the last frame contained visible ink; the native smoke test reads this. */
  painted = false;

  constructor(private canvas: HTMLCanvasElement, private font: Map<number, Uint8Array>, private crt: HTMLElement) {
    // A canvas keeps the first context type it hands out, so GL is proven on a probe first.
    this.gl = this.initGl(document.createElement('canvas')) ? this.initGl(canvas) : null;
    if (!this.gl) {
      const context = canvas.getContext('2d', { alpha: false });
      if (!context) throw new Error('Canvas indisponível.');
      this.context = context;
    } else {
      this.crt.hidden = true;
      canvas.addEventListener('webglcontextlost', event => { event.preventDefault(); cancelAnimationFrame(this.frameHandle); });
      canvas.addEventListener('webglcontextrestored', () => { this.gl = this.initGl(canvas); this.source = { w: 0, h: 0 }; });
    }
  }

  get usingGpu() { return this.gl !== null; }

  private initGl(target: HTMLCanvasElement): WebGL2RenderingContext | null {
    const gl = target.getContext('webgl2', { alpha: true, premultipliedAlpha: true, antialias: false, preserveDrawingBuffer: false });
    if (!gl) return null;
    try {
      const compile = (type: number, text: string) => {
        const shader = gl.createShader(type)!;
        gl.shaderSource(shader, text); gl.compileShader(shader);
        if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(shader) ?? 'shader');
        return shader;
      };
      const program = gl.createProgram()!;
      gl.attachShader(program, compile(gl.VERTEX_SHADER, CRT_VERTEX));
      gl.attachShader(program, compile(gl.FRAGMENT_SHADER, CRT_FRAGMENT));
      gl.linkProgram(program);
      if (!gl.getProgramParameter(program, gl.LINK_STATUS)) throw new Error(gl.getProgramInfoLog(program) ?? 'program');
      this.program = program;
      this.uniforms.clear();
      const make = () => {
        const texture = gl.createTexture()!;
        gl.bindTexture(gl.TEXTURE_2D, texture);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR_MIPMAP_LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
        return texture;
      };
      this.textures = { base: make(), emit: make() };
      this.source = { w: 0, h: 0 };
      return gl;
    } catch (error) {
      console.warn('WebGL2 CRT indisponível, usando Canvas 2D:', error);
      return null;
    }
  }

  private uniform(name: string) {
    if (!this.uniforms.has(name)) this.uniforms.set(name, this.gl!.getUniformLocation(this.program!, name));
    return this.uniforms.get(name) ?? null;
  }

  draw(frame: Frame, size: Layout, dpr: number) {
    const w = frame.cols * 8, h = frame.rows * 16;
    const data = new Uint8ClampedArray(w * h * 4), lights = new Uint8ClampedArray(w * h * 4);
    const fallback = this.font.get(63)!;
    let ink = false;
    for (let i = 0; i < frame.glyphs.length; i++) {
      const glyph = this.font.get(frame.glyphs[i]) ?? fallback;
      const fg = frame.fg[i], bg = frame.bg[i], bold = frame.bold[i];
      const fr = (fg >> 16) & 255, fgc = (fg >> 8) & 255, fb = fg & 255;
      const br = (bg >> 16) & 255, bgc = (bg >> 8) & 255, bb = bg & 255;
      const emits = bold || Math.max(fr, fgc, fb) >= 170;
      const x = (i % frame.cols) * 8, y = Math.floor(i / frame.cols) * 16;
      for (let row = 0; row < 16; row++) {
        const bits = glyph[row];
        let p = ((y + row) * w + x) * 4;
        for (let bit = 0; bit < 8; bit++, p += 4) {
          const on = (bits & (128 >> bit)) !== 0;
          data[p] = on ? fr : br; data[p + 1] = on ? fgc : bgc; data[p + 2] = on ? fb : bb; data[p + 3] = 255;
          if (on) {
            if (!ink && fr + fgc + fb > 120) ink = true;
            if (emits) { lights[p] = fr; lights[p + 1] = fgc; lights[p + 2] = fb; }
          }
          lights[p + 3] = 255;
        }
      }
    }
    this.painted = ink;
    this.params = crtParams(frame.crt, frame);
    this.voidColor = [((frame.void >> 16) & 255) / 255, ((frame.void >> 8) & 255) / 255, (frame.void & 255) / 255];
    this.view = { width: w * size.scale, height: h * size.scale, scale: size.scale, dpr };

    const width = this.view.width, height = this.view.height;
    if (this.canvas.width !== width) this.canvas.width = width;
    if (this.canvas.height !== height) this.canvas.height = height;
    const left = `${Math.floor((innerWidth * dpr - width) / 2) / dpr}px`, top = `${Math.floor((innerHeight * dpr - height) / 2) / dpr}px`;
    for (const element of [this.canvas, this.crt]) {
      element.style.width = `${width / dpr}px`; element.style.height = `${height / dpr}px`;
      element.style.left = left; element.style.top = top;
    }
    this.applyTokens(frame, size.scale, dpr);

    if (this.gl && this.textures) {
      const gl = this.gl;
      for (const [texture, pixels] of [[this.textures.base, data], [this.textures.emit, lights]] as const) {
        gl.bindTexture(gl.TEXTURE_2D, texture);
        gl.pixelStorei(gl.UNPACK_ALIGNMENT, 1);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA8, w, h, 0, gl.RGBA, gl.UNSIGNED_BYTE, new Uint8Array(pixels.buffer));
        gl.generateMipmap(gl.TEXTURE_2D);
      }
      this.source = { w, h };
      this.present(performance.now());
      this.schedule();
    } else this.draw2d(frame, data, lights, w, h, width, height, size);
  }

  // ---------------------------------------------------------------- GPU path

  private schedule() {
    cancelAnimationFrame(this.frameHandle);
    // Static scenes only need a new frame for the flicker; 30 fps is plenty and cheap.
    if (this.params.animate && !this.reducedMotion) this.frameHandle = requestAnimationFrame(this.tick);
  }

  private tick = (now: number) => {
    if (now - this.lastPaint >= 33) this.present(now);
    this.frameHandle = requestAnimationFrame(this.tick);
  };

  private present(now: number) {
    const gl = this.gl;
    if (!gl || !this.program || !this.textures || this.source.w === 0) return;
    this.lastPaint = now;
    const { width, height, scale, dpr } = this.view, p = this.params;
    gl.viewport(0, 0, width, height);
    gl.useProgram(this.program);
    gl.activeTexture(gl.TEXTURE0); gl.bindTexture(gl.TEXTURE_2D, this.textures.base);
    gl.activeTexture(gl.TEXTURE1); gl.bindTexture(gl.TEXTURE_2D, this.textures.emit);
    gl.uniform1i(this.uniform('uTex'), 0);
    gl.uniform1i(this.uniform('uEmit'), 1);
    gl.uniform2f(this.uniform('uSrc'), this.source.w, this.source.h);
    gl.uniform2f(this.uniform('uOut'), width, height);
    gl.uniform1f(this.uniform('uScale'), scale);
    gl.uniform1f(this.uniform('uScan'), p.scan);
    gl.uniform1f(this.uniform('uMask'), p.mask);
    gl.uniform1f(this.uniform('uVig'), p.vignette);
    gl.uniform1f(this.uniform('uGlow'), p.glow);
    gl.uniform1f(this.uniform('uCurve'), p.curve);
    gl.uniform1f(this.uniform('uRadius'), p.radius * dpr);
    gl.uniform1f(this.uniform('uTime'), (now / 1000) % 1000);
    gl.uniform1f(this.uniform('uFlicker'), this.reducedMotion ? 0 : p.flicker);
    gl.uniform3f(this.uniform('uVoid'), this.voidColor[0], this.voidColor[1], this.voidColor[2]);
    gl.clearColor(0, 0, 0, 0); gl.clear(gl.COLOR_BUFFER_BIT);
    gl.drawArrays(gl.TRIANGLES, 0, 3);
  }

  // ------------------------------------------------------------ Canvas 2D path

  private draw2d(frame: Frame, data: Uint8ClampedArray, lights: Uint8ClampedArray, w: number, h: number, width: number, height: number, size: Layout) {
    this.base.width = this.glow.width = w;
    this.base.height = this.glow.height = h;
    this.base.getContext('2d')!.putImageData(new ImageData(data as Uint8ClampedArray<ArrayBuffer>, w, h), 0, 0);
    this.glow.getContext('2d')!.putImageData(new ImageData(lights as Uint8ClampedArray<ArrayBuffer>, w, h), 0, 0);
    const ctx = this.context!;
    ctx.imageSmoothingEnabled = false;
    ctx.drawImage(this.base, 0, 0, width, height);
    if (frame.glow > 0) {
      ctx.save(); ctx.globalAlpha = frame.glow;
      ctx.filter = `blur(${size.scale * 1.25}px)`;
      ctx.globalCompositeOperation = 'screen';
      ctx.drawImage(this.glow, 0, 0, width, height);
      ctx.restore();
    }
    this.crt.style.setProperty('--scan-size', `${2 * size.scale / this.view.dpr}px`);
    this.crt.hidden = frame.crt === 0;
  }

  // -------------------------------------------------------------- page tokens

  private applyTokens(frame: Frame, scale: number, dpr: number) {
    const root = document.documentElement.style;
    root.setProperty('--scanline', String(frame.scanline));
    root.setProperty('--vignette', String(frame.vignette));
    const colors = { void: frame.void, text: frame.text, dim: frame.dim, title: frame.title, rule: frame.rule, panel: frame.panelColor, bad: frame.bad };
    for (const [name, color] of Object.entries(colors)) root.setProperty(`--${name}`, cssColor(color));
    root.setProperty('--void-rgb', `${(frame.void >> 16) & 255} ${(frame.void >> 8) & 255} ${frame.void & 255}`);
    root.setProperty('--title-rgb', `${(frame.title >> 16) & 255} ${(frame.title >> 8) & 255} ${frame.title & 255}`);
    // The screen is a lit glass pane: a faint phosphor aura spills onto the page around it.
    const crt = this.params;
    this.canvas.style.borderRadius = `${crt.radius}px`;
    this.canvas.style.boxShadow = crt.aura > 0 ? `0 0 ${Math.round(40 + 60 * crt.aura)}px rgb(var(--title-rgb) / ${(0.07 * crt.aura).toFixed(3)})` : 'none';
    void scale; void dpr;
  }
}
