using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace TLDJam5
{
    [HarmonyPatch]
    public class TLDJam5Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TranslatorWord), MethodType.Constructor, new Type[] { typeof(string), typeof(int), typeof(int), typeof(bool), typeof(float) })]
        public static void TranslatorWordReplacePrefix(ref string translatedText)
        {
            //translatedText = translatedText.Replace("orange", "green");
            if (translatedText.Contains("<"))
            {
                string newValue;
                if (TLDJam5.Instance.shrinkingPlanetControler != null)
                {
                    float secondsOfShrink = TLDJam5.Instance.shrinkingPlanetControler.endTime;
                    newValue = string.Concat(Mathf.Floor(secondsOfShrink/60f));
                    translatedText = translatedText.Replace("<TLDJam5_MinutesToFullShrink>", newValue);
                    newValue = string.Concat(Mathf.Floor(secondsOfShrink % 60f));
                    translatedText = translatedText.Replace("<TLDJam5_SecondsToFullShrink>", newValue);
                    newValue = string.Concat(Mathf.Floor(TLDJam5.Instance.shrinkingPlanetControler.curentScale*100f+0.5f));
                    translatedText = translatedText.Replace("<TLDJam5_ShrinkPercent>", newValue);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.IsGroundedOnRisingSand))]
        public static bool PlayerOnRisingSandPrefix(ref bool __result, ref PlayerCharacterController __instance)
        {
            if (TLDJam5.Instance.isPlayerAroundShrinkingPlanet() && !PlayerState.IsInsideShip() && !PlayerState.IsInsideShuttle()) { 
                __result = __instance.IsGrounded();
                return false;
            }
            return true;
        }
    }
}
