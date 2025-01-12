﻿using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Characters;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;

namespace JunimoDialog
{
    [HarmonyPatch(typeof(JunimoHarvester))]
    [HarmonyPatch("tryToHarvestHere")]
    public class PatchTryToHarvestHere
    {
        public static void Postfix(JunimoHarvester __instance, ref int ___harvestTimer)
        {
            string dialog = Dialog.GetDialog(___harvestTimer);
            if (dialog != null) __instance.showTextAboveHead(dialog);
        }
    }

    [HarmonyPatch(typeof(NPC))]
    [HarmonyPatch("drawAboveAlwaysFrontLayer")]
    public class NPCDrawAboveAlwaysFrontLayer
    {
        public static bool Prefix(NPC __instance, ref SpriteBatch b, int ___textAboveHeadTimer, string ___textAboveHead,
            int ___textAboveHeadStyle, float ___textAboveHeadAlpha, int ___textAboveHeadColor)
        {
            if (__instance is not JunimoHarvester) return true;
            // JunimoDialog.SMonitor.Log($"drawAboveAlwaysFrontLayer: {junimo} is JunimoHarvester", LogLevel.Debug);
            if (___textAboveHeadTimer > 0)
            {
                if (!__instance.modData.ContainsKey("ceruleandeep.junimodialog.lang"))
                {
                    double roll = JunimoDialog.jdRandom.NextDouble();
                    __instance.modData["ceruleandeep.junimodialog.lang"] =
                        roll < JunimoDialog.Config.JunimoTextChance ? "junimo" : "latin";
                }

                Vector2 local = Game1.GlobalToLocal(new Vector2(__instance.getStandingX(),
                    __instance.getStandingY() - __instance.Sprite.SpriteHeight * 4 - 64 + __instance.yJumpOffset));
                if (___textAboveHeadStyle == 0)
                {
                    local += new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
                }

                if (__instance.shouldShadowBeOffset)
                {
                    local += __instance.drawOffset.Value;
                }

                bool junimoText = __instance.modData["ceruleandeep.junimodialog.lang"] == "junimo";
                SpriteText.drawStringWithScrollCenteredAt(b, ___textAboveHead, (int) local.X, (int) local.Y, "",
                    ___textAboveHeadAlpha, ___textAboveHeadColor, 1,
                    __instance.getTileY() * 64 / 10000f + 0.001f + __instance.getTileX() / 10000f,
                    junimoText);
            }
            else
            {
                __instance.modData.Remove("ceruleandeep.junimodialog.lang");
            }

            return false;

        }
    }
}