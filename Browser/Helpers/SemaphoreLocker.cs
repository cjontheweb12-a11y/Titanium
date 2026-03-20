// Browser/Helpers/SemaphoreLocker.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Browser.Helpers
{
    /// <summary>
    /// Clase de utilidad para proporcionar un bloqueo asíncrono utilizando SemaphoreSlim.
    /// Motivo: Permite sincronizar el acceso a recursos compartidos (como archivos)
    /// en métodos asíncronos para evitar condiciones de carrera y asegurar la integridad de los datos.
    /// Esto es crucial para operaciones de E/S que pueden ser accedidas concurrentemente.
    /// </summary>
    public class SemaphoreLocker
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Motivo: Inicializa el semáforo con un recuento máximo de 1,
                                                                               // lo que lo convierte en un bloqueo de exclusión mutua.

        /// <summary>
        /// Espera asincrónicamente para adquirir el bloqueo del semáforo.
        /// Motivo: Permite a un bloque de código ejecutar de forma segura sin interferencia
        /// de otras tareas que intenten acceder al mismo recurso.
        /// </summary>
        /// <returns>Un objeto IDisposable que libera el bloqueo cuando se desecha.</returns>
        public async Task<IDisposable> LockAsync()
        {
            await _semaphore.WaitAsync(); // Espera hasta que el semáforo esté disponible.
            return new Releaser(_semaphore); // Devuelve un objeto que liberará el semáforo.
        }

        /// <summary>
        /// Ejecuta una función asíncrona con el bloqueo del semáforo.
        /// Motivo: Proporciona una forma más concisa de ejecutar una operación protegida por el semáforo,
        /// asegurando que el bloqueo se libere automáticamente.
        /// </summary>
        /// <typeparam name="TResult">El tipo de resultado de la función.</typeparam>
        /// <param name="func">La función asíncrona a ejecutar.</param>
        /// <returns>El resultado de la función.</returns>
        public async Task<TResult> LockAsync<TResult>(Func<Task<TResult>> func)
        {
            await _semaphore.WaitAsync();
            try
            {
                return await func();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Clase interna que implementa IDisposable para liberar el semáforo.
        /// Motivo: Permite usar el patrón `using` para asegurar que el semáforo se libere
        /// correctamente incluso si ocurren excepciones.
        /// </summary>
        private class Releaser : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            /// <summary>
            /// Constructor de <see cref="Releaser"/>.
            /// </summary>
            /// <param name="semaphore">El semáforo a liberar.</param>
            internal Releaser(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            /// <summary>
            /// Libera el semáforo.
            /// Motivo: Se llama automáticamente cuando el objeto Releaser sale del ámbito `using`.
            /// </summary>
            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }
}
