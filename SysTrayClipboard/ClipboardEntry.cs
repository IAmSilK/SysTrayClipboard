using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using SysTrayClipboard.Annotations;

namespace SysTrayClipboard
{
    public class ClipboardEntry : Grid, INotifyPropertyChanged
    {
        private readonly MainWindow _window;

        public string Title { get; set; }

        private string _content;

        public string Content
        {
            get => _content;
            set
            {
                if (_content == value) return;

                _content = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Content)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ClipboardEntry(string title, string content, MainWindow window)
        {
            _window = window;

            Title = title;
            Content = content;

            Margin = new Thickness(0, 0, 0, 5);

            ColumnDefinitions.Clear();
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(5)});
            ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});

            Children.Add(new Label
            {
                Content = title
            });

            var button = new Button
            {
                Content = "Remove Entry"
            };

            SetColumn(button, 2);

            button.Click += (sender, args) => Delete();

            Children.Add(button);

            MouseLeftButtonUp += (sender, args) => Select();
        }

        private void Delete()
        {
            _window.Entries.Remove(this);

            if (_window.SelectedEntry == this) _window.SelectedEntry = null;
        }

        private void Select()
        {
            _window.SelectedEntry = this;
        }
    }
}
