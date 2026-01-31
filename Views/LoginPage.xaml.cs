using Microsoft.Maui.Controls;
using Examen.Services;

namespace Examen.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private async void OnRegistrarClicked(object sender, EventArgs e)
        {
            var correo = CorreoEntry.Text?.Trim();

            // Ocultar error previo
            ErrorLabel.IsVisible = false;

            // Validación: no vacío
            if (string.IsNullOrWhiteSpace(correo))
            {
                ShowError("Por favor ingresa tu correo corporativo.");
                return;
            }

            // Validación: debe terminar en @exploraie.com
            if (!SheetsService.ValidarCorreoCorporativo(correo))
            {
                ShowError("El correo debe terminar en @exploraie.com");
                return;
            }

            // Navegar a la página de asistencia con el correo
            await Shell.Current.GoToAsync("//AsistenciaPage?correo=" + Uri.EscapeDataString(correo));
        }

        private void ShowError(string mensaje)
        {
            ErrorLabel.Text = mensaje;
            ErrorLabel.IsVisible = true;
        }

        private async void OnAdminClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//AdminPage");
        }
    }
}