using System.Collections;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using TamakenService.Log;
using TamakenService.Services.TextFileFlter;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine($"Windows:{RuntimeInformation.IsOSPlatform(OSPlatform.Windows)}");
        Console.WriteLine($"Linux:{RuntimeInformation.IsOSPlatform(OSPlatform.Linux)}");
        Console.WriteLine($"OSX:{RuntimeInformation.IsOSPlatform(OSPlatform.OSX)}");
        var logger = new NlogService($"log/{DateTime.Now.ToString("yyyyMMdd")}.log");

        Console.WriteLine($"是否需要開啟debugmod，如不需要可以直接跳過");
        string? debugmodeInput = logger.ReadLine();
        if (!string.IsNullOrWhiteSpace(debugmodeInput)) logger.IsDebugMod = true;

        logger.WriteLine("請輸入篩選條件檔的完整路徑(檔案必須為CSV檔，且第一欄為判斷條件，第二欄為欄位顯示名稱，第三欄為數值化條件)：", true);
        string SNPfilePath = @"C:\cubic\Chip\SNP.csv";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) SNPfilePath = $"{AppDomain.CurrentDomain.BaseDirectory}Chip/SNP.csv";
        logger.WriteLine("預設：" + SNPfilePath, true);
        string? inputSNPfilePath = logger.ReadLine();
        if (!String.IsNullOrEmpty(inputSNPfilePath))
        {
            SNPfilePath = inputSNPfilePath;
        }
        var filterCriteria = new FilterCriteria(SNPfilePath, logger);//設定讀取Sample資料的條件物件物件
        var SNPIndexList = filterCriteria.GetSNPIndexList();//取得欲分析的基因名稱
        var SNPHashtable = filterCriteria.GetSNPHashtable();//取得顯示基因名稱與sample基因名稱對應Hashtable
        var SNPMathFeatureHashtable = filterCriteria.GetSNPMathFeatureHashtable();//取得數值化參數與sample基因名稱對應hashtabll

        logger.WriteLine("請輸入Sample存放資料夾根路徑：", true);
        string rootFilePath = @"C:\cubic\Chip\";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) rootFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}Chip/";
        logger.WriteLine("預設：" + rootFilePath, true);
        string? inputfilePath = logger.ReadLine();
        if (!String.IsNullOrEmpty(inputfilePath))
        {
            rootFilePath = inputfilePath;
        }
        var folderList = new SampleFileGetter(rootFilePath, logger);
        HashSet<string> fileList = folderList.GetFileList;

        logger.WriteLine("請輸入檢測結果的完整路徑：", true);
        string outputPath = @"C:\cubic\Chip\output.csv";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) outputPath = $"{AppDomain.CurrentDomain.BaseDirectory}Chip/output.csv";
        logger.WriteLine("預設：" + outputPath, true);
        string? inputoutputPath = logger.ReadLine();
        if (!String.IsNullOrEmpty(inputoutputPath))
        {
            outputPath = inputoutputPath;
        }
        var ExportFile = new ExportFile(SNPIndexList, SNPHashtable, logger);
        logger.WriteLine("請輸入每次分析資料大小：", true);
        int batchSize = 100; // 每份的大小
        logger.WriteLine("預設：" + batchSize, true);
        string? inputbatchSize = logger.ReadLine();
        if (!String.IsNullOrEmpty(inputbatchSize) && Regex.IsMatch(inputbatchSize, "^[0-9]*$"))
        {
            batchSize = int.Parse(inputbatchSize);
        }
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
            for (int i = 0; i < fileList.Count; i += batchSize)
            {
                HashSet<string> batch = new HashSet<string>(fileList.Skip(i).Take(batchSize));

                logger.WriteLine($"目前份數:{i}/{fileList.Count}", true);

                ThreadWorker threadWorker = new ThreadWorker(batch, logger);//使用ThreadWorker避免巢狀
                HashSet<ReadSampleData> sampleData = threadWorker.Run();

                ExportFile.SaveSetToCSV(sampleData, outputPath);
                ExportFile.SaveSetToMathCSV(sampleData, SNPMathFeatureHashtable, outputPath.Replace(".csv", "Math.csv"));

                fileList.ExceptWith(batch);// 從原始 fileList 中移除已處理的部分
                sampleData.Clear();
                batch.Clear();
                threadWorker = null;
            }
            logger.WriteLine($"掃描完畢", true);
        }
        catch (Exception ex)
        {
            logger.WriteLine(ex.Message);
            Console.ReadLine();
        }
    }

}
