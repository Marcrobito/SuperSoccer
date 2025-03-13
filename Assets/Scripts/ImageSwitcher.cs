using UnityEngine;
using UnityEngine.UI;

public class ImageSwitcher : MonoBehaviour
{
    public RawImage rawImage;
    public Texture imageA, imageB;

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
    }

    public void SetImageA() { 
        rawImage.texture  = imageA; 
        rawImage.enabled = true;
    }

    public void SetImageB()
    {
        rawImage.texture = imageB;
        rawImage.enabled = true;
    }

    public void ClearImage() {
        rawImage.texture = null;
        rawImage.enabled = false; 
    }
}
