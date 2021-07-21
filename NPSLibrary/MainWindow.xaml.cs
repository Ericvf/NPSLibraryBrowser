using System.ComponentModel.Composition;
using System.Windows;

namespace NPSLibrary
{
    public partial class MainWindow : Window
    {
        [Import]
        public IMainViewModel? MainViewModel { get; set; }

        public MainWindow()
        {
            Composition.Initialize(this);

            InitializeComponent();

            DataContext = MainViewModel;

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (MainViewModel != null)
            {
                await MainViewModel.Load();
            }
        }
    }
}
