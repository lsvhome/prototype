﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net.Fex.Api;
using Net.Fex.Api.Tests;

namespace FexSync.Data.Tests
{
    [TestClass]
    public class TestsConfiguration
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            System.Configuration.Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);

            if (!config.AppSettings.Settings.AllKeys.Contains("net.fex.api.trace"))
            {
                config.AppSettings.Settings.Add("net.fex.api.trace", "false");
            }
            else
            {
                config.AppSettings.Settings["net.fex.api.trace"].Value = "false";
            }

            config.Save(System.Configuration.ConfigurationSaveMode.Modified);
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            System.Configuration.Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);
            config.AppSettings.Settings.Remove("net.fex.api.trace");
            config.Save(System.Configuration.ConfigurationSaveMode.Modified);
        }
    }
}
