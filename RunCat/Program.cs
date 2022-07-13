// Copyright 2020-2022 Takuto Nakamura
// Copyright 2022 somni
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Resources;
using System.Windows.Forms;
using Microsoft.Win32;
using RunCat.Properties;

namespace RunCat {
    static class Program {
        [STAThread]
        static void Main() {
            // terminate runcat if there's any existing instance
            var procMutex = new System.Threading.Mutex(true, "_RUNCAT_MUTEX", out var result);
            if(!result) {
                return;
            }

            UserSettings.Default.Reload();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RunCatApplicationContext());

            procMutex.ReleaseMutex();
        }
    }

    public class RunCatApplicationContext : ApplicationContext {
        private const int CPU_TIMER_DEFAULT_INTERVAL = 3000;
        private const int ANIMATE_TIMER_DEFAULT_INTERVAL = 200;
        private readonly PerformanceCounter cpuUsage;
        private readonly ToolStripMenuItem runnerMenu;
        private readonly ToolStripMenuItem themeMenu;
        private readonly ToolStripMenuItem startupMenu;
        private readonly ToolStripMenuItem runnerSpeedLimit;
        private readonly ToolStripMenuItem counterTypeMenu;
        private readonly NotifyIcon notifyIcon;
        private string runner = UserSettings.Default.Runner;
        private int current = 0;
        private float minCPU;
        private float interval;
        private string systemTheme = "";
        private string manualTheme = UserSettings.Default.Theme;
        private string speed = UserSettings.Default.Speed;
        private string counterType = UserSettings.Default.CounterType;
        private Icon[] icons;
        private readonly Timer animateTimer = new();
        private readonly Timer cpuTimer = new();

        public RunCatApplicationContext() {
            Application.ApplicationExit += new EventHandler(OnApplicationExit);

            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(UserPreferenceChanged);

            cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ = cpuUsage.NextValue(); // discards first return value

            runnerMenu = new ToolStripMenuItem(Strings.Strings.Runner_Title, null, new ToolStripMenuItem[] {
                new ToolStripMenuItem("Cat", null, SetRunner) {
                    Checked = runner.Equals("cat")
                },
                new ToolStripMenuItem("Parrot", null, SetRunner) {
                    Checked = runner.Equals("parrot")
                },
                new ToolStripMenuItem("Horse", null, SetRunner) {
                    Checked = runner.Equals("horse")
                }
            });

            themeMenu = new ToolStripMenuItem(Strings.Strings.Theme_Title, null, new ToolStripMenuItem[] {
                new ToolStripMenuItem("Default", null, SetThemeIcons) {
                    Checked = manualTheme.Equals("")
                },
                new ToolStripMenuItem("Light", null, SetLightIcons) {
                    Checked = manualTheme.Equals("light")
                },
                new ToolStripMenuItem("Dark", null, SetDarkIcons) {
                    Checked = manualTheme.Equals("dark")
                }
            });

            startupMenu = new ToolStripMenuItem(Strings.Strings.Startup_Title, null, SetStartup);
            if(IsStartupEnabled()) {
                startupMenu.Checked = true;
            }

            runnerSpeedLimit = new ToolStripMenuItem(Strings.Strings.SpeedLimit_Title, null, new ToolStripMenuItem[] {
                new ToolStripMenuItem("Default", null, SetSpeedLimit) {
                    Checked = speed.Equals("default")
                },
                new ToolStripMenuItem("CPU 10%", null, SetSpeedLimit) {
                    Checked = speed.Equals("cpu 10%")
                },
                new ToolStripMenuItem("CPU 20%", null, SetSpeedLimit) {
                    Checked = speed.Equals("cpu 20%")
                },
                new ToolStripMenuItem("CPU 30%", null, SetSpeedLimit) {
                    Checked = speed.Equals("cpu 30%")
                },
                new ToolStripMenuItem("CPU 40%", null, SetSpeedLimit) {
                    Checked = speed.Equals("cpu 40%")
                }
            });

            counterTypeMenu = new ToolStripMenuItem(Strings.Strings.CounterType_Title, null, new ToolStripMenuItem[] {
                new ToolStripMenuItem(Strings.Strings.CounterType_Time_Title, null, SetCounterType) {
                    Checked = counterType.Equals("time"),
                    Tag = "time"
                },
                new ToolStripMenuItem($"�� {Strings.Strings.CounterType_Time_Description}") {
                    Enabled = false,
                },
                new ToolStripMenuItem(Strings.Strings.CounterType_Util_Title, null, SetCounterType) {
                    Checked = counterType.Equals("utility"),
                    Tag = "utility"
                },
                new ToolStripMenuItem($"�� {Strings.Strings.CounterType_Util_Description}") {
                    Enabled = false,
                },
            });

            ContextMenuStrip contextMenuStrip = new ContextMenuStrip(new Container());
            contextMenuStrip.Items.AddRange(new ToolStripItem[] {
                runnerMenu,
                themeMenu,
                startupMenu,
                runnerSpeedLimit,
                counterTypeMenu,
                new ToolStripSeparator(),
                new ToolStripMenuItem($"{Application.ProductName} v{Application.ProductVersion}", null) { Enabled = false },
                new ToolStripMenuItem(Strings.Strings.Exit_Title, null, Exit)
            });

            notifyIcon = new NotifyIcon() {
                Icon = Resources.light_cat_0,
                ContextMenuStrip = contextMenuStrip,
                Text = "0.0%",
                Visible = true
            };

            notifyIcon.DoubleClick += new EventHandler(HandleDoubleClick);

            UpdateThemeIcons();
            SetAnimation();
            SetSpeed();
            StartObserveCPU();

            current = 1;
        }

        private void OnApplicationExit(object sender, EventArgs e) {
            UserSettings.Default.Runner = runner;
            UserSettings.Default.Theme = manualTheme;
            UserSettings.Default.Speed = speed;
            UserSettings.Default.CounterType = counterType;
            UserSettings.Default.Save();
        }

        private bool IsStartupEnabled() {
            string keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using(RegistryKey rKey = Registry.CurrentUser.OpenSubKey(keyName)) {
                return rKey.GetValue(Application.ProductName) != null;
            }
        }

        private string GetAppsUseTheme() {
            string keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using(RegistryKey rKey = Registry.CurrentUser.OpenSubKey(keyName)) {
                object value;
                if(rKey == null || (value = rKey.GetValue("SystemUsesLightTheme")) == null) {
                    Console.WriteLine("Oh No! Couldn't get theme light/dark");
                    return "light";
                }
                int theme = (int) value;
                return theme == 0 ? "dark" : "light";
            }
        }

        private void SetIcons() {
            string prefix = 0 < manualTheme.Length ? manualTheme : systemTheme;
            ResourceManager rm = Resources.ResourceManager;
            // default runner is cat
            int capacity = 5;
            if(runner.Equals("parrot")) {
                capacity = 10;
            } else if(runner.Equals("horse")) {
                capacity = 14;
            }
            List<Icon> list = new List<Icon>(capacity);
            for(int i = 0; i < capacity; i++) {
                list.Add((Icon)rm.GetObject($"{prefix}_{runner}_{i}"));
            }
            icons = list.ToArray();
        }

        private void UpdateCheckedState(ToolStripMenuItem sender, ToolStripMenuItem menu) {
            foreach(ToolStripMenuItem item in menu.DropDownItems) {
                item.Checked = false;
            }
            sender.Checked = true;
        }

        private void SetRunner(object sender, EventArgs e) {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            UpdateCheckedState(item, runnerMenu);
            runner = item.Text.ToLower();
            SetIcons();
        }

        private void SetThemeIcons(object sender, EventArgs e) {
            UpdateCheckedState((ToolStripMenuItem)sender, themeMenu);
            manualTheme = "";
            systemTheme = GetAppsUseTheme();
            SetIcons();
        }

        private void SetSpeed() {
            if(speed.Equals("default"))
                return;
            else if(speed.Equals("cpu 10%"))
                minCPU = 100f;
            else if(speed.Equals("cpu 20%"))
                minCPU = 50f;
            else if(speed.Equals("cpu 30%"))
                minCPU = 33f;
            else if(speed.Equals("cpu 40%"))
                minCPU = 25f;
        }

        private void SetSpeedLimit(object sender, EventArgs e) {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            UpdateCheckedState(item, runnerSpeedLimit);
            speed = item.Text.ToLower();
            SetSpeed();
        }

        private void SetCounterType(object sender, EventArgs e) {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            UpdateCheckedState(item, counterTypeMenu);
            counterType = ((string)item.Tag).ToLower();

            if(counterType.Equals("time")) {
                cpuUsage.CategoryName = "Processor";
                cpuUsage.CounterName = "% Processor Time";
                cpuUsage.NextValue();
            } else if(counterType.Equals("utility")) {
                cpuUsage.CategoryName = "Processor Information";
                cpuUsage.CounterName = "% Processor Utility";
                cpuUsage.NextValue();
            }
        }

        private void UpdateThemeIcons() {
            if(0 < manualTheme.Length) {
                SetIcons();
                return;
            }
            string newTheme = GetAppsUseTheme();
            if(systemTheme.Equals(newTheme))
                return;
            systemTheme = newTheme;
            SetIcons();
        }

        private void SetLightIcons(object sender, EventArgs e) {
            UpdateCheckedState((ToolStripMenuItem)sender, themeMenu);
            manualTheme = "light";
            SetIcons();
        }

        private void SetDarkIcons(object sender, EventArgs e) {
            UpdateCheckedState((ToolStripMenuItem)sender, themeMenu);
            manualTheme = "dark";
            SetIcons();
        }
        private void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) {
            if(e.Category == UserPreferenceCategory.General)
                UpdateThemeIcons();
        }

        private void SetStartup(object sender, EventArgs e) {
            startupMenu.Checked = !startupMenu.Checked;
            string keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using(RegistryKey rKey = Registry.CurrentUser.OpenSubKey(keyName, true)) {
                if(startupMenu.Checked) {
                    rKey.SetValue(Application.ProductName, Process.GetCurrentProcess().MainModule.FileName);
                } else {
                    rKey.DeleteValue(Application.ProductName, false);
                }
                rKey.Close();
            }
        }

        private void Exit(object sender, EventArgs e) {
            cpuUsage.Close();
            animateTimer.Stop();
            cpuTimer.Stop();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void AnimationTick(object sender, EventArgs e) {
            if(icons.Length <= current)
                current = 0;
            notifyIcon.Icon = icons[current];
            current = (current + 1) % icons.Length;
        }

        private void SetAnimation() {
            animateTimer.Interval = ANIMATE_TIMER_DEFAULT_INTERVAL;
            animateTimer.Tick += new EventHandler(AnimationTick);
        }

        private void CPUTickSpeed() {
            if(!speed.Equals("default")) {
                float manualInterval = (float)Math.Max(minCPU, interval);
                animateTimer.Stop();
                animateTimer.Interval = (int)manualInterval;
                animateTimer.Start();
            } else {
                animateTimer.Stop();
                animateTimer.Interval = (int)interval;
                animateTimer.Start();
            }
        }

        private void CPUTick() {
            interval = Math.Min(100.0f, cpuUsage.NextValue());
            notifyIcon.Text = $"CPU: {interval:f1}%";
            interval = 200.0f / (float)Math.Max(1.0f, Math.Min(20.0f, interval / 5.0f));
            _ = interval;
            CPUTickSpeed();
        }
        private void ObserveCPUTick(object sender, EventArgs e) {
            CPUTick();
        }

        private void StartObserveCPU() {
            cpuTimer.Interval = CPU_TIMER_DEFAULT_INTERVAL;
            cpuTimer.Tick += new EventHandler(ObserveCPUTick);
            cpuTimer.Start();
        }

        private void HandleDoubleClick(object Sender, EventArgs e) {
            var startInfo = new ProcessStartInfo {
                FileName = "powershell",
                UseShellExecute = false,
                Arguments = " -c Start-Process taskmgr.exe",
                CreateNoWindow = true,
            };
            Process.Start(startInfo);
        }
    }
}
