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
        public static int PlayerHandLayerMask { get; set; }
        public static int FarHoverHandLayerMask { get; set; }
        public enum ForcePullMode
        {
            OFF,
            ON,
            ANYTHING,
            PER_ENTITY,
        }

        // Preferences variables
        public static MelonPreferences_Category InteractableSettings_Category;
        public static MelonPreferences_Entry<bool> InteractableIcon_HandHover_Enabled;
        public static MelonPreferences_Entry<bool> InteractableIcon_FarHandHover_Enabled;
        public static MelonPreferences_Entry<ForcePullMode> ForcePullGrip_ForcePullMode;
        public static MelonPreferences_Entry<bool> ForcePullGrip_Collision_Enabled;
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
                    BoneMenu_ForcePullGripPage.CreateEnumPref("Force Pull", Color.cyan, ref ForcePullGrip_ForcePullMode, OnChangeForcePullMode, prefDefaultValue:ForcePullMode.ON);
                    BoneMenu_ForcePullGripPage.CreateBoolPref("Force Pull Collision", Color.cyan, ref ForcePullGrip_Collision_Enabled, prefDefaultValue: true);
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

            LoggerInstance.Msg(" Initialized.");
        }

        // Set layermasks for pulling through objects
        private void LevelLoaded(LevelInfo info)
        {
            PlayerHandLayerMask = Player.LeftHand.playerLayerMask;
            if (ForcePullGrip_ForcePullThroughObjects_Enabled.Value)
            {
                Player.LeftHand.playerLayerMask = int.MaxValue;
                Player.RightHand.playerLayerMask = int.MaxValue;
            }
            FarHoverHandLayerMask = Player.LeftHand.farHoverLayerMask;
            if (ForcePullGrip_ForcePullMode.Value == ForcePullMode.ANYTHING || ForcePullGrip_ForcePullMode.Value == ForcePullMode.PER_ENTITY)
            {
                Player.LeftHand.farHoverLayerMask = int.MaxValue;
                Player.RightHand.farHoverLayerMask = int.MaxValue;
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
                Player.LeftHand.playerLayerMask = PlayerHandLayerMask;
                Player.RightHand.playerLayerMask = PlayerHandLayerMask;
            }
        }

        public static void OnChangeForcePullMode(Enum value)
        {
            if ((ForcePullMode)value == ForcePullMode.ANYTHING || (ForcePullMode)value == ForcePullMode.PER_ENTITY)
            {
                Player.LeftHand.farHoverLayerMask = int.MaxValue;
                Player.RightHand.farHoverLayerMask = int.MaxValue;
            }
            else
            {
                Player.LeftHand.farHoverLayerMask = FarHoverHandLayerMask;
                Player.RightHand.farHoverLayerMask = FarHoverHandLayerMask;
            }
        }
    }


    [HarmonyLib.HarmonyPatch]
    public static class Patches
    {
        private const float ForcePullGripIdentifier = -57f;
        private const float ExtraForcePullGripIdentifier = -58f;

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
            switch (InteractableSettingsMod.ForcePullGrip_ForcePullMode.Value)
            {
                case InteractableSettingsMod.ForcePullMode.OFF:        // Never force pull
                    return false;

                case InteractableSettingsMod.ForcePullMode.ON:         // Only force pulls if not using either identifier float
                    if (__instance.maxSpeed != ForcePullGripIdentifier && __instance.maxSpeed != ExtraForcePullGripIdentifier)
                    {
                        return true;
                    }
                    else return false;

                case InteractableSettingsMod.ForcePullMode.ANYTHING:   // Always allow force pull
                    return true;

                case InteractableSettingsMod.ForcePullMode.PER_ENTITY: // Disallow force pull on identified "extra" force pulls
                    if (__instance.maxSpeed == ExtraForcePullGripIdentifier)
                    {
                        return false;
                    }
                    else return true;

                default:
                    return true;
            }
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
            if (!__instance.GetComponent<ForcePullGrip>())
            { // Execute if there is no Force Pull Grip on the grip
                if (!__instance.transform.root.gameObject.GetComponentInChildren<ForcePullGrip>())
                { // Execute if there are no Force Pull Grips on the entire entity
                    ForcePullGrip fpg = __instance.gameObject.AddComponent<ForcePullGrip>();
                    fpg.maxSpeed = ForcePullGripIdentifier;
                }
                else
                { // Execute if there is at least one Force Pull Grip already
                    ForcePullGrip fpg = __instance.gameObject.AddComponent<ForcePullGrip>();
                    fpg.maxSpeed = ExtraForcePullGripIdentifier;
                }
            }
            
        }
    }
}