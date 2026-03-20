// Browser/App.xaml.cs
using Browser.Helpers;
using Browser.Models;
using Browser.Services.Implementations;
using Browser.Services.Interfaces;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Browser
{
    /// <summary>
    /// Proporciona un comportamiento específico de la aplicación para complementar la clase Application.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Inicializa el objeto Application singleton. Esta es la primera línea de código ejecutada
        /// y, lógicamente, el equivalente de Main() o WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            // Motivo: Realizar la configuración inicial de los servicios para la inyección de dependencias.
            // Se utiliza un ServiceLocator simple para UWP para facilitar la gestión de las instancias de servicio.
            // Esto permite que las ViewModels y otras partes de la aplicación accedan a los servicios sin
            // necesidad de pasarlos explícitamente en cada constructor, promoviendo un código más limpio y modular.
            ConfigureServices();
        }

        /// <summary>
        /// Configura los servicios de la aplicación para la inyección de dependencias.
        /// </summary>
        private void ConfigureServices()
        {
            // Motivo: Se registran las implementaciones concretas de los servicios para sus interfaces.
            // Esto permite que las ViewModels dependan de las interfaces, no de las implementaciones,
            // facilitando la sustitución de implementaciones (por ejemplo, para pruebas) y mejorando la modularidad.
            ServiceLocator.Register<ISettingsService, SettingsService>();
            ServiceLocator.Register<IBookmarkService, BookmarkService>();
            ServiceLocator.Register<IHistoryService, HistoryService>();
            ServiceLocator.Register<IDownloadService, DownloadService>();

            // El NavigationService requiere el Frame principal de la aplicación, por lo que se registra
            // después de que el Frame raíz haya sido creado en OnLaunched.
        }

        /// <summary>
        /// Se invoca cuando la aplicación es lanzada normalmente por el usuario final. Se usarán otros
        /// puntos de entrada, como el lanzamiento de un archivo para abrir una sugerencia de mosaico, etc.
        /// </summary>
        /// <param name="e">Detalles sobre la solicitud de lanzamiento y el proceso.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Motivo: No repetir la inicialización de la aplicación cuando la ventana ya tiene contenido,
            // solo se asegura de que la ventana esté activa.
            if (rootFrame == null)
            {
                // Crear un Frame para actuar como contexto de navegación y navegar a la primera página.
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // Motivo: Cargar el estado de la aplicación guardado previamente.
                    // Esto es crucial para la funcionalidad de "restaurar última sesión".
                    try
                    {
                        var settingsService = ServiceLocator.Resolve<ISettingsService>();
                        await settingsService.LoadSettingsAsync();
                    }
                    catch (Exception ex)
                    {
                        // Motivo: Registrar el error si no se pueden cargar las configuraciones, pero permitir que la aplicación continúe.
                        // Esto evita un bloqueo en caso de corrupción de datos de configuración.
                        System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                    }
                }

                // Colocar el frame en la ventana actual.
                Window.Current.Content = rootFrame;
            }

            // Motivo: Después de que el Frame raíz esté disponible, se puede registrar el NavigationService.
            // Esto asegura que el servicio tenga acceso al Frame correcto para realizar las operaciones de navegación.
            ServiceLocator.Register<INavigationService>(new NavigationService(rootFrame));


            // Motivo: Navegar a la página principal solo si no se restauró el estado anterior o si es un nuevo lanzamiento.
            // Para la funcionalidad de "restaurar última sesión", la MainViewModel manejará la creación de pestañas iniciales.
            if (e.PrelaunchActivated == false)
            {
                // Si la página principal ya está en la pila de navegación, no es necesario volver a navegar.
                if (rootFrame.Content == null)
                {
                    // Cuando la pila de navegación no se restaura, navegar a la primera página,
                    // configurando la nueva página mediante el argumento como información requerida.
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Asegurarse de que la ventana actual esté activa.
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Se invoca cuando la navegación a una página específica falla.
        /// </summary>
        /// <param name="sender">El Frame al que falló la navegación.</param>
        /// <param name="e">Detalles sobre el error de navegación.</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            // Motivo: Lanzar una excepción para indicar un error de navegación crítico.
            // En un entorno de producción, esto podría ser reemplazado por un registro de errores
            // y una navegación a una página de error genérica.
            throw new Exception($"Failed to load Page {e.SourcePageType.FullName}: {e.Exception.Message}");
        }

        /// <summary>
        /// Se invoca cuando la ejecución de la aplicación está siendo suspendida.
        /// El estado de la aplicación se guarda sin saber si la aplicación se terminará o se reanudará con el
        /// contenido de la memoria aún intacto.
        /// </summary>
        /// <param name="sender">Origen de la solicitud de suspensión.</param>
        /// <param name="e">Detalles sobre la solicitud de suspensión.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            // Motivo: Deferir la suspensión para asegurarse de que las operaciones asíncronas de guardado se completen.
            // Esto es vital para guardar el estado de la aplicación (como las pestañas de la última sesión)
            // de forma fiable antes de que la aplicación sea suspendida o terminada por el sistema.
            var deferral = e.SuspendingOperation.GetDeferral();

            // Motivo: Guardar la configuración y el estado de la aplicación.
            // Esto incluye las URLs de las pestañas abiertas para la función "restaurar última sesión".
            try
            {
                var settingsService = ServiceLocator.Resolve<ISettingsService>();
                await settingsService.SaveSettingsAsync();
            }
            catch (Exception ex)
            {
                // Motivo: Registrar el error si no se pueden guardar las configuraciones, pero permitir que la suspensión continúe.
                // Esto evita que un error de guardado impida que la aplicación se suspenda correctamente.
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }

            deferral.Complete();
        }
    }
}
