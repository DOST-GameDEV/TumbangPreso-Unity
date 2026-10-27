"""Create frame-based picture/sound handoff from the encoded trailer witness. No audio is generated."""
import argparse
import csv
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
MEDIA=ROOT/"Logs/trailer/bring-it-home"


def tc(frame):return f"{frame//108000:02}:{frame//1800%60:02}:{frame//30%60:02}:{frame%30:02}"


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument("tag");args=parser.parse_args()
    witness=json.loads((MEDIA/"edits"/args.tag/"picture-witness.json").read_text(encoding="utf-8"))
    if hashlib.sha256(Path(witness["master"]).read_bytes()).hexdigest()!=witness["sha256"]:
        raise SystemExit("Picture changed after its witness was written")
    cues=[]
    def cue(frame,cut,action,role,evidence):
        cues.append({"master_frame":frame,"timecode_30fps":tc(frame),"seconds":f"{frame/30:.5f}",
            "cut":cut,"visual_action":action,"sound_opportunity":role,"evidence":evidence})
    cinema=[(0,"Opening object","Place and object identity"),(119,"Slipper release","Air and physical commitment"),
        (134,"Can impact","First distinctive accent; match visible contact"),(174,"Stadium reveal","Open spatial crowd after impact"),
        (384,"Can restored","Concrete state change; renewed danger"),(543,"Rago decision","Leave room for the held question")]
    for frame,action,role in cinema:cue(frame,"opening",action,role,"Authored picture clock; check contact against retained encoded frame")
    for cut in witness["cuts"]:
        start=cut["master_from"]
        cue(start,cut["id"],"Cut begins: "+cut["kind"],"Section or phrase boundary; not every cut needs an accent","Encoded cut boundary")
        if cut["kind"]!="gameplay":continue
        cast=("Totoy","Tikboy","Bebang","Jun-Jun") if cut["take"].startswith("classic") else \
            ("Paete","Phaister","Amihan","Rago") if cut["take"].startswith("hero-street") else ("Dante","Rago","Amihan","Nemu")
        def who(value):
            slot=int(value)
            return cast[slot]+f" (seat {slot})" if 0<=slot<4 else "world"
        events=MEDIA/cut["take"]/"events.tsv"
        if not events.exists():continue
        with events.open(encoding="utf-8") as source:
            for event in csv.DictReader(source,delimiter="\t"):
                frame=int(event["frame"])+1
                if not cut.get("start_frame",0)<=frame<cut.get("start_frame",0)+cut["frames"]:continue
                if event["kind"] not in ("Throw","LataDown","Tag","ability","ultimate","SeanSlam","HeroHit","NearMiss","Block"):continue
                master=start+frame-cut.get("start_frame",0)
                action=event["kind"]+(" / "+event["id"] if event["id"] else "")+" / "+who(event["actor"])
                if int(event["subject"])>=0:action+=" -> "+who(event["subject"])
                cue(master,cut["id"],action,"Team chooses the audible priority; suppress nonessential layers",
                    "Accepted native event aligned +1 capture frame; verify impact/footage before final mix")
        telemetry=MEDIA/cut["take"]/"telemetry.tsv"
        with telemetry.open(encoding="utf-8") as source:rows=list(csv.DictReader(source,delimiter="\t"))
        lo=cut.get("start_frame",0);hi=lo+cut["frames"]
        for index in range(max(1,lo),min(hi,len(rows))):
            for slot in range(4):
                previous=rows[index-1][f"p{slot}"].split(",");current=rows[index][f"p{slot}"].split(",")
                pickup=previous[3]=="0" and current[3:5]==["1","1"]
                step=sum((float(current[i])-float(previous[i]))**2 for i in range(3))**.5
                escape=previous[4]=="1" and current[3:5]==["1","0"] and step<1 and max(abs(float(current[0])),abs(float(current[2])))>6.98
                if pickup or escape:
                    cue(start+index-lo,cut["id"],("Retrieval / " if pickup else "Court exit / ")+who(slot),
                        "Small physical pickup" if pickup else "Release tension when the runner reaches the boundary",
                        "Captured holding/taggable/position transition; teleport returns excluded; spot against visible action")
    for frame,action,role in [(2640,"Cinematic story returns","Pull toward the athlete's point of view"),
        (2790,"Committed feint","Body intent and defender response"),(2888,"Hand reaches slipper","Small physical grip contact"),
        (2900,"Slipper lift begins","Grip and prop weight"),
        (2910,"Turn into escape","Build the chase"),(3034,"Crossing the established exit","Release / late tag attempt, no hit"),
        (3060,"Immediate escape resolved","Crowd and character breathing room"),(3180,"Neighborhood echo","Bring back human scale and warmth"),
        (3249,"Friends react","Small human reaction; not another large trailer hit"),(3360,"TUMP identity","Title arrival and musical resolution"),
        (3372,"Final invitation begins","Keep recognition and copy clear"),(3540,"Final hold","Reserve a deliberate tail within 120 seconds")]:
        cue(frame,"cinematic-ending/title",action,role,"Authored picture clock; adjust if the final scene timing changes")
    cues.sort(key=lambda item:(item["master_frame"],item["cut"]))
    out=MEDIA/"edits"/args.tag
    with (out/"TRAILER_SYNC.csv").open("w",newline="",encoding="utf-8-sig") as file:
        writer=csv.DictWriter(file,fieldnames=list(cues[0]));writer.writeheader();writer.writerows(cues)
    (out/"SOUND_BRIEF.md").write_text(f'''# TUMP silent picture: sound handoff

Picture: `{args.tag}.mp4`. SHA256 `{witness['sha256']}`.
120.000 seconds, 3600 frames, 30 fps non-drop timecode. Zero audio streams.

The team owns all composition, recording, music, voices, ambience and spot sound.
No temp score or assistant-made sound is included. Use TRAILER_SYNC.csv to locate
picture events. Native event rows are source evidence with a documented +1-frame
capture alignment, not an instruction to put a sound on every event.

The opening grows from a small can into a world sport. The middle builds through
actual playable decisions. The cinematic return holds a personal decision, then
releases through retrieval and escape. The neighborhood brings that scale home.
The last eight seconds belong to TUMP recognition and the team's musical tail.

Keep music, voices, spot effects and ambience separable. Allow small concrete
sounds to lead where the can's state or a hand contact matters. Leave headroom
for the climax; constant maximum density will flatten it. No narrator is required.
Use human voice recordings if dialogue is added. Essential meaning survives mute.

Picture changes require a new witness and regenerated cue sheet. Verify chosen
contacts on the encoded movie before final mix. Listening and creative acceptance
belong to the team and owner; metadata cannot certify emotional impact.

After the team has one final stereo mix, copy the picture stream without another
picture encode. Run this inside the portable editing folder with ffmpeg on PATH:

```
ffmpeg -n -i TUMP_BringItHome_1080p30_SILENT.mp4 -i TEAM_FINAL_MIX.wav -map 0:v:0 -map 1:a:0 -c:v copy -c:a aac -b:a 320k -af apad -t 120 -movflags +faststart TUMP_BringItHome_WITH_SOUND.mp4
```

This pads a short mix with silence and caps the output at 120 seconds. The team
should author the intended final musical tail inside that picture duration.
''',encoding="utf-8")
    print(f"Wrote {len(cues)} picture/sound sync entries to {out}")


if __name__=="__main__":main()
