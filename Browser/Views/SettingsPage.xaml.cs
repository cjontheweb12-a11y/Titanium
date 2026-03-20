// Browser/Views/SettingsPage.xaml.cs
using Windows.UI.Xaml.Controls;

namespace Browser.Views
{
    /// <summary>
    /// Página para la configuración del navegador.
    /// Motivo: Esta View es la interfaz de usuario para la página de configuración.
    /// Su DataContext se establece en el XAML.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        /// <summary>
        /// Constructor de <see cref="SettingsPage"/>.
        /// Motivo: Inicializa los componentes de la UI.
        /// </summary>
        public SettingsPage()
        {
            this.InitializeComponent();
        }
    }
}
