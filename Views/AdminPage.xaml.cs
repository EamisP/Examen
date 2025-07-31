

using Microsoft.Maui.Controls;

namespace Examen.Views
{
    public partial class AdminPage : ContentPage
    {
        public AdminPage()
        {
            InitializeComponent();
        }

        private async void OnUsuariosClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//CRUDUsuariosPage");
        }

        private async void OnUbicacionesClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//CRUDUbicacionesPage");
        }

        private async void OnAsistenciasClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//ConsultaAsistenciaPage");
        }
    }
}