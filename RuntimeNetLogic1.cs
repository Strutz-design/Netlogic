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

public class RuntimeNetLogic1 : BaseNetLogic
{
    public override void Start()
    {
        var store = Project.Current.Get<SQLiteStore>("DataStores/myDbName");
        var table = store.Tables.Get<SQLiteStoreTable>("myTableName");

        if (store == null || table == null)
        {
            Log.Error("InsertData", "Database store or table not found.");
            return;
        }

        var storageFolder = Project.Current.Get<Folder>("Objects/StorageObjects");

        if (storageFolder == null)
        {
            Log.Error("InsertData", "StorageObjects folder not found.");
            return;
        }

        var partNumbers = storageFolder.Get<Folder>("PartNumbers");
        var descriptions = storageFolder.Get<Folder>("Descriptions");
        var quantities = storageFolder.Get<Folder>("Quantities");

        if (partNumbers == null || descriptions == null || quantities == null)
        {
            Log.Error("InsertData", "One or more storage folders not found.");
            return;
        }

        // **Manually collect only IUAVariable**
        var partVars = GetIUAVariablesFromFolder(partNumbers);
        var descVars = GetIUAVariablesFromFolder(descriptions);
        var qtyVars = GetIUAVariablesFromFolder(quantities);

        int rowCount = Math.Min(partVars.Count, Math.Min(descVars.Count, qtyVars.Count));

        if (rowCount == 0)
        {
            Log.Info("InsertData", "No data found in storage.");
            return;
        }

        try
        {
            for (int i = 0; i < rowCount; i++)
            {
                store.Insert(
                    table.BrowseName,
                    new[] { "Part Number", "Description", "Qty" },
                    new object[,] { { partVars[i].Value, descVars[i].Value, qtyVars[i].Value } }
                );

                Log.Info("InsertData", $"Inserted: {partVars[i].Value}, {descVars[i].Value}, {qtyVars[i].Value}");
            }

            // ✅ **Remove variables after insertion**
            ClearStoredData(partNumbers, descriptions, quantities);
        }
        catch (Exception ex)
        {
            Log.Error("InsertData", $"Failed to insert data: {ex.Message}");
        }
    }

private List<IUAVariable> GetIUAVariablesFromFolder(Folder folder)
    {
        var variables = new List<IUAVariable>();

        foreach (var child in folder.Children)
        {
            if (child is IUAVariable variable)
            {
                variables.Add(variable);
            }
        }

        return variables;
    }

    private void ClearStoredData(Folder partNumbers, Folder descriptions, Folder quantities)
    {
        RemoveAllVariables(partNumbers);
        RemoveAllVariables(descriptions);
        RemoveAllVariables(quantities);

        Log.Info("InsertData", "Stored objects cleared after insertion.");
    }

    private void RemoveAllVariables(Folder folder)
    {
        var toRemove = new List<IUANode>();

        foreach (var child in folder.Children)
        {
            if (child is IUAVariable)
                toRemove.Add(child);
        }

        foreach (var node in toRemove)
        {
            folder.Remove(node);
        }
    }
    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
}
