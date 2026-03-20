// Browser/Services/Implementations/HistoryService.cs
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
    /// Implementación del servicio de gestión del historial de navegación.
    /// Motivo: Proporciona la lógica concreta para añadir, obtener y borrar entradas del historial,
    /// persistiendo los datos en un archivo JSON local.
    /// Esto desacopla la lógica de negocio de la UI y del almacenamiento.
    /// </summary>
    public class HistoryService : IHistoryService
    {
        private const string HistoryFileName = "history.json"; // Motivo: Nombre de archivo para la persistencia.
        private List<HistoryEntry> _historyEntries; // Motivo: Cache en memoria para acceso rápido al historial.
        private readonly SemaphoreLocker _locker = new SemaphoreLocker(); // Motivo: Para evitar condiciones de carrera en operaciones de lectura/escritura asíncronas.

        /// <summary>
        /// Constructor de la clase <see cref="HistoryService"/>.
        /// Motivo: Inicializa el servicio cargando el historial existente al arrancar.
        /// </summary>
        public HistoryService()
        {
            // Motivo: Cargar historial de forma asíncrona en el constructor es una buena práctica,
            // pero el constructor no puede ser async. Se usa .Result para asegurar que la carga se complete
            // antes de que el servicio esté completamente inicializado.
            _historyEntries = LoadHistoryInternalAsync().Result;
        }

        /// <summary>
        /// Carga las entradas del historial desde el archivo JSON de forma asíncrona.
        /// Motivo: Encapsula la lógica de deserialización y manejo de archivos.
        /// </summary>
        /// <returns>Una lista de entradas del historial.</returns>
        private async Task<List<HistoryEntry>> LoadHistoryInternalAsync()
        {
            // Motivo: Proteger el acceso al archivo para evitar conflictos si múltiples operaciones intentan leer/escribir simultáneamente.
            using (await _locker.LockAsync())
            {
                try
                {
                    return await JsonFileStorageHelper.LoadFromJsonAsync<List<HistoryEntry>>(HistoryFileName) ?? new List<HistoryEntry>();
                }
                catch (Exception ex)
                {
                    // Motivo: Manejar errores de carga del archivo (ej. corrupción o no existente).
                    // Devolver una lista vacía permite que la aplicación continúe sin historial,
                    // en lugar de fallar. Se imprime el error para depuración.
                    System.Diagnostics.Debug.WriteLine($"Error loading history: {ex.Message}");
                    return new List<HistoryEntry>();
                }
            }
        }

        /// <summary>
        /// Guarda las entradas del historial en el archivo JSON de forma asíncrona.
        /// Motivo: Encapsula la lógica de serialización y manejo de archivos.
        /// </summary>
        private async Task SaveHistoryInternalAsync()
        {
            // Motivo: Proteger el acceso al archivo para evitar conflictos si múltiples operaciones intentan leer/escribir simultáneamente.
            using (await _locker.LockAsync())
            {
                try
                {
                    await JsonFileStorageHelper.SaveAsJsonAsync(_historyEntries, HistoryFileName);
                }
                catch (Exception ex)
                {
                    // Motivo: Manejar errores de guardado del archivo.
                    // Esto evita que un fallo en la persistencia bloquee la aplicación.
                    System.Diagnostics.Debug.WriteLine($"Error saving history: {ex.Message}");
                    // Podría ser útil notificar al usuario en una aplicación real.
                }
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task<IEnumerable<HistoryEntry>> GetHistoryAsync()
        {
            // Motivo: Devolver una copia para evitar que el consumidor modifique directamente la caché interna.
            // La lista se ordena por fecha de visita de forma descendente para mostrar los más recientes primero.
            return (await _locker.LockAsync(async () => _historyEntries.OrderByDescending(h => h.Timestamp).ToList())).Value;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task AddHistoryEntryAsync(string title, string url)
        {
            // Motivo: Evitar agregar entradas de historial duplicadas en rápida sucesión para la misma URL.
            // Esto mejora la calidad del historial al reducir el "ruido".
            using (await _locker.LockAsync())
            {
                if (string.IsNullOrWhiteSpace(url)) return; // No agregar URLs vacías o nulas.

                // Verificar si la última entrada es la misma URL dentro de un corto período.
                // Esto es para evitar entradas duplicadas cuando el WebView navega a la misma URL varias veces (ej. redirecciones).
                var lastEntry = _historyEntries.OrderByDescending(h => h.Timestamp).FirstOrDefault();
                if (lastEntry != null && lastEntry.Url == url && (DateTime.Now - lastEntry.Timestamp).TotalSeconds < 5)
                {
                    // Motivo: Actualizar el timestamp de la última entrada en lugar de añadir una nueva si es la misma URL.
                    // Esto consolida las visitas rápidas a la misma página.
                    lastEntry.Timestamp = DateTime.Now;
                    lastEntry.Title = title; // Asegurar que el título se actualice también.
                }
                else
                {
                    // Motivo: Añadir una nueva entrada si no hay una duplicada reciente.
                    _historyEntries.Add(new HistoryEntry
                    {
                        Title = string.IsNullOrEmpty(title) ? url : title, // Usar la URL si el título está vacío.
                        Url = url,
                        Timestamp = DateTime.Now
                    });
                }
                await SaveHistoryInternalAsync();
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task ClearHistoryAsync()
        {
            // Motivo: Asegurar que las operaciones de modificación de la lista sean thread-safe.
            using (await _locker.LockAsync())
            {
                _historyEntries.Clear();
                await SaveHistoryInternalAsync();
            }
        }
    }
}
