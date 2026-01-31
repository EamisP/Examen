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
        private string correoActual;

        public AsistenciaPage()
        {
            InitializeComponent();
            _sheetsService = SheetsService.Instance;
            camara = new CameraService();

            // Mostrar indicador de modo debug si está activo
            DebugModeLabel.IsVisible = SheetsService.DebugMode;

            // Seleccionar ENTRADA por defecto
            TipoRegistroPicker.SelectedIndex = 0;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.ContainsKey("correo"))
            {
                correoActual = query["correo"].ToString();
                CorreoLabel.Text = $"Correo: {correoActual}";
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

            if (TipoRegistroPicker.SelectedIndex < 0)
            {
                MensajeLabel.Text = "Por favor selecciona el tipo de registro (ENTRADA/SALIDA).";
                MensajeLabel.TextColor = Colors.Red;
                return;
            }

            var tipoRegistro = TipoRegistroPicker.SelectedItem?.ToString() ?? "ENTRADA";

            // Mostrar indicador de carga
            SetLoading(true);

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

                // 3. Verificar si ya tiene registro del mismo tipo hoy
                bool yaRegistrado = await _sheetsService.TieneRegistroHoyAsync(correoActual, tipoRegistro);
                if (yaRegistrado)
                {
                    bool continuar = await DisplayAlert(
                        "Advertencia",
                        $"Ya tienes un registro de {tipoRegistro} hoy. ¿Deseas registrar otro?",
                        "Sí, registrar",
                        "Cancelar");

                    if (!continuar)
                    {
                        SetLoading(false);
                        return;
                    }
                }

                // 4. Obtener ubicación
                var location = await Geolocation.GetLastKnownLocationAsync() ?? await Geolocation.GetLocationAsync();
                if (location == null)
                {
                    MensajeLabel.Text = "No se pudo obtener la ubicación.";
                    MensajeLabel.TextColor = Colors.Red;
                    SetLoading(false);
                    return;
                }

                // 5. Tomar foto (opcional - si falla, continúa sin foto)
                string fotoBase64 = null;
                try
                {
                    fotoBase64 = await camara.TomarFotoComoBase64Async();
                }
                catch (Exception fotoEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error capturando foto: {fotoEx.Message}");
                    // Continuar sin foto
                }

                // 6. Registrar en Google Sheets
                await _sheetsService.RegistrarAsistenciaAsync(
                    correoActual,
                    tipoRegistro,
                    location.Latitude,
                    location.Longitude);

                // 7. Mostrar éxito
                MensajeLabel.TextColor = Colors.Green;
                MensajeLabel.Text = $"✅ {tipoRegistro} registrada correctamente a las {DateTime.Now:HH:mm:ss}";

                // Mostrar foto si se capturó
                if (!string.IsNullOrEmpty(fotoBase64))
                {
                    FotoImage.Source = ImageSource.FromStream(() =>
                        new System.IO.MemoryStream(Convert.FromBase64String(fotoBase64)));
                    FotoImage.IsVisible = true;
                }
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
            TipoRegistroPicker.IsEnabled = !isLoading;
        }
    }
}