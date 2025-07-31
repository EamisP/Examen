

using Microsoft.Maui.Devices.Sensors;
using Examen.Models;

namespace Examen.Services
{
    public class GeoValidatorService
    {
        public static bool EstaDentroDelRadio(Location actual, UbicacionPermitida permitida)
        {
            if (actual == null || permitida == null)
                return false;

            var destino = new Location(permitida.Latitud, permitida.Longitud);
            double distanciaKm = Location.CalculateDistance(actual, destino, DistanceUnits.Kilometers);

            return distanciaKm * 1000 <= permitida.RadioMetros;
        }
    }
}