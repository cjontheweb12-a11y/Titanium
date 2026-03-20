// Browser/ViewModels/BrowserTabViewModel.cs
using Browser.Helpers;
using Browser.Models;
using Browser.Services.Interfaces;
using Browser.ViewModels.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Web;

namespace Browser.ViewModels
{
    /// <summary>
    /// ViewModel para una sola pestaña del navegador (BrowserTabUserControl).
    /// Motivo: Encapsula la lógica y el estado de una pestaña individual,
    /// incluyendo la navegación del WebView, el progreso de carga, el título y la barra de direcciones.
    /// Adhiere al patrón MVVM.
    /// </summary>
    public class BrowserTabViewModel : ObservableObject, IDisposable
    {
        private readonly ISettingsService _settingsService;
        private readonly IHistoryService _historyService;
        private readonly IDownloadService _downloadService;

        private WebView _webView; // Motivo: Referencia al control WebView de la UI para controlar la navegación.

        private string _title;
        /// <summary>
        /// Obtiene o establece el título de la página actual en la pestaña.
        /// Motivo: Enlazado al encabezado de la pestaña y la barra de direcciones.
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _currentUri;
        /// <summary>
        /// Obtiene o establece la URI actual de la página en la pestaña.
        /// Motivo: La URI real de la página, útil para marcadores, historial, etc.
        /// </summary>
        public string CurrentUri
        {
            get => _currentUri;
            set => SetProperty(ref _currentUri, value);
        }

        private string _addressBarText;
        /// <summary>
        /// Obtiene o establece el texto que se muestra en la barra de direcciones.
        /// Motivo: Esto puede ser una URL o un término de búsqueda.
        /// Enlazado directamente al TextBox de la barra de direcciones.
        /// </summary>
        public string AddressBarText
        {
            get => _addressBarText;
            set => SetProperty(ref _addressBarText, value);
        }

        private double _loadingProgress;
        /// <summary>
        /// Obtiene o establece el progreso de carga de la página (0-100).
        /// Motivo: Enlazado a la barra de progreso de la UI.
        /// </summary>
        public double LoadingProgress
        {
            get => _loadingProgress;
            set => SetProperty(ref _loadingProgress, value);
        }

        private bool _isLoading;
        /// <summary>
        /// Obtiene un valor que indica si la página está cargando.
        /// Motivo: Enlazado a la visibilidad de la barra de progreso y al icono de detener/refrescar.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _canGoBack;
        /// <summary>
        /// Obtiene un valor que indica si el WebView puede navegar hacia atrás.
        /// Motivo: Enlazado al botón "Atrás".
        /// </summary>
        public bool CanGoBack
        {
            get => _canGoBack;
            set => SetProperty(ref _canGoBack, value);
        }

        private bool _canGoForward;
        /// <summary>
        /// Obtiene un valor que indica si el WebView puede navegar hacia adelante.
        /// Motivo: Enlazado al botón "Adelante".
        /// </summary>
        public bool CanGoForward
        {
            get => _canGoForward;
            set => SetProperty(ref _canGoForward, value);
        }

        private bool _isHttps;
        /// <summary>
        /// Obtiene un valor que indica si la página actual utiliza HTTPS.
        /// Motivo: Enlazado al icono de seguridad (candado).
        /// </summary>
        public bool IsHttps
        {
            get => _isHttps;
            set => SetProperty(ref _isHttps, value);
        }

        /// <summary>
        /// Comando para navegar a una URL.
        /// Motivo: Se llama desde MainPage cuando el usuario introduce una URL o un término de búsqueda.
        /// </summary>
        public RelayCommand<string> NavigateCommand { get; }

        /// <summary>
        /// Comando para ir hacia atrás.
        /// Motivo: Enlazado al botón "Atrás".
        /// </summary>
        public RelayCommand GoBackCommand { get; }

        /// <summary>
        /// Comando para ir hacia adelante.
        /// Motivo: Enlazado al botón "Adelante".
        /// </summary>
        public RelayCommand GoForwardCommand { get; }

        /// <summary>
        /// Comando para recargar la página.
        /// Motivo: Enlazado al botón "Recargar".
        /// </summary>
        public RelayCommand RefreshCommand { get; }

        /// <summary>
        /// Comando para detener la carga de la página.
        /// Motivo: Enlazado al botón "Detener".
        /// </summary>
        public RelayCommand StopCommand { get; }

        /// <summary>
        /// Constructor de <see cref="BrowserTabViewModel"/>.
        /// Motivo: Inicializa la ViewModel con las dependencias de servicio necesarias
        /// y configura los comandos. Sigue el principio de Inversión de Control.
        /// </summary>
        /// <param name="settingsService">Servicio de configuración de la aplicación.</param>
        /// <param name="historyService">Servicio de historial de navegación.</param>
        /// <param name="downloadService">Servicio de descargas.</param>
        public BrowserTabViewModel(
            ISettingsService settingsService,
            IHistoryService historyService,
            IDownloadService downloadService)
        {
            _settingsService = settingsService;
            _historyService = historyService;
            _downloadService = downloadService;

            // Motivo: Se asignan los comandos a sus respectivos métodos.
            // Los comandos encapsulan la lógica de la acción y la capacidad de ejecución.
            NavigateCommand = new RelayCommand<string>(url => Navigate(url), url => !string.IsNullOrEmpty(url));
            GoBackCommand = new RelayCommand(GoBack, () => CanGoBack);
            GoForwardCommand = new RelayCommand(GoForward, () => CanGoForward);
            RefreshCommand = new RelayCommand(Refresh);
            StopCommand = new RelayCommand(Stop);

            // Valores iniciales
            Title = "Nueva Pestaña";
            AddressBarText = "";
            CurrentUri = "";
            IsHttps = false;
        }

        /// <summary>
        /// Asocia un control WebView a esta ViewModel.
        /// Motivo: El WebView se crea en la View (BrowserTabUserControl) y se inyecta en la ViewModel.
        /// Esto mantiene la separación MVVM al permitir que la ViewModel controle el WebView
        /// sin que la ViewModel tenga que instanciarlo.
        /// </summary>
        /// <param name="webView">El control WebView a asociar.</param>
        public void SetWebView(WebView webView)
        {
            if (_webView != null)
            {
                // Motivo: Desuscribirse de eventos anteriores para evitar fugas de memoria
                // si la ViewModel se reutiliza o el WebView se reemplaza.
                _webView.NavigationStarting -= WebView_NavigationStarting;
                _webView.ContentLoading -= WebView_ContentLoading;
                _webView.NavigationCompleted -= WebView_NavigationCompleted;
                _webView.NavigationFailed -= WebView_NavigationFailed;
                _webView.NewWindowRequested -= WebView_NewWindowRequested;
                _webView.UnsupportedUriSchemeIdentified -= WebView_UnsupportedUriSchemeIdentified;
                _webView.UnviewableContentIdentified -= WebView_UnviewableContentIdentified;
                _webView.FrameNavigationCompleted -= WebView_FrameNavigationCompleted;
                _webView.ContainsFullScreenElementChanged -= WebView_ContainsFullScreenElementChanged;
                // WebView de EdgeHTML no tiene un método explícito Dispose(), pero se puede limpiar su Source.
                _webView.Source = null;
            }

            _webView = webView;

            if (_webView != null)
            {
                // Motivo: Suscribirse a los eventos del WebView para actualizar el estado de la ViewModel.
                _webView.NavigationStarting += WebView_NavigationStarting;
                _webView.ContentLoading += WebView_ContentLoading;
                _webView.NavigationCompleted += WebView_NavigationCompleted;
                _webView.NavigationFailed += WebView_NavigationFailed;
                _webView.NewWindowRequested += WebView_NewWindowRequested;
                _webView.UnsupportedUriSchemeIdentified += WebView_UnsupportedUriSchemeIdentified;
                _webView.UnviewableContentIdentified += WebView_UnviewableContentIdentified;
                _webView.FrameNavigationCompleted += WebView_FrameNavigationCompleted;
                _webView.ContainsFullScreenElementChanged += WebView_ContainsFullScreenElementChanged;

                // Motivo: Aplicar la configuración de JavaScript del usuario.
                _webView.Settings.IsJavaScriptEnabled = _settingsService.CurrentSettings.IsJavaScriptEnabled;
                // Motivo: Habilitar IndexedDB si es deseado para ciertas funcionalidades web modernas.
                _webView.Settings.IsIndexedDBEnabled = true;
                // Motivo: Habilitar desplazamiento de la WebView.
                _webView.Settings.IsScrollEnabled = true;
            }
        }

        /// <summary>
        /// Navega el WebView a la URL especificada.
        /// Motivo: Este es el método principal para iniciar la navegación.
        /// Se asegura de que la URL sea un URI válido antes de navegar.
        /// </summary>
        /// <param name="url">La URL a navegar.</param>
        private void Navigate(string url)
        {
            if (_webView == null || string.IsNullOrEmpty(url)) return;

            try
            {
                // Motivo: Intentar crear un URI. Si falla, el formato no es válido.
                Uri uri = new Uri(url);
                _webView.Navigate(uri);
            }
            catch (FormatException)
            {
                // Motivo: Si la URL no es válida, mostrar un error amigable.
                Title = "Error de URL";
                CurrentUri = "";
                AddressBarText = url; // Mantener el texto original para que el usuario pueda corregirlo.
                DisplayErrorPage($"No se pudo navegar a '{url}'. Asegúrese de que sea una URL válida.");
            }
            catch (Exception ex)
            {
                // Motivo: Capturar otras excepciones inesperadas durante la navegación.
                Title = "Error de Navegación";
                CurrentUri = "";
                AddressBarText = url;
                DisplayErrorPage($"Error inesperado: {ex.Message}");
            }
        }

        /// <summary>
        /// Muestra una página de error en el WebView.
        /// Motivo: Proporciona una retroalimentación visual al usuario en caso de fallos de navegación,
        /// sin bloquear la aplicación.
        /// </summary>
        /// <param name="errorMessage">El mensaje de error a mostrar.</param>
        private void DisplayErrorPage(string errorMessage)
        {
            // Motivo: Usar una página HTML simple con el mensaje de error.
            // Data URI es una forma sencilla de inyectar contenido sin archivos locales.
            string htmlContent = $"<!DOCTYPE html><html><head><title>Error</title>" +
                                 $"<style>body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f8d7da; color: #721c24; margin: 20px; padding: 20px; border: 1px solid #f5c6cb; border-radius: 5px; }}" +
                                 $"h1 {{ color: #dc3545; }}</style></head>" +
                                 $"<body><h1>Error de Navegación</h1><p>{errorMessage}</p></body></html>";
            _webView.NavigateToString(htmlContent);
            IsLoading = false;
        }

        /// <summary>
        /// Actualiza el estado de las propiedades de navegación (CanGoBack, CanGoForward, IsLoading).
        /// Motivo: Se llama después de eventos clave del WebView para reflejar el estado actual.
        /// También notifica a los comandos que sus estados de CanExecute podrían haber cambiado.
        /// </summary>
        public void RefreshState()
        {
            if (_webView != null)
            {
                CanGoBack = _webView.CanGoBack;
                CanGoForward = _webView.CanGoForward;
                IsLoading = _webView.IsLoading;
            }
            else
            {
                CanGoBack = false;
                CanGoForward = false;
                IsLoading = false;
            }
            GoBackCommand.RaiseCanExecuteChanged();
            GoForwardCommand.RaiseCanExecuteChanged();
            RefreshCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Navega hacia atrás en el WebView.
        /// Motivo: Método para el comando GoBackCommand.
        /// </summary>
        private void GoBack()
        {
            _webView?.GoBack();
        }

        /// <summary>
        /// Navega hacia adelante en el WebView.
        /// Motivo: Método para el comando GoForwardCommand.
        /// </summary>
        private void GoForward()
        {
            _webView?.GoForward();
        }

        /// <summary>
        /// Recarga la página actual del WebView.
        /// Motivo: Método para el comando RefreshCommand.
        /// </summary>
        private void Refresh()
        {
            _webView?.Refresh();
        }

        /// <summary>
        /// Detiene la carga de la página actual del WebView.
        /// Motivo: Método para el comando StopCommand.
        /// </summary>
        private void Stop()
        {
            _webView?.Stop();
        }

        /// <summary>
        /// Manejador para el evento NavigationStarting del WebView.
        /// Motivo: Se dispara cuando la navegación a un nuevo URI está a punto de comenzar.
        /// Útil para actualizar la UI con el nuevo URI y el estado de carga.
        /// </summary>
        private void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            IsLoading = true;
            LoadingProgress = 0;
            AddressBarText = args.Uri?.OriginalString ?? "";
            IsHttps = args.Uri?.Scheme == "https"; // Actualizar el indicador HTTPS
            RefreshState(); // Actualizar el estado de los botones de navegación.
        }

        /// <summary>
        /// Manejador para el evento ContentLoading del WebView.
        /// Motivo: Se dispara cuando el WebView ha comenzado a cargar el contenido HTML.
        /// </summary>
        private void WebView_ContentLoading(WebView sender, WebViewContentLoadingEventArgs args)
        {
            // Motivo: El progreso no es lineal, pero podemos establecer un valor inicial para indicar que algo está sucediendo.
            if (LoadingProgress < 10) LoadingProgress = 10;
        }

        /// <summary>
        /// Manejador para el evento FrameNavigationCompleted del WebView.
        /// Motivo: Se dispara cuando un frame (incluido el frame principal) ha completado su navegación.
        /// Esto es útil para actualizar el progreso de carga y el título de la página.
        /// </summary>
        private void WebView_FrameNavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            // Motivo: WebView no expone directamente el progreso. Podemos simular un progreso al 80% cuando el frame principal carga.
            if (args.IsSuccess && args.Uri == sender.Source)
            {
                LoadingProgress = 80;
                // Motivo: Recuperar el título del documento JavaScript.
                // Esto es asíncrono y puede que no sea instantáneo.
                _ = UpdateTitleAsync(sender);
            }
        }

        /// <summary>
        /// Manejador para el evento NavigationCompleted del WebView.
        /// Motivo: Se dispara cuando la navegación a un URI ha finalizado (con éxito o error).
        /// Se utiliza para actualizar el estado final de la UI y el historial.
        /// </summary>
        private async void WebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            IsLoading = false;
            LoadingProgress = 100;

            if (args.IsSuccess)
            {
                CurrentUri = args.Uri?.OriginalString ?? "about:blank";
                IsHttps = args.Uri?.Scheme == "https"; // Actualizar el indicador HTTPS
                await UpdateTitleAsync(sender); // Asegurarse de tener el título final.
                AddressBarText = CurrentUri;

                // Motivo: Añadir la entrada al historial después de una navegación exitosa.
                // Se utiliza el título real de la página para una mejor experiencia de usuario.
                await _historyService.AddHistoryEntryAsync(Title, CurrentUri);
            }
            else
            {
                // Motivo: Si la navegación falló, mostrar un mensaje de error.
                CurrentUri = args.Uri?.OriginalString ?? "";
                AddressBarText = CurrentUri;
                Title = "Error de Carga";
                DisplayErrorPage($"No se pudo cargar la página: {args.WebErrorStatus}. Verifique su conexión o la URL.");
            }
            RefreshState(); // Actualizar el estado final de los botones de navegación.
        }

        /// <summary>
        /// Manejador para el evento NavigationFailed del WebView.
        /// Motivo: Captura errores de navegación más específicos, como problemas de red o certificado.
        /// </summary>
        private void WebView_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            IsLoading = false;
            LoadingProgress = 0;
            CurrentUri = e.Uri?.OriginalString ?? "";
            AddressBarText = CurrentUri;
            Title = "Error de Navegación";
            IsHttps = false; // Un fallo de navegación no debería indicar HTTPS exitoso.

            // Motivo: Proporcionar un mensaje de error más específico para el usuario.
            string errorMessage;
            switch (e.WebErrorStatus)
            {
                case WebErrorStatus.CannotConnect:
                    errorMessage = "No se pudo conectar al servidor. Verifique su conexión a Internet.";
                    break;
                case WebErrorStatus.HostNameNotResolved:
                    errorMessage = "No se pudo encontrar la dirección del servidor. Verifique la URL.";
                    break;
                case WebErrorStatus.BadArgument:
                    errorMessage = "La URL proporcionada no es válida.";
                    break;
                case WebErrorStatus.CertificateIsInvalid:
                    errorMessage = "El certificado de seguridad de este sitio no es válido.";
                    break;
                case WebErrorStatus.Timeout:
                    errorMessage = "La conexión ha expirado. Inténtelo de nuevo.";
                    break;
                default:
                    errorMessage = $"Ocurrió un error inesperado durante la navegación: {e.WebErrorStatus}.";
                    break;
            }
            DisplayErrorPage(errorMessage);
            RefreshState();
        }

        /// <summary>
        /// Manejador para el evento NewWindowRequested del WebView.
        /// Motivo: Controla los enlaces que intentan abrir una nueva ventana o pestaña.
        /// En lugar de abrir una nueva ventana, se abre una nueva pestaña en el navegador.
        /// </summary>
        private void WebView_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs args)
        {
            // Motivo: Marcar el evento como manejado para que el WebView no intente abrir una nueva ventana por sí mismo.
            args.Handled = true;
            // Motivo: Acceder al MainViewModel para añadir una nueva pestaña.
            // Esto es una ligera desviación del MVVM estricto si se accede directamente,
            // pero es práctico aquí. Una alternativa sería un sistema de mensajes global.
            if (Window.Current.Content is Frame rootFrame && rootFrame.Content is MainPage mainPage)
            {
                if (mainPage.DataContext is MainViewModel mainViewModel)
                {
                    mainViewModel.AddNewTab(args.Uri.OriginalString);
                }
            }
        }

        /// <summary>
        /// Manejador para el evento UnsupportedUriSchemeIdentified del WebView.
        /// Motivo: Se dispara cuando el WebView encuentra un esquema URI que no puede manejar (ej. "mailto:", "tel:").
        /// Se intenta lanzar el esquema con el sistema operativo si es posible.
        /// </summary>
        private async void WebView_UnsupportedUriSchemeIdentified(WebView sender, WebViewUnsupportedUriSchemeIdentifiedEventArgs args)
        {
            args.Handled = true; // Motivo: Indicar que hemos gestionado el esquema.
            try
            {
                // Motivo: Intentar abrir la URI con la aplicación predeterminada del sistema.
                await Launcher.LaunchUriAsync(args.Uri);
            }
            catch (Exception ex)
            {
                // Motivo: Si no se puede lanzar, informar al usuario.
                // Se registra el error para depuración.
                System.Diagnostics.Debug.WriteLine($"No se pudo lanzar URI '{args.Uri}': {ex.Message}");
                // Opcional: MessageDialog al usuario.
            }
        }

        /// <summary>
        /// Manejador para el evento UnviewableContentIdentified del WebView.
        /// Motivo: Se dispara cuando el WebView encuentra contenido que no puede mostrar (ej. un archivo PDF, un archivo ZIP).
        /// Generalmente, esto indica que se debe iniciar una descarga.
        /// </summary>
        private async void WebView_UnviewableContentIdentified(WebView sender, WebViewUnviewableContentIdentifiedEventArgs args)
        {
            args.Handled = true; // Motivo: Indicar que hemos gestionado el contenido no visible.

            // Motivo: Ofrecer al usuario la opción de guardar el archivo.
            FileSavePicker savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.Downloads,
                SuggestedFileName = System.IO.Path.GetFileName(args.Uri.AbsolutePath) // Nombre de archivo sugerido desde la URL
            };
            // Añadir extensiones de archivo comunes.
            savePicker.FileTypeChoices.Add("Archivo", new System.Collections.Generic.List<string> { "." + savePicker.SuggestedFileName.Split('.').LastOrDefault() });
            savePicker.FileTypeChoices.Add("Todos los archivos", new System.Collections.Generic.List<string> { "*" });

            // Motivo: Obtener el hwnd de la ventana para que el FileSavePicker aparezca correctamente sobre la aplicación UWP.
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.Windows[0]);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            StorageFile file = await savePicker.PickSaveFileAsync();

            if (file != null)
            {
                // Motivo: Iniciar la descarga a través del DownloadService.
                // Esto garantiza que la descarga sea manejada en segundo plano.
                await _downloadService.StartDownloadAsync(args.Uri, file.Name, file);
                // Opcional: Notificar al usuario que la descarga ha comenzado.
            }
        }

        /// <summary>
        /// Manejador para el evento ContainsFullScreenElementChanged del WebView.
        /// Motivo: Se dispara cuando un elemento dentro del WebView entra o sale del modo de pantalla completa.
        /// Útil para ajustar el diseño de la aplicación UWP (ej. ocultar la barra de direcciones).
        /// </summary>
        private void WebView_ContainsFullScreenElementChanged(WebView sender, object args)
        {
            // Motivo: Notificar al MainPage que la UI debe reaccionar al cambio de pantalla completa.
            // Esto es un ejemplo de cómo una ViewModel de pestaña podría comunicar a la ViewModel principal.
            if (Window.Current.Content is Frame rootFrame && rootFrame.Content is MainPage mainPage)
            {
                mainPage.SetFullScreenMode(sender.ContainsFullScreenElement);
            }
        }

        /// <summary>
        /// Actualiza el título de la pestaña obteniéndolo del documento HTML del WebView.
        /// Motivo: El título de la pestaña y de la barra de direcciones deben reflejar el título de la página web.
        /// </summary>
        /// <param name="sender">El WebView que ha completado la navegación.</param>
        private async Task UpdateTitleAsync(WebView sender)
        {
            try
            {
                // Motivo: Ejecutar JavaScript para obtener el título del documento.
                // Es un método asíncrono y puede fallar si la página no tiene un título o si JS está deshabilitado.
                string title = await sender.InvokeScriptAsync("eval", new string[] { "document.title;" });

                if (!string.IsNullOrEmpty(title))
                {
                    Title = title;
                }
                else if (sender.Source != null)
                {
                    // Motivo: Si no hay título, usar el host de la URL o la URL completa como fallback.
                    Title = sender.Source.Host ?? sender.Source.OriginalString;
                }
                else
                {
                    Title = "Página sin título";
                }
            }
            catch (Exception ex)
            {
                // Motivo: Manejar errores al intentar obtener el título via JavaScript.
                // Por ejemplo, si JavaScript está deshabilitado o si hay un problema con el script.
                System.Diagnostics.Debug.WriteLine($"Error getting WebView title: {ex.Message}");
                if (sender.Source != null)
                {
                    Title = sender.Source.Host ?? sender.Source.OriginalString;
                }
                else
                {
                    Title = "Página sin título";
                }
            }
        }

        /// <summary>
        /// Libera los recursos gestionados por esta ViewModel.
        /// Motivo: Es crucial desuscribirse de los eventos del WebView y anular su Source
        /// para liberar memoria cuando la pestaña se cierra, evitando fugas de memoria.
        /// </summary>
        public void Dispose()
        {
            SetWebView(null); // Desuscribirse de todos los eventos del WebView y liberar la referencia.
            // No es necesario llamar a GC.Collect() explícitamente; el recolector de basura lo gestionará.
        }
    }
}
