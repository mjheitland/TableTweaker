using System.ComponentModel;

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
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!_isClosing)
            {
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
    }
}
