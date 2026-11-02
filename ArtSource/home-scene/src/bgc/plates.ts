export { BLEND } from '../lib/plates';
import { platesOf } from '../lib/plates';

/** The recorded real map behind this clip's frame. plates/ is served by scripts/platesrv.mjs. */
export const { plateAt, preAt } = platesOf('zack');
