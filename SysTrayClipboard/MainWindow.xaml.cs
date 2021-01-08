using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;

namespace SysTrayClipboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private ClipboardEntry _selectedEntry;

        public ClipboardEntry SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (_selectedEntry == value) return;

                _selectedEntry = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedEntry)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public BindingList<ClipboardEntry> Entries { get; set; }

        private readonly NotifyIcon _notifyIcon;

        private WindowState _prevState;

        private readonly ContextMenu _contextMenu;

        public MainWindow()
        {
            Entries = new BindingList<ClipboardEntry>();

            _contextMenu = new ContextMenu();
            
            _notifyIcon = new NotifyIcon
            {
                Text = "System Tray Clipboard",
                Icon = Properties.Resources.MainIcon,
                ContextMenu = _contextMenu,
                Visible = true
            };

            _notifyIcon.DoubleClick += (sender, args) =>
            {
                if (IsVisible) return;
                
                Show();
                WindowState = _prevState;
            };

            DataContext = this;

            InitializeComponent();

            // Ran synchronously as app should not show until loaded
            LoadEntries();

            Entries.ListChanged += (sender, args) =>
            {
                SaveEntries();
                UpdateContextMenu();
            };

            UpdateContextMenu();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            _notifyIcon.Dispose();
        }

        public const string EntriesFile = "SysTrayClipboardEntries.json";

        public string GetFullEntriesFile() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), EntriesFile);

        // Only to be called on window load
        public void LoadEntries()
        {
            SelectedEntry = null;
            Entries.Clear();

            var fileName = GetFullEntriesFile();

            if (!File.Exists(fileName)) return;

            var content = File.ReadAllText(fileName);

            if (string.IsNullOrWhiteSpace(content)) return;

            var entries = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);

            if (entries == null) return;

            foreach (var entryData in entries)
            {
                var entry = new ClipboardEntry(entryData.Key, entryData.Value, this);

                entry.PropertyChanged += (sender, args) => SaveEntries();

                Entries.Add(entry);
            }
        }

        public void SaveEntries()
        {
            var entries = Entries.ToDictionary(entry => entry.Title, entry => entry.Content);

            Task.Run(() => SaveEntries(entries));
        }

        public async Task SaveEntries(Dictionary<string, string> entries)
        {
            using var writer = new StreamWriter(GetFullEntriesFile());

            var content = JsonConvert.SerializeObject(entries);

            await writer.WriteAsync(content);
        }

        public void UpdateContextMenu()
        {
            _contextMenu.MenuItems.Clear();

            foreach (var entry in Entries)
            {
                _contextMenu.MenuItems.Add(entry.Title, (sender, args) => SetClipboard((sender as MenuItem)?.Text));
            }
        }

        public void SetClipboard(string entryTitle)
        {
            if (entryTitle == null) return;

            var entry = Entries.FirstOrDefault(x => x.Title == entryTitle);

            if (entry == null) return;

            Clipboard.SetText(entry.Content);
        }

        private void AddEntry_Click(object sender, RoutedEventArgs e)
        {
            var title = EntryTitle.Text;

            if (string.IsNullOrWhiteSpace(title)) return;

            if (Entries.Any(x => x.Title == title)) return;

            var entry = new ClipboardEntry(title, string.Empty, this);

            entry.PropertyChanged += (o, args) => SaveEntries();

            Entries.Add(entry);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
            else
            {
                _prevState = WindowState;
            }
        }
    }
}
