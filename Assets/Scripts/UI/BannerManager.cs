using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BannerManager : MonoBehaviour
{
    [SerializeField] private ConfigLoader configLoader;
    [SerializeField] private List<BannerSlider> plantaA;
    [SerializeField] private List<BannerSlider> plantaB;
    [SerializeField] private List<BannerSlider> plantaC;

    /// <summary>
    /// Asigna las imágenes externas a los banners. 
    /// Si no se pasa ninguna, no hace nada (los banners usarán sus imágenes por defecto).
    /// </summary>
    /// 
    /// 
    private void Start()
    {
        List<Texture> loadedTextures = LoadFromConfig();

        if (loadedTextures.Count == 0)
        {
            loadedTextures = LoadFromLegacyFolder();
        }

        if (loadedTextures.Count > 0)
        {
            SetBanners(loadedTextures);
            Debug.Log($"[BannerManager] {loadedTextures.Count} imágenes externas configuradas.");
        }
        else
        {
            Debug.Log("[BannerManager] No se encontraron banners externos. Usando imágenes por defecto.");
        }
    }
    public void SetBanners(List<Texture> externalImages)
    {
        if (externalImages == null || externalImages.Count == 0)
        {
            // No hacemos nada. Cada BannerSlider ya carga sus imágenes por defecto en Start().
            return;
        }

        // Planta A: orden original
        foreach (var banner in plantaA)
        {
            banner.SetExternalImages(new List<Texture>(externalImages));
        }

        // Planta B: rotamos el orden una vez
        List<Texture> rotatedOnce = RotateListLeft(externalImages, 1);
        foreach (var banner in plantaB)
        {
            banner.SetExternalImages(rotatedOnce);
        }

        // Planta C: rotamos dos veces
        List<Texture> rotatedTwice = RotateListLeft(externalImages, 2);
        foreach (var banner in plantaC)
        {
            banner.SetExternalImages(rotatedTwice);
        }
    }

    private List<Texture> RotateListLeft(List<Texture> list, int steps)
    {
        int count = list.Count;
        if (count == 0) return new List<Texture>();

        List<Texture> rotated = new List<Texture>(count);
        for (int i = 0; i < count; i++)
        {
            rotated.Add(list[(i + steps) % count]);
        }
        return rotated;
    }

    private List<Texture> LoadFromConfig()
    {
        List<Texture> textures = new();

        if (configLoader == null)
        {
            return textures;
        }

        var bannerPaths = configLoader.CurrentConfig?.questionSettings?.bannerPaths;
        if (bannerPaths == null || bannerPaths.Count == 0)
        {
            return textures;
        }

        foreach (string relativePath in bannerPaths)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                continue;
            }

            string fullPath = configLoader.ResolveAssetPath(relativePath);
            Texture2D texture = LoadTextureFromFile(fullPath);
            if (texture != null)
            {
                textures.Add(texture);
            }
        }

        return textures;
    }

    private List<Texture> LoadFromLegacyFolder()
    {
        List<Texture> textures = new();
        string folderPath = Path.Combine(Application.dataPath, "banners");

        if (!Directory.Exists(folderPath))
        {
            return textures;
        }

        string[] files = Directory.GetFiles(folderPath, "*.*");
        foreach (string file in files)
        {
            Texture2D texture = LoadTextureFromFile(file);
            if (texture != null)
            {
                textures.Add(texture);
            }
        }

        return textures;
    }

    private Texture2D LoadTextureFromFile(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
        {
            return null;
        }

        if (!fullPath.EndsWith(".png") && !fullPath.EndsWith(".jpg") && !fullPath.EndsWith(".jpeg"))
        {
            return null;
        }

        try
        {
            byte[] data = File.ReadAllBytes(fullPath);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(data))
            {
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                return tex;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[BannerManager] Error al cargar {fullPath}: {ex.Message}");
        }

        return null;
    }
}
