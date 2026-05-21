using UnityEditor;
using UnityEngine;

// Постпроцессор импорта текстур: все PNG из _Project/Kenney/ и
// _Project/Art/Generated/ автоматически импортируются как Sprite c PPU = 128
// (одна плитка земли = 1 unit). Запускается Unity при первом импорте
// ассетов в batch-mode.
public class KenneyTexturePostprocessor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/_Project/Kenney/")
            && !assetPath.Contains("/_Project/Art/Generated/")) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 128f;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
    }
}
