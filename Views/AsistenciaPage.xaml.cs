using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Examen.Services;
using Microsoft.Maui.Devices.Sensors;

namespace Examen.Views
{
    public partial class AsistenciaPage : ContentPage, IQueryAttributable
    {
        private SheetsService _sheetsService;
        private CameraService camara;
        private string? correoActual;

        public AsistenciaPage()
        {
            InitializeComponent();
            _sheetsService = SheetsService.Instance;
            camara = new CameraService();

            // Mostrar indicador de modo debug si está activo
            DebugModeLabel.IsVisible = SheetsService.DebugMode;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.ContainsKey("correo"))
            {
                // Decodificar el correo (arregla el %40 -> @)
                var correoValue = query["correo"]?.ToString();
                if (!string.IsNullOrEmpty(correoValue))
                {
                    correoActual = Uri.UnescapeDataString(correoValue);
                    CorreoLabel.Text = $"Correo: {correoActual}";

                    // Cargar el estado del registro
                    _ = CargarEstadoRegistroAsync();
                }
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (!string.IsNullOrWhiteSpace(correoActual))
            {
                _ = CargarEstadoRegistroAsync();
            }
        }

        private async Task CargarEstadoRegistroAsync()
        {
            if (string.IsNullOrWhiteSpace(correoActual))
                return;

            try
            {
                SetLoading(true);
                await _sheetsService.InitAsync();

                var (estado, _) = await _sheetsService.GetEstadoRegistroHoyAsync(correoActual);

                switch (estado)
                {
                    case SheetsService.EstadoRegistro.SinRegistro:
                        EstadoLabel.Text = "📋 Sin registro hoy\nPresiona el botón para registrar tu ENTRADA";
                        EstadoFrame.BackgroundColor = Color.FromArgb("#E3F2FD"); // Azul claro
                        EstadoFrame.BorderColor = Color.FromArgb("#2196F3");
                        BtnRegistrar.Text = "Registrar ENTRADA";
                        BtnRegistrar.IsEnabled = true;
                        break;

                    case SheetsService.EstadoRegistro.SoloEntrada:
                        EstadoLabel.Text = "✅ ENTRADA registrada\nPresiona el botón para registrar tu SALIDA";
                        EstadoFrame.BackgroundColor = Color.FromArgb("#FFF3E0"); // Naranja claro
                        EstadoFrame.BorderColor = Color.FromArgb("#FF9800");
                        BtnRegistrar.Text = "Registrar SALIDA";
                        BtnRegistrar.IsEnabled = true;
                        break;

                    case SheetsService.EstadoRegistro.EntradaYSalida:
                        EstadoLabel.Text = "🎉 ENTRADA y SALIDA registradas\nYa completaste tu registro de hoy.\nContacta a un administrador si necesitas cambios.";
                        EstadoFrame.BackgroundColor = Color.FromArgb("#E8F5E9"); // Verde claro
                        EstadoFrame.BorderColor = Color.FromArgb("#4CAF50");
                        BtnRegistrar.Text = "Registro Completo";
                        BtnRegistrar.IsEnabled = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                EstadoLabel.Text = $"Error al cargar estado: {ex.Message}";
                EstadoFrame.BackgroundColor = Color.FromArgb("#FFEBEE");
                EstadoFrame.BorderColor = Color.FromArgb("#F44336");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void OnRegistrarAsistenciaClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(correoActual))
            {
                MensajeLabel.Text = "No se puede registrar asistencia: correo no válido.";
                MensajeLabel.TextColor = Colors.Red;
                return;
            }

            // Mostrar indicador de carga
            SetLoading(true);
            MensajeLabel.Text = "";

            try
            {
                // 1. Verificar conexión a internet
                if (!await _sheetsService.HasInternetConnectionAsync())
                {
                    MensajeLabel.Text = "No hay conexión a internet. No se puede registrar la asistencia.";
                    MensajeLabel.TextColor = Colors.Red;
                    SetLoading(false);
                    return;
                }

                // 2. Inicializar servicio de Sheets
                await _sheetsService.InitAsync();

                // 3. Obtener ubicación
                var location = await Geolocation.GetLastKnownLocationAsync() ?? await Geolocation.GetLocationAsync();
                if (location == null)
                {
                    MensajeLabel.Text = "No se pudo obtener la ubicación.";
                    MensajeLabel.TextColor = Colors.Red;
                    SetLoading(false);
                    return;
                }

                // 4. Tomar foto (opcional - si falla, continúa sin foto)
                string? fotoBase64 = null;
                try
                {
                    fotoBase64 = await camara.TomarFotoComoBase64Async();
                }
                catch (Exception fotoEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error capturando foto: {fotoEx.Message}");
                    // Continuar sin foto
                }

                // 5. Registrar en Google Sheets (automáticamente detecta entrada/salida)
                var (exito, mensaje) = await _sheetsService.RegistrarAsistenciaAutoAsync(
                    correoActual,
                    location.Latitude,
                    location.Longitude);

                // 6. Mostrar resultado
                MensajeLabel.TextColor = exito ? Colors.Green : Colors.Orange;
                MensajeLabel.Text = mensaje;

                // 7. Mostrar foto si se capturó
                if (!string.IsNullOrEmpty(fotoBase64))
                {
                    FotoImage.Source = ImageSource.FromStream(() =>
                        new System.IO.MemoryStream(Convert.FromBase64String(fotoBase64)));
                    FotoImage.IsVisible = true;
                }

                // 8. Recargar estado
                await CargarEstadoRegistroAsync();
            }
            catch (Exception ex)
            {
                MensajeLabel.Text = $"Error al registrar asistencia: {ex.Message}";
                MensajeLabel.TextColor = Colors.Red;
                System.Diagnostics.Debug.WriteLine($"Error completo: {ex}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void SetLoading(bool isLoading)
        {
            LoadingIndicator.IsRunning = isLoading;
            LoadingIndicator.IsVisible = isLoading;
            BtnRegistrar.IsEnabled = !isLoading;
        }
    }
}