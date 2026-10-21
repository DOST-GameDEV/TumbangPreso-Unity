# Match record receive boundary

Own only MatchRpc.cs and MatchRecordPacketTests.cs/meta. Source inspection:
OnMatchRecordMsg checks sender/loopback but decodes an unchecked UTF-16 frame
and parses JSON without a refusal boundary. Preserve the previous Last/result
event state on bad packets; accept valid Unicode records with existing normalisation.

Original qualification frozen before candidate. Four actual receiver cases,
one original and one candidate native EditMode run. At most one tooling/fixture
repair; retain failure and do not weaken assertions. Original job waits exclusively
behind the live Classic pair. Product change uses existing SkipWireString and
catches only JsonUtility ArgumentException. No protocol/schema/authority change.
