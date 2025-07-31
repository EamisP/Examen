
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Examen.Models;
using Examen.Services;

namespace Examen.Views
{
    public partial class CRUDUbicacionesPage : ContentPage
    {
        private SQLiteService db;
        private UbicacionPermitida ubicacionSeleccionada;

        public CRUDUbicacionesPage()
        {
            InitializeComponent();
            db = new SQLiteService();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await db.InitAsync();
            await CargarUbicaciones();
        }

        private async Task CargarUbicaciones()
        {
            try
            {
                var lista = await db.GetUbicacionesAsync();
                UbicacionesCollection.ItemsSource = lista;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error al cargar ubicaciones: " + ex.Message, "OK");
            }
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NombreLugarEntry.Text) ||
                string.IsNullOrWhiteSpace(LatitudEntry.Text) ||
                string.IsNullOrWhiteSpace(LongitudEntry.Text) ||
                string.IsNullOrWhiteSpace(RadioEntry.Text))
            {
                await DisplayAlert("Error", "Todos los campos son obligatorios.", "OK");
                return;
            }

            if (!double.TryParse(LatitudEntry.Text, out double latitud) ||
                !double.TryParse(LongitudEntry.Text, out double longitud) ||
                !double.TryParse(RadioEntry.Text, out double radio))
            {
                await DisplayAlert("Error", "Latitud, longitud y radio deben ser números válidos.", "OK");
                return;
            }

            if (ubicacionSeleccionada != null)
            {
                ubicacionSeleccionada.NombreLugar = NombreLugarEntry.Text;
                ubicacionSeleccionada.Latitud = latitud;
                ubicacionSeleccionada.Longitud = longitud;
                ubicacionSeleccionada.RadioMetros = radio;
                await db.UpdateUbicacionAsync(ubicacionSeleccionada);
                ubicacionSeleccionada = null;
            }
            else
            {
                var nueva = new UbicacionPermitida
                {
                    NombreLugar = NombreLugarEntry.Text,
                    Latitud = latitud,
                    Longitud = longitud,
                    RadioMetros = radio
                };
                await db.InsertarUbicacionAsync(nueva);
            }

            await CargarUbicaciones();
            LimpiarFormulario();
        }

        private async void OnEliminarClicked(object sender, EventArgs e)
        {
            var item = (sender as Button)?.CommandParameter as UbicacionPermitida;
            if (item != null)
            {
                await db.EliminarUbicacionAsync(item);
                await CargarUbicaciones();
                LimpiarFormulario();
            }
        }

        private void OnSeleccionado(object sender, SelectionChangedEventArgs e)
        {
            ubicacionSeleccionada = e.CurrentSelection.Count > 0 ? e.CurrentSelection[0] as UbicacionPermitida : null;
            if (ubicacionSeleccionada != null)
            {
                NombreLugarEntry.Text = ubicacionSeleccionada.NombreLugar;
                LatitudEntry.Text = ubicacionSeleccionada.Latitud.ToString();
                LongitudEntry.Text = ubicacionSeleccionada.Longitud.ToString();
                RadioEntry.Text = ubicacionSeleccionada.RadioMetros.ToString();
            }
        }

        private void OnLimpiarClicked(object sender, EventArgs e)
        {
            LimpiarFormulario();
        }

        private void LimpiarFormulario()
        {
            NombreLugarEntry.Text = string.Empty;
            LatitudEntry.Text = string.Empty;
            LongitudEntry.Text = string.Empty;
            RadioEntry.Text = string.Empty;
            UbicacionesCollection.SelectedItem = null;
            ubicacionSeleccionada = null;
        }
    }
}