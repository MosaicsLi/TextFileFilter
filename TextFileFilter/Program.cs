using System.Collections;
using System.Runtime.InteropServices;
using TamakenService.Log;
using TamakenService.Services.TextFileFlter;

internal class Program
{
    private static void Main(string[] args)
    {
        try
        {
            Console.WriteLine($"Windows:{RuntimeInformation.IsOSPlatform(OSPlatform.Windows)}");
            Console.WriteLine($"Linux:{RuntimeInformation.IsOSPlatform(OSPlatform.Linux)}");
            Console.WriteLine($"OSX:{RuntimeInformation.IsOSPlatform(OSPlatform.OSX)}");
            var logger = new NlogService($"log/{DateTime.Now.ToString("yyyyMMdd")}.log");


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
            
            Console.WriteLine("請輸入篩選條件檔的完整路徑(檔案必須為CSV檔，且第一欄為判斷條件，第二欄為欄位顯示名稱，第三欄為數值化條件)：");
            string SNPfilePath = @"C:\cubic\Chip\SNP.csv";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) SNPfilePath = $"{AppDomain.CurrentDomain.BaseDirectory}Chip/SNP.csv";
            Console.WriteLine("預設：" + SNPfilePath);
            string inputSNPfilePath = Console.ReadLine();
            if (!String.IsNullOrEmpty(inputSNPfilePath))
            {
                SNPfilePath = inputSNPfilePath;
            }
            var filterCriteria = new FilterCriteria(SNPfilePath, logger);
            /*
            var SNPList = getSNPList(SNPfilePath);
            var SNPHashTable = getSNPHashtable(SNPfilePath);
            var SNPMathFeature = getSNPMathFeatureHashtable(SNPfilePath);
            */

            Console.WriteLine("請輸入Sample存放資料夾根路徑：");
            string rootFilePath = @"C:\cubic\Chip\";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) rootFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}Chip/";
            Console.WriteLine("預設：" + rootFilePath);
            string inputfilePath = Console.ReadLine();
            if (!String.IsNullOrEmpty(inputfilePath))
            {
                rootFilePath = inputfilePath;
            }
            var folderList = new SampleFileGetter(rootFilePath);
            HashSet<string> fileList = folderList.GetFileList;

            Console.WriteLine("請輸入檢測結果的完整路徑：");
            string outputPath = @"C:\cubic\Chip\output.csv";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) outputPath = $"{AppDomain.CurrentDomain.BaseDirectory}Chip/output.csv";
            Console.WriteLine("預設：" + outputPath);
            string inputoutputPath = Console.ReadLine();
            if (!String.IsNullOrEmpty(inputoutputPath))
            {
                outputPath = inputoutputPath;
            }

            var SampleData = new ReadSampleData();
            var exportFile = new ExportFile(filterCriteria.GetSNPIndexList(), filterCriteria.GetSNPHashtable());
            foreach (var file in fileList)
            {
                SampleData.FilePath = file;
                exportFile.SaveToCSV(SampleData, outputPath);
                exportFile.SaveToMathCSV(SampleData, filterCriteria.GetSNPMathFeatureHashtable(), outputPath.Replace(".csv", "Math.csv"));
            }
            /*
            foreach (var folder in folderList)
            {
                Console.WriteLine($"正在讀取資料夾: {folder}");
                string[] txtFiles = Directory.GetFiles(folder, "*.txt");
                foreach (string file in txtFiles)
                {
                    if (file.Contains("Sample_Map"))
                    {
                        Console.WriteLine($"已排除Sample_Map:{file}");
                        continue;
                    }
                    if (file.Contains("SNP_Map"))
                    {
                        Console.WriteLine($"已排除SNP_Map:{file}");
                        continue;
                    }
                    ReadSampleData sampleData = new ReadSampleData(file);
                    sampleData.SaveToCSV(SNPList, SNPHashTable, outputPath);
                    sampleData.SaveToMathCSV(SNPList, SNPHashTable, SNPMathFeature, outputPath.Replace(".csv", "Math.csv"));
                    Console.WriteLine($"已輸出: {Path.GetFileName(file)} 至 {outputPath}");
                }
            }
            */
            Console.WriteLine($"掃描完畢");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message); Console.ReadLine();
        }
    }

}
