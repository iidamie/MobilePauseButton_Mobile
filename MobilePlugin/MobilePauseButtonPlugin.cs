using StArray.ModManager.Manager;
using StArray.ModManager.Runtime;

[assembly: ModEntryPoint(typeof(MobilePauseButton.Mobile.MobilePauseButtonPlugin))]

namespace MobilePauseButton.Mobile;

/// <summary>
/// Restores the mobile gameplay pause button in the PC-assembly Android port.
/// </summary>
public sealed class MobilePauseButtonPlugin : IModPlugin
{
    internal const string LogTag = "MobilePauseButton";

    private GameApi? _game;

    public string Id => "MobilePauseButton";
    public string Name => "Mobile Pause & Menu Buttons";
    public string Version => "1.6.0";
    public string Author => "ADOFAI mobile mod";
    public string Description => "Show ADOFAI's mobile pause/menu button on the Android PC port";
    public IReadOnlyList<string> Dependencies => Array.Empty<string>();

    public void OnLoad()
    {
        GameApi? game = GameApi.Create();
        if (game == null)
            throw new InvalidOperationException(
                "ADOFAI runtime bindings for the mobile pause button could not be resolved");

        _game = game;
        if (!GameHooks.Install(game))
        {
            _game = null;
            throw new InvalidOperationException("The scrController.Awake hook could not be installed");
        }

        // The mobile menu reuses scrUIController.pauseButton as its outside-game
        // menu button. This hook is optional because PC-shaped builds may omit
        // the MobileMenu scene entirely.
        bool outsideMenuHookInstalled = MenuHooks.Install(game);
        bool pauseMenuButtonHooksInstalled = PauseMenuButtonHooks.Install(game);
        bool mobileMenuHookInstalled = MobileMenuIntroHooks.Install(game);
        bool levelSelectAwakeHookInstalled = LevelSelectAwakeHooks.Install(game);
        bool customLevelSelectHookInstalled = CustomLevelSelectHooks.Install(game);

        Logger.Info(LogTag, game.DescribeBindings()
                         + $"; outsideMenuHooks={outsideMenuHookInstalled}"
                         + $"; pauseMenuButtonHooks={pauseMenuButtonHooksInstalled}"
                         + $"; mobileMenuHook={mobileMenuHookInstalled}"
                         + $"; levelSelectAwakeHook={levelSelectAwakeHookInstalled}"
                         + $"; customLevelSelectHook={customLevelSelectHookInstalled}");
        Logger.Info(LogTag, "Loaded");
    }

    public void OnUnload()
    {
        CustomLevelSelectHooks.Uninstall();
        LevelSelectAwakeHooks.Uninstall();
        MobileMenuIntroHooks.Uninstall();
        PauseMenuButtonHooks.Uninstall();
        MenuHooks.Uninstall();
        GameHooks.Uninstall();
        _game = null;
        Logger.Info(LogTag, "Unloaded");
    }
}
