// Browser/Services/Implementations/SettingsService.cs
using Browser.Helpers;
using Browser.Models;
using Browser.Services.Interfaces;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace Browser.Services.Implementations
{
    /// <summary>
    /// Implementación del servicio de gestión de configuraciones de la aplicación.
    /// Motivo: Proporciona la lógica para cargar y guardar las configuraciones de la aplicación
    /// en el almacenamiento local de UWP (ApplicationData.Current.LocalSettings o LocalFolder).
    /// Centraliza el acceso y la persistencia de las configuraciones.
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private const string SettingsFileName = "appsettings.json"; // Motivo: Nombre del archivo para persistir las configuraciones complejas.
        private AppSettings _currentSettings; // Motivo: Cache en memoria para acceso rápido a las configuraciones.
        private readonly SemaphoreLocker _locker = new SemaphoreLocker(); // Motivo: Para evitar condiciones de carrera en operaciones de lectura/escritura asíncronas.

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public AppSettings CurrentSettings
        {
            get
            {
                // Motivo: Asegurarse de que las configuraciones se carguen si se accede antes de LoadSettingsAsync.
                // Esto proporciona una experiencia robusta aunque no sea el patrón ideal de inicialización.
                if (_currentSettings == null)
                {
                    LoadSettingsAsync().Wait(); // Bloquear hasta que las configuraciones estén cargadas. No es ideal en UI thread, pero por simplicidad para este ejemplo.
                                                // En una app real, se aseguraría que LoadSettingsAsync se llame en App.xaml.cs o similar.
                }
                return _currentSettings;
            }
        }

        /// <summary>
        /// Constructor de la clase <see cref="SettingsService"/>.
        /// Motivo: Inicializa la instancia del servicio, preparando para cargar o establecer las configuraciones.
        /// </summary>
        public SettingsService()
        {
            _currentSettings = new AppSettings(); // Inicializar con valores predeterminados.
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task LoadSettingsAsync()
        {
            // Motivo: Proteger el acceso al archivo para evitar conflictos si múltiples operaciones intentan leer/escribir simultáneamente.
            using (await _locker.LockAsync())
            {
                try
                {
                    // Motivo: Cargar las configuraciones desde un archivo JSON.
                    // Esto permite almacenar objetos complejos que no caben en ApplicationData.Current.LocalSettings.
                    var settings = await JsonFileStorageHelper.LoadFromJsonAsync<AppSettings>(SettingsFileName);
                    if (settings != null)
                    {
                        _currentSettings = settings;
                    }
                    else
                    {
                        // Motivo: Si no hay configuraciones guardadas, usar los valores predeterminados.
                        _currentSettings = new AppSettings();
                        await SaveSettingsAsync(); // Guardar los predeterminados para futuras cargas.
                    }
                }
                catch (Exception ex)
                {
                    // Motivo: Manejar errores de carga del archivo (ej. corrupción o no existente).
                    // Volver a los valores predeterminados y registrar el error para depuración.
                    System.Diagnostics.Debug.WriteLine($"Error loading app settings: {ex.Message}. Using default settings.");
                    _currentSettings = new AppSettings();
                    await SaveSettingsAsync(); // Intentar guardar los valores predeterminados.
                }
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task SaveSettingsAsync()
        {
            // Motivo: Proteger el acceso al archivo para evitar conflictos si múltiples operaciones intentan leer/escribir simultáneamente.
            using (await _locker.LockAsync())
            {
                try
                {
                    // Motivo: Guardar el objeto AppSettings completo en un archivo JSON.
                    await JsonFileStorageHelper.SaveAsJsonAsync(_currentSettings, SettingsFileName);
                }
                catch (Exception ex)
                {
                    // Motivo: Manejar errores de guardado del archivo.
                    // Esto evita que un fallo en la persistencia bloquee la aplicación.
                    System.Diagnostics.Debug.WriteLine($"Error saving app settings: {ex.Message}");
                    // Podría ser útil notificar al usuario en una aplicación real.
                }
            }
        }
    }
}
