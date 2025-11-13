using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class VideoManager : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public RawImage rawImage;

    private void Start()
    {
        // Suscribirse al evento de finalización del video
        videoPlayer.loopPointReached += OnVideoEnd;
        PlayVideo();
    }

    public void PlayVideo()
    {
        if (videoPlayer.clip != null)
        {
            videoPlayer.Play();
        }
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        SceneManager.LoadScene("Game");
    }
}