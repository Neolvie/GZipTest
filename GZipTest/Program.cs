using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using GZipTest.ApplicationParams;
using GZipTest.Archiver;
using GZipTest.Threading;

namespace GZipTest
{
  class Program
  {
    private static Exception threadException;

    static void Main(string[] args)
    {
      var stopwatch = Stopwatch.StartNew();
      var appParams = new ApplicationParams.ApplicationParams();

      try
      {
        appParams.ParseCommandLineParams(args);
        CheckFileParams(appParams);

        switch (appParams.OperationType)
        {
          case OperationType.Compress: Compress(appParams.SourceFilePath, appParams.TargetFilePath); break;
          case OperationType.Decompress: Decompress(appParams.SourceFilePath, appParams.TargetFilePath); break;
          default: Compress(appParams.SourceFilePath, appParams.TargetFilePath); break;
        }
      }
      catch (Exception e)
      {
        if (e is IndexOutOfRangeException || e is OutOfMemoryException)
          Console.WriteLine("Недостаточно места на диске.");
        else 
          Console.WriteLine(e);
      }

      Console.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms.");
      Console.WriteLine("Press any key to exit...");
      Console.ReadKey();
    }

    /// <summary>
    /// Выполнить архивацию файла.
    /// </summary>
    /// <param name="sourceFile">Исходный файл.</param>
    /// <param name="targetFile">Архивированный файл.</param>
    private static void Compress(string sourceFile, string targetFile)
    {
      using (var sourceStream = new FileStream(sourceFile, FileMode.Open))
      using (var targetStream = new FileStream(targetFile, FileMode.Create))
      using (var archiver = ArchiverFactory.GetArchiver(sourceStream, targetStream, ThreadExceptionHandler))
      {
        archiver.Compress();
      }

      if (threadException != null)
        throw threadException;

      Console.WriteLine($"File successfully compressed to {targetFile}");
    }

    /// <summary>
    /// Выполнить разархивацию файла.
    /// </summary>
    /// <param name="sourceFile">Архивированный файл.</param>
    /// <param name="targetFile">Разархивированный файл.</param>
    private static void Decompress(string sourceFile, string targetFile)
    {
      using (var sourceStream = new FileStream(sourceFile, FileMode.Open))
      using (var targetStream = new FileStream(targetFile, FileMode.Create))
      using (var archiver = ArchiverFactory.GetArchiver(sourceStream, targetStream, ThreadExceptionHandler))
      {
        archiver.Decompress();
      }

      if (threadException != null)
        throw threadException;

      Console.WriteLine($"File successfully decompressed to {targetFile}");
    }

    /// <summary>
    /// Обработчик исключений в потоках.
    /// </summary>
    /// <param name="exception">Исключение.</param>
    private static void ThreadExceptionHandler(Exception exception)
    {
      threadException = exception;
    }

    /// <summary>
    /// Проверить переданные файлы на доступность.
    /// </summary>
    private static void CheckFileParams(ApplicationParams.ApplicationParams appParams)
    {
      if (!File.Exists(appParams.SourceFilePath))
        throw new FileNotFoundException("Source file not found or not accessible");

      var path = Path.GetDirectoryName(appParams.TargetFilePath);

      if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        throw new DirectoryNotFoundException("Target directory not exists or not accessible");

      if (File.Exists(appParams.TargetFilePath))
      {
        try
        {
          File.Delete(appParams.TargetFilePath);
        }
        catch (Exception e)
        {
          throw new Exception("Can`t delete existing target file", e);
        }
      }
    }
  }
}
