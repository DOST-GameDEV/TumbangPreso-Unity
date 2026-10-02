from pathlib import Path
import hashlib,json,sys
folder=Path(sys.argv[1]).resolve()
run=json.loads((folder/'result.json').read_text(encoding='utf-8-sig'))
assert run['passed'] and run['profileSeedsRestored'] and run['runtimeUnchanged'],'Native tournament did not qualify'
result=dict(passed=True,roles={},sameSavedMatch=False,sameSavedScores=False,scope='After terminal exits, compare task-owned fresh eight-round career files and actual peer scores; private account IDs, handles, tokens and profile contents excluded.')
records={}
for role in ('host','client'):
    root=Path(run[role+'Profile'])
    command=run[role+'Command'];name=command[command.index('-tp-profile')+1]
    assert root.name==hashlib.sha256(name.encode()).hexdigest(),'Profile ownership mismatch'
    settings=json.loads((root/'settings.json').read_text(encoding='utf-8-sig'))
    local=settings.get('AccountPlayerId') or settings['PlayerToken']+'_'+name
    path=root/'career.json';cache=json.loads(path.read_text(encoding='utf-8-sig')) if path.is_file() else {}
    history=cache.get('History',[]);queue=cache.get('Queue',[]);record=history[0] if len(history)==1 else {}
    lines=record.get('Players',[]);scores=[p['Score'] for p in sorted(lines,key=lambda p:p['Slot'])]
    own=[p for p in lines if p.get('PlayerId')==local]
    summary=dict(fileExists=path.is_file(),historyCount=len(history),queueCount=len(queue),witnessCount=len(cache.get('QueueWitness',[])),localHumanLines=len(own) if len(own)==1 and not own[0].get('IsBot',True) else 0,online=record.get('Online'),rounds=record.get('Rounds'),mode=record.get('Mode'),scores=scores,actualScoresMatch=scores==run['terminalScores'][role],queueMatchesHistory=len(queue)==1 and queue[0]==record,abandonMarkerCleared=not cache.get('InMatchSinceUtc'),appliedMatchCount=len(cache.get('Profile',{}).get('AppliedMatchIds',[])))
    summary['passed']=all((summary['fileExists'],summary['historyCount']==1,summary['queueCount']==1,summary['witnessCount']==1,summary['localHumanLines']==1,summary['online'] is True,summary['rounds']==8,summary['mode']==run.get('modeRequested','Classic'),summary['actualScoresMatch'],summary['queueMatchesHistory'],summary['abandonMarkerCleared'],summary['appliedMatchCount']==1))
    result['roles'][role]=summary;result['passed'] &= summary['passed'];records[role]=record
result['sameSavedMatch']=bool(records['host'].get('MatchId')) and records['host'].get('MatchId')==records['client'].get('MatchId')
result['sameSavedScores']=result['roles']['host']['scores']==result['roles']['client']['scores']
result['passed'] &= result['sameSavedMatch'] and result['sameSavedScores']
(folder/'career-aggregate.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2));sys.exit(0 if result['passed'] else 1)
