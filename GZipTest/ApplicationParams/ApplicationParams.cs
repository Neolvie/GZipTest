using System;
using System.IO;

namespace GZipTest.ApplicationParams
{
  /// <summary>
  /// Параметры запуска приложения.
  /// </summary>
  public class ApplicationParams
  {
    #region Поля и свойства

    public OperationType OperationType { get; private set; } = OperationType.Compress;
    public string SourceFilePath { get; private set; }
    public string TargetFilePath { get; private set; }

    private readonly string _toFewParametersError =
        $"To few parameters. {Environment.NewLine}Example: GZipText.exe compress \"C:\\sourceFile.dat\" [\"C:\\[resultFile.arc]\"] or GZipText.exe decompress \"C:\\sourceFile.arc\" \"C:\\resultFile.dat\"";

    #endregion

    #region Методы

    /// <summary>
    /// Разобрать командную строку на параметры.
    /// </summary>
    /// <param name="args">Параметры командной строки.</param>
    public void ParseCommandLineParams(string[] args)
    {
      if (args.Length < 2)
        throw new ApplicationException(_toFewParametersError);

      try
      {
        OperationType = (OperationType)Enum.Parse(typeof(OperationType), args[0], true);
      }
      catch (Exception)
      {
        throw new ApplicationException("Not supported operation. Use \"compress\" or \"decompress\"");
      }

      if (OperationType == OperationType.Decompress && args.Length < 3)
        throw new ApplicationException(_toFewParametersError);

      SourceFilePath = args[1];
      TargetFilePath = args.Length > 2 ? args[2] : $"{SourceFilePath}.arc";

      var targetFileName = Path.GetFileName(TargetFilePath);
      var sourceFileName = Path.GetFileName(SourceFilePath);

      if (string.IsNullOrEmpty(targetFileName) && !string.IsNullOrEmpty(sourceFileName))
        TargetFilePath = Path.Combine(TargetFilePath, $"{sourceFileName}.arc");
    }

    #endregion
  }
}