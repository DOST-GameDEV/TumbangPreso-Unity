"""Join a folder of numbered frames and one wav into an mp4, in Blender's sequencer.

  blender -b --factory-startup -P tools/mux_paete_ult_video.py -- <frames dir> <wav> <out.mp4> <fps> [label]

There is no ffmpeg on this machine, and Blender carries its own: this is how the LIANA LEAP
sound options were put over their animation on 2026-10-07 (that script was a throwaway; this is
the same one kept). tools/build_paete_ult_sfx.py runs it once per soundtrack. H.264 and AAC,
frames shown at twice their size so a 640x360 film is watchable, the label small in a corner so
the owner knows which option is playing. Frame 1 of the video is the first frame and the first
sample of the wav.
"""
import glob
import os
import sys

import bpy

args = sys.argv[sys.argv.index("--") + 1:]
frames_dir, wav, out, fps = args[0], args[1], args[2], int(args[3])
label = args[4] if len(args) > 4 else ""
frames = sorted(glob.glob(os.path.join(frames_dir, "f*.png")))
if not frames:
    raise SystemExit("no frames in " + frames_dir)
width, height = bpy.data.images.load(frames[0]).size[:]

bpy.ops.wm.read_factory_settings(use_empty=True)
s = bpy.context.scene
s.render.resolution_x, s.render.resolution_y, s.render.resolution_percentage = width * 2, height * 2, 100
s.render.fps, s.render.fps_base = fps, 1.0
s.frame_start, s.frame_end = 1, len(frames)
s.sequence_editor_create()
se = s.sequence_editor
strips = se.strips if hasattr(se, "strips") else se.sequences        # Blender 5 renamed it
try:
    st = strips.new_image("frames", frames[0], 1, 1, fit_method="FIT")
except TypeError:
    st = strips.new_image("frames", frames[0], 1, 1)
    st.transform.scale_x = st.transform.scale_y = 2.0
for f in frames[1:]:
    st.elements.append(os.path.basename(f))
strips.new_sound("track", wav, 2, 1)
if label:
    try:
        end = len(frames) + 1
        try:
            tx = strips.new_effect("label", "TEXT", 3, 1, length=len(frames))
        except TypeError:
            tx = strips.new_effect("label", "TEXT", 3, 1, frame_end=end)
        tx.text = label
        tx.font_size = 30
        tx.location = (0.02, 0.04)
        for name, value in (("anchor_x", "LEFT"), ("align_x", "LEFT"), ("alignment_x", "LEFT"), ("anchor_y", "BOTTOM"), ("align_y", "BOTTOM")):
            try:
                setattr(tx, name, value)
            except (AttributeError, TypeError):
                pass
        tx.color = (1.0, 1.0, 0.6, 1.0)
        tx.use_shadow = True
    except Exception as e:                                            # a label is not worth losing the video for
        print("no label:", e)
s.view_settings.view_transform = "Standard"
s.render.image_settings.media_type = "VIDEO"
s.render.image_settings.file_format = "FFMPEG"
s.render.ffmpeg.format = "MPEG4"
s.render.ffmpeg.codec = "H264"
s.render.ffmpeg.constant_rate_factor = "HIGH"
s.render.ffmpeg.audio_codec = "AAC"
s.render.ffmpeg.audio_bitrate = 192
s.render.ffmpeg.audio_mixrate = 44100
s.render.ffmpeg.audio_channels = "MONO"                           # the drafts are mono; stereo would lay them in 3 dB down
s.render.filepath = out
s.render.use_file_extension = False
bpy.ops.render.render(animation=True)
print("muxed", out)
