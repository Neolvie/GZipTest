using System;

namespace GZipTest.Threading
{
  /// <summary>
  /// Задача обработки файла.
  /// </summary>
  public class ProcessingTask
  {
    public Action Action { get; }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="action">Выполняемое действие.</param>
    public ProcessingTask(Action action)
    {
      Action = action;
    }
  }
}