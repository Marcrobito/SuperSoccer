using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameConfig
{
    public QuestionSettings questionSettings = new QuestionSettings();
    public LogoSettings logoSettings = new LogoSettings();
}

[Serializable]
public class QuestionSettings
{
    [Min(1)]
    public int questionsPerSession = 3;
    public List<string> bannerPaths = new List<string>();
}

[Serializable]
public class LogoSettings
{
    public LogoSlotConfig topLeft = new LogoSlotConfig();
    public LogoSlotConfig topRight = new LogoSlotConfig();
    public LogoSlotConfig bottomLeft = new LogoSlotConfig();
    public LogoSlotConfig bottomRight = new LogoSlotConfig();

    [Range(0.25f, 4f)]
    public float logoScale = 1f;
}

[Serializable]
public class LogoSlotConfig
{
    public bool enabled = false;
    public string relativePath = string.Empty;
}

public enum LogoPlacement
{
    Hidden,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}
