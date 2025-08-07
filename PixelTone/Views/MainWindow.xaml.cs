using PixelTone.Views;
using System.Windows;
using System.Windows.Input;

namespace PixelTone
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Evento para abrir la ventana de filtros
        private void BtnFiltros_Click(object sender, RoutedEventArgs e)
        {
            FiltrosWindow filtrosWindow = new FiltrosWindow();
            filtrosWindow.Closed += (s, args) => this.Show(); 
            this.Hide();
            filtrosWindow.Show();
        }

        // Evento para abrir la ventana de cámara
        private void BtnCamara_Click(object sender, RoutedEventArgs e)
        {
            CamaraWindow camaraWindow = new CamaraWindow();
            camaraWindow.Closed += (s, args) => this.Show(); 
            this.Hide();
            camaraWindow.Show();
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {

            this.WindowState = WindowState.Minimized;
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {

            this.Close();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}
