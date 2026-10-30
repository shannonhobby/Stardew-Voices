using StardewModdingAPI;
using System.Collections.Generic;
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
        /// <param name="characters">The characters to validate, or null for every supported character.</param>
        public static void RunValidation(bool listUnusedAudio = false, List<string> characters = null)
        {
            Managers.TestingManager.ValidationIsRunning = true;

            ValidationManager validation = new ValidationManager(Helper, Managers, listUnusedAudio, characters);
            validation.RunValidation();

        }

    }
}
