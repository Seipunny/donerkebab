using UnityEngine;

/// <summary>
/// Оптимизирует настройки игры для максимальной производительности.
/// Устанавливает Target FPS и VSync.
/// </summary>
public class GameOptimizer : MonoBehaviour
{
    [Header("FPS Settings")]
    [Tooltip("Целевой FPS (0 = unlimited, -1 = default)")]
    [SerializeField] private int targetFrameRate = 180;

    [Tooltip("Включить VSync (0 = off, 1 = every VBlank, 2 = every second VBlank)")]
    [Range(0, 2)]
    [SerializeField] private int vSyncCount = 0;

    [Header("Quality Settings")]
    [Tooltip("Применить оптимальные настройки качества")]
    [SerializeField] private bool applyQualityOptimizations = true;

    [Tooltip("Уровень качества (0-5, или -1 для текущего)")]
    [Range(-1, 5)]
    [SerializeField] private int qualityLevel = -1;

    [Header("Performance Settings")]
    [Tooltip("Отключить тени для лучшей производительности")]
    [SerializeField] private bool disableShadows = false;

    [Tooltip("Установить Anti-Aliasing (0 = off, 2, 4, 8)")]
    [SerializeField] private AntiAliasingMode antiAliasing = AntiAliasingMode.Off;

    [Tooltip("Pixel Light Count (количество источников света в реальном времени)")]
    [Range(0, 8)]
    [SerializeField] private int pixelLightCount = 4;

    [Header("Debug")]
    [Tooltip("Показывать FPS на экране")]
    [SerializeField] private bool showFPS = true;

    [Tooltip("Позиция FPS счетчика")]
    [SerializeField] private Vector2 fpsPosition = new Vector2(10, 10);

    public enum AntiAliasingMode
    {
        Off = 0,
        MSAA_2x = 2,
        MSAA_4x = 4,
        MSAA_8x = 8
    }

    // FPS Counter
    private float deltaTime = 0.0f;
    private GUIStyle fpsStyle;

    private void Awake()
    {
        ApplySettings();
    }

    private void Start()
    {
        InitializeFPSCounter();
    }

    private void ApplySettings()
    {
        // Устанавливаем Target FPS
        Application.targetFrameRate = targetFrameRate;
        Debug.Log($"[GameOptimizer] Target FPS set to: {(targetFrameRate <= 0 ? "Unlimited" : targetFrameRate.ToString())}");

        // Устанавливаем VSync
        QualitySettings.vSyncCount = vSyncCount;
        Debug.Log($"[GameOptimizer] VSync set to: {vSyncCount} ({GetVSyncDescription(vSyncCount)})");

        // Применяем настройки качества
        if (applyQualityOptimizations)
        {
            ApplyQualitySettings();
        }
    }

    private void ApplyQualitySettings()
    {
        // Устанавливаем уровень качества
        if (qualityLevel >= 0)
        {
            QualitySettings.SetQualityLevel(qualityLevel, true);
            Debug.Log($"[GameOptimizer] Quality Level set to: {qualityLevel} ({QualitySettings.names[qualityLevel]})");
        }

        // Настройки теней
        if (disableShadows)
        {
            QualitySettings.shadows = ShadowQuality.Disable;
            Debug.Log("[GameOptimizer] Shadows disabled");
        }

        // Anti-Aliasing
        QualitySettings.antiAliasing = (int)antiAliasing;
        Debug.Log($"[GameOptimizer] Anti-Aliasing set to: {antiAliasing}");

        // Pixel Light Count
        QualitySettings.pixelLightCount = pixelLightCount;
        Debug.Log($"[GameOptimizer] Pixel Light Count set to: {pixelLightCount}");
    }

    private void InitializeFPSCounter()
    {
        if (!showFPS) return;

        fpsStyle = new GUIStyle
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 200,
            normal = { textColor = Color.white }
        };
    }

    private void Update()
    {
        if (showFPS)
        {
            UpdateFPSCounter();
        }
    }

    private void UpdateFPSCounter()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    private void OnGUI()
    {
        if (!showFPS || fpsStyle == null) return;

        float fps = 1.0f / deltaTime;
        string text = $"FPS: {Mathf.Ceil(fps)}";

        // Цвет в зависимости от FPS
        if (fps >= 60)
            fpsStyle.normal.textColor = Color.green;
        else if (fps >= 30)
            fpsStyle.normal.textColor = Color.yellow;
        else
            fpsStyle.normal.textColor = Color.red;

        Rect rect = new Rect(fpsPosition.x, fpsPosition.y, 2000, 300);
        GUI.Label(rect, text, fpsStyle);

        // Дополнительная информация
        if (targetFrameRate > 0)
        {
            rect.y += 250;
            fpsStyle.normal.textColor = Color.white;
            fpsStyle.fontSize = 140;
            GUI.Label(rect, $"Target: {targetFrameRate} FPS", fpsStyle);
            fpsStyle.fontSize = 200;
        }
    }

    private string GetVSyncDescription(int vsync)
    {
        switch (vsync)
        {
            case 0: return "Disabled";
            case 1: return "Every VBlank";
            case 2: return "Every Second VBlank";
            default: return "Unknown";
        }
    }

    #region Public API

    /// <summary>
    /// Установить целевой FPS
    /// </summary>
    public void SetTargetFPS(int fps)
    {
        targetFrameRate = fps;
        Application.targetFrameRate = fps;
        Debug.Log($"[GameOptimizer] Target FPS changed to: {fps}");
    }

    /// <summary>
    /// Включить/выключить VSync
    /// </summary>
    public void SetVSync(bool enabled)
    {
        vSyncCount = enabled ? 1 : 0;
        QualitySettings.vSyncCount = vSyncCount;
        Debug.Log($"[GameOptimizer] VSync {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Установить VSync count
    /// </summary>
    public void SetVSyncCount(int count)
    {
        vSyncCount = Mathf.Clamp(count, 0, 2);
        QualitySettings.vSyncCount = vSyncCount;
        Debug.Log($"[GameOptimizer] VSync count set to: {vSyncCount}");
    }

    /// <summary>
    /// Включить/выключить отображение FPS
    /// </summary>
    public void SetShowFPS(bool show)
    {
        showFPS = show;
    }

    /// <summary>
    /// Получить текущий FPS
    /// </summary>
    public float GetCurrentFPS()
    {
        return 1.0f / deltaTime;
    }

    /// <summary>
    /// Применить preset настроек
    /// </summary>
    public void ApplyPreset(PerformancePreset preset)
    {
        switch (preset)
        {
            case PerformancePreset.UltraPerformance:
                SetTargetFPS(300);
                SetVSync(false);
                qualityLevel = 0; // Very Low
                disableShadows = true;
                antiAliasing = AntiAliasingMode.Off;
                pixelLightCount = 1;
                ApplySettings();
                break;

            case PerformancePreset.HighPerformance:
                SetTargetFPS(180);
                SetVSync(false);
                qualityLevel = 1; // Low
                disableShadows = false;
                antiAliasing = AntiAliasingMode.Off;
                pixelLightCount = 2;
                ApplySettings();
                break;

            case PerformancePreset.Balanced:
                SetTargetFPS(144);
                SetVSync(false);
                qualityLevel = 2; // Medium
                disableShadows = false;
                antiAliasing = AntiAliasingMode.MSAA_2x;
                pixelLightCount = 4;
                ApplySettings();
                break;

            case PerformancePreset.Quality:
                SetTargetFPS(60);
                SetVSync(true);
                qualityLevel = 4; // High
                disableShadows = false;
                antiAliasing = AntiAliasingMode.MSAA_4x;
                pixelLightCount = 6;
                ApplySettings();
                break;

            case PerformancePreset.UltraQuality:
                SetTargetFPS(60);
                SetVSync(true);
                qualityLevel = 5; // Ultra
                disableShadows = false;
                antiAliasing = AntiAliasingMode.MSAA_8x;
                pixelLightCount = 8;
                ApplySettings();
                break;
        }

        Debug.Log($"[GameOptimizer] Applied preset: {preset}");
    }

    #endregion

    public enum PerformancePreset
    {
        UltraPerformance,   // 300 FPS, минимальное качество
        HighPerformance,    // 180 FPS, низкое качество
        Balanced,           // 144 FPS, среднее качество
        Quality,            // 60 FPS, высокое качество
        UltraQuality        // 60 FPS, ультра качество
    }
}
