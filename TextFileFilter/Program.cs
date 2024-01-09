using System.Collections;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using TamakenService.Log;
using TamakenService.Models.TextFileFilter;
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
        var ExportFile = new ExportFile(SNPIndexList, SNPHashtable, logger);//新建輸出檔案物件

        logger.WriteLine("請輸入Sample年份資料：", true);
        string YearfilePath = @"C:\cubic\Chip\File20XX.csv";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) YearfilePath = $"{AppDomain.CurrentDomain.BaseDirectory}Chip/File20XX.csv";
        logger.WriteLine("預設：" + YearfilePath, true);
        string? inputYearfilePath = logger.ReadLine();
        if (!String.IsNullOrEmpty(inputYearfilePath))
        {
            YearfilePath = inputYearfilePath;
        }
        var SampleListFileExists = File.Exists(YearfilePath);

        if(!SampleListFileExists)
        {
            logger.WriteLine($"路徑 {YearfilePath} 不存在，將新建Sample年份資料", true);

            logger.WriteLine("請輸入Sample存放資料夾根路徑：", true);
            string rootFilePath = @"C:\cubic\Chip\";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) rootFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}Chip/";
            logger.WriteLine("預設：" + rootFilePath, true);
            string? inputfilePath = logger.ReadLine();
            if (!String.IsNullOrEmpty(inputfilePath))
            {
                rootFilePath = inputfilePath;
            }
            var folderListAll = new SampleFileGetter(rootFilePath, logger);
            HashSet<string> fileListAll = folderListAll.GetFileList;
            ExportFile.SaveFilePath(YearfilePath, fileListAll);
            logger.WriteLine($"已將 {fileListAll.Count} 筆資料寫入 路徑： {YearfilePath}", true);
        }
        var folderList = new SampleFileGetter(logger);
        folderList.GetFileFromCsv(YearfilePath);
        HashSet<string> fileList = folderList.GetFileList;
        int totalfile = fileList.Count;
        logger.WriteLine($"Sample年份資料中尚有 {totalfile} 筆資料未掃描", true);


        logger.WriteLine("請輸入是否繼續：", true);
        string YNcontinue = logger.ReadLine();
        if (!String.IsNullOrEmpty(YNcontinue))
        {
            return;
        }

        #region 請輸入檢測結果的完整路徑
        logger.WriteLine("請輸入檢測結果的完整路徑：", true);
        string outputPath = @"C:\cubic\Chip\output.csv";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) outputPath = $"{AppDomain.CurrentDomain.BaseDirectory}Chip/output.csv";
        logger.WriteLine("預設：" + outputPath, true);
        string? inputoutputPath = logger.ReadLine();
        if (!String.IsNullOrEmpty(inputoutputPath))
        {
            outputPath = inputoutputPath;
        }
        #endregion

        #region 每次分析資料大小
        logger.WriteLine("請輸入每次分析資料大小：", true);
        int batchSize = 20; // 每份的大小
        logger.WriteLine("預設：" + batchSize, true);
        string? inputbatchSize = logger.ReadLine();
        if (!String.IsNullOrEmpty(inputbatchSize) && Regex.IsMatch(inputbatchSize, "^[0-9]*$"))
        {
            batchSize = int.Parse(inputbatchSize);
        }
        #endregion

        try
        {

            for (int i = 0; i < totalfile; i += batchSize)
            {
                HashSet<string> batch = new HashSet<string>(fileList.Take(batchSize));

                logger.WriteLine($"目前份數:{i} 剩餘: {fileList.Count}", true);

                logger.WriteLine($"開始工作", true);
                ThreadWorker threadWorker = new ThreadWorker(batch, SNPMathFeatureHashtable, logger);//使用ThreadWorker避免巢狀
                HashSet<ExportSampleData> sampleData = threadWorker.Run();
                logger.WriteLine($"工作結束", true);

                logger.WriteLine($"開始輸出基因:{i} 剩餘: {fileList.Count}", true);
                ExportFile.SaveSetToCSV(sampleData, outputPath);
                logger.WriteLine($"開始輸出數據:{i} 剩餘: {fileList.Count}", true);
                ExportFile.SaveSetToMathCSV(sampleData, outputPath.Replace(".csv", "Math.csv"));

                logger.WriteLine($"已輸出當前批次資料", true);
                folderList.UpdateCsvFile(YearfilePath,batch);
                logger.WriteLine($"已更新{YearfilePath}", true);
                fileList.ExceptWith(batch);// 從原始 fileList 中移除已處理的部分

                logger.WriteLine($"開始清除sampleData : {sampleData.Count} 筆", true);
                sampleData.Clear();
                logger.WriteLine($"已移除sampleData", true);
                batch.Clear();
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
