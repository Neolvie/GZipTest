using System;
using System.IO;
using GZipTest.Threading;

namespace GZipTest.Archiver
{
  /// <summary>
  /// Фабрика архиваторов.
  /// </summary>
  public static class ArchiverFactory
  {
    /// <summary>
    /// Получить экземпляр архиватора.
    /// </summary>
    /// <param name="sourceStream">Исходный поток.</param>
    /// <param name="targetStream">Поток с результатом работы архиватора.</param>
    /// <param name="errorCallback">Функция-обработчик исключений.</param>
    /// <returns>Экземпляр архиватора.</returns>
    public static Archiver GetArchiver(Stream sourceStream, Stream targetStream, Action<Exception> errorCallback = null)
    {
      return new Archiver(sourceStream, targetStream, new QueueManager(errorCallback));
    }
  }
}