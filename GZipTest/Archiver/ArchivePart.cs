using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace GZipTest.Archiver
{
  /// <summary>
  /// Блок архива.
  /// </summary>
  public class ArchivePart
  {
    #region Поля и свойства

    public long Offset;
    public int Size;
    public byte[] Input;
    public byte[] Result;
    public bool HasResult;

    private const string InputNotAssigned = "Input not assigned";

    #endregion

    #region Методы  

    /// <summary>
    /// Выполнить архивацию блока.
    /// </summary>
    public void Compress()
    {
      if (Input == null)
        throw new ArgumentNullException(InputNotAssigned);


      using (var zippedMemoryStream = new MemoryStream())
      {
        using (var zipStream = new GZipStream(zippedMemoryStream, CompressionMode.Compress, true))
        {
          zipStream.Write(Input, 0, Input.Length);       
        }

        var resultLength = BitConverter.GetBytes(zippedMemoryStream.Length);
        Result = new byte[resultLength.Length + zippedMemoryStream.Length];
        resultLength.CopyTo(Result, 0);
        zippedMemoryStream.ToArray().CopyTo(Result, resultLength.Length);
      }

      HasResult = true;
    }

    /// <summary>
    /// Выполнить разархивацию блока.
    /// </summary>
    /// <param name="bufferSize">Размер буфера.</param>
    public void Decompress(int bufferSize = 4096)
    {
      if (Input == null)
        throw new ArgumentNullException(InputNotAssigned);

      using (var unzippedMemoryStream = new MemoryStream())
      {
        using (var zippedMemoryStream = new MemoryStream(Input))
        using (var zipStream = new GZipStream(zippedMemoryStream, CompressionMode.Decompress))
        {          
          var buffer = new byte[bufferSize];
          int read;
          while ((read = zipStream.Read(buffer, 0, bufferSize)) != 0)
            unzippedMemoryStream.Write(buffer, 0, read);
        }

        Result = unzippedMemoryStream.ToArray();
      }

      HasResult = true;
    }

    /// <summary>
    /// Записать результат в поток.
    /// </summary>
    /// <param name="targetStream">Результат.</param>
    public void WriteResult(Stream targetStream)
    {
      if (Result == null)
        return;

      targetStream.Write(Result, 0, Result.Length);
      targetStream.Flush();
    }

    #endregion

    #region Конструктор

    public ArchivePart(byte[] input)
    {
      Input = input;
      Size = input.Length;
    }

    public ArchivePart(long offset, int size)
    {
      Offset = offset;
      Size = size;
    }

    public ArchivePart()
    {

    }

    #endregion
  }
}