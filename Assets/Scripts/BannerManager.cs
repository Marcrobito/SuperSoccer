using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BannerManager : MonoBehaviour
{
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
        string folderPath = Path.Combine(Application.dataPath, "banners");
        if (!Directory.Exists(folderPath))
        {
            Debug.Log("[BannerManager] Carpeta 'banners' no encontrada. Usando imágenes por defecto.");
            return;
        }

        string[] files = Directory.GetFiles(folderPath, "*.*");
        List<Texture> loadedTextures = new();

        foreach (string file in files)
        {
            if (file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith(".jpeg"))
            {
                byte[] data = File.ReadAllBytes(file);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(data))
                {
                    loadedTextures.Add(tex);
                }
            }
        }

        if (loadedTextures.Count > 0)
        {
            SetBanners(loadedTextures);
            Debug.Log($"[BannerManager] {loadedTextures.Count} imágenes externas cargadas desde /banners.");
        }
        else
        {
            Debug.Log("[BannerManager] No se encontraron imágenes válidas en la carpeta. Usando imágenes por defecto.");
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
}