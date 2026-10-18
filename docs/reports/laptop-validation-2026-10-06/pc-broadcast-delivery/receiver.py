from pathlib import Path
import datetime,json,os,socket,time

out=Path('work/route-diagnostic1006');out.mkdir(exist_ok=False)
finish=out/'sender-finished.marker'
def utc():return datetime.datetime.now(datetime.timezone.utc).isoformat()
prefix=b'tump-route-diagnostic1006'
packets=[];ignored=0;started=time.monotonic();deadline=None
with socket.socket(socket.AF_INET,socket.SOCK_DGRAM) as receiver:
    receiver.bind(('0.0.0.0',18991));receiver.settimeout(.25)
    ready={'pid':os.getpid(),'boundAddress':'0.0.0.0','port':18991,'readyUtc':utc(),'scope':'One agreed diagnostic UDP round, prefix-filtered; not a game socket or discovery acceptance'}
    (out/'ready.json').write_text(json.dumps(ready,indent=2));print(json.dumps(ready),flush=True)
    while True:
        if finish.exists() and deadline is None:deadline=time.monotonic()+15
        if deadline is not None and time.monotonic()>=deadline:break
        try:data,source=receiver.recvfrom(2048)
        except socket.timeout:continue
        if not data.startswith(prefix):ignored+=1;continue
        row={'utc':utc(),'secondsSinceReady':round(time.monotonic()-started,6),'senderAddress':source[0],'senderPort':source[1],'payload':data.decode('ascii',errors='backslashreplace'),'bytes':len(data)}
        packets.append(row)
        with (out/'packets.jsonl').open('a',encoding='utf-8') as stream:stream.write(json.dumps(row)+'\n')
        print(json.dumps(row),flush=True)
    result=ready|{'terminal':True,'socketClosed':True,'endedUtc':utc(),'packets':packets,'ignoredPacketCount':ignored,'settleSecondsAfterSenderAck':15}
(out/'result.json').write_text(json.dumps(result,indent=2));print(json.dumps({'terminal':True,'socketClosed':True,'matchingPackets':len(packets)}),flush=True)
