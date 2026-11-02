import React from 'react';

/**
 * ⚠️ THE PICTURE SITS IN HOME'S COLOURS (owner, 2026-11-02: "it has to look really well with the ui"). HOME is maroon, orange and lime on warm cream;
 * the maps' clear morning is cool teal and blue-grey, and fought it. So the plate is warmed, its cold colours are pulled toward the UI's, and its
 * corners sink into the board's maroon under the buttons. Both clips of a film use this one recipe, so nothing changes at the swap.
 */
export const GRADE_STYLE: React.CSSProperties = { filter: 'url(#uigrade)' };

export const GradeDefs: React.FC = () => (
  <defs>
    <filter id="uigrade" x="0" y="0" width="100%" height="100%" colorInterpolationFilters="sRGB">
      {/* Warm: red up, blue down, and teal bent toward a warm green. */}
      <feColorMatrix type="matrix" values="1.07 0.05 -0.07 0 0.012  0.02 1.0 -0.05 0 0.006  -0.05 0.06 0.84 0 0.004  0 0 0 1 0" />
      <feComponentTransfer>
        <feFuncR type="gamma" amplitude="1" exponent="1.04" offset="0" />
        <feFuncG type="gamma" amplitude="1" exponent="1.06" offset="0" />
        <feFuncB type="gamma" amplitude="1" exponent="1.1" offset="0" />
      </feComponentTransfer>
    </filter>
    <radialGradient id="uicorners" cx="50%" cy="48%" r="72%">
      <stop offset="0.55" stopColor="#5A0F24" stopOpacity={0} />
      <stop offset="1" stopColor="#5A0F24" stopOpacity={0.6} />
    </radialGradient>
  </defs>
);

export const GradeCorners: React.FC = () => <rect width={1920} height={1080} fill="url(#uicorners)" style={{ mixBlendMode: 'multiply' }} />;
