using System;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest.Threading
{
  /// <summary>
  /// Реализация шаблона "Производитель-потребитель".
  /// </summary>
  /// <typeparam name="T">Тип объекта очереди.</typeparam>
  public class ProducerConsumer<T> where T : class
  {
    #region Поля и свойства

    private readonly object mutex = new object();

    private readonly Queue<T> queue = new Queue<T>();

    #endregion

    #region Методы

    /// <summary>
    /// Извлечь следующую задачу из очереди.
    /// </summary>
    /// <returns></returns>
    public T Dequeue()
    {
      lock (mutex)
      {
        if (queue.Count == 0)
          return null;

        var msg = queue.Dequeue();
        Monitor.Pulse(mutex);
        return msg;
      }
    }

    /// <summary>
    /// Поставить задачу в очередь.
    /// </summary>
    /// <param name="task">Задача.</param>
    public void Enqueue(T task)
    {
      if (task == null)
        throw new ArgumentNullException(nameof(task));

      lock (mutex)
      {
        queue.Enqueue(task);
        Monitor.Pulse(mutex);
      }
    }

    #endregion
  }
}