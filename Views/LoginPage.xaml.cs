

using Microsoft.Maui.Controls;

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
            var matricula = MatriculaEntry.Text?.Trim();

            if (string.IsNullOrEmpty(matricula))
            {
                await DisplayAlert("Error", "Por favor ingrese su matrícula.", "OK");
                return;
            }

            // Simulación: matrícula válida si contiene al menos 4 caracteres
            if (matricula.Length < 4)
            {
                await DisplayAlert("No encontrado", "Matrícula no registrada.", "OK");
                return;
            }

            // Aquí podrías guardar matrícula temporalmente con Shell.Current.GoToAsync
            await Shell.Current.GoToAsync("//AsistenciaPage?matricula=" + matricula);
        }

        private async void OnAdminClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//AdminPage");
        }
    }
}