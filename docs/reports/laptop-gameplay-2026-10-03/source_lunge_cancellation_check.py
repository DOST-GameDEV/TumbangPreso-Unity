"""Source-bound managed branch check; deliberately not Unity or operator acceptance.

Extracts shipping StepLunge and reader retirement callbacks from the fetched base
and qualified input-cancellation commit. Hardware-discard and engine release side effects are supplied
seams. The assertion is whether the real StepLunge calls ReleaseLunge after the
real reader callback; no physics, Unity lifecycle, input device or peer is run.
"""
import argparse
import hashlib
import json
import pathlib
import re
import subprocess


def method(source, name, optional=False):
    found = re.search(r"(?:private|internal) void " + name + r"\([^\n]*\)\s*\{", source)
    if not found:
        if optional:
            return ""
        raise ValueError("Missing shipping method " + name)
    start = found.start()
    cursor = source.index("{", found.start())
    depth = 1
    cursor += 1
    while depth:
        if source[cursor] == "{":
            depth += 1
        elif source[cursor] == "}":
            depth -= 1
        cursor += 1
    return source[start:cursor]


def generate(verbs, reader):
    return """using System;
enum Verb { Lunge }
static class Mathf { public static float Clamp(float v,float a,float b)=>Math.Clamp(v,a,b); }
static class Balance { public const float LungeChargeTime=1, LungeMinPower=.2f; }
sealed class Intent { public bool Held; public bool Pressed(Verb v)=>Held; public void Clear()=>Held=false; public void CommitFrame(){} }
sealed class Motor { public Intent Intent=new(); public Carrier Carrier=new(); public CombatVerbs Combat;
 public T GetComponent<T>() where T:class => typeof(T)==typeof(Carrier)?Carrier as T:Combat as T; }
sealed class Carrier { public float ChannelRatio; public void CancelPendingInput(){} }
sealed class CombatVerbs {
 private Motor _motor; private Carrier _carrier; private float _lungeCooldown,_lungeCharge,_observedLunge=-1;
 private bool _lungeCharging; public int Releases; public float Contact;
 public CombatVerbs(Motor motor){_motor=motor;_carrier=motor.Carrier;motor.Combat=this;}
 public void Step(float dt)=>StepLunge(dt);
 public void Commit(){Contact=1;_lungeCooldown=1;}
 public float Cooldown=>_lungeCooldown;
 // Engine side-effect seam records invocation, not travel, scoring or presentation.
 private void ReleaseLunge(float power){Releases++;Contact=1;_lungeCooldown=1;}
""" + method(verbs, "StepLunge") + method(verbs, "CancelPendingInput", True) + """
}
sealed class PlayerInputReader {
 private Motor _motor; private bool _readyUseHeld,_readyInteractHeld;
 public PlayerInputReader(Motor motor){_motor=motor;}
 // Hardware/menu seam publishes the same cleared intent as the shipping discard method.
 private void DiscardMenuButtonsUntilRelease(){_motor.Intent.Clear();_motor.Intent.CommitFrame();}
 private void ResetToggleControls(){}
 public void Focus()=>OnApplicationFocus(false); public void Disable()=>OnDisable();
""" + method(reader, "OnApplicationFocus") + method(reader, "OnDisable") + method(reader, "CancelPendingInput", True) + """
}
static class Program {
 static int Main(){int failures=0;
 foreach(bool focus in new[]{true,false}){
   var motor=new Motor();var verbs=new CombatVerbs(motor);var reader=new PlayerInputReader(motor);
   motor.Intent.Held=true;verbs.Step(.1f);if(focus)reader.Focus();else reader.Disable();verbs.Step(.1f);
   bool passed=verbs.Releases==0&&verbs.Cooldown==0;
   Console.WriteLine((focus?"focus_pending_lunge":"reader_disable_pending_lunge")+"="+(passed?"PASS":"FAIL")+" releaseCalls="+verbs.Releases);if(!passed)failures++;
 }
 {
   var motor=new Motor();var verbs=new CombatVerbs(motor);motor.Intent.Held=true;verbs.Step(.1f);motor.Intent.Held=false;verbs.Step(.1f);
   bool passed=verbs.Releases==1&&verbs.Cooldown>0;Console.WriteLine("ordinary_release="+(passed?"PASS":"FAIL"));if(!passed)failures++;
 }
 {
   var motor=new Motor();var verbs=new CombatVerbs(motor);var reader=new PlayerInputReader(motor);verbs.Commit();reader.Focus();
   bool passed=verbs.Contact==1&&verbs.Cooldown==1;Console.WriteLine("committed_contact_preserved="+(passed?"PASS":"FAIL"));if(!passed)failures++;
 }
 return failures;
 }
}
"""


def main():
    args = argparse.ArgumentParser()
    args.add_argument("--repo", required=True, type=pathlib.Path)
    args.add_argument("--output", required=True, type=pathlib.Path)
    args.add_argument("--baseline", default="f9552d58a4d59ecdc87a15a2203d82893e3aa5a3")
    args.add_argument("--candidate-ref", default="88dba66a1")
    opts = args.parse_args()
    opts.output.mkdir(parents=True, exist_ok=False)
    identity = subprocess.check_output(["git", "rev-parse", opts.baseline], cwd=opts.repo, text=True).strip()
    candidate_identity = subprocess.check_output(["git", "rev-parse", opts.candidate_ref], cwd=opts.repo, text=True).strip()
    sources = ["Assets/TumbangPreso/Runtime/CombatVerbs.cs", "Assets/TumbangPreso/Runtime/PlayerInputReader.cs"]
    report = {"baseline": identity, "candidate": candidate_identity, "evidence": "source-bound managed invocation with supplied engine/hardware seams", "runs": {}}
    for label in ("original", "candidate"):
        revision = identity if label == "original" else candidate_identity
        texts = [subprocess.check_output(["git", "show", revision + ":" + p], cwd=opts.repo).decode("utf-8") for p in sources]
        folder = opts.output / label
        folder.mkdir()
        (folder / "Program.cs").write_text(generate(*texts), encoding="utf-8")
        (folder / "Check.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><EnableNETAnalyzers>false</EnableNETAnalyzers><NoWarn>CS0169;CS0414;CS0649</NoWarn></PropertyGroup></Project>', encoding="utf-8")
        build = subprocess.run(["dotnet", "build", "Check.csproj", "--nologo", "-v:q"], cwd=folder, text=True, capture_output=True)
        (folder / "build.log").write_text(build.stdout + build.stderr, encoding="utf-8")
        if build.returncode:
            raise RuntimeError(label + " build failed; no runtime evidence. See build.log")
        run = subprocess.run(["dotnet", str(folder / "bin/Debug/net9.0/Check.dll")], text=True, capture_output=True)
        (folder / "run.log").write_text(run.stdout + run.stderr, encoding="utf-8")
        report["runs"][label] = {"exit": run.returncode, "cases": run.stdout.splitlines(), "sources": {p: hashlib.sha256(t.encode()).hexdigest() for p,t in zip(sources,texts)}}
    (opts.output / "result.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))
    original_cases = ["focus_pending_lunge=FAIL releaseCalls=1", "reader_disable_pending_lunge=FAIL releaseCalls=1",
                      "ordinary_release=PASS", "committed_contact_preserved=PASS"]
    candidate_cases = ["focus_pending_lunge=PASS releaseCalls=0", "reader_disable_pending_lunge=PASS releaseCalls=0",
                       "ordinary_release=PASS", "committed_contact_preserved=PASS"]
    if (report["runs"]["original"]["exit"] != 2 or report["runs"]["candidate"]["exit"] != 0
            or report["runs"]["original"]["cases"] != original_cases
            or report["runs"]["candidate"]["cases"] != candidate_cases):
        raise RuntimeError("Expected exactly two baseline contract failures and zero candidate failures")


if __name__ == "__main__":
    main()
