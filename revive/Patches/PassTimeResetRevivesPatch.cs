using HarmonyLib;
using lethalCompanyRevive.Helpers;
using lethalCompanyRevive.Managers;

namespace lethalCompanyRevive.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    [HarmonyPatch("PassTimeToNextDay")]
    public static class PassTimeResetRevivesPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (ReviveStore.Instance != null)
            {
                ReviveStore.Instance.ResetDailyRevives();
                Helper.PrintSystem("[ReviveStore] Daily revives reset at the start of the new day/round.");
            }
        }
    }
}
