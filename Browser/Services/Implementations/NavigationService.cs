// Browser/Services/Implementations/NavigationService.cs
using Browser.Services.Interfaces;
using Browser.ViewModels.Base;
using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Browser.Services.Implementations
{
    /// <summary>
    /// Implementación del servicio de navegación.
    /// Motivo: Proporciona una forma desacoplada para que las ViewModels inicien la navegación
    /// sin tener una referencia directa al objeto Frame o a la infraestructura de la UI.
    /// Esto mejora la probabildiad, la modularidad y sigue el patrón MVVM.
    /// </summary>
    public class NavigationService : ObservableObject, INavigationService
    {
        private Frame _frame; // Motivo: El Frame que este servicio utilizará para la navegación.

        /// <summary>
        /// Obtiene o establece el tipo de la página actual.
        /// Motivo: Permite a los suscriptores saber a qué página se ha navegado.
        /// Notifica cambios a la UI para actualizar el estado de navegación (ej. botón "Atrás").
        /// </summary>
        private Type _currentPageType;
        public Type CurrentPageType
        {
            get => _currentPageType;
            private set => SetProperty(ref _currentPageType, value);
        }

        /// <summary>
        /// Obtiene un valor que indica si es posible navegar hacia atrás.
        /// Motivo: Se enlaza a la propiedad CanGoBack del Frame, lo que permite a la UI habilitar/deshabilitar
        /// el botón de retroceso.
        /// </summary>
        public bool CanGoBack => _frame?.CanGoBack ?? false;

        /// <summary>
        /// Constructor predeterminado de la clase <see cref="NavigationService"/>.
        /// Motivo: Permite la creación de la instancia del servicio antes de que el Frame raíz esté disponible,
        /// para luego establecer el Frame con <see cref="SetNavigationFrame"/>.
        /// </summary>
        public NavigationService() { }

        /// <summary>
        /// Constructor de la clase <see cref="NavigationService"/>.
        /// Motivo: Permite inyectar el Frame directamente si ya está disponible, por ejemplo, en App.xaml.cs.
        /// </summary>
        /// <param name="frame">El Frame de navegación principal de la aplicación.</param>
        public NavigationService(Frame frame)
        {
            SetNavigationFrame(frame);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void SetNavigationFrame(Frame frame)
        {
            // Motivo: Asegurarse de que el Frame sea establecido una sola vez y manejar la suscripción de eventos.
            if (_frame != null)
            {
                _frame.Navigated -= OnNavigated;
            }

            _frame = frame ?? throw new ArgumentNullException(nameof(frame), "El Frame de navegación no puede ser nulo.");
            _frame.Navigated += OnNavigated;

            // Motivo: Inicializar el estado de CanGoBack y CurrentPageType si el Frame ya tiene contenido.
            RaisePropertyChanged(nameof(CanGoBack));
            if (_frame.Content is Page currentPage)
            {
                CurrentPageType = currentPage.GetType();
            }
        }

        /// <summary>
        /// Manejador para el evento Navigated del Frame.
        /// Motivo: Actualiza las propiedades CanGoBack y CurrentPageType cada vez que la navegación ocurre.
        /// Esto permite que la UI que está enlazada a estas propiedades reaccione a los cambios de navegación.
        /// </summary>
        /// <param name="sender">El Frame que disparó el evento.</param>
        /// <param name="e">Argumentos del evento de navegación.</param>
        private void OnNavigated(object sender, NavigationEventArgs e)
        {
            RaisePropertyChanged(nameof(CanGoBack));
            CurrentPageType = e.SourcePageType;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void NavigateTo<TPage>() where TPage : Page
        {
            NavigateTo<TPage>(null);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void NavigateTo<TPage>(object parameter) where TPage : Page
        {
            // Motivo: Verificar si el Frame está disponible antes de intentar navegar.
            // Si el Frame es nulo, significa que el servicio no se ha inicializado correctamente.
            if (_frame == null)
            {
                throw new InvalidOperationException("El Frame de navegación no ha sido establecido. Asegúrese de llamar a SetNavigationFrame primero.");
            }

            // Motivo: Evitar la navegación a la misma página si ya estamos en ella con el mismo parámetro,
            // a menos que sea una página que siempre deba recargarse (ej. la página principal sin parámetros).
            if (_frame.Content?.GetType() != typeof(TPage) || (parameter != null && !parameter.Equals(_frame.DataContext)))
            {
                // Motivo: Llamar al método Navigate del Frame subyacente.
                // El parámetro es opcional y se puede usar para pasar datos entre ViewModels.
                _frame.Navigate(typeof(TPage), parameter);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void GoBack()
        {
            // Motivo: Verificar si el Frame está disponible y si es posible retroceder.
            // Proporciona una comprobación de seguridad antes de intentar la navegación.
            if (_frame != null && _frame.CanGoBack)
            {
                _frame.GoBack();
            }
        }
    }
}
