using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[ExecuteInEditMode]
public class TextAnimator : MonoBehaviour
{

    [SerializeField]
    private string _message;

    [SerializeField]
    private float _stringAnimationDuration;

    [SerializeField]
    private TextMeshProUGUI _animatedText;

    [SerializeField]
    private AnimationCurve _sizeCurve;
    [SerializeField]
    private float _sizeScale;


    [SerializeField]
    private AnimationCurve _heightCurve;
    [SerializeField]
    private float _heightScale;

    [SerializeField]
    private AnimationCurve _rotationCurve;
    [SerializeField]
    private float _rotationScale;

    [SerializeField]
    private Gradient _colour;

    [SerializeField]
    [Range(0.0001f, 1)]
    private float _charAnimationDuration;

    [SerializeField]
    [Range(0.0001f, 1)]
    private float _editorTValue;

    private float _timeElapsed;
    private Coroutine _currentAnimationCoroutine;

    private void Start()
    {
        //StartCoroutine(RunAnimation(3));
    }

    private void Update()
    {
        EvaluateRichText(_editorTValue);
    }

    public void displayAnimation(string message){
        if (_currentAnimationCoroutine != null)
        {
            StopCoroutine(_currentAnimationCoroutine);
        }

        // Restablecer variables al estado inicial
        _message = message;
        _timeElapsed = 0f;
        _animatedText.text = "";  // Limpiar el texto

        // Iniciar la nueva animación
        _currentAnimationCoroutine = StartCoroutine(RunAnimation(0));

    }

    IEnumerator RunAnimation(float waitForSeconds)
    {
        yield return new WaitForSeconds(waitForSeconds);
        float t = 0;
        while (t <= 1f)
        {
            EvaluateRichText(t);
            t = _timeElapsed / _stringAnimationDuration;
            _timeElapsed += Time.deltaTime;
            yield return null;
        }
    }

    void EvaluateRichText(float t)
    {
        _animatedText.text = "";

        for (int i = 0; i < _message.Length; i++)
        {
            _animatedText.text += EvaluateRichText(_message[i], _message.Length, i, t);
        }
    }

    private string EvaluateRichText(char c, int sLength, int cPosition, float t)
    {
        float startPoint = ((1 - _charAnimationDuration) / (sLength - 1)) * cPosition;
        float endPoint = startPoint + _charAnimationDuration;

        float subT = t.Map(startPoint, endPoint, 0, 1);

        string sizeStart = $"<size={_sizeCurve.Evaluate(subT) * _sizeScale}%>";
        string sizeEnd = $"</size>";
        string vOffsetStart = $"<voffset={_heightCurve.Evaluate(subT) * _heightScale}px>";
        string vOffsetEnd = $"</voffset>";
        string rotateStart = $"<rotate={_rotationCurve.Evaluate(subT) * _rotationScale}%>";
        string rotateEnd = $"</rotate>";

        string colourStart = $"<color=#{ColorUtility.ToHtmlStringRGBA(_colour.Evaluate(subT))}>";
        string colourEnd = $"</color>";
        
        return sizeStart + vOffsetStart + rotateStart + colourStart + c + colourEnd + rotateEnd + vOffsetEnd + sizeEnd;
    }
}

public static class Extensions
{
    public static float Map(this float value, float fromLow, float fromHigh, float toLow, float toHigh)
    {
        return (value - fromLow) * (toHigh - toLow) / (fromHigh - fromLow) + toLow;
    }
}
