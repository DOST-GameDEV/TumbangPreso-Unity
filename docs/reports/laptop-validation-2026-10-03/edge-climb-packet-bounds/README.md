# Edge climb request packet framing

The original decoder reads two ints without first checking its complete payload.
Original6 reproduced four failures and two controls: payloads ending at8/12/15
after the consumed NGO ulong envelope throw OverflowException; the trailing-byte
frame advances decode position to16 instead of leaving it at8. The complete
invalid-seat frame and client-authority empty-frame controls pass.

Immutable original3ead4fc275c7f47fc25f47bd2f37ee564ade292d and candidate
c856dfe256210d480a23a0be38ea1e78a663f47c ran locally on gamergmae with
Unity6000.5.8f1. Candidate6 passes6/6. Only MatchRpc.EdgeRecovery.cs differs
among3396 frozen inputs; fixture/meta stayed identical. The original hashes were
verified unchanged before the candidate overlay and candidate hashes after its
job. Both guards were terminal, restored isolated preferences/profile and freed
leases, exit2/0. No fixture repair, retries or PC Unity job occurred.

Headless EditMode CPU2048MiB/reserve1024 with one job and GC helper used isolated
qa-a Library/company/product/profile. Older worker base8e7 and logical immutable
source are disclosed in full maps. Working fixture/meta CRLF conversions match
the immutable Git bytes; candidate production bytes are from Git directly. Raw
XML, maps and receipts are preserved byte-exact. Summary records exact times.

The supplied reader has consumed the eight-byte NGO envelope. A dormant RPC and
claimed seat-1 refuse ownership before any recovery/body/movement epoch access.
This qualifies decoder bounds and position only, not network delivery, SDK or
allocation exhaustion, climb geometry, body recovery, whole-player impact or
current matching peers. No protocol142 or wire/hero/timing change is claimed.
The PC source owner can inspect this evidence and integrate its checked guard.
