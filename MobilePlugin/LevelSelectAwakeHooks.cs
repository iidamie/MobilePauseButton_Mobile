using StArray.ModManager.Hooks;
using StArray.ModManager.Manager;

namespace MobilePauseButton.Mobile;

/// <summary>Fallback refresh for level-select subclasses using the base Awake.</summary>
internal static partial class LevelSelectAwakeHooks
{
    private static GameApi? _game;

    internal static bool Install(GameApi game)
    {
        Uninstall();
        _game = game;
        try
        {
            bool installed = InstallHooks();
            if (!installed)
                Uninstall();
            return installed;
        }
        catch (Exception exception)
        {
            Uninstall();
            Logger.Debug(MobilePauseButtonPlugin.LogTag,
                $"LevelSelectBase button hook unavailable: {exception.Message}");
            return false;
        }
    }

    internal static void Uninstall()
    {
        try { UninstallHooks(); }
        catch (Exception exception)
        {
            Logger.Debug(MobilePauseButtonPlugin.LogTag,
                $"LevelSelectBase button hook removal failed: {exception.Message}");
        }
        finally { _game = null; }
    }

    [UnmanagedHook("Assembly-CSharp.dll", "LevelSelectBase", "Awake", ParameterCount = 0)]
    private static void LevelSelectAwake(nint instance, nint methodInfo)
    {
        LevelSelectAwakeOriginal(instance, methodInfo);
        try
        {
            if (_game?.ShowPauseButtonForMenu() == true)
                Logger.Debug(MobilePauseButtonPlugin.LogTag,
                    "Outside-game menu button enabled (LevelSelectBase)");
        }
        catch (Exception exception)
        {
            Logger.Debug(MobilePauseButtonPlugin.LogTag,
                $"Could not enable LevelSelectBase button: {exception.Message}");
        }
    }
}
