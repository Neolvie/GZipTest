using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using GZipTest.Threading;

namespace GZipTest.Archiver
{
  /// <summary>
  /// Архиватор/разархиватор потоков.
  /// </summary>
  public class Archiver : IDisposable
  {
    #region Поля и свойства

    // TODO. Размер буфера в конфиг.
    private const int BufferSize = 1024 * 1024;
    private readonly QueueManager _queueManager;
    private readonly Stream _sourceStream;
    private readonly Stream _targetStream;
    private List<ArchivePart> _archiveParts;

    #endregion

    #region Методы

    /// <summary>
    /// Выполнить архивацию.
    /// </summary>
    public void Compress()
    {
      try
      {
        if (_sourceStream.Length == 0)
          throw new ArgumentException("Source file has 0 length.");

        CompressInternal(_sourceStream, _targetStream, BufferSize);
        _queueManager.StartThreads();

      }
      catch (Exception e)
      {
        throw new Exception("Error during compressing. See inner exception", e);
      }
    }

    /// <summary>
    /// Выполнить разархивацию.
    /// </summary>
    public void Decompress()
    {
      try
      {
        if (_sourceStream.Length == 0)
          throw new ArgumentException("Source file has 0 length.");

        DecompressInternal(_sourceStream, _targetStream);
        _queueManager.StartThreads();

      }
      catch (Exception e)
      {
        throw new Exception("Error during decompressing. See inner exception", e);
      }
    }

    /// <summary>
    /// Выполнить архивацию файла.
    /// </summary>
    /// <param name="sourceStream">Исходный поток.</param>
    /// <param name="targetStream">Архивированный поток.</param>
    /// <param name="bufferSize">Размер буфера.</param>
    private void CompressInternal(Stream sourceStream, Stream targetStream, long bufferSize)
    {
      var parts = Math.Ceiling((double)sourceStream.Length / bufferSize);

      if (bufferSize > sourceStream.Length)
        bufferSize = sourceStream.Length;

      for (var portion = 0; portion < parts; portion++)
      {
        var currentPortion = portion;
        var archivePart = new ArchivePart();
        var size = bufferSize;

        var offset = currentPortion * size;
        if (offset + size > sourceStream.Length)
          size = sourceStream.Length - offset;

        archivePart.Offset = offset;
        var readingTask = new ProcessingTask(() =>
        {
          var portionArray = new byte[size];
          lock (sourceStream)        
            sourceStream.Read(portionArray, 0, (int)size);

          archivePart.Input = portionArray;
        });

        _queueManager.ReadingQueue.Enqueue(readingTask);

        var compressingTask = new ProcessingTask(() =>
        {
          while (true)
          {
            if (archivePart.Input == null)
              continue;

            archivePart.Compress();
            break;
          }
        });
        
        _queueManager.CompressQueue.Enqueue(compressingTask);

        var writingTask = new ProcessingTask(() =>
        {
          while (true)
          {
            if (!archivePart.HasResult)
              continue;

            lock (targetStream)
              archivePart.WriteResult(targetStream);
            break;
          }
        });

        _queueManager.WritingQueue.Enqueue(writingTask);
      }
    }

    /// <summary>
    /// Получить части архивированного потока.
    /// </summary>
    /// <param name="sourceStream">Архивированный поток.</param>
    /// <returns>Список частей архива.</returns>
    private List<ArchivePart> GetArchiveParts(Stream sourceStream)
    {
      if (_archiveParts != null && _archiveParts.Any())
        return _archiveParts;

      long offset = 0;
      var portionSizeBufferLength = sizeof(long);
      var portionSizeBuffer = new byte[portionSizeBufferLength];

      var parts = new List<ArchivePart>();

      while (offset + portionSizeBufferLength < sourceStream.Length)
      {
        sourceStream.Seek(offset, SeekOrigin.Begin);
        sourceStream.Read(portionSizeBuffer, 0, portionSizeBufferLength);
        var portionBufferLength = BitConverter.ToInt32(portionSizeBuffer, 0);
        offset += portionSizeBufferLength;

        var archivePart = new ArchivePart(offset, portionBufferLength);
        parts.Add(archivePart);

        offset += portionBufferLength;
      }

      return parts;
    }

    /// <summary>
    /// Разархивировать файл.
    /// </summary>
    /// <param name="sourceStream">Архивированный поток.</param>
    /// <param name="targetStream">Разархивированный поток.</param>
    private void DecompressInternal(Stream sourceStream, Stream targetStream)
    {
      _archiveParts = GetArchiveParts(sourceStream);

      foreach (var archivePart in _archiveParts)
      {
        var readingTask = new ProcessingTask(() =>
        {
          lock (sourceStream)
          {
            sourceStream.Seek(archivePart.Offset, SeekOrigin.Begin);
            var buffer = new byte[archivePart.Size];
            sourceStream.Read(buffer, 0, archivePart.Size);
            archivePart.Input = buffer;
          }
        });

        _queueManager.ReadingQueue.Enqueue(readingTask);

        var decompressingTask = new ProcessingTask(() =>
        {
          while (true)
          {
            if (archivePart.Input == null)
              continue;

            archivePart.Decompress();
            break;
          }
        });

        _queueManager.CompressQueue.Enqueue(decompressingTask);

        var writingTask = new ProcessingTask(() =>
        {
          while (true)
          {
            if (!archivePart.HasResult)
              continue;

            lock (targetStream)
              archivePart.WriteResult(targetStream);
            break;
          }
        });

        _queueManager.WritingQueue.Enqueue(writingTask);
      }
    }

    #endregion

    #region Конструктор

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="sourceStream">Исходный поток.</param>
    /// <param name="targetStream">Поток с результатом работы архиватора.</param>
    /// <param name="queueManager">Менеджер очередей обработки потоков.</param>
    public Archiver(Stream sourceStream, Stream targetStream, QueueManager queueManager)
    {
      _sourceStream = sourceStream;
      _targetStream = targetStream;
      _queueManager = queueManager;
    }

    #endregion

    public void Dispose()
    {
      _sourceStream?.Dispose();
      _targetStream?.Dispose();
    }
  }
}