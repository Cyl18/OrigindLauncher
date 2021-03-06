﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using OrigindLauncher.Resources;
using OrigindLauncher.Resources.Configs;
using OrigindLauncher.Resources.Core;
using OrigindLauncher.Resources.FileSystem;
using OrigindLauncher.Resources.Sound;
using OrigindLauncher.Resources.UI;
using OrigindLauncher.Resources.Utils;
using OrigindLauncher.UI;

namespace OrigindLauncher
{
    internal static class Program
    {
        static Program()
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName));
        }

        private static bool IsRunAsAdmin()
        {
            var id = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(id);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        [STAThread]
        private static void Main(string[] args)
        {
            if (Config.Instance.DisableHardwareSpeedup)
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            if (Config.Instance.EnableDebug)
                DebugHelper.EnableDebug();
            Thread.CurrentThread.CurrentCulture = new CultureInfo("zh-CN");
            
            if (args.Any(s => s == "AddPermissions"))
            {
                DirectoryHelper.AddCurrentDirectoryWritePermissionInternal();
                return;
            }

            if (args.Length == 3 && args.Any(s => s == "Update"))
            {
                var app = new App();
                app.InitializeComponent();
                AutoUpdater.UpdateInternal(args[1], args[2], app);
                return;
            }
            
            if (Config.Instance.UseAdmin && !IsRunAsAdmin())
            {
                Process.Start(
                    new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName)
                    { Verb = "runas" });
                return;
            }
            
            using (var mutex = new Mutex(false,
                $"Global\\OrigindLauncher_{Process.GetCurrentProcess().MainModule.FileName.GetHashCode()}"))
            {
                if (!mutex.WaitOne(4000, false))
                {
                    MessageBox.Show("有一个 Origind Launcher 进程正在运行. 这个进程将会退出.");
                    return;
                }
                Trace.WriteLine("Init done.");

                if (args.Any(s => s == "Setup") || !File.Exists(Definitions.ConfigJsonPath) ||
                    Config.Instance.PlayerAccount == null)
                {
                    var app1 = new App();
                    app1.InitializeComponent();
                    app1.Run(new SetupWindow());
                }
                else
                {
                    var app = new App();
                    app.InitializeComponent();
                    ThemeManager.Init();
                    app.Run(new LauncherWindow());
                }
            }
        }
    }
}