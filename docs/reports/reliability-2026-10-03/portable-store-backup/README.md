# Preserve a usable portable save backup

Windows replacement checked validPrevious before rotating a primary into .bak.
The portable move sequence ignored that validation, so a corrupt primary could
replace the last usable backup. A later interrupted primary then had no recovery.

Both paths now use the existing validation decision. The portable path discards
an invalid primary while retaining its backup; a usable primary still rotates.
Validator exceptions remain invalid through SafeValid. Public methods, file
names, schemas and Windows File.Replace behavior are preserved. A private helper
accepts the runtime platform so tests exercise the production portable branch
without changing global OS state or touching live profiles.

Original six cases fail4: Android/OSX/Linux branch selections and a throwing
validator. Two valid/legacy rotation controls pass. First candidate6/6, unchanged
assertions, independently reviewed. The cases use fresh task-owned temporary
files and verify recovery through the ordinary Read path after another corruption.
Both guards terminated, restored profiles/preferences and released their leases.
This is Windows native execution of the exact portable file operation sequence;
actual portable filesystems and process-crash atomicity remain separate gates.
