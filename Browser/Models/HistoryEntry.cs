// Browser/Models/HistoryEntry.cs
using System;

namespace Browser.Models
{
    /// <summary>
    /// Modelo que representa una entrada en el historial de navegación.
    /// Motivo: Define la estructura de datos para un elemento del historial, encapsulando
    /// toda la información relevante en un solo objeto. Esto es parte del patrón Model
    /// y facilita la persistencia y manipulación del historial.
    /// </summary>
    public class HistoryEntry
    {
        /// <summary>
        /// Obtiene o establece un identificador único para la entrada del historial.
        /// Motivo: Permite identificar de forma única cada entrada, lo cual es útil
        /// si se necesita eliminar entradas específicas o realizar un seguimiento.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Obtiene o establece el título de la página visitada.
        /// Motivo: Proporciona una descripción legible para el usuario en la lista del historial.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Obtiene o establece la URL completa de la página visitada.
        /// Motivo: La dirección web real que se visitó.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Obtiene o establece la fecha y hora en que se visitó la página.
        /// Motivo: Permite ordenar las entradas del historial por fecha y mostrar al usuario
        /// cuándo se visitó una página.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
