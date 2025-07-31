using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Examen.Models;
using Examen.Services;
using Microsoft.Maui.Devices.Sensors;

namespace Examen.Views
{
    public partial class ConsultaAsistenciaPage : ContentPage
    {
        private SQLiteService db;

        public ConsultaAsistenciaPage()
        {
            InitializeComponent();
            db = new SQLiteService();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await db.InitAsync();
        }

        private async void OnConsultarClicked(object sender, EventArgs e)
        {
            var fecha = FechaPicker.Date;
            var asistencias = await db.GetAsistenciasPorFechaAsync(fecha);

            // Si las fotos están en Base64, convertimos la propiedad a ImageSource
            foreach (var item in asistencias)
            {
                if (!string.IsNullOrEmpty(item.FotoBase64))
                {
                    byte[] imageBytes = Convert.FromBase64String(item.FotoBase64);
                    item.FotoImagen = ImageSource.FromStream(() => new System.IO.MemoryStream(imageBytes));
                }
            }

            AsistenciasCollection.ItemsSource = asistencias;
        }
    }
}