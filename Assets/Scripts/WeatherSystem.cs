using UnityEngine;

/// <summary>
/// Tipos de clima posibles en la simulación.
/// </summary>
public enum WeatherType
{
    Clear,  // Despejado: visión normal
    Rain,   // Lluvia: visión reducida
    Storm   // Tormenta: visión muy reducida
}

/// <summary>
/// FEATURE: Eventos climáticos.
/// Cambia el clima de la simulación cada cierto tiempo (lluvia o tormenta)
/// y reduce la visión de conejos y depredadores mientras dura.
///
/// Cómo funciona:
///  1. Cada clima dura un tiempo aleatorio entre minDuration y maxDuration.
///  2. Al terminar, se elige el siguiente clima al azar según su probabilidad.
///  3. Cada clima tiene un multiplicador de visión (1 = normal, 0.3 = 30%).
///     Bunny y Predator multiplican su visionRange por WeatherSystem.VisionMultiplier.
///  4. El fondo de la cámara se oscurece suavemente según el clima y en la
///     esquina superior izquierda se muestra el clima actual.
///
/// El objeto se crea automáticamente al iniciar la escena (no hay que
/// agregarlo a mano), así no se modifica la escena base del proyecto.
/// </summary>
public class WeatherSystem : MonoBehaviour
{
    // Instancia única para que Bunny y Predator puedan consultar el clima
    public static WeatherSystem Instance { get; private set; }

    /// <summary>
    /// Multiplicador de visión actual. Si no existe el sistema de clima,
    /// devuelve 1 (visión normal), así el resto del código nunca falla.
    /// </summary>
    public static float VisionMultiplier
    {
        get { return Instance != null ? Instance.GetCurrentMultiplier() : 1f; }
    }

    [Header("Estado actual")]
    public WeatherType currentWeather = WeatherType.Clear;
    public float timeRemaining;

    [Header("Duración de cada clima (segundos)")]
    public float minDuration = 8f;
    public float maxDuration = 15f;

    [Header("Probabilidad de cada clima (se normalizan)")]
    public float clearChance = 0.5f;
    public float rainChance = 0.35f;
    public float stormChance = 0.15f;

    [Header("Multiplicador de visión por clima")]
    [Range(0f, 1f)] public float clearVision = 1f;
    [Range(0f, 1f)] public float rainVision = 0.6f;
    [Range(0f, 1f)] public float stormVision = 0.3f;

    [Header("Visual")]
    public Color rainColor = new Color(0.25f, 0.30f, 0.40f);
    public Color stormColor = new Color(0.08f, 0.08f, 0.14f);
    public float colorTransitionSpeed = 1.5f;

    private Camera cam;
    private Color clearColor; // color original del fondo de la cámara

    /// <summary>
    /// Se ejecuta automáticamente después de cargar la escena.
    /// Si no hay un WeatherSystem en la escena, lo crea.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (FindFirstObjectByType<WeatherSystem>() == null)
        {
            new GameObject("WeatherSystem").AddComponent<WeatherSystem>();
        }
    }

    private void Awake()
    {
        // Si ya existe otro, este se destruye (solo debe haber uno)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        cam = Camera.main;
        if (cam != null) clearColor = cam.backgroundColor;

        // La simulación empieza despejada
        SetWeather(WeatherType.Clear);
    }

    private void Update()
    {
        // Cuenta regresiva del clima actual
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            SetWeather(PickRandomWeather());
        }

        // Transición suave del color del fondo hacia el color del clima
        if (cam != null)
        {
            cam.backgroundColor = Color.Lerp(
                cam.backgroundColor,
                GetTargetColor(),
                colorTransitionSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// Cambia al clima indicado y le asigna una duración aleatoria.
    /// </summary>
    void SetWeather(WeatherType weather)
    {
        currentWeather = weather;
        timeRemaining = Random.Range(minDuration, maxDuration);
        Debug.Log($"[Clima] Ahora: {weather} durante {timeRemaining:F1} s (visión x{GetCurrentMultiplier()})");
    }

    /// <summary>
    /// Elige un clima al azar usando las probabilidades configuradas.
    /// </summary>
    WeatherType PickRandomWeather()
    {
        float total = clearChance + rainChance + stormChance;
        float roll = Random.Range(0f, total);

        if (roll < clearChance) return WeatherType.Clear;
        if (roll < clearChance + rainChance) return WeatherType.Rain;
        return WeatherType.Storm;
    }

    float GetCurrentMultiplier()
    {
        switch (currentWeather)
        {
            case WeatherType.Rain: return rainVision;
            case WeatherType.Storm: return stormVision;
            default: return clearVision;
        }
    }

    Color GetTargetColor()
    {
        switch (currentWeather)
        {
            case WeatherType.Rain: return rainColor;
            case WeatherType.Storm: return stormColor;
            default: return clearColor;
        }
    }

    /// <summary>
    /// Muestra el clima actual en la esquina superior izquierda del Game.
    /// </summary>
    private void OnGUI()
    {
        string name = currentWeather == WeatherType.Clear ? "Despejado"
                    : currentWeather == WeatherType.Rain ? "Lluvia"
                    : "Tormenta";

        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 20 };
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(10, 10, 400, 30),
            $"Clima: {name}  |  Visión x{GetCurrentMultiplier():0.0}  |  {timeRemaining:0}s",
            style);
    }
}