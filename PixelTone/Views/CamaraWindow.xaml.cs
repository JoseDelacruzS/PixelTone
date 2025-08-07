using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;

namespace PixelTone.Views
{
    public partial class CamaraWindow : Window
    {
        private VideoCaptureDevice _videoSource;
        private bool _isCameraActive = false;
        private int _frameCounter = 0;
        private Color? _selectedColor = null;
        private float _colorTolerance = 0.2f;

        public CamaraWindow()
        {
            InitializeComponent();
            this.Closed += CamaraWindow_Closed; // Manejar el cierre de la ventana
        }

        // Método para minimizar la ventana
        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void InicioButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
        private void RevertirButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Restablecer el color seleccionado a null
                _selectedColor = null;

                // Actualizar la interfaz de usuario
                cameraImage.Dispatcher.Invoke(() =>
                {
                    // Si la cámara está activa, mostrar el fotograma original sin filtro
                    if (_videoSource != null && _videoSource.IsRunning)
                    {
                        MessageBox.Show("Se han revertido los cambios del filtro de color.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                });

                Debug.WriteLine("[DEBUG] Cambios del filtro de color revertidos.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Error al revertir los cambios: {ex.Message}");
                MessageBox.Show($"Error al revertir los cambios: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para manejar el clic en el botón "Activar Cámara"
        private void ActivarCamaraButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"[DEBUG] Estado actual de la cámara: {_isCameraActive}");

            if (_isCameraActive)
            {
                Debug.WriteLine("[DEBUG] Se va a detener la cámara.");
                StopCamera();
                ActivarCamaraTextBlock.Text = "Activar Cámara";
                cameraImage.Source = null; // Limpiar el área de la cámara
                MessageBox.Show("Cámara desactivada", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                Debug.WriteLine("[DEBUG] Se intentará iniciar la cámara.");
                if (InitializeCamera())
                {
                    Debug.WriteLine("[DEBUG] Cámara activada correctamente.");
                    ActivarCamaraTextBlock.Text = "Desactivar Cámara";
                    MessageBox.Show("Cámara activada", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    Debug.WriteLine("[ERROR] No se pudo activar la cámara.");
                    MessageBox.Show("No se pudo activar la cámara", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private bool InitializeCamera()
        {
            try
            {
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Count == 0)
                {
                    MessageBox.Show("No se encontró ninguna cámara.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                Debug.WriteLine($"Número de cámaras detectadas: {videoDevices.Count}");

                // Construimos la lista de cámaras para que el usuario elija
                string[] cameraNames = videoDevices.Cast<FilterInfo>().Select(d => d.Name).ToArray();
                string cameraList = string.Join("\n", cameraNames.Select((name, index) => $"{index}: {name}"));

                // Mostrar opciones de cámaras disponibles
                string input = Microsoft.VisualBasic.Interaction.InputBox(
                    $"Selecciona el número de la cámara:\n\n{cameraList}",
                    "Seleccionar Cámara",
                    "0" // Valor por defecto
                );

                if (!int.TryParse(input, out int selectedIndex) || selectedIndex < 0 || selectedIndex >= videoDevices.Count)
                {
                    MessageBox.Show("Selección no válida.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Detener cualquier cámara en uso antes de iniciar la nueva
                StopCamera();

                // Seleccionar la cámara elegida por el usuario
                _videoSource = new VideoCaptureDevice(videoDevices[selectedIndex].MonikerString);

                // Verificar si ya estaba suscrito antes de agregarlo
                _videoSource.NewFrame -= VideoSource_NewFrame;
                _videoSource.NewFrame += VideoSource_NewFrame;

                _videoSource.Start();

                _isCameraActive = true; // Marcar que la cámara está activa
                Debug.WriteLine($"[DEBUG] Cámara seleccionada y activada correctamente: {cameraNames[selectedIndex]}");
                MessageBox.Show($"Cámara activada: {cameraNames[selectedIndex]}", "Información", MessageBoxButton.OK, MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar la cámara: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"[ERROR] {ex.Message}");
                return false;
            }
        }
        private void StopCamera()
        {
            try
            {
                Debug.WriteLine("[DEBUG] Intentando detener la cámara...");

                if (_videoSource != null && _videoSource.IsRunning)
                {
                    Debug.WriteLine("[DEBUG] Enviando señal para detener la cámara.");
                    _videoSource.SignalToStop();
                    _videoSource.WaitForStop(); // Asegurar que se detiene antes de limpiar referencias

                    Debug.WriteLine("[DEBUG] Eliminando evento de nuevos fotogramas.");
                    _videoSource.NewFrame -= VideoSource_NewFrame;
                    _videoSource = null;
                }

                _isCameraActive = false;
                Debug.WriteLine("[DEBUG] Cámara detenida correctamente.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Error al detener la cámara: {ex.Message}");
                MessageBox.Show($"Error al detener la cámara: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Manejar el cierre de la ventana
        private void CamaraWindow_Closed(object sender, EventArgs e)
        {
            Debug.WriteLine("[DEBUG] Ventana cerrada, deteniendo cámara.");
            StopCamera();
        }
        //private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        //{
        //    try
        //    {
        //        Debug.WriteLine("Nuevo fotograma recibido");

        //        var bitmap = (System.Drawing.Bitmap)eventArgs.Frame.Clone();

        //        // Asegurar que la UI se actualice en el hilo correcto
        //        cameraImage.Dispatcher.Invoke(() =>
        //        {
        //            var bitmapImage = ConvertBitmapToBitmapImage(bitmap);
        //            cameraImage.Source = bitmapImage;

        //            // Actualizar el histograma cada 10 fotogramas
        //            _frameCounter++;
        //            if (_frameCounter % 20 == 0) // Actualizar cada 20 fotogramas
        //            {
        //                string canalSeleccionado = (CanalComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "RGB";
        //                var histogramaImage = GenerarHistograma(bitmap, canalSeleccionado);
        //                Histograma.Source = histogramaImage;
        //            }

        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Error al mostrar el fotograma: {ex.Message}");
        //    }
        //}
        private BitmapImage ConvertBitmapToBitmapImage(System.Drawing.Bitmap bitmap)
        {
            try
            {
                using var memory = new System.IO.MemoryStream();
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // Asegurar que sea seguro para múltiples hilos

                Debug.WriteLine("[DEBUG] Bitmap convertido a BitmapImage correctamente.");
                return bitmapImage;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Error al convertir Bitmap a BitmapImage: {ex.Message}");
                return null;
            }
        }

        // Histograma 
        private BitmapImage GenerarHistograma(System.Drawing.Bitmap bitmap, string canal)
        {
            try
            {
                // Crear arrays para almacenar los histogramas de cada canal
                int[] histogramaR = new int[256];
                int[] histogramaG = new int[256];
                int[] histogramaB = new int[256];

                // Bloquear los datos del bitmap para acceder a los píxeles
                var rect = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
                var bitmapData = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

                int bytesPerPixel = System.Drawing.Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                int stride = bitmapData.Stride;
                byte[] pixelData = new byte[stride * bitmap.Height];

                // Copiar los datos del bitmap a un array
                System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, pixelData, 0, pixelData.Length);
                bitmap.UnlockBits(bitmapData);

                // Calcular histogramas para cada canal
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        int index = y * stride + x * bytesPerPixel;

                        histogramaB[pixelData[index]]++;     // Canal azul
                        histogramaG[pixelData[index + 1]]++; // Canal verde
                        histogramaR[pixelData[index + 2]]++; // Canal rojo
                    }
                }

                // Crear imagen para representar el histograma
                int histWidth = 400;
                int histHeight = 150;
                using var histImage = new System.Drawing.Bitmap(histWidth, histHeight);

                // Encontrar el valor máximo para normalizar
                int maxGlobal = Math.Max(histogramaR.Max(), Math.Max(histogramaG.Max(), histogramaB.Max()));

                using (var g = System.Drawing.Graphics.FromImage(histImage))
                {
                    g.Clear(System.Drawing.Color.Black);

                    // Dibujar colores sólidos para cada canal
                    for (int x = 0; x < histWidth; x++)
                    {
                        int index = x * 256 / histWidth;

                        // Normalizar la altura de las barras
                        int barHeightR = (int)((histogramaR[index] / (float)maxGlobal) * histHeight);
                        int barHeightG = (int)((histogramaG[index] / (float)maxGlobal) * histHeight);
                        int barHeightB = (int)((histogramaB[index] / (float)maxGlobal) * histHeight);

                        for (int y = histHeight - 1; y >= 0; y--)
                        {
                            var color = System.Drawing.Color.Black;

                            if (canal == "R" && y >= histHeight - barHeightR)
                                color = System.Drawing.Color.Red;
                            else if (canal == "G" && y >= histHeight - barHeightG)
                                color = System.Drawing.Color.Green;
                            else if (canal == "B" && y >= histHeight - barHeightB)
                                color = System.Drawing.Color.Blue;
                            else if (canal == "RGB")
                            {
                                bool inRed = y >= histHeight - barHeightR;
                                bool inGreen = y >= histHeight - barHeightG;
                                bool inBlue = y >= histHeight - barHeightB;

                                if (inRed && inGreen && inBlue)
                                    color = System.Drawing.Color.White; 
                                else if (inRed)
                                    color = System.Drawing.Color.Red;
                                else if (inGreen)
                                    color = System.Drawing.Color.Green;
                                else if (inBlue)
                                    color = System.Drawing.Color.Blue;
                            }

                            histImage.SetPixel(x, y, color);
                        }
                    }
                }

                // Convertir el histograma a BitmapImage
                return ConvertBitmapToBitmapImage(histImage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Error al generar el histograma: {ex.Message}");
                return null;
            }
        }
        private void CanalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_videoSource != null && _videoSource.IsRunning)
            {
                Debug.WriteLine("Canal cambiado, actualizando histograma...");
            }
        }

        //Selector de color
        private void ColorCanvas_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            if (e.NewValue.HasValue)
            {
                _selectedColor = e.NewValue.Value;
                Debug.WriteLine($"Color seleccionado: {_selectedColor}");
            }
            else
            {
                _selectedColor = null;
            }
        }
        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                using (var bitmap = (System.Drawing.Bitmap)eventArgs.Frame.Clone())
                {
                    // Aplicar filtro si hay un color seleccionado
                    if (_selectedColor.HasValue)
                    {
                        ApplyColorFilter(bitmap, _selectedColor.Value, _colorTolerance);
                    }

                    // Actualizar la interfaz de usuario
                    cameraImage.Dispatcher.Invoke(() =>
                    {
                        var bitmapImage = ConvertBitmapToBitmapImage(bitmap);
                        cameraImage.Source = bitmapImage;

                        // Actualizar histograma periódicamente
                        _frameCounter++;
                        if (_frameCounter % 20 == 0)
                        {
                            string canalSeleccionado = (CanalComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "RGB";
                            var histogramaImage = GenerarHistograma(bitmap, canalSeleccionado);
                            Histograma.Source = histogramaImage;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al procesar el fotograma: {ex.Message}");
            }
        }
        private void ApplyColorFilter(System.Drawing.Bitmap bitmap, Color selectedColor, float tolerance)
        {
            // Bloquear los bits del bitmap para procesamiento
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                int stride = bitmapData.Stride;
                int bytesPerPixel = 4; // BGRA formato de 32 bits
                int totalBytes = stride * bitmapData.Height;

                // Copiar los datos del bitmap a un array
                byte[] pixelData = new byte[totalBytes];
                System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, pixelData, 0, totalBytes);

                // Convertir el color seleccionado a HSL para comparación
                var selectedHsl = RgbToHsl(
                    selectedColor.R / 255f,
                    selectedColor.G / 255f,
                    selectedColor.B / 255f);

                for (int y = 0; y < bitmapData.Height; y++)
                {
                    for (int x = 0; x < bitmapData.Width; x++)
                    {
                        int index = y * stride + x * bytesPerPixel;

                        byte b = pixelData[index];
                        byte g = pixelData[index + 1];
                        byte r = pixelData[index + 2];

                        // Convertir el píxel actual a HSL
                        var pixelHsl = RgbToHsl(r / 255f, g / 255f, b / 255f);

                        // Calcular diferencia en el matiz (hue)
                        float hueDifference = Math.Abs(pixelHsl.H - selectedHsl.H);
                        if (hueDifference > 0.5f) hueDifference = 1f - hueDifference;

                        // Determinar si el píxel coincide con el color seleccionado
                        bool isMatch = hueDifference <= tolerance &&
                                       Math.Abs(pixelHsl.S - selectedHsl.S) <= tolerance;

                        if (!isMatch)
                        {
                            // Convertir a escala de grises
                            byte gray = (byte)(0.299 * r + 0.587 * g + 0.114 * b);
                            pixelData[index] = gray;     // B
                            pixelData[index + 1] = gray; // G
                            pixelData[index + 2] = gray; // R
                        }
                    }
                }

                // Copiar los datos modificados de vuelta al bitmap
                System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, bitmapData.Scan0, totalBytes);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }
        private struct HslColor
        {
            public float H; // Hue (0-1)
            public float S; // Saturation (0-1)
            public float L; // Lightness (0-1)
        }
        private HslColor RgbToHsl(float r, float g, float b)
        {
            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float h, s, l = (max + min) / 2f;

            if (max == min)
            {
                h = s = 0f; // Achromatic
            }
            else
            {
                float d = max - min;
                s = l > 0.5f ? d / (2f - max - min) : d / (max + min);

                if (max == r) h = (g - b) / d + (g < b ? 6f : 0f);
                else if (max == g) h = (b - r) / d + 2f;
                else h = (r - g) / d + 4f;

                h /= 6f;
            }

            return new HslColor { H = h, S = s, L = l };
        }

    }
}