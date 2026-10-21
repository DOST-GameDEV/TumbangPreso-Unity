# Hosted lobby creation and cleanup lifetime candidate

A stop waiting for lobby A could delete whichever lobby B became current later;
older creation could also overwrite newer active ID/count state. Capture each
creation completion and generation, detach deletion state before awaiting, and
clean up only the lobby belonging to that operation. Obsolete create completion
cannot adopt or clear newer host state; current latest-count behavior stays intact.

Seven deferred public create/delete/update cases cover three causal orderings and
four current/count/cleanup controls. Static review and fresh Runtime/fixture
compilation pass. Original/seam-only source retained. No SDK/socket/native call;
actual lobby allocation and cleanup acceptance remain pending laptop validation.
