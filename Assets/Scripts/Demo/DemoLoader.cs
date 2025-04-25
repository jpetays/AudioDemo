using System.Collections;
using System.IO;
using System.Text;
using Demo.Audio;
using Demo.UnityDemo;
using Microsoft.Extensions.Logging;
using Prg;
using Prg.Util;
using Prg.Window;
using UnityEngine;
using ZLogger;
using ZLogger.Unity;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Demo;

public static class MyLoggerFactory
{
    private static ILoggerFactory? _loggerFactory;

    public static ILogger CreateLogger<T>()
    {
        _loggerFactory ??= LoggerFactory.Create(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Trace);
            // log to UnityDebug
            logging.AddZLoggerUnityDebug();
            // Log to file.
            logging.AddZLoggerFile(Path.Combine(Application.persistentDataPath, LogFilename()));
        });
        return _loggerFactory.CreateLogger<T>();
    }

    private static string LogFilename()
    {
        var prefix = AppPlatform.IsEditor
            ? "editor"
            : Application.platform.ToString().ToLower().Replace("player", string.Empty);
        var baseName = Application.productName.Replace(" ", "_").ToLower();
        return $"{prefix}_{baseName}_z.log";
    }
}

internal class DemoLoader : MonoBehaviour
{
    private static readonly ILogger Logger = MyLoggerFactory.CreateLogger<DemoLoader>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void SubsystemRegistration()
    {
        // Manual reset if UNITY Domain Reloading is disabled.
        Logger.ZLogTrace($"SubsystemRegistration");
        LogConfig.Create();
        Logger.ZLogInformation($"Starting {nameof(DemoLoader)}");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BeforeSceneLoad()
    {
        Logger.ZLogTrace($"BeforeSceneLoad");
        var parent = new GameObject(nameof(DemoLoader));
        var loader = parent.AddComponent<DemoLoader>();
        // Start async services ASAP.
        loader.StartCoroutine(LoadServicesAsync());
        WindowLoader.ValidateWindowLoaderTag("WindowLoader");
    }

    private void Awake()
    {
        var startupMessage = new StringBuilder()
            .Append(" Game ").Append(Application.productName)
            .Append(" Ver ").Append(Application.version)
            .Append(" Plat ").Append(AppPlatform.IsSimulator ? "Simulator" : AppPlatform.Name)
            .Append(" Screen ").Append(AppPlatform.ScreeInfo())
            .ToString();
        Logger.ZLogTrace($"{startupMessage}");
    }

    private void Start()
    {
        Logger.ZLogTrace($"Start() {RichText.Yellow($"frame #{Time.frameCount}")} start");
        // UNITY Audio Mixer needs to be started in Start()!
        AudioConfig.Initialize();
        Logger.ZLogTrace($"Start() {RichText.Yellow($"frame #{Time.frameCount}")} done");
    }

    private static IEnumerator LoadServicesAsync()
    {
        Logger.ZLogTrace($"LoadServicesAsync {RichText.Yellow($"frame #{Time.frameCount}")} start");
        yield return null;
        if (AppPlatform.IsMobile)
        {
            MobileAudio.Initialize();
        }
        Logger.ZLogTrace($"LoadServicesAsync {RichText.Yellow($"frame #{Time.frameCount}")} done");
    }
}