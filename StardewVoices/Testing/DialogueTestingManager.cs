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

        /// <param name="listUnusedAudio">List every unused audio file in the recap, instead of just a count.</param>
        public static void RunValidation(bool listUnusedAudio = false)
        {
            Managers.TestingManager.ValidationIsRunning = true;

            ValidationManager validation = new ValidationManager(Helper, Managers, listUnusedAudio);
            validation.RunValidation();

        }

    }
}
