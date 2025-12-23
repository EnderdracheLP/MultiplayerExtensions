using HarmonyLib;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using System;
using System.Linq;
using System.Windows.Forms;
using static PlayerSaveData;

namespace MultiplayerExtensions.Patchers
{
    [HarmonyPatch]
    public class MenuEnvironmentPatcher : IAffinity
    {
        private readonly GameplaySetupViewController _gameplaySetup;
        private readonly EnvironmentsListModel _environmentsListModel;
		private readonly Config _config;
        private readonly SiraLog _logger;

        internal MenuEnvironmentPatcher(
            GameplaySetupViewController gameplaySetup,
            EnvironmentsListModel environmentsListModel,
            Config config,
            SiraLog logger)
        {
            _gameplaySetup = gameplaySetup;
            _environmentsListModel = environmentsListModel;
            _config = config;
            _logger = logger;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameplaySetupViewController), nameof(GameplaySetupViewController.Setup))]
        private static void EnableEnvironmentTab(bool showModifiers, ref bool showEnvironmentOverrideSettings, bool showColorSchemesSettings, bool showMultiplayer, PlayerSettingsPanelController.PlayerSettingsPanelLayout playerSettingsPanelLayout)
        {
            if (showMultiplayer)
                showEnvironmentOverrideSettings = Plugin.Config.SoloEnvironment;
        }

        private EnvironmentInfoSO _originalEnvironmentInfo = null!;

        // TODO: Somehow the original environment is not preloaded, so this only works after having played once on the original environment
        [AffinityPrefix]
        [AffinityPatch(typeof(MultiplayerLevelScenesTransitionSetupDataSO), "Init")]
        private bool SetEnvironmentScene(ref MultiplayerLevelScenesTransitionSetupDataSO __instance, string gameMode, in BeatmapKey beatmapKey, BeatmapLevel beatmapLevel, IBeatmapLevelData beatmapLevelData, ColorScheme overrideColorScheme, GameplayModifiers gameplayModifiers, PlayerSpecificSettings playerSpecificSettings, EnvironmentsListModel environmentsListModel, PracticeSettings practiceSettings, AudioClipAsyncLoader audioClipAsyncLoader, SettingsManager settingsManager, BeatmapDataLoader beatmapDataLoader, GameplayAdditionalInformation gameplayAdditionalInformation, ref EnvironmentInfoSO ____environmentInfo)
        {
            if (!_config.SoloEnvironment)
                return true;
            EnvironmentName envName =
	            beatmapLevel.GetEnvironmentName(beatmapKey.beatmapCharacteristic, beatmapKey.difficulty);
			_originalEnvironmentInfo = environmentsListModel.GetEnvironmentInfoBySerializedName("MultiplayerEnvironment")!; // Save original env info in a temp variable
			____environmentInfo = _environmentsListModel.GetEnvironmentInfoBySerializedNameSafe(envName);
            if (_gameplaySetup.environmentOverrideSettings.overrideEnvironments)
				____environmentInfo = _gameplaySetup.environmentOverrideSettings.GetOverrideEnvironmentInfoForType(____environmentInfo.environmentType);
        
            _logger.Debug($"Setting environment to {____environmentInfo.name} for solo environment in multiplayer");

			__instance.usingOverrideColorScheme = overrideColorScheme != null;
			__instance.colorScheme = overrideColorScheme ?? new ColorScheme(__instance._environmentInfo.colorScheme);
			__instance.gameMode = __instance.gameMode;
			__instance.beatmapKey = beatmapKey;
			__instance.beatmapLevel = beatmapLevel;
			__instance.beatmapLevelData = __instance.beatmapLevelData;
			if (__instance.beatmapLevelData != null)
			{
				__instance.gameplayCoreSceneSetupData = new GameplayCoreSceneSetupData(beatmapKey, beatmapLevel, gameplayModifiers, playerSpecificSettings, practiceSettings, ____environmentInfo, _originalEnvironmentInfo, __instance.colorScheme, settingsManager, audioClipAsyncLoader, beatmapDataLoader, null, true, null, false, null, beatmapLevelData, null);
			}
			else
			{
				__instance.gameplayCoreSceneSetupData = new GameplayCoreSceneSetupData(beatmapKey, beatmapLevel, gameplayModifiers, playerSpecificSettings, practiceSettings, ____environmentInfo, _originalEnvironmentInfo, __instance.colorScheme, settingsManager, audioClipAsyncLoader, beatmapDataLoader, null, true, null, true, null, null, null);
			}
			__instance.gameplayAdditionalInformationSetupData = new GameplayAdditionalInformationSetupData(gameplayAdditionalInformation);
			__instance.InitAndSetupScenes();
			return false;
		}

        [AffinityPostfix]
        [AffinityPatch(typeof(MultiplayerLevelScenesTransitionSetupDataSO), "Init")]
        private void ResetEnvironmentScene(ref EnvironmentInfoSO ____environmentInfo)
        {
            if (_config.SoloEnvironment)
				____environmentInfo = _originalEnvironmentInfo;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(ScenesTransitionSetupDataSO), "Init", AffinityMethodType.Normal, 
            new AffinityArgumentType[] { AffinityArgumentType.Normal, AffinityArgumentType.Normal },
            new Type[] { typeof(string[]), typeof(SceneSetupData[])})]
        private void AddEnvironmentOverrides(ScenesTransitionSetupDataSO __instance, ref string[] newScenes)
        {
			if (_config.SoloEnvironment && newScenes.Any(scene => scene.Contains("Multiplayer")) && __instance is MultiplayerLevelScenesTransitionSetupDataSO mpSceneSetupData)
            {
                _logger.Debug($"At least one scenes name contains Multiplayer, adding original env info");
                // Ensures the original environment info comes before GameCore as some mods rely on GameCore being present on the scene switch callback
                newScenes = newScenes.Take(1)
                    .Concat(new[] { _originalEnvironmentInfo.environmentSceneName })
                    .Concat(newScenes.Skip(1))
                    .ToArray();
            }

			foreach (var scene in newScenes)
			{
				_logger.Trace($"Scene Init: {scene}");
			}
		}
    }
}
