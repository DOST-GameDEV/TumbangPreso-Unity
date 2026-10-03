# Scroll prompt replacement

Owner supplied paired mouse up/down PNG, October 3. Replace only wheel prompt
art through InputGlyphs, retaining live binding labels, keyboard/controller
fallbacks and all existing layout/hitboxes. Built-in image edit cleaned the supplied
pair against the existing Xelu middle-mouse reference. Original is preserved
outside the repository; derived transparent sheet is a runtime resource.

Equal square sprite rectangles center the mouse-plus-arrow groups. Normalized
rectangles retain the existing 512px import budget, bilinear filtering, alpha
and cached sprite ownership. Same high-contrast artwork on both grounds.

Check focused native glyph lookup/rect/cache and unchanged binding coverage;
inspect a native UI row at small sizes on light/dark grounds before publication.
No protocol or actual binding change. Not a vector file: vector-style raster.
