using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace GZipTest.Threading
{
  /// <summary>
  /// Менеджер очередей обработки потоков.
  /// </summary>
  public class QueueManager
  {
    #region Поля и свойства

    public readonly ProducerConsumer<ProcessingTask> ReadingQueue = new ProducerConsumer<ProcessingTask>();
    public readonly ProducerConsumer<ProcessingTask> CompressQueue = new ProducerConsumer<ProcessingTask>();
    public readonly ProducerConsumer<ProcessingTask> WritingQueue = new ProducerConsumer<ProcessingTask>();
    private readonly List<Thread> _threads;

    #endregion

    #region Методы

    /// <summary>
    /// Обработчик очереди чтения потоков.
    /// </summary>
    private void ReaderConsumer()
    {
      while (true)
      {
        var task = ReadingQueue.Dequeue();
        if (task == null)
          break;

        task.Action.Invoke();
      }
    }

    /// <summary>
    /// Обработчик очереди архивации/разархивации потоков.
    /// </summary>
    private void CompressConsumer()
    {
      while (true)
      {
        var task = CompressQueue.Dequeue();
        if (task == null)
          break;

        task.Action.Invoke();
      }
    }

    /// <summary>
    /// Обработчик очереди записи результатов архивации/разархивации.
    /// </summary>
    private void WriterConsumer()
    {
      while (true)
      {
        var task = WritingQueue.Dequeue();
        if (task == null)
          break;

        task.Action.Invoke();
      }
    }

    /// <summary>
    /// Запустить обработчики очередей.
    /// </summary>
    public void StartThreads()
    {
      foreach (var thread in _threads)
        thread.Start();

      foreach (var thread in _threads)
        thread.Join();
    }

    /// <summary>
    /// Обертка для обработки исключений в другом потоке.
    /// </summary>
    /// <param name="action">Метод, выполняемый в потоке.</param>
    /// <param name="exceptionHandler">Функция-обработчик исключений.</param>
    public void ExecuteHandler(Action action, Action<Exception> exceptionHandler)
    {
      try
      {
        action.Invoke();
      }
      catch (Exception e)
      {
        if (!(e is ThreadAbortException))
          exceptionHandler?.Invoke(e);

        foreach (var thread in _threads)
          thread.Abort();
      }
    }

    #endregion

    #region Конструктор

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="exceptionHandler">Функция-обработчик исключений.</param>
    public QueueManager(Action<Exception> exceptionHandler = null)
    {
      _threads = new List<Thread> { new Thread(() => ExecuteHandler(WriterConsumer, exceptionHandler)) };

      if (Environment.ProcessorCount == 1)
        _threads.Add(new Thread(() => ExecuteHandler(CompressConsumer, exceptionHandler)));
      else
        for (var i = 0; i < Environment.ProcessorCount - 1; i++)
          _threads.Add(new Thread(() => ExecuteHandler(CompressConsumer, exceptionHandler)));

      _threads.Add(new Thread(() => ExecuteHandler(ReaderConsumer, exceptionHandler)));
    }

    #endregion
  }
}