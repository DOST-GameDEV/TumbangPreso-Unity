import React from 'react';
import { P } from './palette';
import { onN } from './time';

/**
 * ⚠️ LINE BOIL. Her outlines wobble because they are drawn by hand, and a hand-drawn line
 * redrawn every few frames boils. A turbulence field displacing the artwork, re-seeded every
 * THREE frames (10 drawings a second), gives that without redrawing anything. Re-seeding every
 * frame reads as shimmer rather than as drawing, and was the first thing tried.
 */
export const Defs: React.FC<{ frame: number }> = ({ frame }) => {
  const seed = onN(frame, 3) / 3;
  return (
    <defs>
      <filter id="boil" x="-10%" y="-10%" width="120%" height="120%">
        <feTurbulence type="fractalNoise" baseFrequency="0.022" numOctaves={2} seed={seed} result="n" />
        <feDisplacementMap in="SourceGraphic" in2="n" scale={3.2} xChannelSelector="R" yChannelSelector="G" />
      </filter>
      <filter id="boilSoft" x="-10%" y="-10%" width="120%" height="120%">
        <feTurbulence type="fractalNoise" baseFrequency="0.014" numOctaves={2} seed={seed + 7} result="n" />
        <feDisplacementMap in="SourceGraphic" in2="n" scale={2.2} xChannelSelector="R" yChannelSelector="G" />
      </filter>
      <filter id="boilHard" x="-20%" y="-20%" width="140%" height="140%">
        <feTurbulence type="fractalNoise" baseFrequency="0.03" numOctaves={2} seed={seed + 13} result="n" />
        <feDisplacementMap in="SourceGraphic" in2="n" scale={6} xChannelSelector="R" yChannelSelector="G" />
      </filter>
      <filter id="glow" x="-50%" y="-50%" width="200%" height="200%">
        <feGaussianBlur stdDeviation="10" />
      </filter>
      <filter id="glowBig" x="-80%" y="-80%" width="260%" height="260%">
        <feGaussianBlur stdDeviation="26" />
      </filter>
      <filter id="haze" x="-5%" y="-5%" width="110%" height="110%">
        <feGaussianBlur stdDeviation="2.2" />
      </filter>
      <filter id="defocus" x="-10%" y="-10%" width="120%" height="120%">
        <feGaussianBlur stdDeviation="7" />
      </filter>

      {/* ⚠️ The afterimage: his silhouette as ELECTRICITY, a bright edge and a glow round a faint
          body. A solid yellow fill read as a pale cardboard cut-out of him, not as a charge left
          hanging in the air where he was. */}
      <filter id="electricGhost" x="-20%" y="-20%" width="140%" height="140%">
        <feColorMatrix in="SourceAlpha" type="matrix" values="0 0 0 0 0.91  0 0 0 0 0.96  0 0 0 0 0.23  0 0 0 1 0" result="solid" />
        <feMorphology in="solid" operator="erode" radius="7" result="inner" />
        <feComposite in="solid" in2="inner" operator="out" result="edge" />
        <feGaussianBlur in="edge" stdDeviation="9" result="glow" />
        <feComponentTransfer in="solid" result="faint">
          <feFuncA type="linear" slope="0.22" />
        </feComponentTransfer>
        <feColorMatrix in="edge" type="matrix" values="0 0 0 0 1  0 0 0 0 0.99  0 0 0 0 0.9  0 0 0 1 0" result="core" />
        <feMerge>
          <feMergeNode in="glow" />
          <feMergeNode in="faint" />
          <feMergeNode in="edge" />
          <feMergeNode in="core" />
        </feMerge>
      </filter>
      <radialGradient id="vignette" cx="50%" cy="50%" r="72%">
        <stop offset="55%" stopColor="#2A0508" stopOpacity={0} />
        <stop offset="100%" stopColor="#2A0508" stopOpacity={0.6} />
      </radialGradient>
      <radialGradient id="sunGlow">
        <stop offset="0%" stopColor="#FFE08A" stopOpacity={0.6} />
        <stop offset="25%" stopColor="#FFC45A" stopOpacity={0.4} />
        <stop offset="50%" stopColor="#FFA84A" stopOpacity={0.18} />
        <stop offset="75%" stopColor="#FF9440" stopOpacity={0.06} />
        <stop offset="100%" stopColor="#FF9440" stopOpacity={0} />
      </radialGradient>
      {/* Her persimmon stripes, off the M: darker diagonal bands on the fill. */}
      <pattern id="stripePersimmon" width="26" height="26" patternUnits="userSpaceOnUse" patternTransform="rotate(-38)">
        <rect width="26" height="26" fill={P.persimmon} />
        <rect width="9" height="26" fill={P.persimmonStripe} />
      </pattern>
      <pattern id="stripeGolden" width="44" height="44" patternUnits="userSpaceOnUse" patternTransform="rotate(-35)">
        <rect width="44" height="44" fill={P.golden} />
        <rect width="16" height="44" fill="#FFC94A" />
      </pattern>
      <pattern id="stripeJacket" width="22" height="22" patternUnits="userSpaceOnUse" patternTransform="rotate(-40)">
        <rect width="22" height="22" fill={P.jacket} />
        <rect width="6" height="22" fill="#C9C105" />
      </pattern>

      {/* Paper grain, laid over the whole frame at low strength. Warm, never grey. */}
      <filter id="grain" x="0" y="0" width="100%" height="100%">
        <feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves={2} seed={seed % 4} result="g" />
        <feColorMatrix
          in="g"
          type="matrix"
          values="0 0 0 0 0.42  0 0 0 0 0.16  0 0 0 0 0.06  0 0 0 0.9 -0.32"
        />
      </filter>
    </defs>
  );
};
