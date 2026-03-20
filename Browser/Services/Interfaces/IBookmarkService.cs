// Browser/Services/Interfaces/IBookmarkService.cs
using Browser.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Browser.Services.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de gestión de marcadores.
    /// Motivo: Define el contrato para las operaciones CRUD de marcadores.
    /// Usar una interfaz promueve la inyección de dependencias y la modularidad,
    /// permitiendo que las ViewModels dependan de una abstracción en lugar de una implementación concreta.
    /// Esto facilita el testing y la futura sustitución de la lógica de almacenamiento.
    /// </summary>
    public interface IBookmarkService
    {
        /// <summary>
        /// Obtiene todos los marcadores guardados de forma asíncrona.
        /// </summary>
        /// <returns>Una colección de marcadores.</returns>
        Task<IEnumerable<Bookmark>> GetBookmarksAsync();

        /// <summary>
        /// Añade un nuevo marcador de forma asíncrona.
        /// </summary>
        /// <param name="bookmark">El marcador a añadir.</param>
        Task AddBookmarkAsync(Bookmark bookmark);

        /// <summary>
        /// Actualiza un marcador existente de forma asíncrona.
        /// </summary>
        /// <param name="bookmark">El marcador a actualizar.</param>
        Task UpdateBookmarkAsync(Bookmark bookmark);

        /// <summary>
        /// Elimina un marcador de forma asíncrona.
        /// </summary>
        /// <param name="bookmarkId">El GUID del marcador a eliminar.</param>
        Task DeleteBookmarkAsync(System.Guid bookmarkId);
    }
}
