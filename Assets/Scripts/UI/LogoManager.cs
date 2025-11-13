using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class LogoManager : MonoBehaviour
{
    [SerializeField] private ConfigLoader configLoader;
    [SerializeField] private Image topLeftLogo;
    [SerializeField] private Image topRightLogo;
    [SerializeField] private Image bottomLeftLogo;
    [SerializeField] private Image bottomRightLogo;

    private void Start()
    {
        ApplyConfiguration();
    }

    [ContextMenu("Apply Configuration")]
    public void ApplyConfiguration()
    {
        if (configLoader == null)
        {
            Debug.LogWarning("LogoManager no tiene ConfigLoader asignado.");
            HideAll();
            return;
        }

        LogoSettings settings = configLoader.CurrentConfig?.logoSettings;
        if (settings == null)
        {
            HideAll();
            return;
        }

        ApplyLogo(topLeftLogo, settings.topLeft, LogoPlacement.TopLeft, settings.logoScale);
        ApplyLogo(topRightLogo, settings.topRight, LogoPlacement.TopRight, settings.logoScale);
        ApplyLogo(bottomLeftLogo, settings.bottomLeft, LogoPlacement.BottomLeft, settings.logoScale);
        ApplyLogo(bottomRightLogo, settings.bottomRight, LogoPlacement.BottomRight, settings.logoScale);
    }

    private void ApplyLogo(Image target, LogoSlotConfig slotConfig, LogoPlacement placement, float scale)
    {
        if (target == null)
        {
            return;
        }

        if (slotConfig == null || !slotConfig.enabled)
        {
            HideLogo(target);
            return;
        }

        target.gameObject.SetActive(true);
        PositionLogo(target.rectTransform, placement);
        target.rectTransform.localScale = Vector3.one * scale;

        Sprite sprite = LoadSprite(slotConfig.relativePath);
        if (sprite != null)
        {
            target.sprite = sprite;
            target.SetNativeSize();
            target.rectTransform.localScale = Vector3.one * scale;
        }
        else
        {
            HideLogo(target);
        }
    }

    private void HideLogo(Image target)
    {
        if (target == null)
        {
            return;
        }

        target.gameObject.SetActive(false);
    }

    private void PositionLogo(RectTransform rectTransform, LogoPlacement placement)
    {
        if (rectTransform == null)
        {
            return;
        }

        Vector2 anchor;
        Vector2 pivot;

        switch (placement)
        {
            case LogoPlacement.TopLeft:
                anchor = new Vector2(0f, 1f);
                pivot = new Vector2(0f, 1f);
                break;
            case LogoPlacement.TopRight:
                anchor = new Vector2(1f, 1f);
                pivot = new Vector2(1f, 1f);
                break;
            case LogoPlacement.BottomLeft:
                anchor = new Vector2(0f, 0f);
                pivot = new Vector2(0f, 0f);
                break;
            case LogoPlacement.BottomRight:
                anchor = new Vector2(1f, 0f);
                pivot = new Vector2(1f, 0f);
                break;
            default:
                anchor = new Vector2(0.5f, 0.5f);
                pivot = new Vector2(0.5f, 0.5f);
                break;
        }

        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private void HideAll()
    {
        HideLogo(topLeftLogo);
        HideLogo(topRightLogo);
        HideLogo(bottomLeftLogo);
        HideLogo(bottomRightLogo);
    }

    private Sprite LoadSprite(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || configLoader == null)
        {
            return null;
        }

        string fullPath = configLoader.ResolveAssetPath(relativePath);
        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
        {
            Debug.LogWarning($"LogoManager no encontró el archivo de logo en {fullPath}");
            return null;
        }

        try
        {
            byte[] data = File.ReadAllBytes(fullPath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, data))
            {
                Debug.LogWarning($"No se pudo cargar la textura para {fullPath}");
                return null;
            }

            texture.name = Path.GetFileName(fullPath);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error al cargar logo {fullPath}: {ex.Message}");
            return null;
        }
    }
}
