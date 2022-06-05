using HandyControl.Controls;
using IWshRuntimeLibrary;
using HandyPanel.View;
using HandyPanel.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HandyPanel.Model;
using System.Collections.ObjectModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using System.Diagnostics;
using System.ComponentModel;
using HandyControl.Tools;
using HandyControl.Interactivity;
using Microsoft.Toolkit.Mvvm.Input;
using HandyPanel.Util;
using System.IO;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace HandyPanel
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : GlowWindow
    {
        private bool need_save_config = false;
        private KeyGestureConverter key_gesture_converter = new KeyGestureConverter();
        private MainViewModel vm_ = null;

        public MainWindow()
        {
            this.DataContextChanged += (object sender, DependencyPropertyChangedEventArgs e) =>
            {
                vm_ = e.NewValue as MainViewModel;
            };

            this.ContentRendered += (object sender, EventArgs e) =>
            {
                NonClientAreaContent = new NonClientAreaContent();
                WindowAttach.SetIgnoreAltF4(this, true);

                if (Environment.CommandLine.Contains("-startup")) {
                    Hide();
                }
            };

            this.Activated += (object sender, EventArgs e) =>
            {
                vm_?.CheckReloadConfig();
            };

            this.Deactivated += ( object sender, EventArgs e) =>
            {
                if (vm_?.Settings.HideOnDeactive ?? false) {
                    Hide();
                }
            };

            this.Closed += (object sender, EventArgs e) =>
            {
                vm_?.SaveSettings();
            };

            this.SizeChanged += (object sender, SizeChangedEventArgs e) =>
            {
                vm_?.SaveSettings();
            };

            this.KeyDown += (object sender, KeyEventArgs e) => {
                if (e.Key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None)
                {
                    e.Handled = true;
                    Close();
                }
            };

            WeakReferenceMessenger.Default.Register<MainWindow, SimpleMessage>(this, (r, m) =>
            {
                switch (m.Value)
                {
                    case SimpleMessageDefine.ActivePanelMessage:
                        CheckSwitchVisiableState();
                        break;
                    default:
                        break;
                }
            });

            WeakReferenceMessenger.Default.Register<MainWindow, ShowInTaskBarMessage>(this, (r, m) =>
            {
                this.ShowInTaskbar = m.Value;
            });

            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (vm_.Settings.CloseToTray) {
                e.Cancel = true;
                Hide();
            }
        }

        private void CheckSwitchVisiableState()
        {
            if (this.Visibility != Visibility.Visible)
            {
                Show();
                WindowHelper.SetWindowToForeground(this);
            }
            else {
                Hide();
            }
        }

        private void CheckSaveConfig() {
            if (need_save_config) {
                need_save_config = false;
                (DataContext as MainViewModel).SaveConfig();
            }
        }

        private void CheckClose() {
            if (vm_.Settings.AutoHidePanelWhenItemLaunched) {
                Close();
            }
        }

        private void LaunchProcess(HandyItem item, bool run_as_admin = false) {
            ProcessStartInfo param = new ProcessStartInfo(item.Target, item.Arguments);
            if (Directory.Exists(item.WorkingDir))
            {
                param.WorkingDirectory = item.WorkingDir;
            }
            else
            {
                param.WorkingDirectory = System.IO.Path.GetDirectoryName(item.Target);
            }
            if (run_as_admin) {
                param.Verb = "runas";
            }
            Process.Start(param);
            CheckClose();
        }

        private void HandleBoxItemListPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled) {
                e.Handled = true;

                var evt_arg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                evt_arg.RoutedEvent = UIElement.MouseWheelEvent;
                evt_arg.Source = sender;

                ((sender as Control).Parent as UIElement).RaiseEvent(evt_arg);
            }
        }

        private void HandleTabItemListDragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Link : DragDropEffects.None;
        }

        private void HandleTabItemListDrop(object sender, DragEventArgs e)
        {
            var file_path = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            var items = (sender as ListBox).ItemsSource as ObservableCollection<HandyItem>;
            DoItemListDrop(file_path, items);
        }

        private void HandleBoxItemListDragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Link : DragDropEffects.None;
        }

        private void HandleBoxItemListDrop(object sender, DragEventArgs e)
        {
            var file_path = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            var items = (sender as ListBox).ItemsSource as ObservableCollection<HandyItem>;
            DoItemListDrop(file_path, items);
        }

        private void DoItemListDrop(string file_path, ObservableCollection<HandyItem> items ) {
            try
            {
                WeakReferenceMessenger.Default.Send(new DynamicAddHandyItemMessage(
                    new KeyValuePair<ObservableCollection<HandyItem>, string>(items, file_path)
                    ));
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Error(ex.Message, "目标拖入失败");
            }
        }

        private void HandleTabHandyItemSingleClicked(object sender, MouseButtonEventArgs e)
        {
            if (vm_.Settings.DoubleClickLaunchItem)
            {
                return;
            }
            var list_item = (sender as ListBoxItem);
            list_item.IsSelected = true;
            var item = list_item.DataContext as HandyItem;
            DoRunHandyItem(item);
        }

        private void HandleBoxHandyItemSingleClicked(object sender, MouseButtonEventArgs e)
        {
            if (vm_.Settings.DoubleClickLaunchItem)
            {
                return;
            }
            var list_item = (sender as ListBoxItem);
            list_item.IsSelected = true;
            var item = list_item.DataContext as HandyItem;
            DoRunHandyItem(item);
        }

        private void HandleTabHandyItemDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (vm_.Settings.DoubleClickLaunchItem == false)
            {
                return;
            }
            var item = (sender as ListBoxItem).DataContext as HandyItem;
            DoRunHandyItem(item);
        }

        private void HandleBoxHandyItemDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (vm_.Settings.DoubleClickLaunchItem == false)
            {
                return;
            }
            var item = (sender as ListBoxItem).DataContext as HandyItem;
            DoRunHandyItem(item);
        }

        private void DoRunHandyItem(HandyItem item) {
            try
            {
                LaunchProcess(item);
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Error(ex.Message, "错误");
            }
        }

        private void HandleBoxHandyItemRightPushed(object sender, MouseButtonEventArgs e)
        {
            var item = (sender as ListBoxItem).DataContext as HandyItem;
            DoHandyItemRightPushed(item);
        }

        private void HandleTabHandyItemRightPushed(object sender, MouseButtonEventArgs e)
        {
            var item = (sender as ListBoxItem).DataContext as HandyItem;
            DoHandyItemRightPushed(item);
        }

        private void DoHandyItemRightPushed(HandyItem item) {
            item.PropertyChanged -= OnCurHandyItemPropertyChanged;
            item.PropertyChanged += OnCurHandyItemPropertyChanged;

            vm_.CurDetailItem = item;
            vm_.ItemDetailOpened = true;
        }

        private void OnCurHandyItemPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            need_save_config = true;
        }

        private void HandleItemDetailPanelClosed(object sender, RoutedEventArgs e)
        {
            vm_.CurDetailItem.PropertyChanged -= OnCurHandyItemPropertyChanged;
            vm_.CurDetailItem = null;
            CheckSaveConfig();
        }

        private void HandleLocateItemClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("Explorer", $"/select,{vm_.CurDetailItem.Target}");
                vm_.ItemDetailOpened = false;
                CheckClose();
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Error(ex.Message, "定位失败");
            }
        }

        private void HandleRunItemAsAdminClicked(object sender, RoutedEventArgs e)
        {
            // 判断当前权限
            System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
            bool isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            try
            {
                LaunchProcess(vm_.CurDetailItem, !isAdmin);
                vm_.ItemDetailOpened = false;
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Error(ex.Message, "错误");
            }
        }

        private void HandleCopyItemTargetPathClicked(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(vm_.CurDetailItem.Target);
            vm_.ItemDetailOpened = false;
            CheckClose();
        }

        private void HandleDeleteItemClicked(object sender, RoutedEventArgs e)
        {
            if (HandyControl.Controls.MessageBox.Ask("确定删除当前项目？", "删除确认") == MessageBoxResult.OK) {
                var item = vm_.CurDetailItem;
                item.Container?.Items.Remove(item);
                need_save_config = true;
                vm_.ItemDetailOpened = false;
            }
        }

        private void HandleItemDetailSelectImage(object sender, MouseButtonEventArgs e)
        {
            var dlg = new OpenFileDialog() {
                RestoreDirectory = true,
                Filter = "PNG Files (*.png)|*.png|JPEG Files (*.jpeg)|*.jpeg|JPG Files (*.jpg)|*.jpg"
            };

            if(dlg.ShowDialog() == true) {
                vm_.CurDetailItem.IconValue = dlg.FileName;
            }
        }

        private void HandleItemDetailResetImage(object sender, MouseButtonEventArgs e)
        {
            vm_.CurDetailItem.IconValue = string.Empty;
        }

        private void HandleHotKeyInputKeyDown(object sender, KeyEventArgs e)
        {
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            switch (key)
            {
                case Key.Tab:
                case Key.LeftShift:
                case Key.RightShift:
                case Key.LeftCtrl:
                case Key.RightCtrl:
                case Key.LeftAlt:
                case Key.RightAlt:
                case Key.RWin:
                case Key.LWin:
                    return;
            }

            e.Handled = true;

            var modifier = Keyboard.Modifiers;
            if (modifier == ModifierKeys.None && key == Key.Back)
            {
                vm_.Settings.ActivePanelHotKey = string.Empty;
            }
            else if(modifier != ModifierKeys.None)
            {
                KeyGesture gesture = new KeyGesture(key, modifier);
                vm_.Settings.ActivePanelHotKey = key_gesture_converter.ConvertToString(gesture);
            }
        }

        private void HandleItemDetailSelectTarget(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            if (dlg.ShowDialog() == true)
            {
                vm_.CurDetailItem.TargetValue = dlg.FileName;
            }
        }

        private void HandleItemDetailSelectWorkingDir(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok) {
                vm_.CurDetailItem.WorkingDirValue = dlg.FileName;
            }
        }

        private void HandleOpenConfigClicked(object sender, RoutedEventArgs e)
        {
            vm_.SettingsOpened = false;
            DoRunHandyItem( new HandyItem { TargetValue= $"{System.IO.Path.GetDirectoryName(Application.ResourceAssembly.Location)}\\{vm_.CfgFileName}" } );
        }

        private void HandleLocateAppClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("Explorer", $"/select,{Application.ResourceAssembly.Location}");
                vm_.SettingsOpened = false;
                CheckClose();
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Error(ex.Message, "定位失败");
            }
        }
    }
}
