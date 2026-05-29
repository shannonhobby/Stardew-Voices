using StardewModdingAPI;
using StardewVoices.Testing.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewVoices.FileGenerator
{
    internal static class FileGenerator
    {
        private static IModHelper Helper;
        private static ManagerHub Managers;

        public static void Init(IModHelper helper, ManagerHub managers)
        {
            Helper = helper;
            Managers = managers;
        }

        public static void GenerateFiles(string filesToGenerate)
        {
            Managers.TestingManager.ValidationIsRunning = true;
        }
    }
}
