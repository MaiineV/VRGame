using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace VRGame.EditorTools
{
    /// <summary>
    /// One-shot setup that stamps the game's identity so a sideloaded build shows up
    /// as "Pour Decisions" (instead of the default "My project") under
    /// "Unknown Sources" on the Meta Quest.
    /// Menu:  Tools > Setup > Game Identity
    /// </summary>
    public static class GameIdentity
    {
        [MenuItem("Tools/Setup/Game Identity")]
        public static void Apply()
        {
            // Product/company name. productName is what the Quest uses as the app label.
            PlayerSettings.productName = "Pour Decisions";
            PlayerSettings.companyName = "Pour Decisions";

            // Android bundle id: valid lowercase reverse-DNS, no spaces.
            // NOTE: changing the bundle id makes the device treat this as a brand-new
            // app, so any previously installed build is NOT upgraded in place -- it
            // installs side-by-side as a fresh install (separate save data, etc.).
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.pourdecisions.game");

            AssetDatabase.SaveAssets();

            Debug.Log(
                "[GameIdentity] Identity applied:\n" +
                $"  productName             = {PlayerSettings.productName}\n" +
                $"  companyName             = {PlayerSettings.companyName}\n" +
                $"  Android applicationId   = {PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)}");
        }
    }
}
