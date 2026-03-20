// Browser/Helpers/UrlHelper.cs
using Browser.Models;
using System;

namespace Browser.Helpers
{
    /// <summary>
    /// Clase de utilidad para el manejo y formateo de URLs.
    /// Motivo: Centraliza la lógica de validación y transformación de URLs,
    /// evitando la duplicación de código y mejorando la mantenibilidad.
    /// </summary>
    public static class UrlHelper
    {
        /// <summary>
        /// Formatea una URL o término de búsqueda para la navegación.
        /// Motivo: Asegura que la cadena de entrada se convierta en una URL válida
        /// o en una URL de búsqueda formateada correctamente.
        /// </summary>
        /// <param name="input">La cadena de entrada (URL o término de búsqueda).</param>
        /// <param name="searchEngine">El motor de búsqueda predeterminado a usar si es un término de búsqueda.</param>
        /// <param name="customSearchUrl">La URL personalizada del motor de búsqueda si se usa. Debe contener "{query}".</param>
        /// <returns>La URL formateada para la navegación.</returns>
        public static string FormatUrl(string input, SearchEngine searchEngine, string customSearchUrl)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "about:blank"; // Motivo: Devolver una página en blanco si la entrada es vacía.
            }

            // Motivo: Intentar crear un URI. Si tiene un esquema válido (http/https), se asume que es una URL completa.
            if (Uri.TryCreate(input, UriKind.Absolute, out Uri uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                return uriResult.OriginalString;
            }

            // Motivo: Si no es una URL completa, intentar añadir "https://" como prefijo y verificar de nuevo.
            if (Uri.TryCreate("https://" + input, UriKind.Absolute, out uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                return uriResult.OriginalString;
            }

            // Motivo: Si sigue sin ser una URL válida, se asume que es un término de búsqueda y se formatea.
            string encodedQuery = Uri.EscapeDataString(input); // Codificar el término de búsqueda para la URL.
            switch (searchEngine)
            {
                case SearchEngine.Bing:
                    return $"https://www.bing.com/search?q={encodedQuery}";
                case SearchEngine.Google:
                    return $"https://www.google.com/search?q={encodedQuery}";
                case SearchEngine.Custom:
                    // Motivo: Reemplazar el marcador de posición {query} en la URL personalizada.
                    return customSearchUrl.Replace("{query}", encodedQuery);
                default:
                    return $"https://www.google.com/search?q={encodedQuery}"; // Fallback.
            }
        }

        /// <summary>
        /// Asegura que una URL tenga un esquema HTTP o HTTPS.
        /// Motivo: Algunas entradas de usuario pueden omitir el esquema, lo que causaría errores en `Uri.TryCreate`.
        /// Este método añade un esquema predeterminado si falta.
        /// </summary>
        /// <param name="url">La URL a verificar.</param>
        /// <returns>La URL con un esquema HTTP/HTTPS, o la original si ya lo tiene o es nula.</returns>
        public static string EnsureHttps(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return url;
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return url;
            }

            // Si no tiene un esquema o tiene uno no web, agregar HTTPS.
            return "https://" + url;
        }
    }
}
