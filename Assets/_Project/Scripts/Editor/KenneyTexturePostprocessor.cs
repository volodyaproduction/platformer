using UnityEditor;
using UnityEngine;

// Постпроцессор импорта текстур: все PNG из _Project/Kenney/ автоматически
// импортируются как Sprite c PPU = 128 (одна плитка земли = 1 unit).
// Это запускается Unity сразу при первом импорте ассетов в batch-mode.
public class KenneyTexturePostprocessor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/_Project/Kenney/")) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 128f;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
    }
}
