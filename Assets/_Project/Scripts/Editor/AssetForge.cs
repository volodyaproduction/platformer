using System.IO;
using UnityEditor;
using UnityEngine;

// Генератор плейсхолдер-спрайтов через прямую запись пикселей в Texture2D +
// EncodeToPNG. Сейчас единственный объект — «шипы» (ловушка). По аналогии с
// shooter/AssetForge, но компактно: один метод на один ассет.
public static class AssetForge
{
    const string GeneratedDir = "Assets/_Project/Art/Generated";

    public static void BuildAll()
    {
        // Перерисовываем спрайт на каждой сборке — правки алгоритма
        // BuildSpikeSprite должны подхватываться без ручного удаления PNG.
        EnsureDir(GeneratedDir);
        var spikePath = $"{GeneratedDir}/spike.png";
        BuildSpikeSprite(spikePath);
        AssetDatabase.ImportAsset(spikePath);
        Debug.Log("[AssetForge] Спрайт пересоздан: " + spikePath);
    }

    // ===== Шипы: два металлических треугольных шипа на прозрачном фоне =====

    static void BuildSpikeSprite(string path)
    {
        const int W = 96, H = 48;
        const int spikeCount = 2;
        int spikeW = W / spikeCount;       // ширина одного шипа в пикселях
        int halfW = spikeW / 2;

        var pixels = new Color32[W * H];
        var transparent = new Color32(0, 0, 0, 0);
        var metal = new Color32(120, 130, 145, 255);     // тёмно-серый
        var highlight = new Color32(200, 210, 220, 255); // светлый блик
        var outline = new Color32(35, 40, 50, 255);      // чёрная обводка

        for (int i = 0; i < pixels.Length; i++) pixels[i] = transparent;

        for (int x = 0; x < W; x++)
        {
            int local = x % spikeW;
            int distFromCenter = Mathf.Abs(local - halfW);
            // Высота треугольника в этой колонке: 1 в центре → H, по краям → 0
            int heightAtX = H - (distFromCenter * H / halfW);
            if (heightAtX <= 0) continue;

            for (int y = 0; y < heightAtX; y++)
            {
                int idx = y * W + x;
                bool isOutline = (y == heightAtX - 1)         // вершина
                              || (distFromCenter == halfW - 1) // боковые края
                              || y == 0;                        // основание
                bool isHighlight = local < halfW - 1
                              && distFromCenter > 2
                              && y < heightAtX - 3
                              && (local - distFromCenter) > 1;
                if (isOutline) pixels[idx] = outline;
                else if (isHighlight) pixels[idx] = highlight;
                else pixels[idx] = metal;
            }
        }

        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    static void EnsureDir(string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
    }
}
