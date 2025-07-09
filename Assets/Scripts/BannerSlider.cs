using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BannerSlider : MonoBehaviour
{
    [SerializeField] private List<Texture2D> defaultImages;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float displayTime = 2f;

    private List<Texture2D> activeImages = new();
    private MeshRenderer meshRenderer;
    private int currentIndex = 0;
    private bool isSliding = false;
    private Texture2D currentSlideTexture;

    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        if (defaultImages.Count > 0)
        {
            activeImages.AddRange(defaultImages);
        }

        StartCoroutine(SlideLoop());
    }

    public void SetExternalImages(List<Texture> externalImages)
    {
        activeImages.Clear();

        if (externalImages != null && externalImages.Count > 0)
        {
            foreach (var tex in externalImages)
            {
                if (tex is Texture2D tex2D)
                    activeImages.Add(tex2D);
            }
        }
        else
        {
            activeImages.AddRange(defaultImages);
        }

        currentIndex = 0;
    }

    private IEnumerator SlideLoop()
    {
        while (true)
        {
            PrepareSlideTexture();

            yield return new WaitForSeconds(displayTime);
            yield return StartCoroutine(SlideToNext());

            currentIndex = (currentIndex + 1) % activeImages.Count;
        }
    }

    private void PrepareSlideTexture()
    {
        int nextIndex = (currentIndex + 1) % activeImages.Count;

        Texture2D currentTex = CreateReadableCopy(activeImages[currentIndex]);
        Texture2D nextTex = CreateReadableCopy(activeImages[nextIndex]);

        int width = currentTex.width;
        int height = currentTex.height;

        currentSlideTexture = new Texture2D(width, height * 2, TextureFormat.RGBA32, false);
        currentSlideTexture.SetPixels(0, 0, width, height, currentTex.GetPixels());
        currentSlideTexture.SetPixels(0, height, width, height, nextTex.GetPixels());
        currentSlideTexture.Apply();

        meshRenderer.material.mainTexture = currentSlideTexture;
        meshRenderer.material.mainTextureScale = new Vector2(1f, 0.5f);
        meshRenderer.material.mainTextureOffset = new Vector2(0f, -0.5f);
    }

    private IEnumerator SlideToNext()
    {
        if (isSliding || activeImages.Count <= 1) yield break;

        isSliding = true;

        float elapsed = 0f;
        Vector2 startOffset = new Vector2(0f, -0.5f);
        Vector2 endOffset = Vector2.zero;  // Termina en 0, solo sube media unidad

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            Vector2 offset = Vector2.Lerp(startOffset, endOffset, t);
            meshRenderer.material.mainTextureOffset = offset;
            yield return null;
        }

        isSliding = false;
    }
    private Texture2D CreateReadableCopy(Texture2D source)
    {
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
}