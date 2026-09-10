using UnityEditor;

namespace TumbangPreso.EditorTools
{
    // Ground conformance reads the authored vertices at cast time. Keep these
    // small source meshes readable and free of imported materials/colliders.
    public sealed class PermafrostModelImport : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/TumbangPreso/Resources/Models/Permafrost/")
                && !assetPath.StartsWith("Assets/TumbangPreso/Resources/Models/CheskaIce/")) return;
            var importer = (ModelImporter)assetImporter;
            importer.isReadable = true;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.meshCompression = ModelImporterMeshCompression.Off;
        }
    }
}
