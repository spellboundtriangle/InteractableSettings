using BoneLib;
using BoneLib.BoneMenu;
using MelonLoader;
using Il2CppSLZ.Interaction;
using Harmony;
using UnityEngine;
using Il2CppSLZ.Marrow;
using System.Runtime;
using System.Runtime.CompilerServices;
using Il2CppSLZ.Marrow.Interaction;


[assembly: MelonInfo(typeof(InteractableSettings.InteractableSettingsMod), "Interactable Settings", "1.0.0", "triangle", "https://thunderstore.io/c/bonelab/p/spellboundtriangle/InteractableSettings")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace InteractableSettings
{
    public class InteractableSettingsMod : MelonMod
    {
        public static int HandLayerMask { get; set; }

        // Preferences variables
        public static MelonPreferences_Category InteractableSettings_Category;
        public static MelonPreferences_Entry<bool> InteractableIcon_HandHover_Enabled;
        public static MelonPreferences_Entry<bool> InteractableIcon_FarHandHover_Enabled;
        public static MelonPreferences_Entry<bool> ForcePullGrip_Enabled;
        public static MelonPreferences_Entry<bool> ForcePullGrip_Collision_Enabled;
        public static MelonPreferences_Entry<bool> ForcePullGrip_ForcePullAnything_Enabled;
        public static MelonPreferences_Entry<bool> ForcePullGrip_ForcePullThroughObjects_Enabled;
        public static MelonPreferences_Entry<float> ForcePullGrip_MaxForce;
        public static MelonPreferences_Entry<bool> ForcePullGrip_AprilFooled;

        public override void OnInitializeMelon()
        {
            // Init MelonLoader preferences
            
            InteractableSettings_Category = MelonPreferences.CreateCategory("Interactable Settings");
            ForcePullGrip_AprilFooled = InteractableSettings_Category.CreateEntry("April Fools", false);

            // Init BoneMenu elements
            Page BoneMenu_MainPage = Page.Root.CreatePage("Interactable Settings", Color.green);
            {
                Page BoneMenu_InteractableIconPage = BoneMenu_MainPage.CreatePage("Interactable Icon", Color.white);
                {
                    BoneMenu_InteractableIconPage.CreateBoolPref("Near Hand Icons", Color.white, ref InteractableIcon_HandHover_Enabled, prefDefaultValue: true);
                    BoneMenu_InteractableIconPage.CreateBoolPref("Force Pull Icons", Color.white, ref InteractableIcon_FarHandHover_Enabled, prefDefaultValue: true);
                }
                Page BoneMenu_ForcePullGripPage = BoneMenu_MainPage.CreatePage("Force Pull Grip", Color.cyan);
                {
                    BoneMenu_ForcePullGripPage.CreateBoolPref("Force Pull Grip", Color.cyan, ref ForcePullGrip_Enabled, prefDefaultValue: true);
                    BoneMenu_ForcePullGripPage.CreateBoolPref("Force Pull Collision", Color.cyan, ref ForcePullGrip_Collision_Enabled, prefDefaultValue: true);
                    BoneMenu_ForcePullGripPage.CreateBoolPref("Force Pull Anything", Color.cyan, ref ForcePullGrip_ForcePullAnything_Enabled, prefDefaultValue: false);
                    BoneMenu_ForcePullGripPage.CreateBoolPref("Force Pull Through Objects", Color.cyan, ref ForcePullGrip_ForcePullThroughObjects_Enabled, OnEnableForcePullThroughObjects, prefDefaultValue: false);
                    FloatElement FPGMaxForce = BoneMenu_ForcePullGripPage.CreateFloatPref("Force Pull Max Force", Color.cyan, ref ForcePullGrip_MaxForce, 50f, -500000f, 500000f, prefDefaultValue: 250f);
                        FPGMaxForce.SetTooltip("Default 250");
                    if (!ForcePullGrip_AprilFooled.Value && DateTime.Today.Month == 4 && DateTime.Today.Day == 1)
                    {
                        ForcePullGrip_MaxForce.Value = -750f;
                        FPGMaxForce.Value = ForcePullGrip_MaxForce.Value;
                        ForcePullGrip_AprilFooled.Value = true;
                        SavePreferences();
                    }
                }

            }
            BoneLib.Hooking.OnLevelLoaded += LevelLoaded;

            LoggerInstance.Msg("InteractableSettings Initialized.");
        }

        // Set layermask for pulling through objects
        private void LevelLoaded(LevelInfo info)
        {
            HandLayerMask = Player.LeftHand.playerLayerMask;
            if (ForcePullGrip_ForcePullThroughObjects_Enabled.Value)
            {
                Player.LeftHand.playerLayerMask = int.MaxValue;
                Player.RightHand.playerLayerMask = int.MaxValue;
            }
        }

        public static void SavePreferences() 
        {
            InteractableSettings_Category.SaveToFile(false);
        }

        // BoneMenu methods
        public static void OnEnableForcePullThroughObjects(bool value)
        {
            if (value)
            {
                Player.LeftHand.playerLayerMask = int.MaxValue;
                Player.RightHand.playerLayerMask = int.MaxValue;
            }
            else
            {
                Player.LeftHand.playerLayerMask = HandLayerMask;
                Player.RightHand.playerLayerMask = HandLayerMask;
            }
        }
    }


    [HarmonyLib.HarmonyPatch]
    public static class Patches
    {
        private const float ForcePullGripIdentifier = -1.789f;

        // Interactable Icon Hand Hover disable
        [HarmonyLib.HarmonyPatch(typeof(InteractableIcon), "MyHandHoverBegin")]
        [HarmonyLib.HarmonyPrefix]
        public static bool InteractableIcon_HandHoverIconDisable()
        {
            return InteractableSettingsMod.InteractableIcon_HandHover_Enabled.Value;
        }

        // Interactable Icon Far Hand Hover disable
        [HarmonyLib.HarmonyPatch(typeof(InteractableIcon), "MyFarHandHoverBegin")]
        [HarmonyLib.HarmonyPrefix]
        public static bool InteractableIcon_FarHandHoverIconDisable()
        {
            return InteractableSettingsMod.InteractableIcon_FarHandHover_Enabled.Value;
        }

        // Force Pull Grip disable
        [HarmonyLib.HarmonyPatch(typeof(ForcePullGrip), "OnFarHandHoverUpdate")]
        [HarmonyLib.HarmonyPrefix]
        public static bool ForcePullGrip_OnFarHandHoverUpdateDisable(ForcePullGrip __instance)
        {
            if (InteractableSettingsMod.ForcePullGrip_Enabled.Value)
            {
                if (!InteractableSettingsMod.ForcePullGrip_ForcePullAnything_Enabled.Value)
                {
                    if (__instance.maxSpeed == ForcePullGripIdentifier)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // Force Pull Grip collision disable + Force Pull Max Force override
        [HarmonyLib.HarmonyPatch(typeof(Grip), "add_forcePullCompleteDelegate")]
        [HarmonyLib.HarmonyPostfix]
        public static void ForcePullGrip_OnPullCollisionDisable(Grip __instance)
        {
            if (!InteractableSettingsMod.ForcePullGrip_Collision_Enabled.Value)
            {
                MarrowEntity entity = __instance._marrowEntity;
                entity.EnableColliders(false);
            }
            ForcePullGrip fpg = __instance.GetComponent<ForcePullGrip>();
            {
                fpg.maxForce = InteractableSettingsMod.ForcePullGrip_MaxForce.Value;
            }
        }

        // Force Pull Grip collision re-enable
        [HarmonyLib.HarmonyPatch(typeof(Grip), "remove_forcePullCompleteDelegate")]
        [HarmonyLib.HarmonyPostfix]
        public static void ForcePullGrip_OnAttachCollisionEnable(Grip __instance)
        {
            if (!InteractableSettingsMod.ForcePullGrip_Collision_Enabled.Value)
            {
                MarrowEntity entity = __instance._marrowEntity;
                entity.EnableColliders(true);
            }
        }

        // Force Pull Grip everything
        [HarmonyLib.HarmonyPatch(typeof(Grip), "Start")]
        [HarmonyLib.HarmonyPostfix]
        public static void ForcePullGrip_AddForcePullGrip(Grip __instance)
        {
            if (__instance.GetComponent<ForcePullGrip>() == null)
            {
                ForcePullGrip fpg = __instance.gameObject.AddComponent<ForcePullGrip>();
                fpg.maxSpeed = ForcePullGripIdentifier;
            }
        }
    }
}