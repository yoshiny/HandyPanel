using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HandyPanel.Model;
using System.Xml;
using System.IO;
using System.Windows;
using HandyControl.Controls;
using System.Windows.Input;
using Microsoft.Toolkit.Mvvm.Input;
using System.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using IWshRuntimeLibrary;
using HandyPanel.Util;
using System.Xml.Serialization;
using HandyControl.Tools;
using NHotkey;

namespace HandyPanel.ViewModel
{
    class MainViewModel : ObservableRecipient
    {
        private KeyGestureConverter key_gesture_converter_ = new KeyGestureConverter();

        private const string VarSubNameSpace = "VarSubNameSpace";

        protected override void OnActivated()
        {
            Messenger.Register<MainViewModel, SimpleMessage>(this, (r, m) =>
            {
                switch (m.Value)
                {
                    case SimpleMessageDefine.OpenSettingsMesssage:
                        SettingsOpened = true;
                        break;
                    default:
                        break;
                }
            });

            Messenger.Register<MainViewModel, DynamicAddHandyItemMessage>(this, (r, m) =>
            {
                DynamicAddHandyItem(m.Value.Key, m.Value.Value);
            });
        }

        private PanelSettings settings_;
        public PanelSettings Settings
        {
            get { return settings_; }
            set { SetProperty(ref settings_, value); }
        }

        private string settings_file_ = "HandyPanelSettings.xml";
        public string CfgFileName { get; } = "HandyPanelConfig.xml";
        public DateTime CfgLastModifiedTime { get; set; } = DateTime.MinValue;

        private int tab_list_select_index_ = -1;

        public int TabListSelectIndex
        {
            get { return tab_list_select_index_; }
            set { SetProperty(ref tab_list_select_index_, value); }
        }

        public ObservableCollection<HandyContainer> TabList { get; set; } = new ObservableCollection<HandyContainer>();
        public ObservableCollection<HandyContainer> BoxList { get; set; } = new ObservableCollection<HandyContainer>();

        private HandyItem cur_detail_item_;
        private HandyItem dummy_detail_item_ = new HandyItem();
        public HandyItem CurDetailItem
        {
            get { return cur_detail_item_; }
            set { SetProperty(ref cur_detail_item_, value ?? dummy_detail_item_); }
        }

        private bool item_detail_opened_ = false;
        public bool ItemDetailOpened
        {
            get { return item_detail_opened_; }
            set { SetProperty(ref item_detail_opened_, value); }
        }

        private bool settings_opened_ = false;
        public bool SettingsOpened
        {
            get { return settings_opened_; }
            set { SetProperty(ref settings_opened_, value); }
        }

        public MainViewModel() {

            this.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == nameof(SettingsOpened))
                {
                    SaveSettings();
                }
                else if (e.PropertyName == nameof(Settings)) {
                    // 更新设置信息
                    MakeStartUpWithOS(Settings.StartUpWithSystem);
                    MakeHotKeyActivePanel(Settings.HotKeyActivePanel, Settings.ActivePanelHotKey);
                    MakePanelShowInTaskBar();

                    // 监视设置变更
                    Settings.PropertyChanged += (object sender1, PropertyChangedEventArgs e1) =>
                    {
                        if (e1.PropertyName == nameof(Settings.StartUpWithSystem))
                        {
                            MakeStartUpWithOS(Settings.StartUpWithSystem);
                        }
                        else if (e1.PropertyName == nameof(Settings.HotKeyActivePanel))
                        {
                            MakeHotKeyActivePanel(Settings.HotKeyActivePanel, Settings.ActivePanelHotKey);
                        }
                        else if (e1.PropertyName == nameof(Settings.ShowInTaskBar)) {
                            MakePanelShowInTaskBar();
                        }
                    };
                }
            };

            // 加载面板配置
            LoadSettings();
            if (Settings == null) {
                Settings = new PanelSettings();
            }

            CurDetailItem = dummy_detail_item_;
            CheckReloadConfig(!DesignerHelper.IsInDesignMode);
            IsActive = true;
        }

        public void CheckReloadConfig(bool init = false) {
            var file_info = new FileInfo(CfgFileName);
            if (CfgLastModifiedTime != file_info.LastWriteTime)
            {
                List<HandyContainer> tab_list = new List<HandyContainer>(), box_list = new List<HandyContainer>();
                try
                {
                    LoadConfig(tab_list, box_list);
                    int tab_list_cur_select_index = TabListSelectIndex;
                    TabList.Clear();
                    int last_selected = 0;
                    foreach (var item in tab_list)
                    {
                        if (item.Selected) {
                            last_selected = TabList.Count;
                        }
                        TabList.Add(item);
                    }
                    BoxList.Clear();
                    foreach (var item in box_list)
                    {
                        BoxList.Add(item);
                    }
                    CfgLastModifiedTime = file_info.LastWriteTime;
                    if (init)
                    {
                        TabListSelectIndex = last_selected;
                    }
                    else
                    {
                        TabListSelectIndex = tab_list_cur_select_index < TabList.Count ? tab_list_cur_select_index : 0;
                        Growl.Success("配置文件重新加载成功~");
                    }
                }
                catch (Exception ex)
                {
                    if (init)
                    {
                        HandyControl.Controls.MessageBox.Error(ex.Message, "配置文件加载失败");
                    }
                    else {
                        Growl.Error(ex.Message, "配置文件加载失败");
                    }
                }
            }
        }

        public void SaveConfig() {
            var settings = new XmlWriterSettings {
                Indent = true,
                NewLineOnAttributes = false,
                IndentChars = "\t",
                Encoding = Encoding.UTF8,
                WriteEndDocumentOnClose = true
            };

            var list_pair = new List<KeyValuePair<string, ObservableCollection<HandyContainer>>>();
            list_pair.Add(new KeyValuePair<string, ObservableCollection<HandyContainer>>("TabList", TabList));
            list_pair.Add(new KeyValuePair<string, ObservableCollection<HandyContainer>>("BoxList", BoxList));

            using (StreamWriter stream = System.IO.File.CreateText(CfgFileName))
            {
                using (XmlWriter writer = XmlWriter.Create(stream, settings))
                {
                    writer.WriteStartDocument();
                    {
                        writer.WriteStartElement("Config");
                        {
                            writer.WriteAttributeString("xmlns", "sub", null, VarSubNameSpace);
                            foreach (var pair in list_pair)
                            {
                                writer.WriteStartElement(pair.Key);
                                {
                                    foreach (var container in pair.Value)
                                    {
                                        writer.WriteStartElement("Container");
                                        {
                                            writer.WriteAttributeString("Name", container.Name);
                                            writer.WriteAttributeString("Selected", container.Selected.ToString());

                                            foreach (var item in container.SubVariables)
                                            {
                                                writer.WriteAttributeString(item.Key, VarSubNameSpace, item.Value);
                                            }

                                            foreach (var item in container.Items)
                                            {
                                                writer.WriteStartElement("Item");
                                                writer.WriteAttributeString("Name", item.NameValue);
                                                writer.WriteAttributeString("Icon", item.IconValue);
                                                writer.WriteAttributeString("Arguments", item.ArgumentsValue);
                                                writer.WriteAttributeString("WorkingDir", item.WorkingDirValue);
                                                writer.WriteAttributeString("Target", item.TargetValue);
                                                writer.WriteEndElement();
                                            }
                                        }
                                        writer.WriteEndElement();
                                    }
                                }
                                writer.WriteEndElement();
                            }
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndDocument();
                }
            }

            var file_info = new FileInfo(CfgFileName);
            CfgLastModifiedTime = file_info.LastWriteTime;
        }

        private void LoadConfig(List<HandyContainer> tab_list, List<HandyContainer> box_list) {
            using (FileStream fs = System.IO.File.OpenRead(CfgFileName))
            {
                var doc = new XmlDocument();
                doc.Load(fs);
                var cfg_root = doc.DocumentElement;

                Dictionary<string, List<HandyContainer>> list_pair = new Dictionary<string, List<HandyContainer>>()
                {
                    {"TabList/Container", tab_list },
                    {"BoxList/Container", box_list }
                };

                foreach (var it in list_pair)
                {
                    foreach (XmlElement cont_node in cfg_root.SelectNodes(it.Key))
                    {
                        var container = new HandyContainer()
                        {
                            Name = cont_node.GetAttribute("Name"),
                            Selected = cont_node.GetAttribute("Selected").ToLower() == "true"
                        };
                        foreach (XmlAttribute attr in cont_node.Attributes)
                        {
                            if (attr.NamespaceURI == VarSubNameSpace) {
                                container.SubVariables.Add(attr.LocalName, attr.Value);
                            }
                        }

                        foreach (XmlElement item_node in cont_node.SelectNodes("Item"))
                        {
                            var item = new HandyItem()
                            {
                                NameValue = item_node.GetAttribute("Name"),
                                IconValue = item_node.GetAttribute("Icon"),
                                TargetValue = item_node.GetAttribute("Target"),
                                WorkingDirValue = item_node.GetAttribute("WorkingDir"),
                                ArgumentsValue = item_node.GetAttribute("Arguments"),
                            };
                            container.Items.Add(item);
                        }

                        it.Value.Add(container);
                    }
                }
            }
        }

        private void DynamicAddHandyItem(ObservableCollection<HandyItem> items, string file_path) {
            var new_item = new HandyItem() { TargetValue = file_path };
            if (System.IO.Path.GetExtension(file_path).Equals(".lnk", StringComparison.OrdinalIgnoreCase)) {
                // 快捷方式，获取目标信息
                WshShell ws = new WshShell();
                IWshShortcut short_cut = ws.CreateShortcut(file_path) as IWshShortcut;
                new_item.TargetValue = short_cut.TargetPath;
                new_item.ArgumentsValue = short_cut.Arguments;
                new_item.WorkingDirValue = short_cut.WorkingDirectory;
            }
            items.Add(new_item);
            SaveConfig();
        }

        private void MakeStartUpWithOS(bool startup) {
            string app_path = Application.ResourceAssembly.Location;
            string app_name = Path.GetFileNameWithoutExtension(app_path);
            string startup_file = $"{Environment.GetFolderPath(Environment.SpecialFolder.Startup)}\\{app_name}.lnk";
            if (startup)
            {
                if (!System.IO.File.Exists(startup_file)) {
                    WshShell shell = new WshShell();
                    IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(startup_file);
                    shortcut.TargetPath = app_path;
                    shortcut.Arguments = "-startup";
                    shortcut.WorkingDirectory = Path.GetDirectoryName(app_path);
                    shortcut.WindowStyle = 7;
                    shortcut.Save();
                }
            }
            else if (System.IO.File.Exists(startup_file))
            {
                System.IO.File.Delete(startup_file);
            }
        }

        private void MakeHotKeyActivePanel(bool enable, string hotkey_str)
        {
            string active_panel = "active_panel";
            if (enable)
            {
                try
                {
                    var gesture = key_gesture_converter_.ConvertFromString(hotkey_str) as KeyGesture;
                    NHotkey.Wpf.HotkeyManager.Current.AddOrReplace(active_panel, gesture, FireActivePanel);
                }
                catch (Exception)
                {
                }
            }
            else { 
                NHotkey.Wpf.HotkeyManager.Current.Remove(active_panel);
            }
        }

        private void FireActivePanel(object sender, HotkeyEventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new SimpleMessage(SimpleMessageDefine.ActivePanelMessage));
        }

        private void MakePanelShowInTaskBar() {
            WeakReferenceMessenger.Default.Send(new ShowInTaskBarMessage(Settings.ShowInTaskBar));
        }

        private void SaveSettings()
        {
            if (Settings.IsDirty)
            {
                Settings.IsDirty = false;
                XmlSerializer xs = new XmlSerializer(typeof(PanelSettings));
                using (Stream fs = new FileStream(settings_file_, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    xs.Serialize(fs, Settings);
                }
            }
        }

        private void LoadSettings() {
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(PanelSettings));
                using (Stream fs = new FileStream(settings_file_, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    Settings = xs.Deserialize(fs) as PanelSettings;
                }
            }
            catch (Exception)
            {
                // TODO: 给个提示
            }
        }
    }
}
