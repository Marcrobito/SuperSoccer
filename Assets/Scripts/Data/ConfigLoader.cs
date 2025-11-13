using System;
using System.IO;
using UnityEngine;

public class ConfigLoader : MonoBehaviour
{
    [SerializeField] private string relativeFolder = "Config";
    [SerializeField] private string fileName = "config.json";
    [SerializeField] private GameConfig defaultConfig = new GameConfig();

    private GameConfig cachedConfig;

    public GameConfig CurrentConfig
    {
        get
        {
            if (cachedConfig == null)
            {
                cachedConfig = LoadFromDisk() ?? CloneConfig(defaultConfig);
            }

            EnsureValidValues(cachedConfig);
            return cachedConfig;
        }
    }

    public void Reload()
    {
        cachedConfig = null;
    }

    private GameConfig LoadFromDisk()
    {
        try
        {
            string folderPath = GetConfigDirectory();
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogError("No se pudo determinar la ruta base del ejecutable para ConfigLoader.");
                return null;
            }

            string configPath = Path.Combine(folderPath, fileName);

            if (!File.Exists(configPath))
            {
                Debug.LogWarning($"No se encontró config.json en {configPath}. Usando valores por defecto.");
                return null;
            }

            string jsonContent = File.ReadAllText(configPath);
            GameConfig parsed = JsonUtility.FromJson<GameConfig>(jsonContent);

            if (parsed == null)
            {
                Debug.LogWarning("El archivo de configuración no se pudo parsear. Usando valores por defecto.");
                return null;
            }

            return parsed;
        }
        catch (Exception ex)
        {
            Debug.LogError($"ConfigLoader encontró un error: {ex.Message}");
            return null;
        }
    }

    public string GetConfigDirectory()
    {
        string basePath = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(basePath))
        {
            return null;
        }

        return Path.Combine(basePath, relativeFolder);
    }

    public string ResolveAssetPath(string relativeAssetPath)
    {
        if (string.IsNullOrWhiteSpace(relativeAssetPath))
        {
            return null;
        }

        if (Path.IsPathRooted(relativeAssetPath))
        {
            return relativeAssetPath;
        }

        string folder = GetConfigDirectory();
        if (string.IsNullOrEmpty(folder))
        {
            return null;
        }

        return Path.Combine(folder, relativeAssetPath);
    }

    private void EnsureValidValues(GameConfig config)
    {
        if (config == null)
        {
            return;
        }

        if (config.questionSettings == null)
        {
            config.questionSettings = new QuestionSettings();
        }

        if (config.questionSettings.questionsPerSession < 1)
        {
            config.questionSettings.questionsPerSession = 1;
        }

        if (config.questionSettings.bannerPaths == null)
        {
            config.questionSettings.bannerPaths = new System.Collections.Generic.List<string>();
        }

        if (config.logoSettings == null)
        {
            config.logoSettings = new LogoSettings();
        }

        EnsureSlot(ref config.logoSettings.topLeft);
        EnsureSlot(ref config.logoSettings.topRight);
        EnsureSlot(ref config.logoSettings.bottomLeft);
        EnsureSlot(ref config.logoSettings.bottomRight);

        config.logoSettings.logoScale = Mathf.Clamp(config.logoSettings.logoScale, 0.25f, 4f);
    }

    private GameConfig CloneConfig(GameConfig source)
    {
        if (source == null)
        {
            return new GameConfig();
        }

        string json = JsonUtility.ToJson(source);
        return JsonUtility.FromJson<GameConfig>(json);
    }

    private void EnsureSlot(ref LogoSlotConfig slot)
    {
        if (slot == null)
        {
            slot = new LogoSlotConfig();
        }
    }
}
