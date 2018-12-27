using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Smod2.API;

namespace PlayerPreferences.Tests
{
    [TestClass]
    public class IO
    {
        private const string Path = "PlayerPrefs";

        private readonly List<string> errors;
        private readonly Action<string> log;

        public IO()
        {
            errors = new List<string>();
            log = x => errors.Add(x);
        }

        private void CheckForErrors()
        {
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join("\n", errors));
            }
        }

        [TestMethod]
        [ExpectedException(typeof(SuccessException))]
        public void Init()
        {
            new Preferences(Path, log);
            CheckForErrors();

            throw new SuccessException();
        }

        [TestMethod]
        [ExpectedException(typeof(SuccessException))]
        public void Write()
        {
            Preferences prefs = new Preferences(Path, log);
            CheckForErrors();
            try
            {
                prefs.Add("123456789", new Role[1]);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new SuccessException();
            }

            throw new Exception();
        }

        [TestMethod]
        [ExpectedException(typeof(SuccessException))]
        public void Read()
        {
            Preferences prefs = new Preferences(Path, log);
            CheckForErrors();
            File.WriteAllText($"{Path}/123456789.txt", string.Join(",", Plugin.Roles.Select(x => (int)x.Value).Reverse()));
            prefs.Read();
            CheckForErrors();

            throw new SuccessException();
        }

        [TestMethod]
        [ExpectedException(typeof(SuccessException))]
        public void ModifyPreferenceCollection()
        {
            Preferences prefs = new Preferences(Path, log);
            CheckForErrors();
            prefs.Add("1234567890", Plugin.Roles.Select(x => x.Value).ToArray());
            CheckForErrors();
            prefs.Remove("1234567890");
            CheckForErrors();

            throw new SuccessException();
        }
    }
}
