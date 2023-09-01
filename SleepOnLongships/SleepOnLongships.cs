using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SleepOnLongships
{
    public class PluginInfo
    {
        public const string Name = "Sleep on Longships";
        public const string Guid = "beardedkwan.SleepOnLongships";
        public const string Version = "1.0.0";
    }

    public class SleepOnLongshipsConfig
    {
        public static ConfigEntry<string> KeyUse { get; set; }
    }

    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    [BepInProcess("valheim.exe")]
    public class SleepOnLongships : BaseUnityPlugin
    {
        void Awake()
        {
            // Initialize config
            SleepOnLongshipsConfig.KeyUse = Config.Bind("General", "KeyUse", "F", "Shift + [KeyUse] when focused on a stool on the longship (or the bench on the karve) will initiate sleep.\nMake this key the same as your 'use' key in your game settings.");

            Harmony harmony = new Harmony(PluginInfo.Guid);
            harmony.PatchAll();
        }

        // Hover text patch
        [HarmonyPatch(typeof(Chair), "GetHoverText")]
        public static class GetHoverTextPatch
        {
            private static void Postfix(Chair __instance, ref String __result)
            {
                if (__instance.m_inShip && Traverse.Create(__instance).Method("InUseDistance", new object[] { Player.m_localPlayer }).GetValue<bool>())
                {
                    __result = __result + $"\n[<color=yellow><b>Shift + {SleepOnLongshipsConfig.KeyUse.Value}</b></color>] Sleep";
                }
            }
        }

        // Sleep on stool patch
        [HarmonyPatch(typeof(Chair), "Interact")]
        public static class SleepOnStoolPatch
        {
            private static bool Prefix(Chair __instance, ref bool __result, Humanoid human)
            {
                if (__instance.m_inShip)
                {
                    // left shift + [KeyUse]
                    string keyUse = SleepOnLongshipsConfig.KeyUse.Value;
                    if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode), keyUse)))
                    {
                        Player player = human as Player;
                        if (!Traverse.Create(__instance).Method("InUseDistance", new object[] { player }).GetValue<bool>())
                        {
                            return true; // return true = go to the default method
                        }

                        Player closestPlayer = Player.GetClosestPlayer(__instance.m_attachPoint.position, 0.1f);
                        if (closestPlayer != null && closestPlayer != Player.m_localPlayer)
                        {
                            return true;
                        }

                        player.AttachStart(__instance.m_attachPoint, null, hideWeapons: false, isBed: true, __instance.m_inShip, __instance.m_attachAnimation, __instance.m_detachOffset);
                        return false;
                    }

                    return true;
                }

                return true;
            }
        }

        // Make ships immune to water damage
        [HarmonyPatch(typeof(Ship), "Awake")]
        public static class NoShipWaterDamage
        {
            private static void Prefix(ref Ship __instance)
            {
                __instance.m_waterImpactDamage = 0f;
            }
        }
    }
}
