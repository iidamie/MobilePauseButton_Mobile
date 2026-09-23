using StArray.ModManager.Hooks;
using StArray.ModManager.Manager;

namespace MobilePauseButton.Mobile;

/// <summary>
/// Restores the outside-game menu button on the PC port's active
/// scnLevelSelect scene. Other optional scene entry points have independent
/// hook groups so one missing target cannot disable this path.
/// </summary>
internal static partial class MenuHooks
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
            {
                UninstallHooks();
                _game = null;
            }
            return installed;
        }
        catch (Exception exception)
        {
            try { UninstallHooks(); } catch { }
            _game = null;
            Logger.Debug(MobilePauseButtonPlugin.LogTag,
                $"Outside-game menu hook installation skipped: {exception.Message}");
            return false;
        }
    }

    internal static void Uninstall()
    {
        try
        {
            UninstallHooks();
        }
        catch (Exception exception)
        {
            Logger.Debug(MobilePauseButtonPlugin.LogTag,
                $"Outside-game menu hook removal failed: {exception.Message}");
        }
        finally
        {
            _game = null;
        }
    }

    /// <summary>
    /// The current port reaches this concrete Start method in scnLevelSelect.
    /// This is the reliable point after the level-select UI and controller have
    /// both been created.
    /// </summary>
    [UnmanagedHook("Assembly-CSharp.dll", "scnLevelSelect", "Start", ParameterCount = 0)]
    private static void LevelSelectStart(nint instance, nint methodInfo)
    {
        LevelSelectStartOriginal(instance, methodInfo);

        try
        {
            bool shown = _game?.ShowPauseButtonForOutsideGame() == true;
            Logger.Debug(MobilePauseButtonPlugin.LogTag,
                shown
                    ? "Outside-game menu button enabled (scnLevelSelect.Start)"
                    : "Outside-game menu button SetActive failed (scnLevelSelect.Start)");
        }
        catch (Exception exception)
        {
            Logger.Warn(MobilePauseButtonPlugin.LogTag,
                $"Could not enable scnLevelSelect menu button: {exception.Message}");
        }
    }

}
