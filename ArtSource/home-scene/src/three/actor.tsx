import React, { useEffect, useState } from 'react';
import { continueRender, delayRender, staticFile } from 'remotion';
import * as THREE from 'three';
import { GLTFLoader, GLTF } from 'three/examples/jsm/loaders/GLTFLoader.js';
import * as SkeletonUtils from 'three/examples/jsm/utils/SkeletonUtils.js';

// ⚠️ COLOUR MANAGEMENT OFF. Every hex in this project is an sRGB value meant to reach the screen
// as written, and the output is not converted. With three's default on, `Color.set('#FFC23A')`
// linearised the value and the impact frame's wash came out a darker orange than the flat field
// it was meant to vanish into; every light colour was darker than written for the same reason.
THREE.ColorManagement.enabled = false;

/**
 * ⚠️⚠️ THE CHARACTERS ARE THE GAME'S OWN MODELS, NOT DRAWINGS OF THEM.
 * 🧑 2026-09-23, on the hand-drawn version: *"that video still loooks ugly as fuck"*,
 * *"specifically the character's face and movement and animation"*, *"his hair for example is
 * weird"*, *"side view looks so bad"*. Every earlier pass drew a voxel chibi out of SVG boxes and
 * got the hair, the face and every angle other than the front wrong, because a flat drawing of a
 * 3D model is a guess at every angle it was not traced from. So each figure is now
 * `team-zack.glb` / `team-sean.glb` itself, posed per frame, rendered offscreen and composited
 * into her painted set as an SVG image at the exact point the shot puts it. The tsinelas is the
 * game's own `tsinelas_tsinelas.glb`.
 *
 * THE LOOK IS THE GAME'S `TumbangPreso/Toon`, TRANSCRIBED:
 *  - Palette remap by atlas cell: glTF v is flipped against Unity's, so Unity's `row <= 7` is
 *    `row >= 8` here and its `row <= 3` is `row >= 12`.
 *  - Two hard bands (`_BandEdge` 0.03), shaded PER FACE (see the fragment shader).
 *  - An inverted hull pushed along WELDED normals (the game bakes them into the tangent channel),
 *    in near-black `ToonSkin.Ink` (0.02, 0.02, 0.03).
 *  - Plus a warm rim, which the game's `_RimColor` carries, because this scene is lit by a sunset
 *    behind him and a figure with no rim reads as pasted onto the set.
 */

export const PALETTES: Record<string, number[][]> = {
  'team-zack': [
    [0.909804, 0.819608, 0.121569], [0.078431, 0.070588, 0.101961], [0.980392, 0.94902, 0.219608], [0.960784, 0.658824, 0.12549],
    [0.878431, 0.741176, 0.101961], [0.831373, 0.886275, 0.92549], [0.078431, 0.070588, 0.101961], [0.65098, 0.501961, 0.078431],
    [0.121569, 0.121569, 0.141176], [0.321569, 0.329412, 0.376471], [0.960784, 0.921569, 0.34902], [0.980392, 0.980392, 0.65098],
    [1, 1, 1], [0.780392, 0.478431, 0.270588], [0.572549, 0.313725, 0.152941], [0.780392, 0.478431, 0.270588],
  ],
  // person_team-sean.tres
  'team-sean': [
    [0.721569, 0.454902, 0.25098], [0.588235, 0.34902, 0.180392], [0.796078, 0.52549, 0.305882], [0.788235, 0.164706, 0.164706],
    [0.6, 0.105882, 0.105882], [0.941176, 0.647059, 0], [0.721569, 0.478431, 0], [0.109804, 0.101961, 0.141176],
    [0.066667, 0.066667, 0.082353], [1, 0.756863, 0.027451], [0.862745, 0.14902, 0.14902], [0.360784, 0.227451, 0.129412],
    [0.788235, 0.164706, 0.164706], [0.941176, 0.94902, 0.960784], [1, 0.419608, 0.101961], [1, 0.533333, 0],
  ],
};

export type Light = {
  /** World direction the light comes FROM. The camera looks down -z, so +z is toward it. */
  dir: [number, number, number];
  lit: string;
  shade: string;
  rim: string;
  rim_strength: number;
  /** 0..1 flat colour wash over the whole figure (impact frames, the powered glow). */
  flash?: number;
  flashColour?: string;
};

/**
 * The wide's light. ⚠️ THE KEY IS IN FRONT OF HIM ON PURPOSE even though the sun is behind: it is
 * the bounce off the warm roofdeck, and a hero lit only from behind is a silhouette with no
 * face. The sun itself is the RIM, gold on every edge that turns away from the camera.
 */
export const DUSK: Light = {
  dir: [-0.45, 0.62, 0.64],
  lit: '#FFF0DC',
  shade: '#C7917E',
  rim: '#FFD27A',
  rim_strength: 0.7,
};

const toonVertex = `
  varying vec2 vUv; varying vec3 vN; varying vec3 vView; varying vec3 vWp; varying vec3 vBind;
  #include <common>
  #include <skinning_pars_vertex>
  void main() {
    vUv = uv;
    vBind = position;
    #include <skinbase_vertex>
    #include <begin_vertex>
    #include <beginnormal_vertex>
    #include <skinnormal_vertex>
    #include <skinning_vertex>
    vN = normalize(mat3(modelMatrix) * objectNormal);
    vec4 wp = modelMatrix * vec4(transformed, 1.0);
    vView = normalize(cameraPosition - wp.xyz);
    vWp = wp.xyz;
    gl_Position = projectionMatrix * viewMatrix * wp;
  }`;

const toonFragment = `
  uniform sampler2D map; uniform vec3 palette[16]; uniform float usePalette; uniform vec3 tint;
  uniform vec3 lightDir; uniform vec3 lit; uniform vec3 shade; uniform vec3 rim; uniform float rimStrength;
  uniform float flash; uniform vec3 flashColour; uniform float hasMap;
  uniform float eyeMix; uniform vec3 eyeColour;
  varying vec2 vUv; varying vec3 vN; varying vec3 vView; varying vec3 vWp; varying vec3 vBind;
  void main() {
    vec3 c = vec3(1.0);
    float col = floor(clamp(vUv.x, 0.0, 0.9999) * 16.0);
    float row = floor(clamp(vUv.y, 0.0, 0.9999) * 16.0);
    if (usePalette > 0.5 && row >= 8.0) {
      int slot = int(floor(col / 2.0)) + (row >= 12.0 ? 8 : 0);
      c = palette[slot];
    } else if (hasMap > 0.5) {
      c = texture2D(map, vUv).rgb;
    }
    c *= tint;
    // ⚠️ FLAT, PER FACE. The glb's normals are shared across some block edges, so an
    // interpolated normal drew a diagonal shadow line across a flat jacket panel (first board).
    // A voxel face is one plane, so it gets one normal: the derivative of its own position.
    vec3 n = normalize(cross(dFdx(vWp), dFdy(vWp)));
    if (dot(n, normalize(vN)) < 0.0) n = -n;
    float nl = dot(n, normalize(lightDir));
    float band = smoothstep(0.0, 0.03, nl);
    vec3 lc = c * mix(shade, lit, band);
    float graze = pow(1.0 - clamp(dot(n, normalize(vView)), 0.0, 1.0), 2.6);
    float back = clamp(-dot(normalize(vView), normalize(lightDir)) * 0.5 + 0.6, 0.0, 1.0);
    lc = mix(lc, rim, clamp(graze * back * rimStrength * step(0.08, graze), 0.0, 1.0));
    lc = mix(lc, flashColour, flash);
    // ⚠️ HIS OWN EYES, RECOLOURED IN PLACE. Read off the glb: the eyes are PALETTE SLOT 8 (atlas
    // rows 12 to 15, columns 0 to 1), on the head at bind-pose height 0.464 to 0.488 and depth
    // 0.160. Guessing them by darkness or depth failed twice (the fringe lit up, the eyes did
    // not), so the test is the slot itself, limited to the eye band so no other slot-8 surface
    // can light. Nothing is laid over the face, so nothing can float off it at an angle.
    float eye = step(0.5, usePalette) * step(12.0, row) * (1.0 - step(2.0, col))
      * step(0.455, vBind.y) * (1.0 - step(0.497, vBind.y)) * step(0.1, vBind.z);
    lc = mix(lc, eyeColour, eyeMix * eye);
    gl_FragColor = vec4(lc, 1.0);
  }`;

const outlineVertex = `
  attribute vec3 outlineNormal;
  uniform float width;
  #include <common>
  #include <skinning_pars_vertex>
  void main() {
    #include <skinbase_vertex>
    #include <begin_vertex>
    transformed += normalize(outlineNormal) * width;
    #include <skinning_vertex>
    gl_Position = projectionMatrix * modelViewMatrix * vec4(transformed, 1.0);
  }`;

const outlineFragment = `
  uniform vec3 ink; uniform float flash; uniform vec3 flashColour;
  void main() { gl_FragColor = vec4(mix(ink, flashColour, flash), 1.0); }`;

/** The game welds the hull normals so a cube's hull closes instead of splitting at its edges. */
const weldNormals = (g: THREE.BufferGeometry) => {
  const pos = g.getAttribute('position');
  const nor = g.getAttribute('normal');
  const sum = new Map<string, THREE.Vector3>();
  const key = (i: number) => `${pos.getX(i).toFixed(4)},${pos.getY(i).toFixed(4)},${pos.getZ(i).toFixed(4)}`;
  for (let i = 0; i < pos.count; i++) {
    const k = key(i);
    const v = sum.get(k) ?? new THREE.Vector3();
    v.x += nor.getX(i); v.y += nor.getY(i); v.z += nor.getZ(i);
    sum.set(k, v);
  }
  const out = new Float32Array(pos.count * 3);
  for (let i = 0; i < pos.count; i++) {
    const v = sum.get(key(i))!.clone().normalize();
    out[i * 3] = v.x; out[i * 3 + 1] = v.y; out[i * 3 + 2] = v.z;
  }
  g.setAttribute('outlineNormal', new THREE.BufferAttribute(out, 3));
};

type Mats = { toon: THREE.ShaderMaterial[]; hull: { m: THREE.ShaderMaterial; w: number | null }[] };

const toonMaterial = (map: THREE.Texture | null, palette: number[][] | null, tint = '#ffffff') =>
  new THREE.ShaderMaterial({
    uniforms: {
      map: { value: map },
      hasMap: { value: map ? 1 : 0 },
      palette: { value: (palette ?? PALETTES['team-zack']).map(([r, g, b]) => new THREE.Vector3(r, g, b)) },
      usePalette: { value: palette ? 1 : 0 },
      tint: { value: new THREE.Color(tint) },
      lightDir: { value: new THREE.Vector3(0, 1, 1) },
      lit: { value: new THREE.Color() },
      shade: { value: new THREE.Color() },
      rim: { value: new THREE.Color() },
      rimStrength: { value: 0 },
      flash: { value: 0 },
      flashColour: { value: new THREE.Color() },
      eyeMix: { value: 0 },
      eyeColour: { value: new THREE.Color() },
    },
    vertexShader: toonVertex,
    fragmentShader: toonFragment,
  });

/** Swap every mesh under `root` onto the toon look and give it an ink hull. */
const toonify = (root: THREE.Object3D, palette: number[][] | null, mats: Mats, fixedInk: number | null = null) => {
  const meshes: THREE.Mesh[] = [];
  root.traverse((o) => { if ((o as THREE.Mesh).isMesh) meshes.push(o as THREE.Mesh); });
  for (const m of meshes) {
    const srcs = Array.isArray(m.material) ? m.material : [m.material];
    const geo = m.geometry.clone();
    if (!geo.getAttribute('normal')) geo.computeVertexNormals();
    weldNormals(geo);
    m.geometry = geo;
    const toon = srcs.map((src) => {
      const s = src as THREE.MeshStandardMaterial;
      const t = toonMaterial(s.map ?? null, palette, `#${(s.color ?? new THREE.Color(1, 1, 1)).getHexString()}`);
      mats.toon.push(t);
      return t;
    });
    m.material = Array.isArray(m.material) ? toon : toon[0];
    const ink = new THREE.ShaderMaterial({
      uniforms: { width: { value: fixedInk ?? 0.0075 }, ink: { value: new THREE.Color(0.02, 0.02, 0.03) }, flash: { value: 0 }, flashColour: { value: new THREE.Color() } },
      vertexShader: outlineVertex,
      fragmentShader: outlineFragment,
      side: THREE.BackSide,
    });
    mats.hull.push({ m: ink, w: fixedInk });
    const skinned = m as THREE.SkinnedMesh;
    let shell: THREE.Mesh;
    if (skinned.isSkinnedMesh) {
      const sm = new THREE.SkinnedMesh(geo, ink);
      sm.bind(skinned.skeleton, skinned.bindMatrix);
      shell = sm;
    } else {
      shell = new THREE.Mesh(geo, ink);
      shell.position.copy(m.position);
      shell.quaternion.copy(m.quaternion);
      shell.scale.copy(m.scale);
    }
    shell.frustumCulled = false;
    m.frustumCulled = false;
    m.parent!.add(shell);
  }
};

const loads: Record<string, Promise<GLTF>> = {};
const loadGlb = (name: string) =>
  (loads[name] ??= new Promise<GLTF>((ok, fail) => new GLTFLoader().load(staticFile(`ref/${name}.glb`), ok, undefined, fail)));

// ------------------------------------------------------------------------------------ face

/**
 * ⚠️⚠️ HIS EYES NEVER CHANGE SHAPE. 🧑 2026-09-23, on drawn "open" and "sharp" eyes laid over the
 * face: *"the eyes u put looks like shit"*, *"that shit is floating on his face"*, *"if ur gonan do
 * eye shit js make it glow dont fucking change the actual eyes"*, *"he doesnt have eyeballs too
 * just black shits"*. So there is no expression system: `rest` is the model, `glow` lights his own
 * black eye shapes electric yellow, and `ink` holds them near-black through a full-figure wash
 * (the gold impact frame). The recolour happens in the toon shader, on the texels themselves.
 */
export type Face = 'rest' | 'glow' | 'ink';

// Where the eyes are, for anchoring glows and sparks: head space (the head bone sits at
// y 0.343, z -0.0024; the eyes are at model y 0.479, x -0.06 and +0.05, face plane z 0.218).
const FACE_Z = 0.2205;
const EYE_Y = 0.136;
const EYE_X = 0.057;

// ------------------------------------------------------------------------------------ build

export type Built = {
  model: string;
  scene: THREE.Group;
  mixer: THREE.AnimationMixer;
  clips: Record<string, THREE.AnimationClip>;
  bones: Record<string, THREE.Object3D>;
  rest: Record<string, THREE.Quaternion>;
  restPos: Record<string, THREE.Vector3>;
  mats: Mats;
  slipper: THREE.Group | null;
};

/** The game's tsinelas at his scale: 0.432 m in the world over a PERSON_SCALE 2.38 rig. */
// ⚠️ 1.3x its true size on purpose: at 0.42 it read as a black sliver at every distance the loop
// uses (the first boards). A hero shot stylises the prop the shot is about.
const SLIPPER_SCALE = 0.55;

const buildActor = (gltf: GLTF, model: string, slipper: GLTF | null): Built => {
  const scene = SkeletonUtils.clone(gltf.scene) as THREE.Group;
  const mats: Mats = { toon: [], hull: [] };
  toonify(scene, PALETTES[model], mats);
  const bones: Record<string, THREE.Object3D> = {};
  const rest: Record<string, THREE.Quaternion> = {};
  const restPos: Record<string, THREE.Vector3> = {};
  scene.traverse((o) => {
    if (['root', 'torso', 'head', 'arm-left', 'arm-right', 'leg-left', 'leg-right'].includes(o.name) && !bones[o.name]) {
      bones[o.name] = o;
      rest[o.name] = o.quaternion.clone();
      restPos[o.name] = o.position.clone();
    }
  });
  const clips: Record<string, THREE.AnimationClip> = {};
  for (const a of gltf.animations) clips[a.name] = a;
  let held: THREE.Group | null = null;
  if (slipper) {
    const s = slipper.scene.clone(true);
    toonify(s, null, mats, 0.005);
    s.scale.setScalar(SLIPPER_SCALE);
    held = new THREE.Group();
    held.add(s);
    held.visible = false;
    scene.add(held);
  }
  return { model, scene, mixer: new THREE.AnimationMixer(scene), clips, bones, rest, restPos, mats, slipper: held };
};

// ------------------------------------------------------------------------------------ pose

/** Degrees, applied AFTER the clip, in the bone's own frame (YXZ). */
export type Pose = Partial<Record<'root' | 'torso' | 'head' | 'armL' | 'armR' | 'legL' | 'legR', [number, number, number]>>;

const BONE: Record<keyof Pose, string> = {
  root: 'root', torso: 'torso', head: 'head', armL: 'arm-left', armR: 'arm-right', legL: 'leg-left', legR: 'leg-right',
};

export const addPose = (...ps: (Pose | undefined)[]): Pose => {
  const out: Pose = {};
  for (const p of ps) {
    if (!p) continue;
    for (const k of Object.keys(p) as (keyof Pose)[]) {
      const a = out[k] ?? [0, 0, 0];
      const b = p[k]!;
      out[k] = [a[0] + b[0], a[1] + b[1], a[2] + b[2]];
    }
  }
  return out;
};

export const mixPose = (a: Pose, b: Pose, t: number): Pose => {
  const out: Pose = {};
  for (const k of Object.keys(BONE) as (keyof Pose)[]) {
    if (!a[k] && !b[k]) continue;
    const pa = a[k] ?? [0, 0, 0];
    const pb = b[k] ?? [0, 0, 0];
    out[k] = [pa[0] + (pb[0] - pa[0]) * t, pa[1] + (pb[1] - pa[1]) * t, pa[2] + (pb[2] - pa[2]) * t];
  }
  return out;
};

/**
 * Where the tsinelas is. `hand` hangs it from his right fist by the strap; `free` puts it
 * anywhere in model space (the flip, the throw); `none` hides it.
 */
export type SlipperState =
  | { at: 'hand'; swing?: number; twist?: number }
  | { at: 'free'; pos: [number, number, number]; rot: [number, number, number] }
  /** Model space, relative to wherever his right fist is in THIS pose: the flip, the spin. */
  | { at: 'fromHand'; offset: [number, number, number]; rot: [number, number, number] }
  | { at: 'none' };

export type ActorProps = {
  /** SVG point where the model's focus point lands. */
  x: number;
  y: number;
  /** Pixels per model unit. The model is 0.79 units tall. */
  ppu: number;
  /** Model turn, degrees. 0 faces the camera, 90 turns him to face screen right. */
  yaw?: number;
  /** Camera elevation, degrees. Positive looks down on him. */
  pitch?: number;
  /** Camera roll, degrees (dutch). */
  roll?: number;
  fov?: number;
  clip?: string;
  t?: number;
  /** Arms hang at the sides when no clip holds them: the rig is authored in a T-pose. */
  armsDown?: boolean;
  pose?: Pose;
  /** Model-unit offset of the whole figure (hops, crouches, travel inside the frame). */
  lift?: [number, number, number];
  light?: Light;
  ink?: number;
  face?: Face;
  /** 0..1 how lit the eyes are when `face` is 'glow' (the strike-up flicker). Default 1. */
  eyeGlow?: number;
  slipper?: SlipperState;
  /** Box half-size in model units around the focus. */
  reach?: number;
  /** Model-space height the camera looks at, and the point (x, y) names. 0.4 hips, 0.46 eyes. */
  focus?: number;
  /** Full model-space look-at point, when the camera frames something off his centre line. */
  target?: [number, number, number] | 'handR';
  /** Render pixels per SVG pixel. Raise it for close-ups the shot's own camera scales up. */
  res?: number;
  /** Draw only the tsinelas (the thrown one flies in its own image). */
  only?: 'slipper';
};

const deg = Math.PI / 180;

const poseRig = (b: Built, p: ActorProps) => {
  for (const k of Object.keys(b.bones)) {
    b.bones[k].quaternion.copy(b.rest[k]);
    b.bones[k].position.copy(b.restPos[k]);
  }
  b.mixer.stopAllAction();
  if (p.clip && b.clips[p.clip]) {
    const a = b.mixer.clipAction(b.clips[p.clip]);
    a.reset().play();
    const d = b.clips[p.clip].duration;
    b.mixer.setTime((((p.t ?? 0) % d) + d) % d);
  }
  const q = new THREE.Quaternion();
  const e = new THREE.Euler();
  const drop = p.armsDown !== false && !p.clip ? 78 : 0;
  for (const k of Object.keys(BONE) as (keyof Pose)[]) {
    const v = p.pose?.[k] ?? [0, 0, 0];
    const bone = b.bones[BONE[k]];
    if (!bone) continue;
    // ⚠️ ARMS: PITCH IN THE SHOULDER'S FRAME, THEN DROP. The T-posed arm lies along its own X
    // axis, so an X rotation applied after the drop only TWISTS it (the first board's "arm
    // raise" did nothing). The game's PoseKey writes one Unity euler (x, y, 80 - z), and Unity's
    // order is Z first, then X, then Y, which is three's 'YXZ': the drop innermost, the pitch
    // outside it. So an arm's x swings it forward and back, y swings it across, z lifts it out.
    const z = k === 'armL' ? -drop + v[2] : k === 'armR' ? drop - v[2] : v[2];
    if (k === 'armL' || k === 'armR' || v[0] || v[1] || v[2]) bone.quaternion.multiply(q.setFromEuler(e.set(v[0] * deg, v[1] * deg, z * deg, 'YXZ')));
  }
  if (p.lift) b.bones.root.position.add(new THREE.Vector3(...p.lift));
  b.scene.rotation.set(0, (p.yaw ?? 0) * deg, 0);

  b.scene.traverse((o) => {
    if ((o as THREE.Mesh).isMesh && !isInSlipper(b, o)) o.visible = p.only !== 'slipper';
  });

  if (b.slipper) {
    const s = p.slipper ?? { at: 'none' };
    b.slipper.visible = s.at !== 'none';
    b.slipper.removeFromParent();
    if (s.at === 'hand') {
      // ⚠️ HUNG FROM THE FIST BY THE TOE STRAP. The model lies flat along its own x with the
      // strap up; the arm bone's -x runs down the arm to the fist. So the slipper's long axis is
      // laid along the arm, turned 90 about it so the sole faces sideways, and pushed past the
      // fist by its own half length, pivoting at the grip so `swing` is a pendulum.
      b.bones['arm-right'].add(b.slipper);
      b.slipper.position.set(HAND_R[0] + 0.03, HAND_R[1], HAND_R[2]);
      b.slipper.rotation.set(0, 0, 0);
      b.slipper.rotateZ((s.swing ?? 0) * deg);
      b.slipper.rotateX((90 + (s.twist ?? 0)) * deg);
      b.slipper.children[0].position.set(-0.2 * SLIPPER_SCALE - 0.02, 0, 0);
    } else if (s.at === 'fromHand') {
      b.scene.updateMatrixWorld(true);
      const hand = b.bones['arm-right'].localToWorld(new THREE.Vector3(...HAND_R));
      b.scene.worldToLocal(hand);
      b.scene.add(b.slipper);
      b.slipper.position.set(hand.x + s.offset[0], hand.y + s.offset[1], hand.z + s.offset[2]);
      b.slipper.rotation.set(s.rot[0] * deg, s.rot[1] * deg, s.rot[2] * deg);
      b.slipper.children[0].position.set(0, 0, 0);
    } else if (s.at === 'free') {
      b.scene.add(b.slipper);
      b.slipper.position.set(...s.pos);
      b.slipper.rotation.set(s.rot[0] * deg, s.rot[1] * deg, s.rot[2] * deg);
      b.slipper.children[0].position.set(0, 0, 0);
    }
  }
  b.scene.updateMatrixWorld(true);
};

const isInSlipper = (b: Built, o: THREE.Object3D) => {
  for (let q: THREE.Object3D | null = o; q; q = q.parent) if (q === b.slipper) return true;
  return false;
};

const light = (b: Built, p: ActorProps) => {
  const L = p.light ?? DUSK;
  const ld = new THREE.Vector3(...L.dir).normalize();
  for (const m of b.mats.toon) {
    m.uniforms.lightDir.value.copy(ld);
    (m.uniforms.lit.value as THREE.Color).set(L.lit);
    (m.uniforms.shade.value as THREE.Color).set(L.shade);
    (m.uniforms.rim.value as THREE.Color).set(L.rim);
    m.uniforms.rimStrength.value = L.rim_strength;
    m.uniforms.flash.value = L.flash ?? 0;
    (m.uniforms.flashColour.value as THREE.Color).set(L.flashColour ?? '#ffffff');
    const face = p.face ?? 'rest';
    m.uniforms.eyeMix.value = face === 'rest' ? 0 : face === 'glow' ? (p.eyeGlow ?? 1) : 1;
    (m.uniforms.eyeColour.value as THREE.Color).set(face === 'glow' ? '#F6FFA0' : '#140806');
  }
  for (const h of b.mats.hull) {
    h.m.uniforms.width.value = h.w ?? p.ink ?? 0.0075;
    h.m.uniforms.flash.value = L.flash ?? 0;
    (h.m.uniforms.flashColour.value as THREE.Color).set(L.flashColour ?? '#ffffff');
  }
};

/**
 * ⚠️ ONE OFFSCREEN RENDERER, READ BACK AS AN IMAGE. A WebGL canvas inside an SVG
 * `foreignObject` renders blank in the headless renderer (measured on the first test board), and
 * an HTML canvas over the SVG cannot sit BETWEEN two SVG layers (the chase's foreground poles,
 * TUMP! over the court). So each figure renders here, synchronously, and goes into the SVG as an
 * `<image>` at the point the shot puts it. One GL context serves every figure in the loop.
 */
let renderer: THREE.WebGLRenderer | null = null;
const getRenderer = () => {
  if (!renderer) {
    const canvas = document.createElement('canvas');
    renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true, preserveDrawingBuffer: true });
    renderer.setClearColor(0x000000, 0);
    renderer.outputColorSpace = THREE.LinearSRGBColorSpace;
  }
  return renderer;
};

/**
 * Points any shot anchors 2D effects to. The arm bone's local -x runs down the T-posed right
 * arm (and +x down the left), so after the drop it runs down to the fist.
 */
export const HAND_R: [number, number, number] = [-0.2, 0, 0.02];
export const HAND_L: [number, number, number] = [0.2, 0, 0.02];
export const HEAD_TOP: [number, number, number] = [0, 0.46, 0];
export const EYES: [number, number, number] = [0, EYE_Y, FACE_Z];
export const EYE_R: [number, number, number] = [-EYE_X, EYE_Y, FACE_Z];
export const EYE_L: [number, number, number] = [EYE_X, EYE_Y, FACE_Z];
export const FOOT: [number, number, number] = [0, -0.17, 0.03];
export const HIPS: [number, number, number] = [0, 0.2, 0];
export const GROUND: [number, number, number] = [0, 0, 0];

export type Drawn = {
  url: string;
  x: number;
  y: number;
  size: number;
  /** Screen point of a point in a bone's space ('model' for model space). Snapshotted at draw. */
  at: (bone: string, local?: [number, number, number]) => [number, number];
};

export const drawActor = (b: Built, p: ActorProps): Drawn => {
  poseRig(b, p);
  light(b, p);
  const fov = p.fov ?? 16;
  const reach = p.reach ?? 0.62;
  const size = Math.round(2 * reach * p.ppu);
  const px = Math.min(4096, Math.round(size * (p.res ?? 1.5)));
  const dist = reach / Math.tan((fov / 2) * deg);
  const pitch = (p.pitch ?? 4) * deg;
  const tgt = p.target === 'handR'
    ? b.bones['arm-right'].localToWorld(new THREE.Vector3(...HAND_R))
    : new THREE.Vector3(...(p.target ?? [0, p.focus ?? 0.4, 0])).applyMatrix4(b.scene.matrixWorld);
  const cam = new THREE.PerspectiveCamera(fov, 1, 0.01, 100);
  cam.position.set(tgt.x, tgt.y + Math.sin(pitch) * dist, tgt.z + Math.cos(pitch) * dist);
  const roll = (p.roll ?? 0) * deg;
  cam.up.set(Math.sin(roll), Math.cos(roll), 0);
  cam.lookAt(tgt);
  cam.updateMatrixWorld(true);
  const scene = new THREE.Scene();
  scene.add(b.scene);
  const r = getRenderer();
  r.setPixelRatio(1);
  r.setSize(px, px, false);
  r.clear();
  r.render(scene, cam);
  const url = r.domElement.toDataURL('image/png');
  const x0 = p.x - size / 2;
  const y0 = p.y - size / 2;
  // ⚠️ The rig is shared and the next draw re-poses it, so the world matrices are copied now
  // and `at` projects against the copies.
  const mats: Record<string, THREE.Matrix4> = { model: b.scene.matrixWorld.clone() };
  for (const [k, v] of Object.entries(b.bones)) mats[k] = v.matrixWorld.clone();
  const cam2 = cam.clone();
  scene.remove(b.scene);
  const at = (bone: string, local: [number, number, number] = [0, 0, 0]): [number, number] => {
    const v = new THREE.Vector3(...local).applyMatrix4(mats[bone] ?? mats.model);
    v.project(cam2);
    return [x0 + ((v.x + 1) / 2) * size, y0 + ((1 - v.y) / 2) * size];
  };
  return { url, x: x0, y: y0, size, at };
};

// ------------------------------------------------------------------------------------ hooks

const cacheBuilt: Record<string, Built> = {};
const pending: Record<string, Promise<Built>> = {};

/** Load once; the rig is re-posed on every draw, so one instance per model serves every shot. */
export const useActor = (model: 'team-zack' | 'team-sean', withSlipper = false): Built | null => {
  const key = `${model}${withSlipper ? '+s' : ''}`;
  const [built, setBuilt] = useState<Built | null>(cacheBuilt[key] ?? null);
  const [handle] = useState(() => (cacheBuilt[key] ? null : delayRender(`model ${key}`, { timeoutInMilliseconds: 120000 })));
  useEffect(() => {
    if (cacheBuilt[key]) {
      setBuilt(cacheBuilt[key]);
      if (handle !== null) continueRender(handle);
      return;
    }
    pending[key] ??= Promise.all([loadGlb(model), withSlipper ? loadGlb('tsinelas_tsinelas') : Promise.resolve(null)]).then(([g, s]) => {
      cacheBuilt[key] = buildActor(g, model, s);
      return cacheBuilt[key];
    });
    pending[key].then((bb) => {
      setBuilt(bb);
      if (handle !== null) continueRender(handle);
    });
  }, [key, model, withSlipper, handle]);
  return built;
};

/** The drawn figure as an SVG image. */
export const ActorImage: React.FC<{ d: Drawn; opacity?: number; filter?: string }> = ({ d, opacity, filter }) => (
  <image href={d.url} x={d.x} y={d.y} width={d.size} height={d.size} opacity={opacity} filter={filter} />
);
