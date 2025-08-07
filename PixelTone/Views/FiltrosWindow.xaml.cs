using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Runtime.InteropServices;
using Emgu.CV.Util;
using System.Windows.Media;
using System.Diagnostics;

namespace PixelTone.Views
{
    public partial class FiltrosWindow : Window
    {
        private VideoCapture _capture;
        private DispatcherTimer _timer;
        private DispatcherTimer _histogramTimer;

        public FiltrosWindow()
        {
            InitializeComponent();
            AntesVideo.LoadedBehavior = MediaState.Manual;
            AntesVideo.MediaOpened += AntesVideo_MediaOpened; // Nuevo evento

            _histogramTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _histogramTimer.Tick += (sender, e) => ActualizarHistogramas();
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            AntesVideo.Stop();
            this.Close();
        }
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void InicioButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        //Logica de uso 
        private void FiltrosButton_Click(object sender, RoutedEventArgs e)
        {
            FiltrosPopup.IsOpen = !FiltrosPopup.IsOpen;
        }
        private void ImportarButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Archivos multimedia|*.jpg;*.png;*.mp4;*.avi|Todos los archivos|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string extension = System.IO.Path.GetExtension(filePath).ToLower();

                if (extension == ".jpg" || extension == ".png")
                {
                    // Es una imagen
                    AntesImage.Source = new BitmapImage(new Uri(filePath));
                    AntesImage.Visibility = Visibility.Visible;
                    AntesVideo.Visibility = Visibility.Collapsed;
                    VideoButtonsPanel.Visibility = Visibility.Collapsed;
                    ExportarButton.IsEnabled = true;
                    ExportarButton.Visibility = Visibility.Visible;

                    // Actualizar histograma inmediatamente
                    ActualizarHistogramas();
                }
                else if (extension == ".mp4" || extension == ".avi")
                {
                    // Es un video
                    AntesVideo.Source = new Uri(filePath);
                    AntesVideo.Visibility = Visibility.Visible;
                    AntesImage.Visibility = Visibility.Collapsed;
                    VideoButtonsPanel.Visibility = Visibility.Visible;
                    ExportarButton.IsEnabled = false;
                    ExportarButton.Visibility = Visibility.Collapsed;
                    CanalAntesComboBox.SelectedIndex = 0;

                    // El timer se iniciará cuando el video esté listo (MediaOpened)
                }
            }
        }
        private void RevertirButton_Click(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            _capture?.Dispose();
            _histogramTimer.Stop(); // Detener el timer al revertir

            // Limpiar las fuentes de las imágenes y videos
            AntesImage.Source = null;
            AntesVideo.Source = null;
            DespuesImage.Source = null;
            DespuesVideo.Source = null;

            // Limpiar los histogramas
            HistogramaAntes.Source = null;
            HistogramaDespues.Source = null;

            // Ocultar paneles y botones
            VideoButtonsPanel.Visibility = Visibility.Collapsed;
            ExportarButton.IsEnabled = false;
            ExportarButton.Visibility = Visibility.Collapsed;
        }
        private void ExportarButton_Click(object sender, RoutedEventArgs e)
        {
            if (DespuesImage.Source != null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Imágenes|*.jpg;*.png|Todos los archivos|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        BitmapSource bitmapSource = (BitmapSource)DespuesImage.Source;
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                        using (var stream = File.Create(saveFileDialog.FileName))
                        {
                            encoder.Save(stream);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al exportar la imagen: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (AntesVideo.Source != null)
            {
                AntesVideo.Play();
                DespuesVideo.Play();
                SincronizarVideos();
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (AntesVideo.Source != null)
            {
                AntesVideo.Pause();
                DespuesVideo.Pause();
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (AntesVideo.Source != null)
            {
                AntesVideo.Stop();
                DespuesVideo.Stop();
            }
        }

        private void SincronizarVideos()
        {
            if (DespuesVideo.Source != null)
            {
                DespuesVideo.Position = AntesVideo.Position;
            }
        }

        // Lógica de filtros
        private void FiltrosListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AntesImage.Source == null && AntesVideo.Source == null)
            {
                MessageBox.Show("Por favor, carga una imagen o video antes de aplicar un filtro.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                FiltrosListBox.SelectedItem = null;
                return;
            }

            // Detener el procesamiento actual
            _timer?.Stop();
            _capture?.Dispose();

            // Si se ha seleccionado un filtro
            if (FiltrosListBox.SelectedItem != null)
            {
                string filtroSeleccionado = (FiltrosListBox.SelectedItem as ListBoxItem).Content.ToString();

                // Aplicar el nuevo filtro después de detener el procesamiento actual
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    switch (filtroSeleccionado)
                    {
                        case "Pixelado":
                            if (AntesImage.Source != null)
                                AplicarFiltroPixelado();
                            else if (AntesVideo.Source != null)
                                AplicarFiltroPixeladoAVideo();
                            break;

                        case "Negativo":
                            if (AntesImage.Source != null)
                                AplicarFiltroNegativo();
                            else if (AntesVideo.Source != null)
                                AplicarFiltroNegativoAVideo();
                            break;

                        case "Degradado":
                            if (AntesImage.Source != null)
                                AplicarFiltroDegradado();
                            else if (AntesVideo.Source != null)
                                AplicarFiltroDegradadoLinealAVideo();
                            break;

                        case "Aberración Cromática":
                            if (AntesImage.Source != null)
                                AplicarFiltroAberracionCromatica();
                            else if (AntesVideo.Source != null)
                                AplicarFiltroAberracionCromaticaAVideo();
                            break;

                        case "Brillo":
                            if (AntesImage.Source != null)
                                AplicarFiltroBrillo(50);
                            else if (AntesVideo.Source != null)
                                AplicarFiltroBrilloAVideo(50);
                            break;

                        case "Ruido":
                            if (AntesImage.Source != null)
                                AplicarFiltroRuido(50);
                            else if (AntesVideo.Source != null)
                                AplicarFiltroRuidoAVideo(50);
                            break;

                        case "Umbral Adaptativo":
                            if (AntesImage.Source != null)
                                AplicarFiltroUmbralAdaptativo(15, 10); // Tamaño de vecindario = 15, constante C = 10
                            else if (AntesVideo.Source != null)
                                AplicarFiltroUmbralAdaptativoAVideo(15, 10); // Tamaño de vecindario = 15, constante C = 10
                            break;

                        case "Rojo":
                            if (AntesImage.Source != null)
                                AplicarFiltroRojo();
                            else if (AntesVideo.Source != null)
                                AplicarFiltroRojoAVideo();
                            break;

                        case "Verde":
                            if (AntesImage.Source != null)
                                AplicarFiltroVerde();
                            else if (AntesVideo.Source != null)
                                AplicarFiltroVerdeAVideo();
                            break;

                        case "Azul":
                            if (AntesImage.Source != null)
                                AplicarFiltroAzul();
                            else if (AntesVideo.Source != null)
                                AplicarFiltroAzulAVideo();
                            break;

                        case "Sobel":
                            if (AntesImage.Source != null)
                                AplicarFiltroSobel();
                            else if (AntesVideo.Source != null)
                                AplicarFiltroSobelVideo();
                            break;
                    }
                    // Actualizar el histograma del "Después"
                    ActualizarHistogramas();
                }), DispatcherPriority.Background);
            }
        }

        //Imagenes ---------------------------------------------------------------
        private byte[] GetPixels(WriteableBitmap bitmap)
        {
            int stride = bitmap.PixelWidth * 4;
            byte[] pixels = new byte[bitmap.PixelHeight * stride];
            bitmap.CopyPixels(pixels, stride, 0);
            return pixels;
        }
        private void SetPixels(WriteableBitmap bitmap, byte[] pixels)
        {
            int stride = bitmap.PixelWidth * 4;
            bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), pixels, stride, 0);
        }
        private System.Windows.Media.Color GetPixel(byte[] pixels, int stride, int x, int y)
        {
            int index = y * stride + 4 * x;
            return System.Windows.Media.Color.FromRgb(pixels[index + 2], pixels[index + 1], pixels[index]);
        }
        private void SetPixel(byte[] pixels, int stride, int x, int y, System.Windows.Media.Color color)
        {
            int index = y * stride + 4 * x;
            pixels[index] = color.B;
            pixels[index + 1] = color.G;
            pixels[index + 2] = color.R;
        }
        private void AplicarFiltroColor(Func<System.Windows.Media.Color, System.Windows.Media.Color> transformacion)
        {
            if (AntesImage.Source is BitmapImage bitmapImage)
            {
                WriteableBitmap writeableBitmap = new WriteableBitmap(bitmapImage);
                byte[] pixels = GetPixels(writeableBitmap);
                int stride = writeableBitmap.PixelWidth * 4;

                for (int y = 0; y < writeableBitmap.PixelHeight; y++)
                {
                    for (int x = 0; x < writeableBitmap.PixelWidth; x++)
                    {
                        System.Windows.Media.Color color = GetPixel(pixels, stride, x, y);
                        System.Windows.Media.Color nuevoColor = transformacion(color);
                        SetPixel(pixels, stride, x, y, nuevoColor);
                    }
                }

                SetPixels(writeableBitmap, pixels);
                DespuesImage.Source = writeableBitmap;
                DespuesImage.Visibility = Visibility.Visible;
                DespuesVideo.Visibility = Visibility.Collapsed;
            }
        }

        //Pixeleado 
        private void AplicarFiltroPixelado()
        {
            if (AntesImage.Source is BitmapSource bitmapImage)
            {
                WriteableBitmap writeableBitmap = new WriteableBitmap(bitmapImage);
                byte[] pixels = GetPixels(writeableBitmap);
                int stride = writeableBitmap.PixelWidth * 4;
                int ancho = writeableBitmap.PixelWidth;
                int alto = writeableBitmap.PixelHeight;

                int pixelSize = 10;

                for (int y = 0; y < alto; y += pixelSize)
                {
                    for (int x = 0; x < ancho; x += pixelSize)
                    {
                        System.Windows.Media.Color promedio = GetPromedioColor(pixels, stride, x, y, pixelSize, ancho, alto);

                        for (int dy = 0; dy < pixelSize && y + dy < alto; dy++)
                        {
                            for (int dx = 0; dx < pixelSize && x + dx < ancho; dx++)
                            {
                                SetPixel(pixels, stride, x + dx, y + dy, promedio);
                            }
                        }
                    }
                }

                SetPixels(writeableBitmap, pixels);
                DespuesImage.Source = writeableBitmap;
                DespuesImage.Visibility = Visibility.Visible;
                DespuesVideo.Visibility = Visibility.Collapsed;
            }
        }
        private System.Windows.Media.Color GetPromedioColor(byte[] pixels, int stride, int startX, int startY, int pixelSize, int ancho, int alto)
        {
            int r = 0, g = 0, b = 0;
            int count = 0;

            for (int y = startY; y < startY + pixelSize && y < alto; y++)
            {
                for (int x = startX; x < startX + pixelSize && x < ancho; x++)
                {
                    System.Windows.Media.Color color = GetPixel(pixels, stride, x, y);
                    r += color.R;
                    g += color.G;
                    b += color.B;
                    count++;
                }
            }

            if (count > 0)
            {
                r /= count;
                g /= count;
                b /= count;
            }

            return System.Windows.Media.Color.FromRgb((byte)r, (byte)g, (byte)b);
        }
        //Negativo
        private void AplicarFiltroNegativo()
        {
            if (AntesImage.Source is BitmapSource bitmapImage)
            {
                WriteableBitmap writeableBitmap = new WriteableBitmap(bitmapImage);
                int width = writeableBitmap.PixelWidth;
                int height = writeableBitmap.PixelHeight;
                int stride = width * 4;
                byte[] pixels = new byte[height * stride];

                writeableBitmap.CopyPixels(pixels, stride, 0);

                for (int i = 0; i < pixels.Length; i += 4)
                {
                    pixels[i] = (byte)(255 - pixels[i]);       // B
                    pixels[i + 1] = (byte)(255 - pixels[i + 1]); // G
                    pixels[i + 2] = (byte)(255 - pixels[i + 2]); // R
                }

                writeableBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
                DespuesImage.Source = writeableBitmap;
                DespuesImage.Visibility = Visibility.Visible;
                DespuesVideo.Visibility = Visibility.Collapsed;
            }
        }
        //Degradado
        private void AplicarFiltroDegradado()
        {
            if (AntesImage.Source is BitmapImage bitmapImage)
            {
                WriteableBitmap writeableBitmap = new WriteableBitmap(bitmapImage);
                byte[] pixels = GetPixels(writeableBitmap);
                int stride = writeableBitmap.PixelWidth * 4;

                for (int y = 0; y < writeableBitmap.PixelHeight; y++)
                {
                    for (int x = 0; x < writeableBitmap.PixelWidth; x++)
                    {
                        // Calcula el factor de degradado basado en la posición x
                        double factor = (double)x / writeableBitmap.PixelWidth;

                        // Obtiene el color original del píxel
                        System.Windows.Media.Color color = GetPixel(pixels, stride, x, y);

                        // Aplica el degradado (interpolación lineal entre el color original y el blanco)
                        System.Windows.Media.Color degradado = System.Windows.Media.Color.FromRgb(
                            (byte)(color.R + (255 - color.R) * factor),
                            (byte)(color.G + (255 - color.G) * factor),
                            (byte)(color.B + (255 - color.B) * factor)
                        );

                        // Establece el nuevo color en el píxel
                        SetPixel(pixels, stride, x, y, degradado);
                    }
                }

                SetPixels(writeableBitmap, pixels);
                DespuesImage.Source = writeableBitmap;
                DespuesImage.Visibility = Visibility.Visible;
                DespuesVideo.Visibility = Visibility.Collapsed;
            }
        }
        //Aberracion Cromatica
        private void AplicarFiltroAberracionCromatica()
        {
            if (AntesImage.Source is BitmapImage bitmapImage)
            {
                WriteableBitmap writeableBitmap = new WriteableBitmap(bitmapImage);
                byte[] pixels = GetPixels(writeableBitmap);
                int stride = writeableBitmap.PixelWidth * 4;
                int ancho = writeableBitmap.PixelWidth;
                int alto = writeableBitmap.PixelHeight;

                // Define el desplazamiento para cada canal de color
                int desplazamientoRojoX = 6;
                int desplazamientoRojoY = 0;
                int desplazamientoVerdeX = 8;
                int desplazamientoVerdeY = 0;
                int desplazamientoAzulX = -8;
                int desplazamientoAzulY = 8;

                // Aplica el filtro de aberración cromática
                for (int y = 0; y < alto; y++)
                {
                    for (int x = 0; x < ancho; x++)
                    {
                        // Obtiene los colores desplazados para cada canal
                        System.Windows.Media.Color colorRojo = GetPixel(pixels, stride,
                            Math.Clamp(x + desplazamientoRojoX, 0, ancho - 1),
                            Math.Clamp(y + desplazamientoRojoY, 0, alto - 1));

                        System.Windows.Media.Color colorVerde = GetPixel(pixels, stride,
                            Math.Clamp(x + desplazamientoVerdeX, 0, ancho - 1),
                            Math.Clamp(y + desplazamientoVerdeY, 0, alto - 1));

                        System.Windows.Media.Color colorAzul = GetPixel(pixels, stride,
                            Math.Clamp(x + desplazamientoAzulX, 0, ancho - 1),
                            Math.Clamp(y + desplazamientoAzulY, 0, alto - 1));

                        // Combina los canales de color desplazados
                        System.Windows.Media.Color colorFinal = System.Windows.Media.Color.FromRgb(
                            colorRojo.R,
                            colorVerde.G,
                            colorAzul.B
                        );

                        // Establece el nuevo color en el píxel
                        SetPixel(pixels, stride, x, y, colorFinal);
                    }
                }

                SetPixels(writeableBitmap, pixels);
                DespuesImage.Source = writeableBitmap;
                DespuesImage.Visibility = Visibility.Visible;
                DespuesVideo.Visibility = Visibility.Collapsed;
            }
        }
        //Brillo
        private void AplicarFiltroBrillo(int incrementoBrillo)
        {
            AplicarFiltroColor(color =>
            {
                int r = color.R + incrementoBrillo;
                int g = color.G + incrementoBrillo;
                int b = color.B + incrementoBrillo;

                r = Math.Min(255, Math.Max(0, r));
                g = Math.Min(255, Math.Max(0, g));
                b = Math.Min(255, Math.Max(0, b));

                return System.Windows.Media.Color.FromRgb((byte)r, (byte)g, (byte)b);
            });
        }
        //Ruido
        private void AplicarFiltroRuido(int intensidadRuido)
        {
            Random random = new Random();
            AplicarFiltroColor(color =>
            {
                int r = color.R + random.Next(-intensidadRuido, intensidadRuido + 1);
                int g = color.G + random.Next(-intensidadRuido, intensidadRuido + 1);
                int b = color.B + random.Next(-intensidadRuido, intensidadRuido + 1);

                r = Math.Min(255, Math.Max(0, r));
                g = Math.Min(255, Math.Max(0, g));
                b = Math.Min(255, Math.Max(0, b));

                return System.Windows.Media.Color.FromRgb((byte)r, (byte)g, (byte)b);
            });
        }
        //Umbral Adaptativo
        private int[,] CalcularMatrizSumasAcumulativas(byte[] pixels, int stride, int ancho, int alto)
        {
            int[,] sumas = new int[alto, ancho];

            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    byte intensidad = pixels[y * stride + 4 * x];

                    sumas[y, x] = intensidad;
                    if (x > 0) sumas[y, x] += sumas[y, x - 1];
                    if (y > 0) sumas[y, x] += sumas[y - 1, x];
                    if (x > 0 && y > 0) sumas[y, x] -= sumas[y - 1, x - 1];
                }
            }

            return sumas;
        }
        private double CalcularPromedioRegion(int[,] sumas, int x, int y, int tamañoVecindario)
        {
            int x1 = Math.Max(x - tamañoVecindario / 2, 0);
            int y1 = Math.Max(y - tamañoVecindario / 2, 0);
            int x2 = Math.Min(x + tamañoVecindario / 2, sumas.GetLength(1) - 1);
            int y2 = Math.Min(y + tamañoVecindario / 2, sumas.GetLength(0) - 1);

            int area = (x2 - x1 + 1) * (y2 - y1 + 1);
            int suma = sumas[y2, x2];

            if (x1 > 0) suma -= sumas[y2, x1 - 1];
            if (y1 > 0) suma -= sumas[y1 - 1, x2];
            if (x1 > 0 && y1 > 0) suma += sumas[y1 - 1, x1 - 1];

            return (double)suma / area;
        }
        private void AplicarFiltroUmbralAdaptativo(int tamañoVecindario, double constanteC)
        {
            if (AntesImage.Source is BitmapImage bitmapImage)
            {
                WriteableBitmap writeableBitmap = new WriteableBitmap(bitmapImage);
                byte[] pixels = GetPixels(writeableBitmap);
                int stride = writeableBitmap.PixelWidth * 4;
                int ancho = writeableBitmap.PixelWidth;
                int alto = writeableBitmap.PixelHeight;

                for (int i = 0; i < pixels.Length; i += 4)
                {
                    byte intensidad = (byte)(0.299 * pixels[i + 2] + 0.587 * pixels[i + 1] + 0.114 * pixels[i]);
                    pixels[i] = pixels[i + 1] = pixels[i + 2] = intensidad;
                }

                int[,] sumas = CalcularMatrizSumasAcumulativas(pixels, stride, ancho, alto);

                for (int y = 0; y < alto; y++)
                {
                    for (int x = 0; x < ancho; x++)
                    {
                        double umbralLocal = CalcularPromedioRegion(sumas, x, y, tamañoVecindario) - constanteC;
                        byte intensidad = pixels[y * stride + 4 * x];
                        byte nuevoValor = (intensidad > umbralLocal) ? (byte)255 : (byte)0;

                        pixels[y * stride + 4 * x] = nuevoValor;
                        pixels[y * stride + 4 * x + 1] = nuevoValor;
                        pixels[y * stride + 4 * x + 2] = nuevoValor;
                    }
                }

                SetPixels(writeableBitmap, pixels);
                DespuesImage.Source = writeableBitmap;
                DespuesImage.Visibility = Visibility.Visible;
                DespuesVideo.Visibility = Visibility.Collapsed;
            }
        }
        //Rojo
        private void AplicarFiltroRojo()
        {
            AplicarFiltroColor(color => System.Windows.Media.Color.FromRgb(
                color.R,
                (byte)(color.G * 0.3),
                (byte)(color.B * 0.3)
            ));
        }
        //Verde
        private void AplicarFiltroVerde()
        {
            AplicarFiltroColor(color => System.Windows.Media.Color.FromRgb(
                (byte)(color.R * 0.3), // Canal rojo atenuado
                color.G, // Canal verde sin cambios
                (byte)(color.B * 0.3)  // Canal azul atenuado
            ));
        }
        //Azul
        private void AplicarFiltroAzul()
        {
            AplicarFiltroColor(color => System.Windows.Media.Color.FromRgb(
                (byte)(color.R * 0.3), // Canal rojo atenuado
                (byte)(color.G * 0.3), // Canal verde atenuado
                color.B // Canal azul sin cambios
            ));
        }
        //Sobel
        private void AplicarFiltroSobel()
        {
            if (AntesImage.Source is BitmapImage bitmapImage)
            {
                WriteableBitmap writeableBitmap = new WriteableBitmap(bitmapImage);
                byte[] pixels = GetPixels(writeableBitmap);
                int stride = writeableBitmap.PixelWidth * 4;
                int ancho = writeableBitmap.PixelWidth;
                int alto = writeableBitmap.PixelHeight;

                // Matrices Sobel
                int[,] sobelX = new int[,]
                {
            { -1, 0, 1 },
            { -2, 0, 2 },
            { -1, 0, 1 }
                };

                int[,] sobelY = new int[,]
                {
            { -1, -2, -1 },
            {  0,  0,  0 },
            {  1,  2,  1 }
                };

                byte[] resultPixels = new byte[pixels.Length];

                for (int y = 1; y < alto - 1; y++)
                {
                    for (int x = 1; x < ancho - 1; x++)
                    {
                        int gx = 0, gy = 0;

                        for (int ky = -1; ky <= 1; ky++)
                        {
                            for (int kx = -1; kx <= 1; kx++)
                            {
                                int pixelIndex = ((y + ky) * stride) + ((x + kx) * 4);
                                byte intensidad = (byte)(0.299 * pixels[pixelIndex + 2] + 0.587 * pixels[pixelIndex + 1] + 0.114 * pixels[pixelIndex]);

                                gx += sobelX[ky + 1, kx + 1] * intensidad;
                                gy += sobelY[ky + 1, kx + 1] * intensidad;
                            }
                        }

                        int magnitude = (int)Math.Sqrt(gx * gx + gy * gy);
                        magnitude = Math.Min(255, Math.Max(0, magnitude));

                        int resultIndex = (y * stride) + (x * 4);
                        resultPixels[resultIndex] = resultPixels[resultIndex + 1] = resultPixels[resultIndex + 2] = (byte)magnitude;
                        resultPixels[resultIndex + 3] = 255; // Alpha
                    }
                }

                writeableBitmap.WritePixels(new Int32Rect(0, 0, ancho, alto), resultPixels, stride, 0);
                DespuesImage.Source = writeableBitmap;
                DespuesImage.Visibility = Visibility.Visible;
                DespuesVideo.Visibility = Visibility.Collapsed;
            }
        }



        // Video ---------------------------------------------------------------
        // Procesamiento de video con filtros
        private void ProcesarVideoConFiltro(Func<Mat, Mat> applyFilter)
        {
            if (AntesVideo.Source == null) return;

            string videoPath = new Uri(AntesVideo.Source.ToString()).LocalPath;
            _capture = new VideoCapture(videoPath);
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _timer.Tick += (sender, e) => ProcessFrame(sender, e, applyFilter);
            _timer.Start();
        }
        private void ProcessFrame(object sender, EventArgs e, Func<Mat, Mat> applyFilter)
        {
            using (Mat frame = new Mat())
            {
                if (!_capture.Read(frame))
                {
                    _timer.Stop();
                    _capture.Dispose();
                    return;
                }

                using (Mat filteredFrame = applyFilter(frame))
                {
                    DespuesImage.Source = ConvertMatToBitmapImage(filteredFrame);
                    DespuesImage.Visibility = Visibility.Visible;
                    DespuesVideo.Visibility = Visibility.Collapsed;

                }
            }
        }
        private BitmapImage ConvertMatToBitmapImage(Mat mat)
        {
            byte[] imageData = CvInvoke.Imencode(".bmp", mat);
            var bitmapImage = new BitmapImage();
            using (var ms = new MemoryStream(imageData))
            {
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }
            return bitmapImage;
        }


        // Aplicar filtro negativo
        private Mat ApplyNegative(Mat frame)
        {
            Mat result = new Mat();
            CvInvoke.BitwiseNot(frame, result);
            return result;
        }
        // Aplicar filtro pixelado
        private Mat ApplyPixelated(Mat frame, int pixelSize)
        {
            Mat resizedFrame = new Mat();
            CvInvoke.Resize(frame, resizedFrame, new System.Drawing.Size(frame.Cols / pixelSize, frame.Rows / pixelSize), interpolation: Inter.Linear);
            CvInvoke.Resize(resizedFrame, resizedFrame, new System.Drawing.Size(frame.Cols, frame.Rows), interpolation: Inter.Nearest);
            return resizedFrame;
        }
        // Aplicar filtro de degradado
        private Mat ApplyLinearGradient(Mat frame, MCvScalar startColor, MCvScalar endColor, bool horizontal)
        {
            Mat gradientFrame = frame.Clone();
            int width = frame.Cols, height = frame.Rows;

            for (int y = 0; y < height; y++)
            {
                double ratio = horizontal ? (double)y / height : (double)y / width;
                byte b = (byte)(startColor.V0 + (endColor.V0 - startColor.V0) * ratio);
                byte g = (byte)(startColor.V1 + (endColor.V1 - startColor.V1) * ratio);
                byte r = (byte)(startColor.V2 + (endColor.V2 - startColor.V2) * ratio);

                for (int x = 0; x < width; x++)
                {
                    int idx = (y * width + x) * 3;
                    IntPtr dataPtr = gradientFrame.DataPointer + idx;
                    Marshal.WriteByte(dataPtr, 0, (byte)((Marshal.ReadByte(dataPtr) + b) / 2));
                    Marshal.WriteByte(dataPtr, 1, (byte)((Marshal.ReadByte(dataPtr + 1) + g) / 2));
                    Marshal.WriteByte(dataPtr, 2, (byte)((Marshal.ReadByte(dataPtr + 2) + r) / 2));
                }
            }

            return gradientFrame;
        }
        //Aplicar filtro de Aberracion Cromatica
        private Mat ApplyChromaticAberration(Mat frame, int shift)
        {
            // Paso 1: Separar los canales BGR
            using (VectorOfMat channels = new VectorOfMat())
            {
                CvInvoke.Split(frame, channels);

                Mat shiftedRed = new Mat();
                Mat shiftedBlue = new Mat();
                double[] mapMatrixRedData = { 1, 0, shift, 0, 1, 0 };
                Mat mapMatrixRed = new Mat(2, 3, DepthType.Cv64F, 1);
                Marshal.Copy(mapMatrixRedData, 0, mapMatrixRed.DataPointer, mapMatrixRedData.Length);

                double[] mapMatrixBlueData = { 1, 0, -shift, 0, 1, 0 };
                Mat mapMatrixBlue = new Mat(2, 3, DepthType.Cv64F, 1);
                Marshal.Copy(mapMatrixBlueData, 0, mapMatrixBlue.DataPointer, mapMatrixBlueData.Length);

                CvInvoke.WarpAffine(channels[2], shiftedRed, mapMatrixRed, frame.Size); // Desplazar el canal rojo
                CvInvoke.WarpAffine(channels[0], shiftedBlue, mapMatrixBlue, frame.Size); // Desplazar el canal azul

                using (VectorOfMat modifiedChannels = new VectorOfMat(shiftedBlue, channels[1], shiftedRed))
                {
                    Mat result = new Mat();
                    CvInvoke.Merge(modifiedChannels, result);  // Fusionar los canales
                    return result;
                }
            }
        }
        // Aplicar filtro de brillo
        private Mat ApplyBrightness(Mat frame, int brightness)
        {
            Mat result = new Mat();
            frame.ConvertTo(result, DepthType.Cv8U, 1, brightness);
            return result;
        }
        // Aplicar filtro de ruido optimizado
        private Mat ApplyNoise(Mat frame, int noiseIntensity)
        {
            Mat noise = new Mat(frame.Size, DepthType.Cv8U, frame.NumberOfChannels);
            CvInvoke.Randu(noise, new MCvScalar(0), new MCvScalar(noiseIntensity));
            Mat result = new Mat();
            CvInvoke.Add(frame, noise, result);
            return result;
        }
        // Aplicar filtro de umbral adaptativo
        private Mat ApplyAdaptiveThreshold(Mat frame, int blockSize, double C)
        {
            Mat grayFrame = new Mat();
            CvInvoke.CvtColor(frame, grayFrame, ColorConversion.Bgr2Gray);
            Mat result = new Mat();
            CvInvoke.AdaptiveThreshold(grayFrame, result, 255, AdaptiveThresholdType.MeanC, ThresholdType.Binary, blockSize, C);
            return result;
        }
        // Aplicar filtro rojo
        private Mat ApplyRedFilter(Mat frame)
        {
            Mat result = new Mat();
            VectorOfMat channels = new VectorOfMat();
            CvInvoke.Split(frame, channels);
            channels[1].SetTo(new MCvScalar(0.3)); // Atenuar canal verde
            channels[0].SetTo(new MCvScalar(0.3)); // Atenuar canal azul
            CvInvoke.Merge(channels, result);
            return result;
        }
        // Aplicar filtro verde
        private Mat ApplyGreenFilter(Mat frame)
        {
            Mat result = new Mat();
            VectorOfMat channels = new VectorOfMat();
            CvInvoke.Split(frame, channels);
            channels[2].SetTo(new MCvScalar(0.3));
            channels[0].SetTo(new MCvScalar(0.3));
            CvInvoke.Merge(channels, result);
            return result;
        }
        // Aplicar filtro azul
        private Mat ApplyBlueFilter(Mat frame)
        {
            Mat result = new Mat();
            VectorOfMat channels = new VectorOfMat();
            CvInvoke.Split(frame, channels);
            channels[2].SetTo(new MCvScalar(0.3)); // Atenuar canal rojo
            channels[1].SetTo(new MCvScalar(0.3)); // Atenuar canal verde
            CvInvoke.Merge(channels, result);
            return result;
        }
        // Aplicar filtro Sobel
        private Mat ApplySobel(Mat frame)
        {
            Mat grayFrame = new Mat();
            CvInvoke.CvtColor(frame, grayFrame, ColorConversion.Bgr2Gray);

            Mat gradX = new Mat();
            Mat gradY = new Mat();

            // Aplicar Sobel en las direcciones X e Y
            CvInvoke.Sobel(grayFrame, gradX, DepthType.Cv16S, 1, 0, 3);
            CvInvoke.Sobel(grayFrame, gradY, DepthType.Cv16S, 0, 1, 3);

            Mat absGradX = new Mat();
            Mat absGradY = new Mat();

            // Convertir a valores absolutos con escala
            CvInvoke.ConvertScaleAbs(gradX, absGradX, 1.0, 0.0); // Escala = 1.0, Offset = 0.0
            CvInvoke.ConvertScaleAbs(gradY, absGradY, 1.0, 0.0); // Escala = 1.0, Offset = 0.0

            Mat result = new Mat();
            // Combinar gradientes
            CvInvoke.AddWeighted(absGradX, 0.5, absGradY, 0.5, 0, result);

            return result;
        }


        // Métodos para iniciar filtros
        private void AplicarFiltroNegativoAVideo() => ProcesarVideoConFiltro(ApplyNegative);
        private void AplicarFiltroPixeladoAVideo() => ProcesarVideoConFiltro(frame => ApplyPixelated(frame, 10));
        private void AplicarFiltroDegradadoLinealAVideo() => ProcesarVideoConFiltro(frame => ApplyLinearGradient(frame, new MCvScalar(255, 255, 255), new MCvScalar(0, 0, 0), true));
        private void AplicarFiltroAberracionCromaticaAVideo() => ProcesarVideoConFiltro(frame => ApplyChromaticAberration(frame, 15));
        private void AplicarFiltroBrilloAVideo(int brightness) => ProcesarVideoConFiltro(frame => ApplyBrightness(frame, brightness));
        private void AplicarFiltroRuidoAVideo(int noiseIntensity) => ProcesarVideoConFiltro(frame => ApplyNoise(frame, noiseIntensity));
        private void AplicarFiltroUmbralAdaptativoAVideo(int blockSize, double C) => ProcesarVideoConFiltro(frame => ApplyAdaptiveThreshold(frame, blockSize, C));
        private void AplicarFiltroRojoAVideo() => ProcesarVideoConFiltro(ApplyRedFilter);
        private void AplicarFiltroVerdeAVideo() => ProcesarVideoConFiltro(ApplyGreenFilter);
        private void AplicarFiltroAzulAVideo() => ProcesarVideoConFiltro(ApplyBlueFilter);
        private void AplicarFiltroSobelVideo() => ProcesarVideoConFiltro(ApplySobel);

        //Histograma-----------------------------------------
        private void CanalAntesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ActualizarHistogramas();
        }
        private void CanalDespuesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ActualizarHistogramas();
        }
        private void ActualizarHistogramas()
        {
            // Histograma "antes" (imagen o video)
            if (AntesImage.Source is BitmapSource imageSource)
            {
                string canal = (CanalAntesComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (!string.IsNullOrEmpty(canal))
                    HistogramaAntes.Source = GenerarHistograma(imageSource, canal);
            }
            else if (AntesVideo.Source != null && AntesVideo.NaturalDuration.HasTimeSpan)
            {
                // Manejo específico para video
                string canal = (CanalAntesComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (!string.IsNullOrEmpty(canal))
                    CapturarYGenerarHistogramaDeVideo(AntesVideo, canal, HistogramaAntes);
            }

            // Histograma "después" (procesado por filtros)
            if (DespuesImage.Source is BitmapSource processedImage)
            {
                string canal = (CanalDespuesComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (!string.IsNullOrEmpty(canal))
                    HistogramaDespues.Source = GenerarHistograma(processedImage, canal);
            }
        }
        private void CapturarYGenerarHistogramaDeVideo(MediaElement video, string canal, Image histogramaImage)
        {
            if (video.ActualWidth == 0 || video.ActualHeight == 0) return;

            try
            {
                var rtb = new RenderTargetBitmap(
                    (int)video.ActualWidth,
                    (int)video.ActualHeight,
                    96, 96, PixelFormats.Pbgra32);

                rtb.Render(video);
                histogramaImage.Source = GenerarHistograma(rtb, canal);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al capturar frame: {ex.Message}");
            }
        }
        private BitmapImage GenerarHistograma(BitmapSource bitmapSource, string canal)
        {
            // Convertir BitmapSource a WriteableBitmap
            WriteableBitmap writeableBitmap = new WriteableBitmap(bitmapSource);
            byte[] pixels = GetPixels(writeableBitmap);
            int stride = writeableBitmap.PixelWidth * 4;
            int[] histogramaR = new int[256];
            int[] histogramaG = new int[256];
            int[] histogramaB = new int[256];

            // Calcular histograma basado en el canal seleccionado
            for (int y = 0; y < writeableBitmap.PixelHeight; y++)
            {
                for (int x = 0; x < writeableBitmap.PixelWidth; x++)
                {
                    int index = y * stride + 4 * x;
                    histogramaR[pixels[index + 2]]++;
                    histogramaG[pixels[index + 1]]++;
                    histogramaB[pixels[index]]++;
                }
            }

            // Crear imagen del histograma
            int histWidth = 400;
            int histHeight = 150;
            WriteableBitmap histBitmap = new WriteableBitmap(histWidth, histHeight, 96, 96, PixelFormats.Bgr32, null);
            byte[] histPixels = new byte[histWidth * histHeight * 4];

            int maxR = histogramaR.Max();
            int maxG = histogramaG.Max();
            int maxB = histogramaB.Max();

            for (int x = 0; x < histWidth; x++)
            {
                int valorR = histogramaR[x * 256 / histWidth] * histHeight / maxR;
                int valorG = histogramaG[x * 256 / histWidth] * histHeight / maxG;
                int valorB = histogramaB[x * 256 / histWidth] * histHeight / maxB;

                for (int y = histHeight - 1; y >= histHeight - valorR; y--)
                {
                    int index = (y * histWidth + x) * 4;
                    if (canal == "R" || canal == "RGB")
                        histPixels[index + 2] = 255; // R
                }

                for (int y = histHeight - 1; y >= histHeight - valorG; y--)
                {
                    int index = (y * histWidth + x) * 4;
                    if (canal == "G" || canal == "RGB")
                        histPixels[index + 1] = 255; // G
                }

                for (int y = histHeight - 1; y >= histHeight - valorB; y--)
                {
                    int index = (y * histWidth + x) * 4;
                    if (canal == "B" || canal == "RGB")
                        histPixels[index] = 255; // B
                }
            }

            histBitmap.WritePixels(new Int32Rect(0, 0, histWidth, histHeight), histPixels, histWidth * 4, 0);
            return ConvertWriteableBitmapToBitmapImage(histBitmap);
        }
        private BitmapImage ConvertWriteableBitmapToBitmapImage(WriteableBitmap writeableBitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));
                encoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = stream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }
        private void AntesVideo_MediaOpened(object sender, RoutedEventArgs e)
        {
            // Iniciar el timer solo cuando el video esté listo
            _histogramTimer.Start();

            // Forzar una primera actualización
            ActualizarHistogramas();
        }
    }
}

