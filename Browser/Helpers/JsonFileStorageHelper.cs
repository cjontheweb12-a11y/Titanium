// Browser/Helpers/JsonFileStorageHelper.cs
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Newtonsoft.Json; // Motivo: Usar Newtonsoft.Json para serialización/deserialización.
                       // Es un paquete NuGet estándar y robusto para trabajar con JSON.

namespace Browser.Helpers
{
    /// <summary>
    /// Clase de utilidad para la lectura y escritura de objetos en archivos JSON locales.
    /// Motivo: Centraliza la lógica de persistencia de datos en formato JSON,
    /// facilitando el almacenamiento de objetos complejos para marcadores, historial y configuraciones.
    /// Utiliza ApplicationData.Current.LocalFolder para el almacenamiento persistente.
    /// </summary>
    public static class JsonFileStorageHelper
    {
        /// <summary>
        /// Guarda un objeto como archivo JSON en el almacenamiento local de la aplicación de forma asíncrona.
        /// Motivo: Proporciona una forma genérica de serializar cualquier objeto a JSON y guardarlo.
        /// </summary>
        /// <typeparam name="T">El tipo del objeto a guardar.</typeparam>
        /// <param name="data">El objeto a guardar.</param>
        /// <param name="fileName">El nombre del archivo JSON.</param>
        /// <returns>Una tarea que representa la operación de guardado.</returns>
        public static async Task SaveAsJsonAsync<T>(T data, string fileName)
        {
            try
            {
                // Motivo: Serializar el objeto a una cadena JSON.
                // Formatting.Indented hace que el JSON sea legible, útil para depuración.
                string jsonString = JsonConvert.SerializeObject(data, Formatting.Indented);

                // Motivo: Obtener la carpeta de datos local de la aplicación.
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                // Motivo: Crear o sobrescribir el archivo.
                StorageFile sampleFile = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

                // Motivo: Escribir la cadena JSON en el archivo.
                await FileIO.WriteTextAsync(sampleFile, jsonString);
            }
            catch (Exception ex)
            {
                // Motivo: Manejar cualquier error durante la serialización o el guardado del archivo.
                // Se imprime el error para depuración.
                System.Diagnostics.Debug.WriteLine($"Error al guardar el archivo JSON '{fileName}': {ex.Message}");
                throw; // Re-lanzar para que los servicios que llaman puedan manejarlo si es necesario.
            }
        }

        /// <summary>
        /// Carga un objeto desde un archivo JSON en el almacenamiento local de la aplicación de forma asíncrona.
        /// Motivo: Proporciona una forma genérica de leer un archivo JSON y deserializarlo a un objeto.
        /// </summary>
        /// <typeparam name="T">El tipo del objeto a cargar.</typeparam>
        /// <param name="fileName">El nombre del archivo JSON.</param>
        /// <returns>El objeto cargado o null si el archivo no existe o hay un error.</returns>
        public static async Task<T> LoadFromJsonAsync<T>(string fileName)
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                // Motivo: Intentar obtener el archivo. Si no existe, se devuelve null.
                StorageFile sampleFile = await localFolder.GetFileAsync(fileName);

                // Motivo: Leer el contenido del archivo como una cadena.
                string jsonString = await FileIO.ReadTextAsync(sampleFile);

                // Motivo: Deserializar la cadena JSON a un objeto del tipo especificado.
                return JsonConvert.DeserializeObject<T>(jsonString);
            }
            catch (FileNotFoundException)
            {
                // Motivo: Si el archivo no existe, no es un error crítico; simplemente no hay datos guardados.
                System.Diagnostics.Debug.WriteLine($"Archivo JSON '{fileName}' no encontrado. Devolviendo valor predeterminado.");
                return default(T);
            }
            catch (Exception ex)
            {
                // Motivo: Manejar cualquier otro error durante la lectura o deserialización del archivo.
                // Esto puede incluir JSON malformado.
                System.Diagnostics.Debug.WriteLine($"Error al cargar o deserializar el archivo JSON '{fileName}': {ex.Message}");
                throw; // Re-lanzar para que los servicios que llaman puedan manejarlo si es necesario.
            }
        }
    }
}
