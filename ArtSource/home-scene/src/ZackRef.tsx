import React, { useEffect, useState } from 'react';
import { AbsoluteFill, continueRender, delayRender, staticFile, useCurrentFrame } from 'remotion';
import { ThreeCanvas } from '@remotion/three';
import { useThree } from '@react-three/fiber';
import * as THREE from 'three';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';

/**
 * A reference board, not part of the loop: his ACTUAL in-game model (team-zack.glb) photographed
 * from the angles the loop draws him at, so the 2D Zack copies the model rather than a memory
 * of it. 🧑 2026-09-23: *"make sure u copy his actual model"*.
 *
 * ⚠️ The glb alone renders in Kenney's default colours, because every colour on a mini comes from
 * WHICH CELL of the shared 512x512 atlas a UV lands in. The game recolours per hero by that cell
 * (`person_palette.gdshader`: slot = col / 2 + (row >= 12 ? 8 : 0)), so this does the same with
 * Zack's sixteen colours from `person_team-zack.tres`, plus a two-band light like the game's toon.
 */
const ZACK_PALETTE = [
  [0.909804, 0.819608, 0.121569], [0.078431, 0.070588, 0.101961], [0.980392, 0.94902, 0.219608], [0.960784, 0.658824, 0.12549],
  [0.878431, 0.741176, 0.101961], [0.831373, 0.886275, 0.92549], [0.078431, 0.070588, 0.101961], [0.65098, 0.501961, 0.078431],
  [0.121569, 0.121569, 0.141176], [0.321569, 0.329412, 0.376471], [0.960784, 0.921569, 0.34902], [0.980392, 0.980392, 0.65098],
  [1, 1, 1], [0.780392, 0.478431, 0.270588], [0.572549, 0.313725, 0.152941], [0.780392, 0.478431, 0.270588],
];

const makeMaterial = (map: THREE.Texture | null, pal: number[][]) =>
  new THREE.ShaderMaterial({
    uniforms: {
      map: { value: map },
      palette: { value: pal.map(([r, g, b]) => new THREE.Vector3(r, g, b)) },
      lightDir: { value: new THREE.Vector3(0.5, 0.8, 0.6).normalize() },
    },
    vertexShader: `
      varying vec2 vUv; varying vec3 vN;
      #include <common>
      #include <skinning_pars_vertex>
      void main() {
        vUv = uv;
        #include <skinbase_vertex>
        #include <begin_vertex>
        #include <beginnormal_vertex>
        #include <skinnormal_vertex>
        #include <skinning_vertex>
        vN = normalize(normalMatrix * objectNormal);
        gl_Position = projectionMatrix * modelViewMatrix * vec4(transformed, 1.0);
      }`,
    fragmentShader: `
      uniform sampler2D map; uniform vec3 palette[16]; uniform vec3 lightDir;
      varying vec2 vUv; varying vec3 vN;
      void main() {
        float col = floor(vUv.x * 16.0);
        float row = floor(vUv.y * 16.0);
        vec3 c;
        if (row >= 8.0) {
          int slot = int(floor(col / 2.0)) + (row >= 12.0 ? 8 : 0);
          c = palette[slot];
        } else {
          c = texture2D(map, vUv).rgb;
        }
        float l = dot(normalize(vN), normalize((viewMatrix * vec4(lightDir, 0.0)).xyz));
        c *= l > 0.15 ? 1.0 : 0.72;
        gl_FragColor = vec4(c, 1.0);
      }`,
  });

const ANGLES: [number, number][] = [
  [0, 0.75],
  [30, 0.75],
  [-30, 0.75],
  [90, 0.75],
  [160, 0.75],
  [200, 0.95],
];

const Cam: React.FC<{ angle: number; h: number }> = ({ angle, h }) => {
  const { camera, invalidate } = useThree();
  useEffect(() => {
    const a = (angle * Math.PI) / 180;
    camera.position.set(Math.sin(a) * 3.4, h, Math.cos(a) * 3.4);
    camera.lookAt(0, 0.6, 0);
    camera.updateProjectionMatrix();
    invalidate();
  }, [angle, h, camera, invalidate]);
  return null;
};

export const ZackRef: React.FC<{ model?: string; palette?: number[][]; clip?: string; angle?: number }> = ({ model = 'team-zack', palette, clip, angle: fixedAngle }) => {
  const f = useCurrentFrame();
  const [scene, setScene] = useState<THREE.Group | null>(null);
  const [handle] = useState(() => delayRender('glb'));
  useEffect(() => {
    new GLTFLoader().load(staticFile(`ref/${model}.glb`), (g) => {
      g.scene.traverse((o) => {
        const m = o as THREE.Mesh;
        if (m.isMesh) {
          const src = m.material as THREE.MeshStandardMaterial;
          m.material = makeMaterial(src.map ?? null, palette ?? ZACK_PALETTE);
        }
      });
      if (clip) {
        const a = g.animations.find((x) => x.name === clip);
        if (a) {
          const mixer = new THREE.AnimationMixer(g.scene);
          const act = mixer.clipAction(a);
          act.play();
          (g.scene as unknown as { userData: { mixer: THREE.AnimationMixer; dur: number } }).userData = { mixer, dur: a.duration };
        }
      }
      setScene(g.scene);
      continueRender(handle);
    });
  }, [handle, model, palette]);
  const [angle0, h] = ANGLES[f % ANGLES.length];
  const angle = fixedAngle ?? angle0;
  if (scene && clip) {
    const ud = (scene as unknown as { userData: { mixer?: THREE.AnimationMixer; dur?: number } }).userData;
    if (ud.mixer && ud.dur) ud.mixer.setTime((f / 8) * ud.dur);
  }
  return (
    <AbsoluteFill style={{ background: '#3A2A2A' }}>
      <ThreeCanvas width={1000} height={1000}
        camera={{ fov: 26, near: 0.1, far: 50, position: [Math.sin((angle * Math.PI) / 180) * 3.4, h, Math.cos((angle * Math.PI) / 180) * 3.4] }}>
        <Cam angle={angle} h={h} />
        {scene && <primitive object={scene} />}
      </ThreeCanvas>
    </AbsoluteFill>
  );
};
