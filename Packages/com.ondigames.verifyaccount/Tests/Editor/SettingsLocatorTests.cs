using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace OnDi.VerifyAccount.Tests
{
    public class SettingsLocatorTests
    {
        readonly List<VerifyAccountSettings> _created = new List<VerifyAccountSettings>();

        VerifyAccountSettings Make(string name)
        {
            var settings = ScriptableObject.CreateInstance<VerifyAccountSettings>();
            settings.name = name;
            _created.Add(settings);
            return settings;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var settings in _created) Object.DestroyImmediate(settings);
            _created.Clear();
        }

        [Test]
        public void Pick_SameNamedAssetInSubfolderWins()
        {
            var sdkDefault = Make("DefaultSettings");
            var own = Make(VerifyAccountSettings.ResourceName);

            var picked = SettingsLocator.Pick(new[] { sdkDefault, own }, sdkDefault, out var warning);

            Assert.AreSame(own, picked);
            Assert.IsNull(warning);
        }

        [Test]
        public void Pick_FallsBackToDefaultWithWarning()
        {
            var sdkDefault = Make("DefaultSettings");

            var picked = SettingsLocator.Pick(new[] { sdkDefault }, sdkDefault, out var warning);

            Assert.AreSame(sdkDefault, picked);
            StringAssert.Contains("No VerifyAccountSettings asset found", warning);
        }

        [Test]
        public void Pick_IgnoresDefaultEvenWhenItCarriesTheGameName()
        {
            var sdkDefault = Make(VerifyAccountSettings.ResourceName);

            var picked = SettingsLocator.Pick(new[] { sdkDefault }, sdkDefault, out var warning);

            Assert.AreSame(sdkDefault, picked);
            Assert.IsNotNull(warning);
        }

        [Test]
        public void Pick_MentionsWronglyNamedAssets()
        {
            var sdkDefault = Make("DefaultSettings");
            var other = Make("GameVerify");

            var picked = SettingsLocator.Pick(new[] { sdkDefault, other }, sdkDefault, out var warning);

            Assert.AreSame(sdkDefault, picked);
            StringAssert.Contains("GameVerify", warning);
        }

        [Test]
        public void Pick_WarnsOnDuplicates()
        {
            var sdkDefault = Make("DefaultSettings");
            var first = Make(VerifyAccountSettings.ResourceName);
            var second = Make(VerifyAccountSettings.ResourceName);

            var picked = SettingsLocator.Pick(new[] { sdkDefault, first, second }, sdkDefault, out var warning);

            Assert.AreSame(first, picked);
            StringAssert.Contains("2 assets", warning);
        }

        [Test]
        public void Pick_NothingAtAllReturnsNull()
        {
            Assert.IsNull(SettingsLocator.Pick(null, null, out _));
            Assert.IsNull(SettingsLocator.Pick(new VerifyAccountSettings[0], null, out _));
        }

        [TestCase("Assets/Resources/VerifyAccountSettings.asset", null)]
        [TestCase("Assets/_Assets/Resources/ScriptableObjects/VerifyAccountSettings.asset", null)]
        [TestCase("Assets/Game/resources/VerifyAccountSettings.asset", null)]
        [TestCase("Assets/Config/VerifyAccountSettings.asset", "outside every Resources folder")]
        [TestCase("Assets/Resources/GameVerify.asset", "named \"GameVerify\"")]
        [TestCase("Assets/Editor/Resources/VerifyAccountSettings.asset", "Editor folder")]
        public void Placement(string path, string expectedFragment)
        {
            var problem = SettingsLocator.DescribePlacementProblem(path);

            if (expectedFragment == null) Assert.IsNull(problem, problem);
            else StringAssert.Contains(expectedFragment, problem);
        }
    }
}
