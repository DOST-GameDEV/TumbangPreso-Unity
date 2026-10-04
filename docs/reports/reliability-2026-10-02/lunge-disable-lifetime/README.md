# A retired body cannot resume its old lunge contact sweep

CombatVerbs normally retires a lunge window when Update observes round inactivity,
stun or fear. Practice removal deactivates the body immediately; Update never sees
that interruption before reactivation restores the actor. The contact sweep can
therefore continue from its old position. OnDisable now clears only that lunge
contact window. Spent cooldown, windup/slide/impulse/timing behavior is unchanged.

A NEW materially different tiny PlayMode3 fixture uses actual public
HostResolveLunge with finite forward/power1, real CharacterMotor/CombatVerbs and
actual GameObject disable/enable. It never assigns the private contact/cooldown
fields. Controls hold the clock on the still-enabled body (positive window and
cooldown preserved), and deactivate an uncharged body (no contact/cooldown created).
The causal case verifies old contact is retired while cooldown survives and a
second public lunge is refused. No rig, map, Lata, range startup, raw input or expiry
wait/framework is involved. Motor simulation disabled; no actual travel/tag claim.

Original97501:1 intended causal failure/2controls; candidate18842:3/3. ZERO repairs,
identical fixture/meta. All12537 protected hashes and exact3 MAIN/q inputs match,
prep terminal0 before launch, both guards restored/terminal/no lease. Unity6000.5.8f1,
GPU-classified PlayMode/nographics2048+2048reserve/450s/profilelunge-disable-lifetime1002.

The earlier practice-bot-resume attempted operator helper remains retired and
unqualified after its two fixture flaws; its original evidence is unchanged. This
accepts the underlying component retirement boundary through the authoritative
public API, not that retired helper, actual SetBot operator, physical tag, network,
windup or slide behavior. Windows1002h predates this fix. No full readiness claim.
