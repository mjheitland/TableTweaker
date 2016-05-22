using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Xml.Linq;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace TableTweaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private bool _isClosing;
        private bool _isClosed;

        public MainWindow()
        {
            var viewModel = new MainViewModel();
            DataContext = viewModel;
            InitializeComponent();
            DocumentsPane.ToggleAutoHide();

            LoadWindowLayout();
            LoadDockLayout();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!_isClosing)
            {
                SaveDockLayout();
                SaveWindowLayout();
                Properties.Settings.Default.Save();

                _isClosing = true;
                IsEnabled = false;
                _isClosed = true;
            }
            else
            {
                e.Cancel = !_isClosed;
            }
        }

        private void LoadWindowLayout()
        {
            var bounds = Properties.Settings.Default.WindowBounds;
            if (bounds != new Rect())
            {
                Left = bounds.Left;
                Top = bounds.Top;
                Width = bounds.Width;
                Height = bounds.Height;
            }
            var state = Properties.Settings.Default.WindowState;
            if (state != WindowState.Minimized)
            {
                WindowState = state;
            } 
        }

        private void SaveWindowLayout()
        {
            Properties.Settings.Default.WindowBounds = RestoreBounds;
            Properties.Settings.Default.WindowState = WindowState;
        }

        private void LoadDockLayout()
        {
            var layout = Properties.Settings.Default.DockLayout;
            if (string.IsNullOrEmpty(layout))
                return;

            var serializer = new XmlLayoutSerializer(DockingManager);
            var reader = new StringReader(layout);
            serializer.Deserialize(reader);
        }

        private void SaveDockLayout()
        {
            var serializer = new XmlLayoutSerializer(DockingManager);
            var document = new XDocument();
            using (var writer = document.CreateWriter())
            {
                serializer.Serialize(writer);
            }
            document.Root?.Element("FloatingWindows")?.Remove();
            Properties.Settings.Default.DockLayout = document.ToString();
        }
    }
}
