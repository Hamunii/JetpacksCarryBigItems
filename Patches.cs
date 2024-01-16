using GameNetcodeStuff;
using HarmonyLib;
using System;
using UnityEngine;

namespace JetpacksCarryBigItems {
    class Patches {
        static private int twoHandedItemIdx = 0;
        static private bool playerHasTwoHandedItemInInventoryButIsCurrentlyOnJetpackItem = false;

        [HarmonyPatch(typeof(PlayerControllerB), "ScrollMouse_performed")]
        [HarmonyPostfix]
        static void PlayerControllerB_ScrollMouse_performed_Postfix(ref PlayerControllerB __instance) {
            // We are missing the check for time between scrolls but I'll do that later
            if (!__instance.inTerminalMenu && ((__instance.IsOwner && __instance.isPlayerControlled && (!__instance.IsServer || __instance.isHostPlayerObject)) || __instance.isTestingPlayer) && !__instance.isGrabbingObjectAnimation && !__instance.quickMenuManager.isMenuOpen && !__instance.inSpecialInteractAnimation && !__instance.throwingObject && !__instance.isTypingChat && !__instance.activatingItem && !__instance.jetpackControls && !__instance.disablingJetpackControls){
                if (playerHasTwoHandedItemInInventoryButIsCurrentlyOnJetpackItem){
                    playerHasTwoHandedItemInInventoryButIsCurrentlyOnJetpackItem = false;
                    SwitchItemSlots(twoHandedItemIdx, ref __instance);
                }
                else if(__instance.currentlyHeldObjectServer != null && __instance.currentlyHeldObjectServer.itemProperties.twoHanded){
                    twoHandedItemIdx = __instance.currentItemSlot;
                    int idx = 0;
                    foreach (var item_ in __instance.ItemSlots){
                        if(item_ is JetpackItem){
                            playerHasTwoHandedItemInInventoryButIsCurrentlyOnJetpackItem = true;
                            SwitchItemSlots(idx, ref __instance);
                            return;
                        }
                        idx++;
                    }
                }
            }
            return;
        }

        // This function is mostly copied from the ItemQuickSwitch mod, with minimal tweaks. https://github.com/vasanex/ItemQuickSwitchMod
        static void SwitchItemSlots(int destination, ref PlayerControllerB __instance){
            var distance = __instance.currentItemSlot - destination;
            var requestedSlotIsLowerThanCurrent = distance > 0;

            if (Math.Abs(distance) == __instance.ItemSlots.Length - 1)
            {
                // we can just skip one slot forwards/backwards here and save RPC calls.
                __instance.SwitchItemSlotsServerRpc(requestedSlotIsLowerThanCurrent);
            }
            else
            {
                while (distance != 0)
                {
                    __instance.SwitchItemSlotsServerRpc(!requestedSlotIsLowerThanCurrent);
                    distance += requestedSlotIsLowerThanCurrent ? -1 : 1;
                }
            }

            ShipBuildModeManager.Instance.CancelBuildMode();
            __instance.playerBodyAnimator.SetBool("GrabValidated", false);

            __instance.SwitchToItemSlot(destination);

            __instance.currentlyHeldObjectServer.gameObject.GetComponent<AudioSource>()
                .PlayOneShot(__instance.currentlyHeldObjectServer.itemProperties.grabSFX, 0.6f);
            return;
        }

        // TODO: Make it so that dropping the jetpack will switch to the two handed item automatically. Should be very easy.
    }
}