using StardewModdingAPI;
using StardewVoices.StardewVoices.Testing;

namespace StardewVoices.Testing
{
    internal static class DialogueTestingManager
    {
        private static IModHelper Helper;
        private static ManagerHub Managers;

        public static void Init(IModHelper helper, ManagerHub managers)
        {
            Helper = helper;
            Managers = managers;
        }

        public static void RunValidation()
        {
            Managers.TestingManager.ValidationIsRunning = true;

            ValidationManager validation = new ValidationManager(Helper, Managers);
            validation.RunValidation();

        }

    }
}
