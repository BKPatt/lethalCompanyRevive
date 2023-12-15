using UnityEngine;

namespace lethalCompanyRevive.Helpers
{

    public static partial class Helper
    {
        public static Camera? CurrentCamera =>
            Helper.LocalPlayer?.gameplayCamera != null && Helper.LocalPlayer.gameplayCamera.enabled
                ? LocalPlayer.gameplayCamera
                : Helper.StartOfRound?.spectateCamera;
    }
}
