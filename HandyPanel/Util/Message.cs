using HandyPanel.Model;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandyPanel.Util
{
    enum SimpleMessageDefine
    {
        OpenSettingsMesssage = 1,
        ActivePanelMessage,
    }

    class SimpleMessage : ValueChangedMessage<SimpleMessageDefine> {
        public SimpleMessage(SimpleMessageDefine value) : base(value)
        {
        }
    }

    class ShowInTaskBarMessage : ValueChangedMessage<bool>
    {
        public ShowInTaskBarMessage(bool value) : base(value)
        {
        }
    }

    class DynamicAddHandyItemMessage : ValueChangedMessage<KeyValuePair<ObservableCollection<HandyItem>, string>>
    {
        public DynamicAddHandyItemMessage(KeyValuePair<ObservableCollection<HandyItem>, string> value) : base(value)
        {
        }
    }
}
