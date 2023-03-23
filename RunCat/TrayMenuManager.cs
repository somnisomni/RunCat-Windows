using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RunCat.Properties;

namespace RunCat {
    internal class TrayMenuManager {
        internal enum TrayMenuItemPreferenceType {
            TagValue,
            Boolean,
        }

        internal class TrayMenuItem {
            private string _Title = "";
            private string _Tag = "";
            private bool _Disabled = false;
            private List<ToolStripItem> _Childrens = new();

            private ToolStripMenuItem instance;

            internal string Title {
                get => _Title;
                set {
                    _Title = value;
                    instance!.Text = value;
                }
            }
            internal string Tag {
                get => _Tag;
                set {
                    _Tag = value;
                    instance!.Tag = value;
                }
            }
            internal bool Disabled {
                get => _Disabled;
                set {
                    _Disabled = value;
                    instance!.Enabled = !value;
                }
            }
            internal List<TrayMenuItem> Childrens {
                get => _Childrens;
                set {
                    _Childrens = value;
                    CreateToolStripMenuItem();
                }
            }
            internal string PreferenceName { get; set; }
            internal TrayMenuItemPreferenceType PreferenceType { get; set; }

            internal TrayMenuItem(bool separator = false) {
                if(separator) {
                    instance = new ToolStripSeparator();
                } else {
                    CreateToolStripMenuItem();
                }
            }

            private void CreateToolStripMenuItem() {
                if(Childrens.Count <= 0) {
                    instance = new ToolStripMenuItem(Title, null, ClickHandler);
                    UpdateChecked();
                } else {
                    ToolStripMenuItem[] instanceChildrens = Childrens.Select((x) => x.instance).ToArray();

                    instance = new ToolStripMenuItem(Title, null, instanceChildrens);
                }
            }

            private void UpdateChecked() {
                if(!string.IsNullOrWhiteSpace(PreferenceName)) {
                    if(PreferenceType == TrayMenuItemPreferenceType.Boolean) {
                        instance.Checked = bool.Parse((string)UserSettings.Default[PreferenceName]);
                    } else if(PreferenceType == TrayMenuItemPreferenceType.TagValue && !string.IsNullOrWhiteSpace(Tag)) {
                        instance.Checked = (string)UserSettings.Default[PreferenceName] == Tag;
                    }
                }
            }

            private void ClickHandler(object sender, EventArgs e) {
                UpdateChecked();
            }
        }

        internal readonly List<TrayMenuItem> menuItemList = new();

        internal TrayMenuManager() {
            // Build default tray menu items
            menuItemList.Add(new TrayMenuItem {
                Title = $"{Application.ProductName} v{Application.ProductVersion}",
                Disabled = true,
            });
            menuItemList.Add
        }
    }
}
