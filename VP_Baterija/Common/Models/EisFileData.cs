using Common.Models;
using System.Collections.Generic;

public class EisFileData
{
    public string BatteryId { get; set; }
    public string TestId { get; set; }
    public int SoCPercentage { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public List<EisSample> Samples { get; set; }
    public int TotalRows { get; set; }

    public EisMeta ToEisMeta()
    {
        return new EisMeta
        {
            BatteryId = BatteryId,
            TestId = TestId,
            SoC = SoCPercentage,
            FileName = FileName,
            TotalRows = TotalRows
        };
    }
}