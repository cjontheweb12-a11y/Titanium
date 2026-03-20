// Browser/ViewModels/SettingsViewModel.cs
using Browser.Models;
using Browser.Services.Interfaces;
using Browser.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace Browser.ViewModels
{
    /// <summary>
    /// ViewModel para la página de configuración (SettingsPage.xaml).
    /// Motivo: Proporciona la lógica para mostrar y modificar las configuraciones de la aplicación,
    /// como la página de inicio, el motor de búsqueda y las opciones de inicio.
    /// Adhiere al patrón MVVM.
    /// </summary>
    public class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly IHistoryService _historyService;
        private readonly IDownloadService _downloadService;

        /// <summary>
        /// Colección de opciones de inicio disponibles para la UI.
        /// Motivo: Enlazada a un ComboBox en la UI, permite al usuario seleccionar una opción de inicio.
        /// </summary>
        public List<StartupOption> StartupOptions { get; } = Enum.GetValues(typeof(StartupOption)).Cast<StartupOption>().ToList();

        /// <summary>
        /// Colección de motores de búsqueda disponibles para la UI.
        /// Motivo: Enlazada a un ComboBox en la UI, permite al usuario seleccionar un motor de búsqueda.
        /// </summary>
        public List<SearchEngine> SearchEngines { get; } = Enum.GetValues(typeof(SearchEngine)).Cast<SearchEngine>().ToList();

        private string _homePageUrl;
        /// <summary>
        /// Obtiene o establece la URL de la página de inicio.
        /// Motivo: Enlazada al TextBox para configurar la página de inicio.
        /// </summary>
        public string HomePageUrl
        {
            get => _homePageUrl;
            set => SetProperty(ref _homePageUrl, value);
        }

        private StartupOption _selectedStartupOption;
        /// <summary>
        /// Obtiene o establece la opción de inicio seleccionada.
        /// Motivo: Enlazada al ComboBox de opciones de inicio.
        /// </summary>
        public StartupOption SelectedStartupOption
        {
            get => _selectedStartupOption;
            set => SetProperty(ref _selectedStartupOption, value);
        }

        private SearchEngine _selectedSearchEngine;
        /// <summary>
        /// Obtiene o establece el motor de búsqueda seleccionado.
        /// Motivo: Enlazada al ComboBox de motores de búsqueda.
        /// </summary>
        public SearchEngine SelectedSearchEngine
        {
            get => _selectedSearchEngine;
            set
            {
                if (SetProperty(ref _selectedSearchEngine, value))
                {
                    // Motivo: Habilitar/deshabilitar el TextBox de URL personalizada
                    // cuando se selecciona/deselecciona "Personalizado".
                    RaisePropertyChanged(nameof(IsCustomSearchEngineVisible));
                }
            }
        }

        private string _customSearchEngineUrl;
        /// <summary>
        /// Obtiene o establece la URL del motor de búsqueda personalizado.
        /// Motivo: Enlazada al TextBox para configurar una URL de búsqueda personalizada.
        /// </summary>
        public string CustomSearchEngineUrl
        {
            get => _customSearchEngineUrl;
            set => SetProperty(ref _customSearchEngineUrl, value);
        }

        /// <summary>
        /// Indica si el TextBox para la URL del motor de búsqueda personalizado debe ser visible.
        /// Motivo: Propiedad computada para controlar la visibilidad condicional en la UI.
        /// </summary>
        public bool IsCustomSearchEngineVisible => SelectedSearchEngine == SearchEngine.Custom;

        private bool _isJavaScriptEnabled;
        /// <summary>
        /// Obtiene o establece si JavaScript está habilitado.
        /// Motivo: Enlazada al ToggleSwitch para habilitar/deshabilitar JavaScript.
        /// </summary>
        public bool IsJavaScriptEnabled
        {
            get => _isJavaScriptEnabled;
            set => SetProperty(ref _isJavaScriptEnabled, value);
        }

        /// <summary>
        /// Comando para guardar las configuraciones.
        /// Motivo: Enlazado al botón "Guardar Configuración".
        /// </summary>
        public RelayCommand SaveSettingsCommand { get; }

        /// <summary>
        /// Comando para borrar el caché del navegador.
        /// Motivo: Enlazado al botón "Borrar Caché".
        /// </summary>
        public RelayCommand ClearCacheCommand { get; }

        /// <summary>
        /// Comando para borrar el historial de navegación.
        /// Motivo: Enlazado al botón "Borrar Historial".
        /// </summary>
        public RelayCommand ClearHistoryCommand { get; }

        /// <summary>
        /// Comando para borrar las cookies del navegador.
        /// Motivo: Enlazado al botón "Borrar Cookies".
        /// </summary>
        public RelayCommand ClearCookiesCommand { get; }

        /// <summary>
        /// Constructor de <see cref="SettingsViewModel"/>.
        /// Motivo: Inicializa la ViewModel con las dependencias de servicio necesarias
        /// y configura los comandos.
        /// </summary>
        /// <param name="settingsService">Servicio de configuración de la aplicación.</param>
        /// <param name="historyService">Servicio de historial de navegación.</param>
        /// <param name="downloadService">Servicio de descargas.</param>
        public SettingsViewModel(ISettingsService settingsService, IHistoryService historyService, IDownloadService downloadService)
        {
            _settingsService = settingsService;
            _historyService = historyService;
            _downloadService = downloadService;

            // Motivo: Inicializar comandos.
            SaveSettingsCommand = new RelayCommand(async () => await SaveSettingsAsync());
            ClearCacheCommand = new RelayCommand(async () => await ClearWebDataAsync(WebViewDataKind.DiskCache | WebViewDataKind.IndexedDb | WebViewDataKind.LocalStorage | WebViewDataKind.SessionStorage));
            ClearHistoryCommand = new RelayCommand(async () => await ClearHistoryAsync());
            ClearCookiesCommand = new RelayCommand(async () => await ClearWebDataAsync(WebViewDataKind.Cookies)); // Limited functionality for EdgeHTML

            // Motivo: Cargar las configuraciones actuales al inicializar la ViewModel.
            LoadSettings();
        }

        /// <summary>
        /// Carga las configuraciones actuales de la aplicación en las propiedades de la ViewModel.
        /// Motivo: Sincroniza la UI con el estado persistente de las configuraciones.
        /// </summary>
        private void LoadSettings()
        {
            HomePageUrl = _settingsService.CurrentSettings.HomePageUrl;
            SelectedStartupOption = _settingsService.CurrentSettings.StartupOption;
            SelectedSearchEngine = _settingsService.CurrentSettings.DefaultSearchEngine;
            CustomSearchEngineUrl = _settingsService.CurrentSettings.CustomSearchEngineUrl;
            IsJavaScriptEnabled = _settingsService.CurrentSettings.IsJavaScriptEnabled;
        }

        /// <summary>
        /// Guarda las configuraciones actuales de la ViewModel en el servicio de configuraciones.
        /// Motivo: Persiste los cambios realizados por el usuario en la UI.
        /// </summary>
        private async Task SaveSettingsAsync()
        {
            // Motivo: Validar la URL de la página de inicio antes de guardar.
            if (!Uri.IsWellFormedUriString(HomePageUrl, UriKind.Absolute))
            {
                var dialog = new MessageDialog("La URL de la página de inicio no es válida.", "Error de URL");
                await dialog.ShowAsync();
                return;
            }
            // Motivo: Validar la URL personalizada del motor de búsqueda si está seleccionada.
            if (SelectedSearchEngine == SearchEngine.Custom && !Uri.IsWellFormedUriString(CustomSearchEngineUrl.Replace("{query}", "test"), UriKind.Absolute))
            {
                var dialog = new MessageDialog("La URL del motor de búsqueda personalizado no es válida. Use '{query}' como marcador de posición.", "Error de URL");
                await dialog.ShowAsync();
                return;
            }


            _settingsService.CurrentSettings.HomePageUrl = HomePageUrl;
            _settingsService.CurrentSettings.StartupOption = SelectedStartupOption;
            _settingsService.CurrentSettings.DefaultSearchEngine = SelectedSearchEngine;
            _settingsService.CurrentSettings.CustomSearchEngineUrl = CustomSearchEngineUrl;
            _settingsService.CurrentSettings.IsJavaScriptEnabled = IsJavaScriptEnabled;

            try
            {
                await _settingsService.SaveSettingsAsync();
                var dialog = new MessageDialog("Configuración guardada correctamente.", "Configuración Guardada");
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog($"Error al guardar la configuración: {ex.Message}", "Error");
                await dialog.ShowAsync();
            }
        }

        /// <summary>
        /// Borra el historial de navegación.
        /// Motivo: Delega la acción al servicio de historial.
        /// </summary>
        private async Task ClearHistoryAsync()
        {
            // Motivo: Confirmar la acción para evitar borrados accidentales.
            var confirmDialog = new MessageDialog("¿Está seguro de que desea borrar todo el historial de navegación?", "Confirmar Eliminación");
            confirmDialog.Commands.Add(new UICommand("Sí") { Id = 0 });
            confirmDialog.Commands.Add(new UICommand("No") { Id = 1 });
            confirmDialog.DefaultCommandIndex = 1;
            confirmDialog.CancelCommandIndex = 1;

            var result = await confirmDialog.ShowAsync();

            if ((int)result.Id == 0) // Si el usuario confirma "Sí"
            {
                try
                {
                    await _historyService.ClearHistoryAsync();
                    var dialog = new MessageDialog("El historial de navegación ha sido borrado.", "Historial Borrado");
                    await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    var dialog = new MessageDialog($"Error al borrar el historial: {ex.Message}", "Error");
                    await dialog.ShowAsync();
                }
            }
        }

        /// <summary>
        /// Borra datos web específicos del WebView (caché, cookies, etc.).
        /// Motivo: Permite al usuario limpiar datos de navegación para mejorar la privacidad o resolver problemas.
        /// </summary>
        /// <param name="dataKinds">Los tipos de datos web a borrar.</param>
        private async Task ClearWebDataAsync(WebViewDataKind dataKinds)
        {
            string confirmationMessage = "";
            string successMessage = "";
            switch (dataKinds)
            {
                case WebViewDataKind.DiskCache | WebViewDataKind.IndexedDb | WebViewDataKind.LocalStorage | WebViewDataKind.SessionStorage:
                    confirmationMessage = "¿Está seguro de que desea borrar la caché y los datos de almacenamiento local?";
                    successMessage = "La caché y los datos de almacenamiento local han sido borrados.";
                    break;
                case WebViewDataKind.Cookies:
                    confirmationMessage = "¿Está seguro de que desea borrar las cookies?";
                    successMessage = "Las cookies han sido borradas.";
                    break;
                default:
                    confirmationMessage = "¿Está seguro de que desea borrar los datos seleccionados?";
                    successMessage = "Los datos seleccionados han sido borrados.";
                    break;
            }

            var confirmDialog = new MessageDialog(confirmationMessage, "Confirmar Borrado");
            confirmDialog.Commands.Add(new UICommand("Sí") { Id = 0 });
            confirmDialog.Commands.Add(new UICommand("No") { Id = 1 });
            confirmDialog.DefaultCommandIndex = 1;
            confirmDialog.CancelCommandIndex = 1;

            var result = await confirmDialog.ShowAsync();

            if ((int)result.Id == 0) // Si el usuario confirma "Sí"
            {
                try
                {
                    // Motivo: Utilizar la API WebView para borrar los datos persistentes.
                    // Nota: Para EdgeHTML WebView, el borrado de cookies es un poco limitado y global.
                    await WebView.ClearTemporaryWebDataAsync(dataKinds, DateTimeOffset.MinValue);

                    // Motivo: Si se borran cookies o caché, es posible que también queramos reiniciar el servicio de descargas
                    // si hay datos específicos de sesión allí, pero BackgroundTransfer gestiona sus propias cookies.
                    var dialog = new MessageDialog(successMessage, "Datos Borrados");
                    await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    var dialog = new MessageDialog($"Error al borrar datos web: {ex.Message}", "Error");
                    await dialog.ShowAsync();
                }
            }
        }
    }
}
