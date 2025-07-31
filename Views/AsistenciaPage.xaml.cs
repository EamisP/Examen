using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Examen.Models;
using Examen.Services;
using Microsoft.Maui.Devices.Sensors;

namespace Examen.Views
{
    public partial class AsistenciaPage : ContentPage, IQueryAttributable
    {
        private SQLiteService db;
        private CameraService camara;
        private Usuario usuarioActual;
        private UbicacionPermitida ubicacionPermitida;
        private string matriculaActual;

        public AsistenciaPage()
        {
            InitializeComponent();
            db = new SQLiteService();
            camara = new CameraService();
        }

        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.ContainsKey("matricula"))
            {
                matriculaActual = query["matricula"].ToString();
                MatriculaLabel.Text = $"Matrícula: {matriculaActual}";

                await db.InitAsync();
                usuarioActual = await db.GetUsuarioPorMatriculaAsync(matriculaActual);
                ubicacionPermitida = await db.GetPrimeraUbicacionPermitidaAsync();

                if (usuarioActual != null)
                    NombreLabel.Text = $"Nombre: {usuarioActual.Nombre} {usuarioActual.Apellidos}";
                else
                    NombreLabel.Text = "Usuario no encontrado";
            }
        }

        private async void OnRegistrarAsistenciaClicked(object sender, EventArgs e)
        {
            if (usuarioActual == null || ubicacionPermitida == null)
            {
                MensajeLabel.Text = "No se puede registrar asistencia: datos incompletos.";
                MensajeLabel.TextColor = Colors.Red;
                return;
            }

            try
            {
                var location = await Geolocation.GetLastKnownLocationAsync() ?? await Geolocation.GetLocationAsync();
                if (location == null)
                {
                    MensajeLabel.Text = "No se pudo obtener la ubicación.";
                    MensajeLabel.TextColor = Colors.Red;
                    return;
                }

                double distancia = Location.CalculateDistance(
                    new Location(location.Latitude, location.Longitude),
                    new Location(ubicacionPermitida.Latitud, ubicacionPermitida.Longitud),
                    DistanceUnits.Kilometers);

                if (distancia * 1000 > ubicacionPermitida.RadioMetros)
                {
                    MensajeLabel.Text = "Fuera del área autorizada. No se registró asistencia.";
                    MensajeLabel.TextColor = Colors.Red;
                    return;
                }

                var fotoBase64 = await camara.TomarFotoComoBase64Async();
                if (fotoBase64 == null)
                {
                    MensajeLabel.Text = "No se pudo capturar la foto.";
                    MensajeLabel.TextColor = Colors.Red;
                    return;
                }

                var asistencia = new Asistencia
                {
                    Matricula = usuarioActual.Matricula,
                    Nombre = $"{usuarioActual.Nombre} {usuarioActual.Apellidos}",
                    FechaHora = DateTime.Now,
                    Latitud = location.Latitude,
                    Longitud = location.Longitude,
                    FotoBase64 = fotoBase64
                };

                await db.InsertarAsistenciaAsync(asistencia);

                MensajeLabel.TextColor = Colors.Green;
                MensajeLabel.Text = "Asistencia registrada correctamente.";
                FotoImage.Source = ImageSource.FromStream(() => new System.IO.MemoryStream(Convert.FromBase64String(fotoBase64)));
                FotoImage.IsVisible = true;
            }
            catch (Exception ex)
            {
                MensajeLabel.Text = $"Error al registrar asistencia: {ex.Message}";
                MensajeLabel.TextColor = Colors.Red;
            }
        }
    }
}