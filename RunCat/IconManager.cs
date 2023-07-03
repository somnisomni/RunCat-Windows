using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using RunCat.Resources.Icons;

namespace RunCat {
    public static class IconManager {
        /// <summary>
        /// Enum of icon color theme names.
        /// </summary>
        public enum IconTheme {
            Light,
            Dark
        };

        /// <summary>
        /// Represents a collection of tray icons, groupped by icon name and theme.
        /// </summary>
        internal class IconInfo {
            internal string ResourceName { get; }
            internal IconTheme Theme { get; }
            internal int FrameNth { get; }
            internal Icon Icon { get; }

            internal IconInfo(string resourceName, IconTheme theme, int frameNth, Icon icon) {
                ResourceName = resourceName;
                Theme = theme;
                FrameNth = frameNth;
                Icon = icon;
            }
        }

        /// <summary>
        /// A dictionary collection of tray icon instances(<c>IconInfo</c>).
        /// </summary>
        /// <value>
        /// <para><c>TKey (string)</c>: Name of icon group, <c>Cat</c>, <c>Horse</c>, <c>Parrot</c> for example.</para>
        /// <para><c>TValue (ReadOnlyCollection&lt;IconInfo&gt;)</c>: Read-only collection of <c>IconInfo</c>, which contains some information of icon resource and <c>System.Drawing.Icon</c> instance.</para>
        /// </value>
        private static readonly ReadOnlyDictionary<string, ReadOnlyCollection<IconInfo>> IconResources;

        static IconManager() {
            // Automatically load tray icon resources

            IEnumerable<DictionaryEntry> resources = Icons.ResourceManager.GetResourceSet(Application.CurrentCulture, true, true)
                .Cast<DictionaryEntry>()
                .Where((e) => e.Value is Icon)
                .OrderBy((e) => e.Key);
            Dictionary<string, List<IconInfo>> icons = new();

            foreach(var item in resources) {
                if(item.Key is string name
                    && item.Value is Icon icon) {
                    Match match = Constants.TRAY_ICONS_NAME_REGEX.Match(name);
                    
                    if(match.Success && match.Groups.Count == 4) {
                        string groupName = match.Groups[1].Value;
                        string themeName = match.Groups[2].Value;
                        int frameNth = int.Parse(match.Groups[3].Value);

                        IconInfo iconInfo = new(name, themeName.ToLower() == "dark" ? IconTheme.Dark : IconTheme.Light, frameNth, icon);

                        if(!icons.ContainsKey(groupName)) {
                            icons.Add(groupName, new List<IconInfo>());
                        }

                        icons[groupName].Add(iconInfo);
                    }
                }
            }

            IconResources = new(icons
                .Select((e) => new KeyValuePair<string, ReadOnlyCollection<IconInfo>>(e.Key, new ReadOnlyCollection<IconInfo>(e.Value)))
                .ToDictionary((e) => e.Key, (e) => e.Value));
        }

        internal static ReadOnlyCollection<string> GetAvailableIconNames() {
            return new ReadOnlyCollection<string>(IconResources.Keys.ToList());
        }

        internal static ReadOnlyCollection<string> GetSpecificIconResourceNames(string iconName) {
            return new ReadOnlyCollection<string>(IconResources[iconName].Select((e) => e.ResourceName).ToList());
        }

        internal static ReadOnlyCollection<string> GetSpecificIconResourceNames(string iconName, IconTheme theme) {
            List<string> names = new(GetSpecificIconResourceNames(iconName));
            int frameCount = names.Count;

            if(theme == IconTheme.Dark) {
                return names.GetRange(0, frameCount / 2).AsReadOnly();
            } else {
                return names.GetRange(frameCount / 2, frameCount / 2).AsReadOnly();
            }
        }

        internal static string GetSpecificIconResourceName(string iconName, IconTheme theme, int frameNth) {
            return GetSpecificIconResourceNames(iconName, theme)[frameNth];
        }

        internal static ReadOnlyCollection<IconInfo> GetSpecificIcons(string iconName) {
            if(!IconResources.ContainsKey(iconName)) {
                throw new KeyNotFoundException($"Icon group name '{iconName}' was not found in auto-loaded icon collection.");
            }

            return IconResources[iconName];
        }

        internal static ReadOnlyCollection<IconInfo> GetSpecificIcons(string iconName, IconTheme theme) {
            List<IconInfo> icons = new(GetSpecificIcons(iconName));
            int frameCount = icons.Count;

            if(theme == IconTheme.Dark) {
                return icons.GetRange(0, frameCount / 2).AsReadOnly();
            } else {
                return icons.GetRange(frameCount / 2, frameCount / 2).AsReadOnly();
            }
        }

        internal static IconInfo GetSpecificIcon(string iconName, IconTheme theme, int frameNth) {
            return GetSpecificIcons(iconName, theme)[frameNth];
        }
    }
}
