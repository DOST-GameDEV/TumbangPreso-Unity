"""Verify signed windup/release in three actual player processes, with optional late rejoin."""
import argparse,csv,hashlib,json,os,re,shutil,subprocess,sys,time,uuid
from pathlib import Path
from run_unity_guarded import profile_root
ROOT=Path(__file__).resolve().parents[1]
PROFILE=Path(os.environ["USERPROFILE"])/"AppData/LocalLow/BH Studios/Tumbang Preso"
def rows(path):
    with path.open(newline="") as f:return [{k:float(v) for k,v in r.items()} for r in csv.DictReader(f)]
def evaluate(folder,rejoin,require_swap=False):
    data={name:rows(folder/(name+".csv")) for name in ("host","owner","observer")}
    if rejoin:data["observer"]+=rows(folder/"rejoined.csv")
    errors=[];measurements={}
    host_log=(folder/"host.log").read_text(errors="replace")
    warmup=host_log.split("[Slice] round 1 begins",1)[0]
    pickup=warmup.find("operation=holding shoe=0 state=1 holder=1")
    disarm=warmup.find("operation=holding shoe=-1 state=-1 holder=-1",max(0,pickup))
    measurements["warmup_foreign_pickup"]=pickup>=0
    measurements["warmup_disarmed_before_whistle"]=pickup>=0 and disarm>pickup
    if require_swap and (pickup<0 or disarm<=pickup):errors.append("Required foreign-shoe warmup handover was not observed and cleared")
    owner=data["owner"]
    for name,records in data.items():
        expected={"host":0,"owner":1,"observer":2}[name]
        if not records or any(r["local"]!=expected for r in records):errors.append(name+" has no correctly seated evidence")
        phases={}
        for label,value in (("straight",0),("negative",-.7),("positive",.8)):
            reference=[r for r in owner if r["held"] and r["charge"]>.5 and abs(r["spin"]-value)<.02]
            if len(reference)<8:errors.append("owner missed "+label);continue
            # A partial-charge straight stage is shorter than the full-charge
            # stages. Trim a bounded proportion; fixed margins erased this stage.
            trim=min(.5,(reference[-1]["time"]-reference[0]["time"])*.15)
            low,high=reference[0]["time"]+trim,reference[-1]["time"]-trim
            samples=[r for r in records if low<=r["time"]<=high]
            good=[r for r in samples if r["held"] and r["charge"]>.4 and abs(r["spin"]-value)<.03 and r["torso"] < -5]
            phases[label]={"samples":len(samples),"visible_matching_samples":len(good)}
            if len(good)<5 or len(good)<len(samples)*.85:errors.append(name+" did not sustain the "+label+" body/charge/spin tell")
        charged=[r for r in records if r["charge"]>.5]
        released=[r for r in records if charged and r["time"]>charged[-1]["time"] and not r["held"] and r["charge"]<0]
        phases["release_samples"]=len(released)
        if len(released)<5:errors.append(name+" never cleared the accepted release")
        stale=[r for r in records if charged and r["time"]>charged[-1]["time"]+1 and r["held"]]
        phases["unexpected_post_release_holding"]=len(stale)
        if stale:errors.append(name+" returned a slipper to the hand without an accepted pickup")
        measurements[name]=phases
    if rejoin:
        joined=rows(folder/"rejoined.csv")
        active=[r for r in joined if r["charge"]>.8 and r["spin"]>.7 and r["torso"] < -5]
        if len(active)<5:errors.append("Rejoined observer missed the existing positive preparation")
        measurements["rejoined_active_samples"]=len(active)
    return {"ok":not errors,"errors":errors,"measurements":measurements}
def main():
    ap=argparse.ArgumentParser();ap.add_argument("exe",type=Path);ap.add_argument("--mode",choices=["classic","hero"],default="classic")
    ap.add_argument("--delay",type=float,default=0);ap.add_argument("--rejoin",action="store_true");ap.add_argument("--require-warmup-swap",action="store_true");ap.add_argument("--out",type=Path,required=True);a=ap.parse_args()
    folder=a.out.resolve();folder.mkdir(parents=True,exist_ok=False)
    backup=folder/"profiles";manifest={};processes=[];handles=[]
    # These processes use exactly three named profiles. Never restore main or
    # unrelated profiles over changes made elsewhere while this review runs.
    for name in ("throwhost","throwowner","throwobserver"):
        scope=profile_root(["-tp-profile",name]).resolve()
        if not scope.is_relative_to(PROFILE.resolve()):raise ValueError("Profile scope escaped player data")
        for source in scope.rglob("*"):
            if not source.is_file() or source.suffix==".log":continue
            rel=source.relative_to(PROFILE);target=backup/rel;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)
            manifest[str(rel)]=hashlib.sha256(source.read_bytes()).hexdigest()
    startup=None
    if os.name=="nt":
        startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
    def launch(args,stdout=subprocess.DEVNULL):
        process=subprocess.Popen(args,cwd=ROOT,stdout=stdout,stderr=subprocess.STDOUT,startupinfo=startup);processes.append(process);return process
    try:
        common=[str(a.exe.resolve()),"-batchmode","-screen-width","640","-screen-height","360","-screen-fullscreen","0","-tp-autostart","3"]
        def peer(name,route,profile=None):
            return launch(common+route+["-tp-profile",profile or "throw"+name,"-tp-throwmode",a.mode,"-tp-throwtrace",str(folder/(name+".csv")),"-logFile",str(folder/(name+".log"))])
        host=peer("host",["-tp-host","8950"]);time.sleep(7)
        port=8950
        if a.delay:
            port=8951;log=open(folder/"link.log","w");handles.append(log)
            launch([sys.executable,str(ROOT/"tools/net_link.py"),"--listen",str(port),"--to","127.0.0.1:8950","--delay",str(a.delay),"--seconds","100"],log);time.sleep(1)
        owner=peer("owner",["-tp-join","127.0.0.1",str(port)])
        deadline=time.monotonic()+35
        while time.monotonic()<deadline:
            logpath=folder/"owner.log";text=logpath.read_text(errors="replace") if logpath.exists() else ""
            if re.search(r"(?:seat changed|arena installed): LocalSlot=1[^\n]*host=False",text):break
            if owner.poll() is not None:raise RuntimeError("Owner exited before seating")
            time.sleep(.25)
        else:raise RuntimeError("Owner did not occupy seat1")
        observer=peer("observer",["-tp-join","127.0.0.1","8950"])
        print("Running real host/owner/observer: "+str(folder),flush=True)
        deadline=time.monotonic()+95;rejoined=None
        while time.monotonic()<deadline and host.poll() is None:
            if a.rejoin and rejoined is None:
                try:active=any(r["spin"]<-.6 and r["charge"]>.9 for r in rows(folder/"owner.csv"))
                except (OSError,ValueError,TypeError):active=False
                if active:
                    observer.terminate();observer.wait(timeout=8)
                    rejoined=peer("rejoined",["-tp-join","127.0.0.1","8950"],"throwobserver")
                    print("Rejoining the observer profile during the held charge",flush=True)
            time.sleep(.5)
        result=evaluate(folder,a.rejoin,a.require_warmup_swap)
        result.update(mode=a.mode,delay_one_way_ms=a.delay,exe_sha256=hashlib.sha256(a.exe.read_bytes()).hexdigest())
        dll=a.exe.parent/(a.exe.stem+"_Data")/"Managed/TumbangPreso.Runtime.dll"
        result["runtime_sha256"]=hashlib.sha256(dll.read_bytes()).hexdigest()
        (folder/"result.json").write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2),flush=True)
        return 0 if result["ok"] else 1
    finally:
        for p in processes:
            if p.poll() is None:p.terminate()
        for p in processes:
            try:p.wait(timeout=8)
            except subprocess.TimeoutExpired:p.kill();p.wait()
        for h in handles:h.close()
        for rel,digest in manifest.items():
            target=PROFILE/rel
            if not target.resolve().is_relative_to(PROFILE.resolve()):raise ValueError("Unsafe profile restore")
            target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(backup/rel,target)
            if hashlib.sha256(target.read_bytes()).hexdigest()!=digest:raise RuntimeError("Profile restore failed")
        print("Preserved "+str(len(manifest))+" existing profile files",flush=True)
if __name__=="__main__":raise SystemExit(main())
