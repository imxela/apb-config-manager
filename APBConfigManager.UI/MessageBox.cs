using System.Collections.Generic;
using System.Threading.Tasks;

using Avalonia.Controls;

using MessageBox.Avalonia.Models;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;

namespace APBConfigManager.UI
{
    public enum MessageBoxIconType
    {
        None,
        Battery,
        Database,
        Error,
        Folder,
        Forbidden,
        Info,
        Plus,
        Question,
        Setting,
        SpeakerLess,
        SpeakerMore,
        Stop,
        Stopwatch,
        Success,
        Warning,
        Wifi
    }

    public class MessageBoxFactory
    {
        private string _title;
        private string _message;

        private MessageBoxIconType _icon;

        private List<ButtonDefinition> _buttons;

        public MessageBoxFactory()
        {
            _title = string.Empty;
            _message = string.Empty;
            _icon = MessageBoxIconType.None;

            _buttons = new List<ButtonDefinition>();
        }

        public MessageBoxFactory Title(string title)
        {
            _title = title;
            return this;
        }

        public MessageBoxFactory Message(string message)
        {
            _message = message;
            return this;
        }

        public MessageBoxFactory Icon(MessageBoxIconType icon)
        {
            _icon = icon;
            return this;
        }

        /// <summary>
        /// Adds a button to the dialog. Can be called multiple times to
        /// create multiple buttons.
        /// </summary>
        public MessageBoxFactory Button(string text, bool isDefault = false, bool isCancel = false)
        {
            _buttons.Add(new ButtonDefinition
            {
                Name = text,
                IsDefault = isDefault,
                IsCancel = isCancel
            });

            return this;
        }

        public Task<string> Show(Window owner)
        {
            return MessageBoxManager.GetMessageBoxCustomWindow(
                new MessageBoxCustomParams
                {
                    ContentTitle = _title,
                    ContentMessage = _message,
                    Icon = (Icon)_icon,
                    ButtonDefinitions = _buttons.ToArray(),
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                }).ShowDialog(owner);
        }
    }
}
