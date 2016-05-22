using System.Windows;

namespace TableTweaker
{
    public partial class App
    {
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            TableTweaker.Properties.Settings.Default.Save();
        }
    }
}
