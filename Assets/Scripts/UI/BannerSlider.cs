using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BannerSlider : MonoBehaviour
{
    [SerializeField] private List<Texture2D> defaultImages;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float displayTime = 2f;

    private readonly List<Texture2D> activeImages = new();
    private readonly List<Texture2D> cachedReadableImages = new();

    private MeshRenderer meshRenderer;
    private Material materialInstance;
    private int currentIndex = 0;
    private bool isSliding = false;
    private Texture2D slideAtlasTexture; // Reutilizado para evitar GC/allocs

    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        materialInstance = meshRenderer.material; // cachear instancia para evitar lookups

        if (defaultImages != null && defaultImages.Count > 0)
        {
            activeImages.AddRange(defaultImages);
            RebuildReadableCache();
        }

        if (activeImages.Count > 0)
        {
            StartCoroutine(SlideLoop());
        }
    }

    public void SetExternalImages(List<Texture> externalImages)
    {
        // Limpiar listas actuales
        activeImages.Clear();
        ClearReadableCache();

        if (externalImages != null && externalImages.Count > 0)
        {
            foreach (var tex in externalImages)
            {
                if (tex is Texture2D tex2D)
                    activeImages.Add(tex2D);
            }
        }
        else if (defaultImages != null && defaultImages.Count > 0)
        {
            activeImages.AddRange(defaultImages);
        }

        currentIndex = 0;

        if (activeImages.Count > 0)
        {
            RebuildReadableCache();
        }
    }

    private IEnumerator SlideLoop()
    {
        while (activeImages.Count > 0)
        {
            PrepareSlideTexture();

            yield return new WaitForSeconds(displayTime);
            yield return SlideToNext();

            currentIndex = (currentIndex + 1) % activeImages.Count;
        }
    }

    private void PrepareSlideTexture()
    {
        if (cachedReadableImages.Count == 0) return;

        int nextIndex = (currentIndex + 1) % cachedReadableImages.Count;

        Texture2D currentTex = cachedReadableImages[currentIndex];
        Texture2D nextTex = cachedReadableImages[nextIndex];

        // Asumimos dimensiones consistentes
        int width = currentTex.width;
        int height = currentTex.height;

        // Asegurar atlas reutilizable
        if (slideAtlasTexture == null || slideAtlasTexture.width != width || slideAtlasTexture.height != height * 2)
        {
            if (slideAtlasTexture != null)
            {
                Destroy(slideAtlasTexture);
            }
            slideAtlasTexture = new Texture2D(width, height * 2, TextureFormat.RGBA32, false);
            slideAtlasTexture.wrapMode = TextureWrapMode.Clamp;
            slideAtlasTexture.filterMode = FilterMode.Bilinear;
        }

        // Copiar regiones sin lecturas a CPU adicionales
        Graphics.CopyTexture(currentTex, 0, 0, 0, 0, width, height, slideAtlasTexture, 0, 0, 0, 0);
        Graphics.CopyTexture(nextTex, 0, 0, 0, 0, width, height, slideAtlasTexture, 0, 0, 0, height);
        slideAtlasTexture.Apply(false, false);

        materialInstance.mainTexture = slideAtlasTexture;
        materialInstance.mainTextureScale = new Vector2(1f, 0.5f);
        materialInstance.mainTextureOffset = new Vector2(0f, 0f); // mostrar mitad inferior (current)
    }

    private IEnumerator SlideToNext()
    {
        if (isSliding || activeImages.Count <= 1) yield break;

        isSliding = true;

        float elapsed = 0f;
        Vector2 startOffset = new Vector2(0f, 0f);     // mitad inferior
        Vector2 endOffset = new Vector2(0f, 0.5f);     // mitad superior

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            Vector2 offset = Vector2.Lerp(startOffset, endOffset, t);
            materialInstance.mainTextureOffset = offset;
            yield return null;
        }

        isSliding = false;
    }

    private void RebuildReadableCache()
    {
        ClearReadableCache();
        if (activeImages.Count == 0) return;

        int baseW = activeImages[0].width;
        int baseH = activeImages[0].height;
        bool warnedResize = false;

        foreach (var src in activeImages)
        {
            if (src.width != baseW || src.height != baseH)
            {
                if (!warnedResize)
                {
                    Debug.Log("[BannerSlider] Las imágenes de banners tienen tamaños distintos. Se redimensionarán al tamaño de la primera.");
                    warnedResize = true;
                }
                cachedReadableImages.Add(CreateReadableCopyResized(src, baseW, baseH));
            }
            else
            {
                cachedReadableImages.Add(CreateReadableCopy(src));
            }
        }
    }

    private void ClearReadableCache()
    {
        foreach (var tex in cachedReadableImages)
        {
            if (tex != null) Destroy(tex);
        }
        cachedReadableImages.Clear();
    }

    private Texture2D CreateReadableCopy(Texture2D source)
    {
        // Si ya es legible y en formato compatible, optimizar ruta
        if (source != null && source.isReadable && source.format == TextureFormat.RGBA32)
        {
            // Crear copia GPU->GPU para evitar modificaciones del original
            Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            Graphics.CopyTexture(source, copy);
            return copy;
        }

        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(source, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readableTex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readableTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        readableTex.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return readableTex;
    }

    private Texture2D CreateReadableCopyResized(Texture2D source, int targetW, int targetH)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetW, targetH, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(source, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
        tex.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        return tex;
    }

    private void OnDestroy()
    {
        ClearReadableCache();
        if (slideAtlasTexture != null)
        {
            Destroy(slideAtlasTexture);
            slideAtlasTexture = null;
        }
    }
}
