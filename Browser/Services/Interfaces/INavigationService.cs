// Browser/Services/Interfaces/INavigationService.cs
using System;
using System.ComponentModel;
using Windows.UI.Xaml.Controls;

namespace Browser.Services.Interfaces
{
    /// <summary>
    /// Interfaz para un servicio de navegación dentro de la aplicación.
    /// Motivo: Proporciona una forma desacoplada para que las ViewModels inicien la navegación
    /// sin tener una referencia directa al objeto Frame o a la infraestructura de la UI.
    /// Esto mejora la probabildiad, la modularidad y sigue el patrón MVVM.
    /// </summary>
    public interface INavigationService : INotifyPropertyChanged
    {
        /// <summary>
        /// Obtiene la página actual del Frame de navegación.
        /// Motivo: Permite a las ViewModels saber qué vista está activa actualmente.
        /// </summary>
        Type CurrentPageType { get; }

        /// <summary>
        /// Navega a una página específica.
        /// </summary>
        /// <typeparam name="TPage">El tipo de la página a la que navegar.</typeparam>
        void NavigateTo<TPage>() where TPage : Page;

        /// <summary>
        /// Navega a una página específica con un parámetro.
        /// </summary>
        /// <typeparam name="TPage">El tipo de la página a la que navegar.</typeparam>
        /// <param name="parameter">El parámetro a pasar a la página.</param>
        void NavigateTo<TPage>(object parameter) where TPage : Page;

        /// <summary>
        /// Navega hacia atrás en la pila de navegación.
        /// </summary>
        void GoBack();

        /// <summary>
        /// Indica si es posible navegar hacia atrás.
        /// Motivo: Permite a las ViewModels habilitar o deshabilitar los botones de "atrás" en la UI.
        /// </summary>
        bool CanGoBack { get; }

        /// <summary>
        /// Establece el Frame que el servicio debe usar para la navegación.
        /// Motivo: El Frame raíz no está disponible al inicio del ciclo de vida de App,
        /// por lo que necesita ser establecido una vez que esté disponible.
        /// </summary>
        /// <param name="frame">El Frame de navegación.</param>
        void SetNavigationFrame(Frame frame);
    }
}
