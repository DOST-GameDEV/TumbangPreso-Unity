import hashlib,json,sys
from pathlib import Path
folder=Path(sys.argv[1]).resolve()
run=json.loads((folder/'result.json').read_text())
assert run['passed'],'Actual peer scenario failed; aggregate cannot qualify it'
peers={r:json.loads((folder/(r+'.json')).read_text()) for r in ('host','client')}
result={'passed':True,'roles':{},'sameSavedMatch':False,'sameSavedScores':False,'scope':'After terminal player exits, inspect only task-owned fresh career files. Export no account identity, handle, token or profile contents.'}
records={}
for role in ('host','client'):
    root=Path(run['profiles'][role]['root'])
    name=run['profiles'][role]['name']
    assert root.name==hashlib.sha256(name.encode()).hexdigest(),'Profile ownership mismatch'
    path=root/'career.json'
    cache=json.loads(path.read_text(encoding='utf-8-sig')) if path.is_file() else {}
    history=cache.get('History',[]);queue=cache.get('Queue',[])
    settings_bytes=(root/'settings.json').read_bytes()
    assert hashlib.sha256(settings_bytes).hexdigest()==run['profiles'][role]['seedSha256'],'Restored seed mismatch'
    settings=json.loads(settings_bytes.decode('utf-8-sig'))
    owner=settings.get('AccountPlayerId') or (settings['PlayerToken']+'_'+name)
    # Mirrors PlayerAccount.ReadLocal/NetIdentity.LocalToken. Guest Cache.OwnerId is deliberately empty until first account adoption.
    record=history[0] if len(history)==1 else {}
    lines=record.get('Players',[])
    scores=[line['Score'] for line in sorted(lines,key=lambda line:line['Slot'])]
    local=[line for line in lines if owner and line.get('PlayerId')==owner]
    summary=dict(fileExists=path.is_file(),historyCount=len(history),queueCount=len(queue),witnessCount=len(cache.get('QueueWitness',[])),ownerPresent=bool(owner),localOwnerLines=len(local),localOwnerIsHuman=len(local)==1 and not local[0].get('IsBot',True),online=record.get('Online'),rounds=record.get('Rounds'),mode=record.get('Mode'),scores=scores,recordMatchesPeer=bool(record.get('MatchId')) and record.get('MatchId')==peers[role].get('recordId'),queueMatchesHistory=len(queue)==1 and queue[0]==record,abandonMarkerCleared=not cache.get('InMatchSinceUtc'))
    summary['passed']=all((summary['fileExists'],len(history)==1,len(queue)==1,summary['witnessCount']==1,summary['ownerPresent'],summary['localOwnerLines']==1,summary['localOwnerIsHuman'],summary['online'] is True,summary['rounds']==1,summary['recordMatchesPeer'],summary['queueMatchesHistory'],summary['abandonMarkerCleared'],scores==peers[role]['scores']))
    result['roles'][role]=summary;result['passed'] &= summary['passed'];records[role]=record
result['sameSavedMatch']=bool(records['host'].get('MatchId')) and records['host'].get('MatchId')==records['client'].get('MatchId')
result['sameSavedScores']=result['roles']['host']['scores']==result['roles']['client']['scores']
result['passed'] &= result['sameSavedMatch'] and result['sameSavedScores']
(folder/'career-aggregate.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result,indent=2))
sys.exit(0 if result['passed'] else 1)
