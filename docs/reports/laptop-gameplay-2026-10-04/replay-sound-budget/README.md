# Replay sound budget agreement

The recorder admits512 sound cues and retention copies every cue in its window.
The decoder rejected counts over256. Encode self-validates using that decoder,
so otherwise bounded257..512-cue recordings throw before retention; the archive
catches that as LastSkip. Shared SoundCueLimit512 now supplies the unchanged
recorder guard and decoder bound. Raw12MiB, packed2MiB, schema13 and protocol143
stay unchanged. Cues are preserved in order, without truncation.

First original6: two intended257/512 failures, three0/1/256 controls and one
invalid513-control expectation. Encode already throws InvalidDataException;
calling TryDecode after that Encode never occurred. One bounded fixture repair
changes ONLY that control to expect the encoder exception. Other five cases,
setup, metadata and original production source are unchanged. First raw output
and fixture03c53 are retained. Corrected fixturee42c9155/meta788bbc64 stays frozen.
Corrected original reproduces2 failures/4 controls; candidate passes6/6.
Every accepted cue's time/id/position/pitch/gain is compared after roundtrip.

Unity6000.5.8f1 ran locally on gamergmae: headless EditMode CPU2048MiB/reserve1024,
one job/GC helper, isolated qa-a Library/company/product/named profile. Logical
a653 and older worker8e7 are disclosed in full maps. Each frozen job's3410 hashes
remained unchanged; the candidate changes only the two Camera source files.
All guards terminal/restored/free. No source repair or candidate repetition.
Nine raw XML/receipt/maps and the first fixture remain byte-exact in Git.

This qualifies public Encode/TryDecode with supplied bounded data. Recorder
capacity, archive retention and playback capacity are source-reviewed. Actual
capture cadence, audible output, rendered playback, live transfer, allocation
exhaustion, performance and current packaged-game acceptance remain separate.
It does not qualify the prioritized opening pan. No Net source, artwork,
hero mechanics or authored cue design changed.
