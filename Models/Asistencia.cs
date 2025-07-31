using SQLite;
using System;
using Microsoft.Maui.Controls;

namespace Examen.Models
{
    public class Asistencia
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Matricula { get; set; }

        public string Nombre { get; set; }

        public DateTime FechaHora { get; set; }

        public double Latitud { get; set; }

        public double Longitud { get; set; }

        public string FotoBase64 { get; set; }

        [Ignore]
        public ImageSource FotoImagen { get; set; }
    }
}