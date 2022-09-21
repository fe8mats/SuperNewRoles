using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using SuperNewRoles.Buttons;
using SuperNewRoles.Roles;

namespace SuperNewRoles.Patch
{
    class HudManagerPatch
    {
        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        public static class HudManagerUpdatePatch
        {
            public static void Prefix(HudManager __instance)
            {
                GameSettingsScale.GameSettingsScalePatch(__instance);
            }

            public static void Postfix(HudManager __instance)
            {
                WallHack.WallHackUpdate();
                if (AmongUsClient.Instance.GameState != AmongUsClient.GameStates.Started) return;
                Freezer.HudUpdate();
                Mode.Zombie.FixedUpdate.ZombieTimerUpdate(__instance);
                CustomButton.HudUpdate();
                ButtonTime.Update();
                Tuna.HudUpdate();
                Arsonist.HudUpdate();
                Shielder.HudUpdate();
                Speeder.HudUpdate();
                Zoom.HudUpdate(__instance);
            }
        }
    }
}
