﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using OrigindLauncher.Resources.Client;
using OrigindLauncher.Resources.FileSystem;
using OrigindLauncher.Resources.Server;
using OrigindLauncher.Resources.Server.Data;
using OrigindLauncher.Resources.String;
using OrigindLauncher.Resources.Utils;
using OrigindLauncher.UI.Code;

namespace OrigindLauncher.UI.Dialogs
{
    /// <summary>
    ///     Interaction logic for GameUpdatingDialog.xaml
    /// </summary>
    public partial class GameUpdatingDialog
    {
        private readonly Dictionary<FileEntry, TextBlock> _deleteDictionary = new Dictionary<FileEntry, TextBlock>();

        private readonly Dictionary<ClientManager.DownloadInfo, ProgressBar> _downloadProgressDictionary =
            new Dictionary<ClientManager.DownloadInfo, ProgressBar>();

        private readonly Dictionary<ClientManager.DownloadInfo, TextBlock> _downloadTbDictionary =
            new Dictionary<ClientManager.DownloadInfo, TextBlock>();

        private bool isRunning = true;

        public GameUpdatingDialog(ClientManager.UpdateInfo updateInfo)
        {
            UpdateInfo = updateInfo;
            InitializeComponent();
            foreach (var deleteEntry in updateInfo.FilesToDelete)
            {
                var textblock = new TextBlock { Text = $"删除 {deleteEntry.Path}", Margin = new Thickness(4) };
                UpdatePanel.Children.Add(textblock);
                _deleteDictionary.Add(deleteEntry, textblock);
            }

            foreach (var downloadEntry in updateInfo.FilesToDownload)
            {
                var textBlock = new TextBlock { Text = $"下载 {downloadEntry.Path}", Margin = new Thickness(4) };
                var progressBar = new ProgressBar { Maximum = 1, Margin = new Thickness(4, 0, 4, 4) };
                _downloadTbDictionary.Add(downloadEntry, textBlock);
                _downloadProgressDictionary.Add(downloadEntry, progressBar);
                UpdatePanel.Children.Add(textBlock);
                UpdatePanel.Children.Add(progressBar);
            }
        }

        private ClientManager.UpdateInfo UpdateInfo { get; }

        public bool Result { get; private set; }

        private async void Confirm(object sender, RoutedEventArgs e)
        {
            ConfirmBtn.IsEnabled = false;
            await Task.Run(() =>
            {
                Parallel.ForEach(UpdateInfo.FilesToDelete, deleteEntry =>
                {
                    if (!isRunning) return;

                    try
                    {
                        var path = ClientManager.GetGameStorageDirectory(deleteEntry.Path);

                        if (File.Exists(path))
                            File.Delete(path);
                        else
                            Trace.WriteLine($"文件不存在: {path}");

                        try
                        {
                            var current =
                                ClientManager.CurrentInfo.Files.Find(f => f.Equals(deleteEntry));
                            ClientManager.CurrentInfo.Files.Remove(current);
                        }
                        catch (Exception exception)
                        {
                            Trace.WriteLine(exception);
                        }
                        Dispatcher.Invoke(() => UpdatePanel.Children.Remove(_deleteDictionary[deleteEntry]));
                    }
                    catch (Exception exception)
                    {
                        MessageUploadManager.CrashReport(new UploadData($"更新器V2发生异常 {exception.SerializeException()} {exception.InnerException?.SerializeException()}"));
                        isRunning = false;
                    }
                });

                Parallel.ForEach(UpdateInfo.FilesToDownload, downloadEntry =>
                {
                    if (!isRunning) return;
                    try
                    {
                        var wc = new WebClient { Proxy = null };
                        var path = ClientManager.GetGameStorageDirectory(downloadEntry.Path);
                        if (File.Exists(path))
                        {
                            using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                if (SHA128Computer.Compute(file) == downloadEntry.Hash)
                                    goto finish;
                            }

                            File.Delete(path);
                        }
                        DirectoryHelper.EnsureDirectoryExists(Path.GetDirectoryName(path));
                        wc.DownloadProgressChanged += (o, args) => Dispatcher.InvokeAsync(() =>
                        {
                            var progress = _downloadProgressDictionary[downloadEntry];
                            progress.Value = args.BytesReceived / (double)args.TotalBytesToReceive;
                        });

                        wc.DownloadFileCompleted += (o, args) =>
                         ClientManager.CurrentInfo.Files.Add(new FileEntry(downloadEntry.Path, downloadEntry.Hash));
                        wc.DownloadFileTaskAsync(downloadEntry.Url, path).Wait();

                        finish:
                        Dispatcher.InvokeAsync(() =>
                        {
                            UpdatePanel.Children.Remove(_downloadProgressDictionary[downloadEntry]);
                            UpdatePanel.Children.Remove(_downloadTbDictionary[downloadEntry]);
                        });
                    }
                    catch (Exception exception)
                    {
                        MessageUploadManager.CrashReport(new UploadData($"更新器V2发生异常 {exception.SerializeException()} {exception.InnerException?.SerializeException()}"));
                        isRunning = false;
                    }
                });

                Result = isRunning;

                Close();
            });
        }

        private void Close()
        {
            Dispatcher.Invoke(() => DialogHost.CloseDialogCommand.Execute(this, this));
        }

        private static readonly Random Random = new Random();

        private void ConfirmBtn_OnMouseMove(object sender, MouseEventArgs e)
        {
            var transformGroup = (TransformGroup)ConfirmBtn.RenderTransform;
            var translate = (TranslateTransform)transformGroup.Children.First(t => t is TranslateTransform);
            translate.X += (Random.NextDouble() - 0.5) * 20;
            translate.Y += (Random.NextDouble() - 0.5) * 20;
            var translate2 = (ScaleTransform)transformGroup.Children.First(t => t is ScaleTransform);
            translate2.ScaleX += (Random.NextDouble() - 0.5) / 5.0;
            translate2.ScaleY += (Random.NextDouble() - 0.5) / 5.0;
            var translate3 = (RotateTransform)transformGroup.Children.First(t => t is RotateTransform);
            translate3.Angle += (Random.NextDouble() - 0.5) * 10;
        }
    }
}