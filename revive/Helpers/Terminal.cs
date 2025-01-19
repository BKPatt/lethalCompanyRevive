using lethalCompanyRevive.Misc;

namespace lethalCompanyRevive.Helpers
{
    public static partial class Helper
    {
        public static Terminal? Terminal =>
            HUDManager is null
                ? null
                : Reflector.Target(HUDManager).GetInternalField<Terminal>("terminalScript");
    }
}
