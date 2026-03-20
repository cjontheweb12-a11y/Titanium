// Browser/Helpers/WebViewHelper.cs
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Controls;
using Browser.Models;
using Browser.ViewModels.Base;
using System.Windows.Input;

namespace Browser.Helpers
{
    /// <summary>
    /// Colección de convertidores de valores para el enlace de datos y otras utilidades de WebView.
    /// Motivo: Centraliza los convertidores reutilizables, mejorando la modularidad y la legibilidad del XAML.
    /// </summary>
    public sealed class WebViewHelper
    {
        // Motivo: Convertidor de booleano a visibilidad para ocultar/mostrar elementos de la UI.
        public static BooleanToVisibilityConverter BooleanToVisibilityConverter => new BooleanToVisibilityConverter();

        // Motivo: Convertidor de objetos a visibilidad para ocultar/mostrar elementos de la UI
        // si un objeto es nulo o no nulo.
        public static ObjectToVisibilityConverter ObjectToVisibilityConverter => new ObjectToVisibilityConverter();

        // Motivo: Convertidor para formatear objetos DateTime a una cadena legible para el usuario.
        public static DateTimeFormatConverter DateTimeFormatConverter => new DateTimeFormatConverter();

        // Motivo: Convertidor para mostrar los nombres de las opciones de inicio de forma amigable.
        public static StartupOptionDisplayNameConverter StartupOptionDisplayNameConverter => new StartupOptionDisplayNameConverter();

        // Motivo: Convertidor para mostrar los nombres de los motores de búsqueda de forma amigable.
        public static SearchEngineDisplayNameConverter SearchEngineDisplayNameConverter => new SearchEngineDisplayNameConverter();

        // Motivo: Convertidor para alternar entre dos comandos basándose en un valor booleano.
        public static BooleanToCommandConverter BooleanToCommandConverter => new BooleanToCommandConverter();

        // Motivo: Convertidor para determinar la visibilidad de la barra de progreso en el gestor de descargas.
        public static DownloadStatusToProgressVisibilityConverter DownloadStatusToProgressVisibilityConverter => new DownloadStatusToProgressVisibilityConverter();
    }

    /// <summary>
    /// Convertidor que toma un valor booleano y devuelve un valor de visibilidad (Visible/Collapsed).
    /// Motivo: Se utiliza ampliamente en XAML para controlar la visibilidad de los elementos de la UI
    /// basándose en una propiedad booleana de la ViewModel.
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Convierte un valor booleano a Visibility.
        /// </summary>
        /// <param name="value">El valor booleano.</param>
        /// <param name="targetType">El tipo de destino.</param>
        /// <param name="parameter">Parámetro opcional. Si es "True" o "true" (cadena), invierte el resultado.</param>
        /// <param name="language">Idioma (no usado).</param>
        /// <returns>Visible si es true (o false si invertido), Collapsed si es false (o true si invertido).</returns>
        public object Convert
