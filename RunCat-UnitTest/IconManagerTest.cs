using System.Collections.ObjectModel;
using System.Drawing;
using System.Text.RegularExpressions;
using static RunCat.IconManager;

namespace RunCat_UnitTest {
    [TestClass]
    public class IconManagerTest {
        readonly IconManager iconManager;

        public IconManagerTest() {
            iconManager = new IconManager();
        }

        public static IEnumerable<object[]> IconManagerTestData() {
            string[] availableIconNames = { "Cat", "Horse", "Parrot" };
            IconTheme[] availableIconThemes = { IconTheme.Light, IconTheme.Dark };

            for(int i = 0; i < availableIconNames.Length; i++) {
                for(int j = 0; j < availableIconThemes.Length; j++) {
                    yield return new object[] {
                        availableIconNames[i],
                        availableIconThemes[j]
                    };
                }
            }
        }

        [TestMethod]
        public void GetSpecificIconResourceNames_Simple() {
            object target = iconManager.GetSpecificIconResourceNames("Cat");
            Assert.IsNotNull(target);
            Assert.IsInstanceOfType(target, typeof(ReadOnlyCollection<string>));

            ReadOnlyCollection<string> targetTyped = (ReadOnlyCollection<string>)target;
            Assert.AreNotEqual(targetTyped.Count, 0);
            CollectionAssert.AllItemsAreNotNull(targetTyped);
            CollectionAssert.AllItemsAreUnique(targetTyped);
            CollectionAssert.AllItemsAreInstancesOfType(targetTyped, typeof(string));
        }

        [DataTestMethod]
        [DynamicData(nameof(IconManagerTestData), DynamicDataSourceType.Method)]
        public void GetSpecificIconResourceNames_Advanced(string iconName, IconTheme theme) {
            object target = iconManager.GetSpecificIconResourceNames(iconName, theme);
            Assert.IsNotNull(target);
            Assert.IsInstanceOfType(target, typeof(ReadOnlyCollection<string>));

            ReadOnlyCollection<string> targetTyped = (ReadOnlyCollection<string>)target;
            Assert.AreNotEqual(targetTyped.Count, 0);
            CollectionAssert.AllItemsAreNotNull(targetTyped);
            CollectionAssert.AllItemsAreUnique(targetTyped);
            CollectionAssert.AllItemsAreInstancesOfType(targetTyped, typeof(string));

            Assert.IsTrue(targetTyped.All((x) => Constants.TRAY_ICONS_NAME_REGEX.IsMatch(x)));
            Assert.IsTrue(targetTyped.All((x) => Constants.TRAY_ICONS_NAME_REGEX.Match(x).Groups[1].Value == iconName));
            Assert.IsTrue(targetTyped.All((x) => Constants.TRAY_ICONS_NAME_REGEX.Match(x).Groups[2].Value == (theme == IconTheme.Dark ? "Dark" : "Light")));
            Assert.IsTrue(targetTyped.All((x) => x.Contains(theme == IconTheme.Dark ? "Dark" : "Light")));
        }

        [TestMethod]
        public void GetSpecificIconResourceName_Simple() {
            object target = iconManager.GetSpecificIconResourceName("Cat", IconTheme.Light, 2);
            Assert.IsNotNull(target);
            Assert.IsInstanceOfType(target, typeof(string));

            string targetTyped = (string)target;
            Assert.IsNotNull(targetTyped);
            Assert.IsInstanceOfType(targetTyped, typeof(string));

            Match targetRegexMatch = Constants.TRAY_ICONS_NAME_REGEX.Match(targetTyped);
            Assert.IsTrue(targetRegexMatch.Success);
            Assert.AreEqual(targetRegexMatch.Groups[1].Value, "Cat");
            Assert.AreEqual(targetRegexMatch.Groups[2].Value, "Light");
            Assert.AreEqual(targetRegexMatch.Groups[3].Value, "2");
        }

        [TestMethod]
        public void GetSpecificIcons_Simple() {
            object target = iconManager.GetSpecificIcons("Cat");
            Assert.IsNotNull(target);
            Assert.IsInstanceOfType(target, typeof(ReadOnlyCollection<IconInfo>));

            ReadOnlyCollection<IconInfo> targetTyped = (ReadOnlyCollection<IconInfo>)target;
            Assert.AreNotEqual(targetTyped.Count, 0);
            CollectionAssert.AllItemsAreNotNull(targetTyped);
            CollectionAssert.AllItemsAreUnique(targetTyped);
            CollectionAssert.AllItemsAreInstancesOfType(targetTyped, typeof(IconInfo));

            Assert.IsTrue(targetTyped.All((x) => x.ResourceName.StartsWith("Cat")));
            Assert.IsTrue(targetTyped.All((x) => x.FrameNth >= 0 && x.FrameNth < targetTyped.Count / 2));
            Assert.IsTrue(targetTyped.All((x) => x.Icon != null && x.Icon is Icon));
        }

        [DataTestMethod]
        [DynamicData(nameof(IconManagerTestData), DynamicDataSourceType.Method)]
        public void GetSpecificIcons_Advanced(string iconName, IconTheme theme) {
            object target = iconManager.GetSpecificIcons(iconName, theme);
            Assert.IsNotNull(target);
            Assert.IsInstanceOfType(target, typeof(ReadOnlyCollection<IconInfo>));

            ReadOnlyCollection<IconInfo> targetTyped = (ReadOnlyCollection<IconInfo>)target;
            Assert.AreNotEqual(targetTyped.Count, 0);
            CollectionAssert.AllItemsAreNotNull(targetTyped);
            CollectionAssert.AllItemsAreUnique(targetTyped);
            CollectionAssert.AllItemsAreInstancesOfType(targetTyped, typeof(IconInfo));

            Assert.IsTrue(targetTyped.All((x) => x.ResourceName.StartsWith(iconName)));
            Assert.IsTrue(targetTyped.All((x) => x.Theme == theme));
            Assert.IsTrue(targetTyped.All((x) => x.FrameNth >= 0 && x.FrameNth < targetTyped.Count));
            Assert.IsTrue(targetTyped.All((x) => x.Icon != null && x.Icon is Icon));
        }

        [TestMethod]
        public void GetSpecificIcon_Simple() {
            object target = iconManager.GetSpecificIcon("Cat", IconTheme.Dark, 3);
            Assert.IsNotNull(target);
            Assert.IsInstanceOfType(target, typeof(IconInfo));

            IconInfo targetTyped = (IconInfo)target;
            Assert.IsNotNull(target);
            Assert.IsInstanceOfType(target, typeof(IconInfo));
            Assert.AreEqual(targetTyped.Theme, IconTheme.Dark);
            Assert.AreEqual(targetTyped.FrameNth, 3);
            Assert.IsNotNull(targetTyped.Icon);
            Assert.IsInstanceOfType(targetTyped.Icon, typeof(Icon));

            Match targetRegexMatch = Constants.TRAY_ICONS_NAME_REGEX.Match(targetTyped.ResourceName);
            Assert.IsTrue(targetRegexMatch.Success);
            Assert.AreEqual(targetRegexMatch.Groups[1].Value, "Cat");
            Assert.AreEqual(targetRegexMatch.Groups[2].Value, "Dark");
            Assert.AreEqual(targetRegexMatch.Groups[3].Value, "3");
        }
    }
}