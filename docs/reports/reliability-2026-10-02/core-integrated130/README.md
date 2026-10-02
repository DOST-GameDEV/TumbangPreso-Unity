# Integrated Core after protocol130

The engine-free suite passed698/698, zero failed or skipped, after the disjoint
Sean Cinder Gate contribution introduced seven Core cases. This was one integration
check for changed Core source, not another unchanged per-fix regression pass.
Command: dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj --no-restore.
Source: 8fee59a36136139a055cf91d19d09efc1fe1ee1b plus only uncommitted Unity
input/probe candidates; those files are outside the Core projects and not tested here.
Native input, actual transport, graphics and the new Windows player remain separate.
The fresh original TRX is retained beside this note.