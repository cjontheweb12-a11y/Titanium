// Browser/Services/Interfaces/IHistoryService.cs
using Browser.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Browser.Services.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de gestión del historial de navegación.
    /// Motivo: Define el contrato para añadir, obtener y borrar entradas del historial.
    /// Usar una interfaz promueve la inyección de dependencias y la modularidad,
    /// permitiendo que las ViewModels dependan de una abstracción en lugar de una implementación concreta.
    /// Esto facilita el testing y la futura sustitución de la lógica de almacenamiento.
    /// </summary>
    public interface IHistoryService
    {
        /// <summary>
        /// Obtiene todas las entradas del historial de forma asíncrona, ordenadas por fecha descendente.
        /// </summary>
        /// <returns>Una colección de entradas del historial.</returns>
        Task<IEnumerable<HistoryEntry>> GetHistoryAsync();

        /// <summary>
        /// Añade una nueva entrada al historial de forma asíncrona.
        /// </summary>
        /// <param name="title">El título de la página visitada.</param>
        /// <param name="url">La URL de la página visitada.</param>
        Task AddHistoryEntryAsync(string title, string url);

        /// <summary>
        /// Borra todo el historial de navegación de forma asíncrona.
        /// </summary>
        Task ClearHistoryAsync();
    }
}
