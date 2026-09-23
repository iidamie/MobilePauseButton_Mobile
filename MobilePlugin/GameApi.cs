using StArray.ModManager.Manager;
using StArray.ModManager.RuntimeAbstractions;

namespace MobilePauseButton.Mobile;

/// <summary>
/// Small runtime-only facade. The Mod is compiled without UnityEngine or
/// Assembly-CSharp references because the game runs as IL2CPP on Android.
/// </summary>
internal sealed unsafe class GameApi
{
    private const int AndroidPlatform = 4;

    private readonly IAppDomain _domain;
    private readonly IRuntimeAssembly _gameAssembly;
    private readonly IRuntimeField? _platformField;
    private readonly IRuntimeField? _controllerGameWorld;
    private readonly IRuntimeField? _controllerPaused;
    private readonly IRuntimeMethod? _getControllerInstance;
    private readonly IRuntimeMethod? _getUiController;
    private readonly IRuntimeField? _pauseButtonField;
    private readonly IRuntimeField? _pauseMenuBackButtonField;
    private readonly IRuntimeMethod? _getGameObject;
    private readonly IRuntimeMethod? _setActive;

    private GameApi(IAppDomain domain, IRuntimeAssembly gameAssembly)
    {
        _domain = domain;
        _gameAssembly = gameAssembly;

        IRuntimeClass? adoBase = gameAssembly.GetClass("", "ADOBase");
        IRuntimeClass? controller = gameAssembly.GetClass("", "scrController");
        IRuntimeClass? uiController = gameAssembly.GetClass("", "scrUIController");

        _platformField = adoBase?.GetField("platform");
        _controllerGameWorld = controller?.GetField("gameworld");
        _controllerPaused = controller?.GetField("_paused");
        _getControllerInstance = controller?.GetMethod("get_instance", 0);
        _getUiController = uiController?.GetMethod("get_instance", 0);
        _pauseButtonField = uiController?.GetField("pauseButton");

        IRuntimeClass? pauseMenu = gameAssembly.GetClass("", "PauseMenu");
        _pauseMenuBackButtonField = pauseMenu?.GetField("backButton");

        IRuntimeClass? component = FindClassInDomain("UnityEngine", "Component");
        IRuntimeClass? gameObject = FindClassInDomain("UnityEngine", "GameObject");
        _getGameObject = component?.GetMethod("get_gameObject", 0);
        _setActive = gameObject?.GetMethod("SetActive", 1);
    }

    internal static GameApi? Create()
    {
        if (!RuntimeManager.IsAvailable)
            RuntimeManager.Detect();

        IAppDomain? domain = RuntimeManager.GetDomain();
        if (domain == null)
            return null;

        IRuntimeAssembly? assembly = domain.OpenAssembly("Assembly-CSharp.dll")
                                    ?? domain.OpenAssembly("Assembly-CSharp");
        if (assembly == null)
            return null;

        try
        {
            GameApi api = new(domain, assembly);
            return api.IsSupported ? api : null;
        }
        catch (Exception exception)
        {
            Logger.Warn(MobilePauseButtonPlugin.LogTag,
                $"Runtime binding construction failed: {exception.Message}");
            return null;
        }
    }

    private bool IsSupported => _platformField != null
                                && _controllerGameWorld != null
                                && _getUiController != null
                                && _pauseButtonField != null
                                && _getGameObject != null
                                && _setActive != null;

    internal string DescribeBindings()
    {
        return $"runtime={RuntimeManager.Backend}; "
               + $"platform={Present(_platformField)}, "
               + $"controller.gameworld={Present(_controllerGameWorld)}, "
               + $"controller.paused={Present(_controllerPaused)}, "
               + $"controller.instance={Present(_getControllerInstance)}, "
               + $"ui.instance={Present(_getUiController)}, "
               + $"ui.pauseButton={Present(_pauseButtonField)}, "
               + $"pauseMenu.backButton={Present(_pauseMenuBackButtonField)}, "
               + $"component.gameObject={Present(_getGameObject)}, "
               + $"gameObject.SetActive={Present(_setActive)}";
    }

    /// <summary>
    /// Makes only the original Awake call observe Android. This is enough for the
    /// original code to execute its mobile branch and create its own UnityAction.
    /// </summary>
    internal bool TryEnterMobilePlatform(out int previousPlatform)
    {
        previousPlatform = 0;
        IRuntimeField? field = _platformField;
        if (field == null || !field.IsStatic)
            return false;

        try
        {
            previousPlatform = field.GetValue<int>(0);
            field.SetValue(0, AndroidPlatform);
            return true;
        }
        catch (Exception exception)
        {
            Logger.Warn(MobilePauseButtonPlugin.LogTag,
                $"Could not temporarily set ADOBase.platform: {exception.Message}");
            return false;
        }
    }

    internal void RestorePlatform(int platform)
    {
        try
        {
            _platformField?.SetValue(0, platform);
        }
        catch (Exception exception)
        {
            Logger.Warn(MobilePauseButtonPlugin.LogTag,
                $"Could not restore ADOBase.platform: {exception.Message}");
        }
    }

    internal bool ShowPauseButtonForGameWorld(nint controller)
    {
        if (controller == 0 || _controllerGameWorld == null || !Read(_controllerGameWorld, controller, false))
            return false;

        return SetPauseButtonActive();
    }

    /// <summary>
    /// The mobile main menu uses the same pauseButton object as an outside-game
    /// menu button. Mirror scnMobileMenu.ShowIntroScreen's !controller.paused
    /// guard before restoring its visibility.
    /// </summary>
    internal bool ShowPauseButtonForMenu()
    {
        nint controller = InvokeStaticObject(_getControllerInstance);
        if (controller == 0 || (_controllerPaused != null && Read(_controllerPaused, controller, true)))
            return false;

        return SetPauseButtonActive();
    }

    /// <summary>
    /// The concrete PC level-select scenes are already outside gameplay. Do not
    /// gate this path on the controller's transient pause flag: only restore the
    /// serialized button object after that scene has finished initializing.
    /// </summary>
    internal bool ShowPauseButtonForOutsideGame()
    {
        return SetPauseButtonActive();
    }

    /// <summary>
    /// PauseMenu.Awake hides this button on the desktop build. The explicit
    /// refresh is needed when PauseMenu was created before this Mod loaded.
    /// </summary>
    internal bool ShowPauseMenuBackButton(nint pauseMenu)
    {
        if (pauseMenu == 0)
            return false;

        nint backButton = ReadObject(_pauseMenuBackButtonField, pauseMenu);
        nint gameObject = InvokeObject(_getGameObject, backButton);
        return SetActive(gameObject);
    }

    private bool SetPauseButtonActive()
    {
        nint uiController = InvokeStaticObject(_getUiController);
        nint pauseButton = ReadObject(_pauseButtonField, uiController);
        nint gameObject = InvokeObject(_getGameObject, pauseButton);
        return SetActive(gameObject);
    }

    private bool SetActive(nint gameObject)
    {
        if (gameObject == 0 || _setActive == null)
            return false;

        byte active = 1;
        try
        {
            _setActive.Invoke(gameObject, [(nint)(&active)]);
            return true;
        }
        catch (Exception exception)
        {
            Logger.Warn(MobilePauseButtonPlugin.LogTag,
                $"GameObject.SetActive failed: {exception.Message}");
            return false;
        }
    }

    private IRuntimeClass? FindClassInDomain(string namespaze, string name)
    {
        foreach (IRuntimeAssembly assembly in _domain.GetAssemblies())
        {
            try
            {
                IRuntimeClass? type = assembly.GetClass(namespaze, name);
                if (type != null)
                    return type;
            }
            catch
            {
                // A malformed optional assembly must not hide the Unity module.
            }
        }

        return null;
    }

    private static nint ReadObject(IRuntimeField? field, nint instance)
    {
        if (field == null || instance == 0)
            return 0;

        try
        {
            // IL2CPP reference fields are pointer-sized in the runtime field API.
            return field.GetValue<nint>(instance);
        }
        catch
        {
            return 0;
        }
    }

    private static T Read<T>(IRuntimeField? field, nint instance, T fallback) where T : unmanaged
    {
        if (field == null || instance == 0)
            return fallback;

        try
        {
            return field.GetValue<T>(instance);
        }
        catch
        {
            return fallback;
        }
    }

    private static nint InvokeStaticObject(IRuntimeMethod? method)
    {
        if (method == null)
            return 0;

        try
        {
            return method.InvokeStatic();
        }
        catch
        {
            return 0;
        }
    }

    private static nint InvokeObject(IRuntimeMethod? method, nint instance)
    {
        if (method == null || instance == 0)
            return 0;

        try
        {
            return method.Invoke(instance);
        }
        catch
        {
            return 0;
        }
    }

    private static string Present(IRuntimeField? field) => field == null ? "missing" : "ok";
    private static string Present(IRuntimeMethod? method) => method == null ? "missing" : "ok";
}
