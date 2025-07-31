using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Examen.Models;
using Examen.Services;
using SQLite;


namespace Examen.Views
{
    public partial class CRUDUsuariosPage : ContentPage
    {
        private SQLiteService db;
        private Usuario usuarioSeleccionado = null;

        public CRUDUsuariosPage()
        {
            InitializeComponent();
            db = new SQLiteService();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await db.InitAsync();
            await CargarUsuarios();
        }

        private async Task CargarUsuarios()
        {
            var lista = await db.GetUsuariosAsync();
            UsuariosCollection.ItemsSource = lista;
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MatriculaEntry.Text) ||
                string.IsNullOrWhiteSpace(NombreEntry.Text) ||
                string.IsNullOrWhiteSpace(ApellidosEntry.Text))
            {
                await DisplayAlert("Error", "Todos los campos son obligatorios.", "OK");
                return;
            }

            if (usuarioSeleccionado != null)
            {
                usuarioSeleccionado.Matricula = MatriculaEntry.Text;
                usuarioSeleccionado.Nombre = NombreEntry.Text;
                usuarioSeleccionado.Apellidos = ApellidosEntry.Text;
                usuarioSeleccionado.EsAdministrador = AdminSwitch.IsToggled;

                await db.UpdateUsuarioAsync(usuarioSeleccionado);
            }
            else
            {
                var nuevo = new Usuario
                {
                    Matricula = MatriculaEntry.Text,
                    Nombre = NombreEntry.Text,
                    Apellidos = ApellidosEntry.Text,
                    EsAdministrador = AdminSwitch.IsToggled
                };

                await db.AddUsuarioAsync(nuevo);
            }

            await CargarUsuarios();
            LimpiarFormulario();
        }

        private void OnUsuarioSeleccionado(object sender, SelectionChangedEventArgs e)
        {
            usuarioSeleccionado = e.CurrentSelection.Count > 0 ? e.CurrentSelection[0] as Usuario : null;
            if (usuarioSeleccionado != null)
            {
                MatriculaEntry.Text = usuarioSeleccionado.Matricula;
                NombreEntry.Text = usuarioSeleccionado.Nombre;
                ApellidosEntry.Text = usuarioSeleccionado.Apellidos;
                AdminSwitch.IsToggled = usuarioSeleccionado.EsAdministrador;
            }
        }

        private async void OnEliminarClicked(object sender, EventArgs e)
        {
            var usuario = (sender as Button)?.CommandParameter as Usuario;
            if (usuario != null)
            {
                await db.DeleteUsuarioAsync(usuario);
                await CargarUsuarios();
                LimpiarFormulario();
            }
        }

        private void OnLimpiarClicked(object sender, EventArgs e)
        {
            LimpiarFormulario();
        }

        private void LimpiarFormulario()
        {
            MatriculaEntry.Text = string.Empty;
            NombreEntry.Text = string.Empty;
            ApellidosEntry.Text = string.Empty;
            AdminSwitch.IsToggled = false;
            UsuariosCollection.SelectedItem = null;
            usuarioSeleccionado = null;
        }
    }
}