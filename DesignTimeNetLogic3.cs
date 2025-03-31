#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.EventLogger;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Alarm;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.RAEtherNetIP;
using FTOptix.System;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.SerialPort;
using System.Linq;
using FTOptix.Core;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using FTOptix.DataLogger;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Data.SqlTypes;
#endregion


public class DesignTimeNetLogic3 : BaseNetLogic
{
[ExportMethod]
public void CreateandPopulateDB()
{
    var headers = new string[] { "Part Number", "Description", "Qty" };
    var embDbName = "myDbName";
    var store = Project.Current.Get<SQLiteStore>($"DataStores/{embDbName}");

    if (store == null)
    {
        store = InformationModel.Make<SQLiteStore>(embDbName);
        store.Filename = embDbName;
        store.InMemory = false;
        Project.Current.Get<Folder>("DataStores").Add(store);
    }

    var tableName = "myTableName";
    var table = store.Tables.Get<SQLiteStoreTable>(tableName);

    if (table == null)
    {
        table = InformationModel.Make<SQLiteStoreTable>(tableName);
        store.Tables.Add(table);
    }

    foreach (var header in headers)
    {
        var column = table.Columns.Get<StoreColumn>(header);
        if (column == null)
        {
            column = InformationModel.Make<StoreColumn>(header);
            column.DataType = OpcUa.DataTypes.String;
            table.Columns.Add(column);
        }
    }
    var column1 = table.Columns.Get<StoreColumn>("Part Number");
    var column2 = table.Columns.Get<StoreColumn>("Description");
    var column3 = table.Columns.Get<StoreColumn>("Qty");
    // ✅ **Create Storage Objects inside the Information Model**
    CreateStorageObjects();

    // ✅ **Load Data into Storage Objects Instead of Direct Insert**
    GetDataFromDirectory(store, table, column1, column2, column3);
}
private void CreateStorageObjects()
{
    // 🔹 Ensure "Objects" folder exists in the Information Model
    var rootFolder = Project.Current.Get<Folder>("Objects");

    if (rootFolder == null)
    {
        rootFolder = InformationModel.Make<Folder>("Objects");
        Project.Current.Add(rootFolder);
        Log.Info("CreateStorageObjects", "Objects folder created in project.");
    }

    // 🔹 Ensure "StorageObjects" exists inside "Objects"
    var storageFolder = rootFolder.Get<Folder>("StorageObjects");
    if (storageFolder == null)
    {
        storageFolder = InformationModel.Make<Folder>("StorageObjects");
        rootFolder.Add(storageFolder);
        Log.Info("CreateStorageObjects", "StorageObjects folder created.");
    }

    // 🔹 Create required subfolders
    CreateFolderIfNotExists(storageFolder, "PartNumbers");
    CreateFolderIfNotExists(storageFolder, "Descriptions");
    CreateFolderIfNotExists(storageFolder, "Quantities");

    Log.Info("CreateStorageObjects", "Storage objects created successfully.");
}

// **Helper Function: Ensures a subfolder exists**
private void CreateFolderIfNotExists(Folder parentFolder, string folderName)
{
    if (parentFolder.Get<Folder>(folderName) == null)
    {
        var newFolder = InformationModel.Make<Folder>(folderName);
        parentFolder.Add(newFolder);
        Log.Info("CreateStorageObjects", $"Created folder: {folderName}");
    }
}


private void HandleCsvData(string csvData)
{
    const int ExpectedColumnCount = 3;
    const string DefaultPlaceholder = "N/A";

    var rows = csvData.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
    Log.Info("HandleCsvData", $"Total rows received: {rows.Length}");

    var storageFolder = Project.Current.Get<Folder>("Objects/StorageObjects");

    if (storageFolder == null)
    {
        Log.Error("HandleCsvData", "StorageObjects folder not found.");
        return;
    }

    var partNumbers = storageFolder.Get<Folder>("PartNumbers");
    var descriptions = storageFolder.Get<Folder>("Descriptions");
    var quantities = storageFolder.Get<Folder>("Quantities");

    foreach (var row in rows)
    {
        var cleanedRow = row.Replace("[", "").Replace("]", "").Replace("'", "").Trim();
        Log.Info("HandleCsvData", $"Cleaned Row data: {cleanedRow}");

        var columns = cleanedRow.Split(',')
                                .Select(col => col.Trim())
                                .ToList();

        while (columns.Count < ExpectedColumnCount)
            columns.Add(DefaultPlaceholder);

        if (columns.Count > ExpectedColumnCount)
            columns = columns.Take(ExpectedColumnCount).ToList();

        Log.Info("HandleCsvData", $"Stored Row data: {string.Join(", ", columns)}");

        // Create variables using MakeVariable
        var partVar = InformationModel.MakeVariable(Guid.NewGuid().ToString(), OpcUa.DataTypes.String);
        partVar.Value = columns[0];
        partNumbers.Add(partVar);

        var descVar = InformationModel.MakeVariable(Guid.NewGuid().ToString(), OpcUa.DataTypes.String);
        descVar.Value = columns[1];
        descriptions.Add(descVar);

        var qtyVar = InformationModel.MakeVariable(Guid.NewGuid().ToString(), OpcUa.DataTypes.String);
        qtyVar.Value = columns[2];
        quantities.Add(qtyVar);
    }
}

    private void GetDataFromDirectory(SQLiteStore store, SQLiteStoreTable table, StoreColumn column1, StoreColumn column2, StoreColumn column3)
    {
        string filePath = "C:\\Users\\engin\\Documents\\Rockwell Automation\\FactoryTalk Optix\\Projects\\G509_3_4CLS200RL_2H_OptReg_LT_Re7\\ProjectFiles\\NetSolution\\Directory.exe";
        
        var startInfo = new ProcessStartInfo
        {
            FileName = filePath,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = false
        };

        using (var process = Process.Start(startInfo))
        {
            using (var reader = process.StandardOutput)
            {
                string result = reader.ReadToEnd();
                HandleCsvData(result);
            }
        }
    }


}
