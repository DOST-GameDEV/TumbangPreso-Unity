"""Three actual player processes, one owning-client familiar driver, independent traces."""
import argparse,csv,hashlib,json,math,os,re,shutil,subprocess,sys,time,uuid
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
PROFILE=Path(os.environ["USERPROFILE"])/"AppData/LocalLow/BH Studios/Tumbang Preso"

def read(path):
    with path.open(newline="") as f:return [{k:float(v) for k,v in r.items()} for r in csv.DictReader(f)]

def evaluate(folder,case,reconnected=False):
    data={name:read(folder/(name+".csv")) for name in ("host","owner","observer")}
    errors=[];details={}
    for name,rows in data.items():
        expected={"host":0,"owner":1,"observer":2}[name]
        if not rows or any(r["local"]!=expected for r in rows):errors.append(name+" occupied the wrong seat; shaped-link coverage is invalid")
        if case=="mash":
            active=[r for r in rows if r["stunLeft"]>0]
            if not active:errors.append(name+" missed the stun");continue
            peak=max(r["mashPresses"] for r in active)
            drops=sum(b["mashPresses"]<a["mashPresses"] for a,b in zip(active,active[1:]) if b["stunLeft"]>.05)
            duration=active[-1]["time"]-active[0]["time"]
            details[name]={"accepted_presses":peak,"progress_rollbacks":drops,"stun_duration":duration}
            if peak<3:errors.append(name+" lost the recovery presses")
            if duration>3.3:errors.append(name+" did not shorten the four-second stun")
            if drops:errors.append(name+" recovery progress went backwards")
            continue
        if case=="impact":
            # Processes begin observing at different server times. Anchor to the
            # actual charge transition, not a local elapsed window that can start
            # after the owner has already integrated the impact.
            cast=next((i for i,r in enumerate(rows) if r["sourceCharges"]==1),None)
            if cast is None or cast==0:
                errors.append(name+" missed the pre-cast charge transition");continue
            before=rows[cast-1]
            after=[r for r in rows[cast:] if r["time"]<=rows[cast]["time"]+5]
            origin=[before["bodyX"],before["bodyZ"]]
            travel=max(math.dist(origin,[r["bodyX"],r["bodyZ"]]) for r in after)
            details[name]={"impact_displacement":travel,"source_charges":after[-1]["sourceCharges"],"rows":len(rows),"cast_time":rows[cast]["time"]}
            closest=min(math.hypot(r["sourceX"]-r["bodyX"],r["sourceZ"]-r["bodyZ"]) for r in after[:5])
            details[name]["cast_separation"]=closest
            details[name]["observed_stun"]=max(r.get("stunLeft",0) for r in after)
            details[name]["body"]=[after[-1]["bodyX"],after[-1]["bodyZ"]]
            if before["sourceCharges"]!=2:errors.append(name+" source did not begin with two charges")
            if closest>2.2:errors.append(name+" victim was outside the actual stomp; fixture invalid")
            if details[name]["observed_stun"]<=0:errors.append(name+" did not observe contact stun")
            if travel<.35:errors.append(name+" victim did not move from the actual stomp")
            if after[-1]["sourceCharges"]!=1:errors.append(name+" did not observe exactly one real source cast")
            continue
        flight=[r for r in rows if r["possessed"]==1]
        if len(flight)<5:errors.append(name+" did not observe sustained possession");continue
        travel=math.hypot(max(r["x"] for r in flight)-min(r["x"] for r in flight),max(r["z"] for r in flight)-min(r["z"] for r in flight))
        details[name]={"flight_span":travel,"rows":len(rows)}
        settled=[r for r in rows if 10<=r["elapsed"]<=18 and "modelX" in r]
        if settled:
            model_error=max(math.hypot(r["modelX"]-r["bodyX"],r["modelZ"]-r["bodyZ"]) for r in settled)
            details[name]["model_error"]=model_error
            if model_error>.25:errors.append(name+" rendered body remained detached from its motor")
        if travel<1:errors.append(name+" familiar did not actually travel")
        if max(r["fieldCount"] for r in rows)>1:errors.append(name+" duplicated the pulling field")
        if case=="ultimate":
            active=[r for r in rows if r["fieldCount"]==1]
            minimum=1 if reconnected and name=="owner" else 20
            if len(active)<minimum:errors.append(name+" did not observe the actual field");continue
            details[name].update(field=[active[-1]["fieldX"],active[-1]["fieldZ"]],ends=active[-1]["time"])
            if not (reconnected and name=="owner") and (rows[-1]["devouring"] or rows[-1]["fieldCount"]):errors.append(name+" leaked its ultimate")
        else:
            if rows[-1]["possessed"]:errors.append(name+" never ended projection")
            settled=[r for r in rows if 10<=r["elapsed"]<=18]
            last=settled[-1] if settled else rows[-1]
            details[name]["body"]=[last["bodyX"],last["bodyZ"]]
            destination=[flight[-1]["x"],flight[-1]["z"]]
            if math.dist(details[name]["body"],destination)>1.1:
                errors.append(name+" did not recall near its last controlled familiar position")
    if len(details)==3:
        key="field" if case=="ultimate" else "body"
        for name in ("owner","observer"):
            if key in details[name] and key in details["host"]:
                error=math.dist(details[name][key],details["host"][key]);details[name]["final_error"]=error
                if error>.35:errors.append(name+" final "+key+" disagreed by "+str(error))
        clocks=[d for n,d in details.items() if not (reconnected and n=="owner")]
        if case=="ultimate" and all("ends" in d for d in clocks):
            spread=max(d["ends"] for d in clocks)-min(d["ends"] for d in clocks)
            if spread>.35:errors.append("field expiry spread exceeds 350 ms: "+str(spread))
    if reconnected:
        joined=read(folder/"rejoined.csv")
        active=[r for r in joined if r["fieldCount"]==1]
        if len(active)<10:errors.append("rejoined owner did not reconstruct the active field")
        if not joined or any(r["local"]!=1 for r in joined):errors.append("rejoined owner did not reclaim seat 1")
        if joined and active:
            field=[active[-1]["fieldX"],active[-1]["fieldZ"]]
            if math.dist(field,details["host"]["field"])>.35:errors.append("rejoined field disagreed")
            if joined[-1]["fieldCount"] or joined[-1]["devouring"]:errors.append("rejoined effect never expired")
            if joined[-1]["s2charges"]!=data["host"][-1]["s2charges"]:errors.append("rejoin refunded possession")
            if abs(joined[-1]["ultcharge"]-data["host"][-1]["ultcharge"])>.05:errors.append("rejoin changed ultimate charge")
            details["rejoined"]={"field":field,"rows":len(joined),"ends":active[-1]["time"],"s2charges":joined[-1]["s2charges"],"ultcharge":joined[-1]["ultcharge"]}
    return {"ok":not errors,"errors":errors,"measurements":details}

def main():
    ap=argparse.ArgumentParser();ap.add_argument("exe",type=Path);ap.add_argument("--case",choices=["recall","ultimate","impact","mash"],default="recall");ap.add_argument("--delay",type=float,default=0);ap.add_argument("--loss",type=float,default=0);ap.add_argument("--out",type=Path);ap.add_argument("--reconnect",action="store_true");a=ap.parse_args()
    if a.reconnect and a.case!="ultimate":ap.error("reconnect uses the ultimate case")
    folder=(a.out or ROOT/"Logs"/("familiar-"+a.case+"-"+uuid.uuid4().hex[:8])).resolve();folder.mkdir(parents=True,exist_ok=False)
    backup=folder/"profiles";manifest={}
    for source in PROFILE.rglob("*"):
        if not source.is_file() or source.suffix==".log":continue
        rel=source.relative_to(PROFILE);target=backup/rel;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)
        manifest[str(rel)]=hashlib.sha256(source.read_bytes()).hexdigest()
    processes=[];handles=[]
    startup=None
    if os.name=="nt":
        startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
    def launch(args):
        p=subprocess.Popen(args,cwd=ROOT,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,startupinfo=startup);processes.append(p);return p
    try:
        common=[str(a.exe.resolve()),"-batchmode","-screen-width","640","-screen-height","360","-screen-fullscreen","0","-tp-autostart","3"]
        def peer(name,route,profile=None,scenario=None):
            return launch(common+route+["-tp-profile",profile or "fam"+name,"-tp-familiarcase",scenario or a.case,"-tp-familiartrace",str(folder/(name+".csv")),"-logFile",str(folder/(name+".log"))])
        host=peer("host",["-tp-host","8930"]);time.sleep(7)
        port=8930
        if a.delay or a.loss:
            port=8931
            log=open(folder/"link.log","w");handles.append(log)
            p=subprocess.Popen([sys.executable,str(ROOT/"tools/net_link.py"),"--listen",str(port),"--to","127.0.0.1:8930","--delay",str(a.delay),"--loss",str(a.loss),"--seconds","100"],cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,startupinfo=startup);processes.append(p);time.sleep(1)
        owner=peer("owner",["-tp-join","127.0.0.1",str(port)])
        seated_until=time.monotonic()+35
        while time.monotonic()<seated_until:
            logpath=folder/"owner.log"
            logged=logpath.read_text(errors="replace") if logpath.exists() else ""
            if re.search(r"(?:seat changed|arena installed): LocalSlot=1[^\n]*host=False",logged):break
            if owner.poll() is not None:raise RuntimeError("owner exited before taking its seat")
            time.sleep(.25)
        else:raise RuntimeError("owner did not take seat 1 before observer launch")
        observer=peer("observer",["-tp-join","127.0.0.1","8930"])
        print("Tracing real host, owning client and observer in "+str(folder),flush=True)
        deadline=time.monotonic()+95
        rejoined=None
        while time.monotonic()<deadline and any(p.poll() is None for p in (host,owner,observer)):
            if a.reconnect and rejoined is None:
                try:active=any(r["fieldCount"]==1 for r in read(folder/"owner.csv"))
                except (OSError,ValueError,TypeError):active=False
                if active:
                    owner.terminate();owner.wait(timeout=8)
                    rejoined=peer("rejoined",["-tp-join","127.0.0.1",str(port)],profile="famowner",scenario="observe")
                    print("Reconnecting the owning profile during the live ultimate",flush=True)
            time.sleep(.5)
        result=evaluate(folder,a.case,a.reconnect)
        result["exe_sha256"]=hashlib.sha256(a.exe.read_bytes()).hexdigest()
        runtime=a.exe.parent/(a.exe.stem+"_Data")/"Managed/TumbangPreso.Runtime.dll"
        result["runtime_sha256"]=hashlib.sha256(runtime.read_bytes()).hexdigest()
        result["delay_one_way_ms"]=a.delay;result["loss"]=a.loss
        (folder/"result.json").write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2),flush=True)
        return 0 if result["ok"] else 1
    finally:
        for p in processes:
            if p.poll() is None:p.terminate()
        for p in processes:
            try:p.wait(timeout=8)
            except subprocess.TimeoutExpired:p.kill();p.wait()
        for h in handles:h.close()
        for rel,expected in manifest.items():
            target=PROFILE/rel
            if not target.resolve().is_relative_to(PROFILE.resolve()):raise ValueError("unsafe profile path")
            target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(backup/rel,target)
            if hashlib.sha256(target.read_bytes()).hexdigest()!=expected:raise RuntimeError("profile restore failed")
        print("Preserved "+str(len(manifest))+" existing profile files",flush=True)
if __name__=="__main__":raise SystemExit(main())
