using Common.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

// Task 5: File Operations and CSV Loading on Client
public class EisFileProcessor
{
    private readonly string _datasetPath;
    private readonly List<string> _validationLog;
    private readonly List<string> _successLog;
    private readonly List<string> _warningLog;
    private const int EXPECTED_ROWS_PER_FILE = 28;

    public EisFileProcessor(string datasetPath)
    {
        _datasetPath = datasetPath ?? throw new ArgumentNullException(nameof(datasetPath));
        _validationLog = new List<string>();
        _successLog = new List<string>();
        _warningLog = new List<string>();
    }

    public List<string> ValidationLog => _validationLog.ToList();
    public List<string> SuccessLog => _successLog.ToList();
    public List<string> WarningLog => _warningLog.ToList();

    private void AddSuccess(string message)
    {
        _successLog.Add($"SUCCESS: {message}");
    }

    private void AddWarning(string message)
    {
        _warningLog.Add($"WARNING: {message}");
    }

    /// <summary>
    /// Recursively processes all EIS CSV files in the dataset structure
    /// Expected structure: Bxx/EIS Measurement/Test_y/
    /// </summary>
    /// <returns>List of processed EIS file data</returns>
    public List<EisFileData> ProcessAllEisFiles()
    {
        var eisFiles = new List<EisFileData>();
        _validationLog.Clear();

        try
        {
            if (!Directory.Exists(_datasetPath))
            {
                _validationLog.Add($"ERROR: Dataset path does not exist: {_datasetPath}");
                return eisFiles;
            }

            // Traži sve Bxx foldere
            var batteryDirs = Directory.GetDirectories(_datasetPath)
                .Where(dir => IsBatteryDirectory(Path.GetFileName(dir)))
                .OrderBy(dir => dir);

            foreach (var batteryDir in batteryDirs)
            {
                ProcessBatteryDirectory(batteryDir, eisFiles);
            }

            LogProcessingSummary(eisFiles);
        }
        catch (Exception ex)
        {
            _validationLog.Add($"ERROR: Exception during processing: {ex.Message}");
        }

        return eisFiles;
    }

    private void ProcessBatteryDirectory(string batteryDir, List<EisFileData> eisFiles)
    {
        var batteryId = Path.GetFileName(batteryDir);

        // Look for EIS Measurement directory
        var eisMeasurementDir = Path.Combine(batteryDir, "EIS Measurement");
        if (!Directory.Exists(eisMeasurementDir))
        {
            // Try alternative naming
            var eisDirs = Directory.GetDirectories(batteryDir).Where(dir => Path.GetFileName(dir).ToLower().Contains("eis")).FirstOrDefault();

            if (eisDirs != null)
                eisMeasurementDir = eisDirs;
            else
            {
                _validationLog.Add($"WARNING: No EIS Measurement directory found in {batteryId}");
                return;
            }
        }

        // Find test directories (Test_1, Test_2)
        var testDirectories = Directory.GetDirectories(eisMeasurementDir).Where(dir => IsTestDirectory(Path.GetFileName(dir))).OrderBy(dir => dir);

        foreach (var testDir in testDirectories)
        {
            ProcessTestDirectory(testDir, batteryId, eisFiles);
        }
    }

    private void ProcessTestDirectory(string testDir, string batteryId, List<EisFileData> eisFiles)
    {
        var testId = Path.GetFileName(testDir);

        // Find all CSV files in test directory
        var csvFiles = Directory.GetFiles(testDir, "*.csv", SearchOption.TopDirectoryOnly);

        foreach (var csvFile in csvFiles)
        {
            try
            {
                var eisFileData = ProcessSingleCsvFile(csvFile, batteryId, testId);
                if (eisFileData != null)
                {
                    eisFiles.Add(eisFileData);
                }
            }
            catch (Exception ex)
            {
                _validationLog.Add($"ERROR: Failed to process {csvFile}: {ex.Message}");
            }
        }
    }

    private EisFileData ProcessSingleCsvFile(string csvFilePath, string batteryId, string testId)
    {
        var fileName = Path.GetFileNameWithoutExtension(csvFilePath);
        var soc = ExtractSoCFromFileName(fileName);
        if (soc == null)
        {
            AddWarning($"Could not extract SoC from filename: {fileName}");
            return null;
        }

        var samples = ParseCsvFile(csvFilePath);
        if (samples == null || samples.Count == 0)
        {
            AddWarning($"No valid samples found in: {fileName}");
            return null;
        }

        if (samples.Count != EXPECTED_ROWS_PER_FILE)
        {
            // Samo WARNING, NE dodajemo u SUCCESS
            AddWarning($"Expected {EXPECTED_ROWS_PER_FILE} rows but found {samples.Count} in {fileName}");
            return null; // vrati null, fajl se neće dodati u uspešne
        }

        var eisFileData = new EisFileData
        {
            BatteryId = batteryId,
            TestId = testId,
            SoCPercentage = soc.Value,
            FileName = Path.GetFileName(csvFilePath),
            FilePath = csvFilePath,
            Samples = samples,
            TotalRows = samples.Count
        };

        // Dodajemo u SUCCESS samo ako je broj redova tačan
        AddSuccess($"Processed {fileName} - {batteryId}/{testId}/{soc}% - {samples.Count} samples");
        return eisFileData;
    }

    private int? ExtractSoCFromFileName(string fileName)
    {
        // Pattern to match SoC percentages: 5, 10, 15, ..., 100
        // Looking for patterns like: filename_50_something.csv, filename50.csv, 50_filename.csv, etc.
        var patterns = new[]
        {
            @"_(\d+)_",           // _50_
            @"_(\d+)\.csv",       // _50.csv  
            @"^(\d+)_",           // 50_filename
            @"(\d+)\.csv$",       // filename50.csv
            @"[^0-9](\d+)[^0-9]"  // any non-digit followed by digits followed by non-digit
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(fileName, pattern, RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int soc))
            {
                // Validate that it's a valid SoC percentage (5, 10, 15, ..., 100)
                if (soc >= 5 && soc <= 100 && soc % 5 == 0)
                {
                    return soc;
                }
            }
        }

        return null;
    }

    private List<EisSample> ParseCsvFile(string csvFilePath)
    {
        var samples = new List<EisSample>();

        try
        {
            using (var reader = new StreamReader(csvFilePath))
            {
                string line;
                int rowIndex = 0;
                bool isFirstLine = true;

                while ((line = reader.ReadLine()) != null)
                {
                    if (isFirstLine && IsHeaderLine(line))
                    {
                        isFirstLine = false;
                        continue;
                    }

                    var sample = ParseCsvLine(line, rowIndex, csvFilePath);
                    if (sample != null)
                    {
                        samples.Add(sample);
                        rowIndex++;
                    }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        AddWarning($"Invalid row in {Path.GetFileName(csvFilePath)}, line {rowIndex + 1}: {line}");
                    }

                    isFirstLine = false;
                }
            }
        }
        catch (Exception ex)
        {
            AddWarning($"Exception reading CSV {csvFilePath}: {ex.Message}");
            return null;
        }

        return samples;
    }

    private EisSample ParseCsvLine(string line, int rowIndex, string filePath)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        try
        {
            // Split by comma and trim whitespace
            var fields = line.Split(',').Select(f => f.Trim()).ToArray();

            // Expected format: FrequencyHz, R_ohm, X_ohm, V, T_degC, Range_ohm
            if (fields.Length < 6)
            {
                return null;
            }

            // Parse using invariant culture (dot as decimal separator)
            var sample = new EisSample
            {
                FrequencyHz = ParseDouble(fields[0], "FrequencyHz"),
                R_ohm = ParseDouble(fields[1], "R_ohm"),
                X_ohm = ParseDouble(fields[2], "X_ohm"),
                V = ParseDouble(fields[3], "V"),
                T_degC = ParseDouble(fields[4], "T_degC"),
                Range_ohm = ParseDouble(fields[5], "Range_ohm"),
                RowIndex = rowIndex
            };

            // Basic validation
            if (sample.FrequencyHz <= 0)
                return null;

            return sample;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private double ParseDouble(string value, string fieldName)
    {
        // Use invariant culture for parsing (dot as decimal separator)
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
        {
            return result;
        }

        throw new FormatException($"Cannot parse {fieldName}: {value}");
    }

    private bool IsHeaderLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        // Check if line contains typical header keywords
        var lowerLine = line.ToLower();
        return lowerLine.Contains("frequency") ||
               lowerLine.Contains("impedance") ||
               lowerLine.Contains("voltage") ||
               lowerLine.Contains("temperature") ||
               lowerLine.Contains("range");
    }

    private bool IsBatteryDirectory(string dirName)
    {
        // Match B01, B02, ..., B11 pattern
        return Regex.IsMatch(dirName, @"^B\d{2}$", RegexOptions.IgnoreCase);
    }

    private bool IsTestDirectory(string dirName)
    {
        // Match Test_1, Test_2 pattern
        return Regex.IsMatch(dirName, @"^Test_[12]$", RegexOptions.IgnoreCase);
    }

    private void LogProcessingSummary(List<EisFileData> eisFiles)
    {
        _successLog.Add("\n=== PROCESSING SUMMARY ===");

        var summary = eisFiles
            .GroupBy(f => f.BatteryId)
            .Select(g => new
            {
                Battery = g.Key,
                Files = g.Count(),
                Tests = g.Select(f => f.TestId).Distinct().Count(),
                SoCLevels = g.Select(f => f.SoCPercentage).Distinct().OrderBy(s => s).ToList()
            });

        foreach (var item in summary)
        {
            // Formatiramo svaki SoC sa procentom
            var socWithPercent = item.SoCLevels.Select(s => $"{s}%");
            _successLog.Add($"{item.Battery}: {item.Files} files, {item.Tests} tests, SoC levels: {string.Join(", ", socWithPercent)}");
        }

        _successLog.Add($"Total processed files: {eisFiles.Count}");
    }
}