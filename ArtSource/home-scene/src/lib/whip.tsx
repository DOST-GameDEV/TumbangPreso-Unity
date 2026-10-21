import React from 'react';

/**
 * A whip pan: the whole shot slides sideways under a horizontal blur. Used to hand one shot to
 * the next, so a cut in this piece is a camera MOVE and not just a change of picture (🧑
 * 2026-09-23: *"dynamic camera movement ... not js liek dat"*). Each use gets its own filter id.
 */
export const Whip: React.FC<{ id: string; blur: number; dx: number; children: React.ReactNode }> = ({ id, blur, dx, children }) => {
  if (blur < 0.5 && Math.abs(dx) < 0.5) return <>{children}</>;
  return (
    <g>
      <defs>
        <filter id={id} x="-15%" y="-5%" width="130%" height="110%">
          <feGaussianBlur stdDeviation={`${Math.max(0.01, blur)} 0`} />
        </filter>
      </defs>
      <g transform={`translate(${dx} 0)`} filter={`url(#${id})`}>
        {children}
      </g>
    </g>
  );
};
