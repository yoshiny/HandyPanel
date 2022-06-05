using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace HandyPanel.Model
{
    [Serializable]
    public class PanelSettings : ObservableObject
    {
        private bool close_to_tray_ = false;
        public bool CloseToTray
        {
            get { return close_to_tray_; }
            set { SetProperty(ref close_to_tray_, value); }
        }

        private bool hot_key_active_panel_ = false;
        public bool HotKeyActivePanel
        {
            get { return hot_key_active_panel_; }
            set { SetProperty( ref hot_key_active_panel_, value); }
        }

        private string active_panel_hot_key_ = string.Empty;
        public string ActivePanelHotKey
        {
            get { return active_panel_hot_key_; }
            set { SetProperty(ref active_panel_hot_key_, value); }
        }

        private bool double_click_launch_item_ = true;
        public bool DoubleClickLaunchItem
        {
            get { return double_click_launch_item_; }
            set { SetProperty(ref double_click_launch_item_, value); }
        }

        private bool auto_hide_panel_when_item_launched_ = false;
        public bool AutoHidePanelWhenItemLaunched
        {
            get { return auto_hide_panel_when_item_launched_; }
            set { SetProperty(ref auto_hide_panel_when_item_launched_, value); }
        }

        private bool startup_with_system_ = false;
        public bool StartUpWithSystem
        {
            get { return startup_with_system_; }
            set { SetProperty( ref startup_with_system_, value); }
        }

        private bool show_in_taskbar_ = true;
        public bool ShowInTaskBar
        {
            get { return show_in_taskbar_; }
            set { SetProperty( ref show_in_taskbar_, value); }
        }

        private bool hide_on_deactive_ = false;
        public bool HideOnDeactive
        {
            get { return hide_on_deactive_; }
            set { SetProperty( ref hide_on_deactive_,value); }
        }

        private double width = 1444f;
        public double Width
        {
            get { return width; }
            set { SetProperty( ref width, value); }
        }

        private double height = 768f;
        public double Height
        {
            get { return height; }
            set { SetProperty(ref height, value); }
        }


        [XmlIgnore]
        public bool IsDirty { get; set; } = false;

        public PanelSettings() {
            this.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                IsDirty = true;
            };
        }
    }
}
