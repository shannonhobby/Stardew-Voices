using StardewModdingAPI;
using StardewVoices.Testing.Models;
using StardewVoices.Testing.Scanners;
using StardewVoices.Testing.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
