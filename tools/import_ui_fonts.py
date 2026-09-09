"""Import the supplied UI fonts, preserving glyphs and correcting signed line metrics."""
from io import BytesIO
from pathlib import Path
from zipfile import ZipFile
import argparse
from fontTools.ttLib import TTFont

parser = argparse.ArgumentParser()
parser.add_argument('--kawit', required=True)
parser.add_argument('--lydian', required=True)
args = parser.parse_args()
destination = Path(__file__).resolve().parent.parent / 'Assets/TumbangPreso/Resources/UI/fonts'
with ZipFile(args.kawit) as archive:
    (destination / 'KawitExtended.ttf').write_bytes(archive.read('KawitFree-ExtItalic.ttf'))
with ZipFile(args.lydian) as archive:
    font = TTFont(BytesIO(archive.read('LYDIAN__.TTF')), recalcTimestamp=False)
    outlines = font.getTableData('glyf')
    # The supplied file declares a positive descender in a signed field. Unity
    # subtracts it, yielding a 441-unit line box for roughly 879-unit ink. Correct
    # the sign, not the glyph outlines, font name, copyright or embedding flags.
    if font['hhea'].descent > 0:
        font['hhea'].descent = -font['hhea'].descent
    if font['OS/2'].sTypoDescender > 0:
        font['OS/2'].sTypoDescender = -font['OS/2'].sTypoDescender
    path = destination / 'Lydian-Regular.ttf'
    font.save(path)
    assert TTFont(path).getTableData('glyf') == outlines, 'Font import changed a letterform.'
print('Imported Kawit Extended and Lydian; letterforms preserved.')
