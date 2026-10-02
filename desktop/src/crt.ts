export interface CrtParams {
  scan: number; mask: number; vignette: number; glow: number; curve: number;
  radius: number; flicker: number; aura: number; animate: boolean;
}

// Engine levels: 0 off, 1 subtle, 2 strong. Scanline/vignette/glow depths come from the engine
// (DisplaySettings.CrtParams); the shader-only knobs (curvature, phosphor mask) hang off the level.
export function crtParams(level: number, frame?: { scanline: number; vignette: number; glow: number }): CrtParams {
  if (level <= 0) return { scan: 0, mask: 0, vignette: 0, glow: 0, curve: 0, radius: 0, flicker: 0, aura: 0, animate: false };
  const strong = level >= 2;
  return {
    scan: (frame?.scanline ?? (strong ? 0.28 : 0.12)) * 2.4,
    mask: strong ? 0.22 : 0.12,
    vignette: (frame?.vignette ?? (strong ? 0.32 : 0.16)) * 1.1,
    glow: (frame?.glow ?? (strong ? 0.55 : 0.30)) * 1.15,
    curve: strong ? 0.065 : 0.032,
    radius: strong ? 22 : 14,
    flicker: strong ? 0.018 : 0.008,
    aura: strong ? 1 : 0.6,
    animate: true,
  };
}

export const CRT_VERTEX = `#version 300 es
out vec2 vUv;
void main() {
  vec2 p = vec2(float((gl_VertexID << 1) & 2), float(gl_VertexID & 2));
  vUv = vec2(p.x, 1.0 - p.y);
  gl_Position = vec4(p * 2.0 - 1.0, 0.0, 1.0);
}`;

export const CRT_FRAGMENT = `#version 300 es
precision highp float;
in vec2 vUv;
out vec4 outColor;
uniform sampler2D uTex;
uniform sampler2D uEmit;
uniform vec2 uSrc;
uniform vec2 uOut;
uniform float uScale, uScan, uMask, uVig, uGlow, uCurve, uRadius, uTime, uFlicker;
uniform vec3 uVoid;

float hash(vec2 p) {
  uvec2 q = uvec2(ivec2(p)) * uvec2(1597334673u, 3812015801u);
  uint n = (q.x ^ q.y) * 1597334673u;
  return float(n >> 8) * (1.0 / 16777216.0);
}

float roundedBox(vec2 p, vec2 hs, float r) {
  vec2 q = abs(p) - hs + r;
  return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}

void main() {
  // Barrel distortion, pre-scaled so the middle of every edge stays on the glass.
  vec2 c = vUv * 2.0 - 1.0;
  vec2 d = c * (1.0 + uCurve * dot(c, c)) / (1.0 + uCurve);
  vec2 uv = d * 0.5 + 0.5;

  // Sharp bilinear: texels stay crisp, only the 1px seam between them is filtered.
  vec2 pix = uv * uSrc;
  vec2 fl = floor(pix + 0.5);
  vec2 fr = clamp((pix - fl) * uScale, -0.5, 0.5);
  vec2 suv = (fl + fr) / uSrc;
  vec3 col = textureLod(uTex, suv, 0.0).rgb;

  // Phosphor bloom: only emitters (bold or bright glyphs) feed it, at three radii.
  vec3 bloom = textureLod(uEmit, uv, 1.5).rgb * 0.45
             + textureLod(uEmit, uv, 2.5).rgb * 0.35
             + textureLod(uEmit, uv, 3.5).rgb * 0.30;
  // Halation: the whole picture bleeds a little, so lit floors glow into their walls.
  vec3 halo = textureLod(uTex, uv, 3.0).rgb;
  col += bloom * uGlow * 1.35 + halo * uGlow * 0.16;

  // Scanlines. Past 2x, the lower part of each source row dims; at 1x, alternate device rows.
  float sy = fract(pix.y);
  float beam = uScale >= 2.0 ? 1.0 - uScan * smoothstep(0.5, 1.0, sy) * 0.95
                             : 1.0 - uScan * 0.16 * mod(floor(gl_FragCoord.y), 2.0);
  col *= beam;

  // Aperture grille: RGB triads across device pixels.
  float tri = mod(floor(gl_FragCoord.x), 3.0);
  vec3 grille = vec3(1.0 - uMask * (tri == 0.0 ? 0.0 : 1.0),
                     1.0 - uMask * (tri == 1.0 ? 0.0 : 1.0),
                     1.0 - uMask * (tri == 2.0 ? 0.0 : 1.0));
  col *= grille * (1.0 + uMask * 0.55);

  // Vignette, a slow rolling hum bar, flicker and a little grain.
  col *= mix(1.0, (1.0 - smoothstep(0.25, 1.55, length(d))), clamp(uVig * 1.7, 0.0, 1.0));
  col *= 1.0 + uFlicker * sin(uTime * 55.0) + uFlicker * 0.8 * sin(uv.y * 5.0 - uTime * 1.3);
  col += (hash(gl_FragCoord.xy + floor(uTime * 24.0) * vec2(37.0, 17.0)) - 0.5) * 0.028 * step(0.001, uScan);

  // Mild contrast curve so colours read saturated on the dark ground.
  col = pow(max(col, 0.0), vec3(0.96));

  // Rounded glass with an antialiased edge.
  vec2 p = d * uOut * 0.5;
  float edge = uRadius > 0.0 ? clamp(0.5 - roundedBox(p, uOut * 0.5, uRadius), 0.0, 1.0)
                             : (abs(d.x) <= 1.0 && abs(d.y) <= 1.0 ? 1.0 : 0.0);
  outColor = vec4(min(col, 1.0) * edge, edge);
}`;
