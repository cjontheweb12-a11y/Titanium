// Browser/Services/Implementations/BookmarkService.cs
using Browser.Helpers;
using Browser.Models;
using Browser.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Browser.Services.Implementations
{
    /// <summary>
    /// Implementación del servicio de gestión de marcadores.
    /// Motivo: Proporciona la lógica concreta para realizar operaciones CRUD (Crear, Leer, Actualizar, Borrar)
    /// sobre los marcadores, persistiendo los datos en un archivo JSON local.
    /// Esto desacopla la lógica de negocio de la UI y del almacenamiento.
    /// </summary>
    public class BookmarkService : IBookmarkService
    {
        private const string BookmarksFileName = "bookmarks.json"; // Motivo: Nombre de archivo para la persistencia.
        private List<Bookmark> _bookmarks; // Motivo: Cache en memoria para acceso rápido a los marcadores.
        private readonly SemaphoreLocker _locker = new SemaphoreLocker(); // Motivo: Para evitar condiciones de carrera en operaciones de lectura/escritura asíncronas.

        /// <summary>
        /// Constructor de la clase <see cref="BookmarkService"/>.
        /// Motivo: Inicializa el servicio cargando los marcadores existentes al arrancar.
        /// </summary>
        public BookmarkService()
        {
            // Motivo: Cargar marcadores de forma asíncrona en el constructor es una buena práctica,
            // pero el constructor no puede ser async. Se usa .Result para asegurar que la carga se complete
            // antes de que el servicio esté completamente inicializado. En un escenario ideal, esto
            // se manejaría con un método de inicialización asíncrono separado y una fase de inicialización de servicios.
            _bookmarks = LoadBookmarksInternalAsync().Result;
        }

        /// <summary>
        /// Carga los marcadores desde el archivo JSON de forma asíncrona.
        /// Motivo: Encapsula la lógica de deserialización y manejo de archivos.
        /// </summary>
        /// <returns>Una lista de marcadores.</returns>
        private async Task<List<Bookmark>> LoadBookmarksInternalAsync()
        {
            // Motivo: Proteger el acceso al archivo para evitar conflictos si múltiples operaciones intentan leer/escribir simultáneamente.
            using (await _locker.LockAsync())
            {
                try
                {
                    return await JsonFileStorageHelper.LoadFromJsonAsync<List<Bookmark>>(BookmarksFileName) ?? new List<Bookmark>();
                }
                catch (Exception ex)
                {
                    // Motivo: Manejar errores de carga del archivo (ej. corrupción o no existente).
                    // Devolver una lista vacía permite que la aplicación continúe sin marcadores,
                    // en lugar de fallar. Se imprime el error para depuración.
                    System.Diagnostics.Debug.WriteLine($"Error loading bookmarks: {ex.Message}");
                    return new List<Bookmark>();
                }
            }
        }

        /// <summary>
        /// Guarda los marcadores en el archivo JSON de forma asíncrona.
        /// Motivo: Encapsula la lógica de serialización y manejo de archivos.
        /// </summary>
        private async Task SaveBookmarksInternalAsync()
        {
            // Motivo: Proteger el acceso al archivo para evitar conflictos si múltiples operaciones intentan leer/escribir simultáneamente.
            using (await _locker.LockAsync())
            {
                try
                {
                    await JsonFileStorageHelper.SaveAsJsonAsync(_bookmarks, BookmarksFileName);
                }
                catch (Exception ex)
                {
                    // Motivo: Manejar errores de guardado del archivo.
                    // Esto evita que un fallo en la persistencia bloquee la aplicación.
                    System.Diagnostics.Debug.WriteLine($"Error saving bookmarks: {ex.Message}");
                    // Podría ser útil notificar al usuario en una aplicación real.
                }
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task<IEnumerable<Bookmark>> GetBookmarksAsync()
        {
            // Motivo: Devolver una copia para evitar que el consumidor modifique directamente la caché interna.
            // La lista se ordena por fecha de adición de forma descendente para mostrar los más recientes primero.
            return (await _locker.LockAsync(async () => _bookmarks.OrderByDescending(b => b.AddedDate).ToList())).Value;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task AddBookmarkAsync(Bookmark bookmark)
        {
            // Motivo: Asegurar que las operaciones de modificación de la lista sean thread-safe.
            using (await _locker.LockAsync())
            {
                _bookmarks.Add(bookmark);
                await SaveBookmarksInternalAsync();
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task UpdateBookmarkAsync(Bookmark bookmark)
        {
            using (await _locker.LockAsync())
            {
                var existingBookmark = _bookmarks.FirstOrDefault(b => b.Id == bookmark.Id);
                if (existingBookmark != null)
                {
                    existingBookmark.Title = bookmark.Title;
                    existingBookmark.Url = bookmark.Url;
                    await SaveBookmarksInternalAsync();
                }
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task DeleteBookmarkAsync(Guid bookmarkId)
        {
            using (await _locker.LockAsync())
            {
                var bookmarkToRemove = _bookmarks.FirstOrDefault(b => b.Id == bookmarkId);
                if (bookmarkToRemove != null)
                {
                    _bookmarks.Remove(bookmarkToRemove);
                    await SaveBookmarksInternalAsync();
                }
            }
        }
    }
}
