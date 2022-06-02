using HandyPanel.ViewModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace HandyPanel.Model
{
    class HandyItem : ObservableObject
    {
        private HandyContainer container_;
        public HandyContainer Container
        {
            get { return container_; }
            set { if (SetProperty(ref container_, value)) {
                    Target = null;
                } }
        }

        private string name_value_ = string.Empty;
        public string NameValue
        {
            get { return name_value_; }
            set { if (SetProperty(ref name_value_, value)) {
                    OnPropertyChanged(nameof(Name));
                } }
        }

        public string Name
        {
            get {
                try
                {
                    if (!string.IsNullOrWhiteSpace(NameValue))
                    {
                        return Container?.VariableSubstitution(NameValue) ?? NameValue;
                    }
                    if (!string.IsNullOrWhiteSpace(Target))
                    {
                        return Path.GetFileNameWithoutExtension(Target);
                    }
                }
                catch (Exception)
                {
                }
                return "[]";
            }
        }

        private string icon_value_ = string.Empty;
        public string IconValue
        {
            get { return icon_value_; }
            set
            {
                if (SetProperty(ref icon_value_, value))
                {
                    if (Icon == null)
                    {
                        OnPropertyChanged(nameof(Icon));
                    }
                    else
                    {
                        Icon = null;
                    }
                }
            }
        }

        private BitmapSource icon_;
        public BitmapSource Icon
        {
            get {
                if (icon_ == null) {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(IconValue))
                        {
                            icon_ = BitmapFrame.Create(new Uri(Container?.VariableSubstitution(IconValue) ?? IconValue, UriKind.RelativeOrAbsolute), BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.None);
                        }
                        else if (Directory.Exists(Target))
                        {
                            var tb = ShellFolder.FromParsingName(Target).Thumbnail;
                            tb.FormatOption = ShellThumbnailFormatOption.IconOnly;
                            icon_ = tb.LargeBitmapSource;
                        }
                        else if (File.Exists(Target))
                        {
                            var tb = ShellFile.FromFilePath(Target).Thumbnail;
                            tb.FormatOption = ShellThumbnailFormatOption.IconOnly;
                            icon_ = tb.LargeBitmapSource;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                return icon_;
            }
            set { SetProperty(ref icon_, value); }
        }

        private string argumments_value_ = string.Empty;
        public string ArgumentsValue
        {
            get { return argumments_value_; }
            set { if (SetProperty(ref argumments_value_, value)) {
                    OnPropertyChanged(nameof(Arguments));
                } }
        }

        public string Arguments
        {
            get { return Container?.VariableSubstitution(ArgumentsValue) ?? ArgumentsValue; }
        }

        private string working_dir_value_ = string.Empty;
        public string WorkingDirValue
        {
            get { return working_dir_value_; }
            set
            {
                if (SetProperty(ref working_dir_value_, value))
                {
                    OnPropertyChanged(nameof(WorkingDir));
                }
            }
        }

        public string WorkingDir
        {
            get { return Container?.VariableSubstitution(WorkingDirValue) ?? WorkingDirValue; }
        }

        private string target_value_ = string.Empty;
        public string TargetValue
        {
            get { return target_value_; }
            set { if (SetProperty(ref target_value_, value)) {
                    Target = null;
                } }
        }

        public string Target
        {
            get {
                try
                {
                    return Path.GetFullPath(Container?.VariableSubstitution(TargetValue) ?? TargetValue);
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }

            set {
                OnPropertyChanged(nameof(Target));
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(IsExecutable));
                if (Icon == null)
                {
                    OnPropertyChanged(nameof(Icon));
                }
                else
                {
                    Icon = null;
                }
            }
        }

        public bool IsExecutable { get {
                try
                {
                    var ext = Path.GetExtension(Target).ToLower();
                    return ext == ".exe" || ext == ".bat" || ext == ".cmd";
                }
                catch (Exception)
                {
                    return false;
                }
            } }
    }

    class HandyContainer : ObservableObject
    {
        public Dictionary<string, string> SubVariables { get; set; }

        private bool selected_ = false;
        public bool Selected
        {
            get { return selected_; }
            set { SetProperty( ref selected_, value); }
        }

        private string name_;
        public string Name
        {
            get { return name_; }
            set { SetProperty(ref name_, value); }
        }

        private ObservableCollection<HandyItem> items_;
        public ObservableCollection<HandyItem> Items
        {
            get { return items_; }
            set { SetProperty(ref items_, value); }
        }

        public HandyContainer()
        {
            SubVariables = new Dictionary<string, string>();
            Name = string.Empty;
            Items = new ObservableCollection<HandyItem>();
            Items.CollectionChanged += Items_CollectionChanged;
        }

        public string VariableSubstitution(string str)
        {
            string rst = str;
            foreach (var sub in SubVariables)
            {
                if (!rst.Contains("${"))
                {
                    break;
                }
                rst = rst.Replace($"${{{sub.Key}}}", sub.Value);
            }
            return rst;
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    (item as HandyItem).Container = this;
                }
            }
        }
    }
}
