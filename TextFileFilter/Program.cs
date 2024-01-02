using System.Collections;
using System.Runtime.InteropServices;
using TamakenService.Log;
using TamakenService.Services.TextFileFlter;

internal class Program
{

    private static readonly object fileWriteLock = new object();
    private static void Main(string[] args)
    {
        Console.WriteLine($"Windows:{RuntimeInformation.IsOSPlatform(OSPlatform.Windows)}");
        Console.WriteLine($"Linux:{RuntimeInformation.IsOSPlatform(OSPlatform.Linux)}");
        Console.WriteLine($"OSX:{RuntimeInformation.IsOSPlatform(OSPlatform.OSX)}");
        var logger = new NlogService($"log/{DateTime.Now.ToString("yyyyMMdd")}.log");
        try
        {
            /* USEDB
            string dbPath = @"Data Source=C:\cubic\DB\Height.db;Version=3;";
            Console.WriteLine("請輸入 SQLite 資料庫連接字串：");
            Console.WriteLine("預設：" + dbPath);
            string inputDBPath = Console.ReadLine();
            if (!String.IsNullOrEmpty(inputDBPath))
            {
                dbPath = inputDBPath;
            }
            var DBdata = new HeightDB(dbPath);
            var SNPList = DBdata.getASAList();
            */

            logger.WriteLine("請輸入篩選條件檔的完整路徑(檔案必須為CSV檔，且第一欄為判斷條件，第二欄為欄位顯示名稱，第三欄為數值化條件)：");
            string SNPfilePath = @"C:\cubic\Chip\SNP.csv";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) SNPfilePath = $"{AppDomain.CurrentDomain.BaseDirectory}Chip/SNP.csv";
            logger.WriteLine("預設：" + SNPfilePath);
            string? inputSNPfilePath = logger.ReadLine();
            if (!String.IsNullOrEmpty(inputSNPfilePath))
            {
                SNPfilePath = inputSNPfilePath;
            }
            var filterCriteria = new FilterCriteria(SNPfilePath, logger);
            var SNPIndexList = filterCriteria.GetSNPIndexList();
            var SNPHashtable = filterCriteria.GetSNPHashtable();
            var SNPMathFeatureHashtable = filterCriteria.GetSNPMathFeatureHashtable();

            logger.WriteLine("請輸入Sample存放資料夾根路徑：");
            string rootFilePath = @"C:\cubic\Chip\";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) rootFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}Chip/";
            logger.WriteLine("預設：" + rootFilePath);
            string? inputfilePath = logger.ReadLine();
            if (!String.IsNullOrEmpty(inputfilePath))
            {
                rootFilePath = inputfilePath;
            }
            var folderList = new SampleFileGetter(rootFilePath, logger);
            HashSet<string> fileList = folderList.GetFileList;

            logger.WriteLine("請輸入檢測結果的完整路徑：");
            string outputPath = @"C:\cubic\Chip\output.csv";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) outputPath = $"{AppDomain.CurrentDomain.BaseDirectory}Chip/output.csv";
            logger.WriteLine("預設：" + outputPath);
            string? inputoutputPath = logger.ReadLine();
            if (!String.IsNullOrEmpty(inputoutputPath))
            {
                outputPath = inputoutputPath;
            }
            var ExportFile = new ExportFile(SNPIndexList, SNPHashtable, logger);
            /*
            var SampleData = new ReadSampleData(logger);
            foreach (var file in fileList)
            {
                logger.WriteLine($"執行續讀取欲掃描之SampleData 路徑: {file}");
                SampleData.FilePath = file;
                logger.WriteLine($"執行續localSampleData 設定完畢");

                logger.WriteLine($"執行續開始輸出 基因資料");
                ExportFile.SaveToCSV(SampleData, outputPath);
                logger.WriteLine($"輸出基因完畢");

                logger.WriteLine($"執行續開始輸出 數據資料");
                ExportFile.SaveToMathCSV(SampleData, SNPMathFeatureHashtable, outputPath.Replace(".csv", "Math.csv"));
                logger.WriteLine($"輸出數據完畢");
            }*/
            int batchSize = 50; // 每份的大小

            for (int i = 0; i < fileList.Count; i += batchSize)
            {
                HashSet<string> batch = new HashSet<string>(fileList.Skip(i).Take(batchSize));

                // 在這裡使用 batch 做你想做的事情，例如迴圈操作、處理資料等等

                HashSet<ReadSampleData> sampleData = new HashSet<ReadSampleData>();
                Parallel.ForEach(batch, file =>
                {
                    logger.WriteLine($"現在進入執行續");
                    // 這裡的 SampleData 和 exportFile 可能需要做相應的處理以避免多執行緒競爭條件，例如使用區域變數
                    var localSampleData = new ReadSampleData(logger);
                    //var localExportFile = new ExportFile(SNPIndexList, SNPHashtable, logger); // 使用區域變數以避免競爭條件

                    logger.WriteLine($"執行續讀取欲掃描之SampleData 路徑: {file}");
                    localSampleData.FilePath = file;
                    logger.WriteLine($"執行續localSampleData 設定完畢");
                    sampleData.Add(localSampleData);
                    /*
                    lock (fileWriteLock)
                    {
                        logger.WriteLine($"執行續開始輸出 基因資料");
                        localExportFile.SaveToCSV(localSampleData, outputPath);
                        logger.WriteLine($"輸出基因完畢");

                        logger.WriteLine($"執行續開始輸出 數據資料");
                        localExportFile.SaveToMathCSV(localSampleData, SNPMathFeatureHashtable, outputPath.Replace(".csv", "Math.csv"));
                        logger.WriteLine($"輸出數據完畢");
                    }*/
                });
                ExportFile.SaveSetToCSV(sampleData, outputPath);
                ExportFile.SaveSetToMathCSV(sampleData, SNPMathFeatureHashtable, outputPath.Replace(".csv", "Math.csv"));

                // 從原始 fileList 中移除已處理的部分
                fileList.ExceptWith(batch);
            }
            logger.WriteLine($"掃描完畢");
        }
        catch (Exception ex)
        {
            logger.WriteLine(ex.Message);
            Console.ReadLine();
        }
    }

}
