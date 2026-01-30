using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LobbyCompatibility.Attributes;
using LobbyCompatibility.Enums;
using UnityEngine;

namespace NoMineshaft;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("BMX.LobbyCompatibility", BepInDependency.DependencyFlags.HardDependency)]
[LobbyCompatibility(CompatibilityLevel.Everyone, VersionStrictness.None)]
public class NoMineshaft : BaseUnityPlugin
{
    public static NoMineshaft Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        Patch();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();

        Logger.LogDebug("Finished patching!");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }

    private static void PluginLogInfoWithPrefix(string content)
    {
        Logger.LogInfo($"[EVIL] {content}");
    }

    [HarmonyPatch(typeof(RoundManager))]
    internal class InteriorPatch
    {
        static readonly string myMinesaftMoonObjName = "Level3Flow";
        static readonly string myFactoryMoonObjName = "Level1Flow";

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void RemoveMineshaftInterior(RoundManager __instance)
        {
            if (__instance.dungeonFlowTypes.Length < 2)
            {
                return;
            } 

            PluginLogInfoWithPrefix("Attempting to disable Mineshaft by setting Rarity to 0");
            int mineshaftId = FindMineshaftDungFlowId(__instance.dungeonFlowTypes);
            if (mineshaftId == -1)
            {
                return;
            }

            //Skip Vanilla fix if LLL is present and try to patch that instead
            bool lllPresent = LethalLevelLoaderFix();
            if (lllPresent)
            {
                return;
            }

            foreach (var level in StartOfRound.Instance.levels)
            {
                if (level.dungeonFlowTypes is null) 
                {
                    continue;
                }

                for (int i = 0; i < level.dungeonFlowTypes.Length; i++)
                {
                    if (level.dungeonFlowTypes[i].id == mineshaftId)
                    {
                        int oldRarity = level.dungeonFlowTypes[i].rarity;
                        level.dungeonFlowTypes[i].rarity = 0;
                        PluginLogInfoWithPrefix($"{level.PlanetName}: Rarity set to 0 (was {oldRarity})");
                    }
                }
            }
        }

        static int FindMineshaftDungFlowId(IndoorMapType[] availableInteriors)
        {
            int i = 0;
            int mineshaftInteriorId = -1;
            foreach (var intr in availableInteriors)
            {
                if (intr.dungeonFlow.ToString().Contains(myMinesaftMoonObjName))
                {
                    mineshaftInteriorId = i;
                    break;

                }
                ++i;
            }
            return mineshaftInteriorId;
        }

        static bool LethalLevelLoaderFix()
        {
            Type extFlowType = Type.GetType("LethalLevelLoader.ExtendedDungeonFlow, LethalLevelLoader");
            if (extFlowType is null)
            {
                return false;
            }

            PluginLogInfoWithPrefix("LethalLevelLoader detected. Patching Dungeons...");
            var allExtendedFlows = Resources.FindObjectsOfTypeAll(extFlowType);

            foreach (var extFlow in allExtendedFlows)
            {
                PropertyInfo dFlowProp = extFlowType.GetProperty("DungeonFlow", BindingFlags.Public | BindingFlags.Instance);
                object actualDungeonFlow = dFlowProp?.GetValue(extFlow);

                if (actualDungeonFlow is UnityEngine.Object dFlowAsset && dFlowAsset.name.Contains(myMinesaftMoonObjName))
                {
                    PluginLogInfoWithPrefix($"Found Mineshaft Extended Object: {extFlow.name}");
                    PropertyInfo matchPropsProp = extFlowType.GetProperty("LevelMatchingProperties", BindingFlags.Public | BindingFlags.Instance);
                    object matchPropsObj = matchPropsProp?.GetValue(extFlow);

                    if (matchPropsObj is null)
                    {
                        continue;
                    }

                    FieldInfo planetNamesField = matchPropsObj.GetType().GetField("planetNames", BindingFlags.Public | BindingFlags.Instance);
                    IList planetList = planetNamesField?.GetValue(matchPropsObj) as IList;
                    if (planetList != null)
                    {
                        int countBefore = planetList.Count;
                        planetList.Clear();
                        PluginLogInfoWithPrefix($"Wiped Mineshaft whitelist. Removed {countBefore} moons.");
                    }
                    else
                    {
                        PluginLogInfoWithPrefix("Could not access planetNames list.");
                    }
                }
            }

            return true;
        }
    }
}
