using System.Windows;

namespace JustFOV
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int minFOV = 1;
        private const int maxFOV = 90;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = new Model();

            SetFov.Click += SetFov_Click;

            FOVSlider.Minimum = minFOV;
            FOVSlider.Maximum = maxFOV;
        }

        private void SetFov_Click(object sender, RoutedEventArgs e)
        {
            float FOVValue;
            if (float.TryParse(FOVText.Text, out FOVValue))
            {
                var model = DataContext as Model;
                if (FOVValue < minFOV || FOVValue > 90)
                {
                    MessageBox.Show("FOV should be between 1 and 90", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                else
                {
                    if (model != null) model.Fov = FOVValue;
                }
            }
            else
            {
                MessageBox.Show("Invalid FOV supplied", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}