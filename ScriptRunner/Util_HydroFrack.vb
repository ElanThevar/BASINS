﻿Imports atcUtility
Imports atcHspfBinOut
Imports HspfSupport
Imports MapWindow.Interfaces
Imports MapWinUtility
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Collections.Specialized
Imports System.Xml
Imports System.Xml.Linq
Imports atcMetCmp
Imports atcData
Imports atcWDM
Imports atcGraph
Imports ZedGraph
Imports Microsoft.Office.Interop


Module Util_HydroFrack
    Public Sub ScriptMain(ByRef aMapWin As IMapWin)
        Dim lTask As Integer = 16
        Select Case lTask
            Case 1 : ConstructHuc8BasedWaterUseFile() ''Task1. get huc8 based water use
            Case 2 : ClassifyWaterYearsForGraph()
            Case 3 : SwapInCBPFlowIntoGCRPRunWDMs() 'turns out the expert system is using the ID 2 as simulated flow
            Case 4 : DurationPlotGCRPvsCBP()
            Case 5 : ClassifyWaterYearsPrecipForGraph()
            Case 6 : ConstructFTableColumnTimeseries()
            Case 7 : ConstructErrorTable()
            Case 81 : ConstructGCRPHspfSubbasinBasedWaterUseFile() 'Step1 of get subbasin based water use
            Case 82 : SumGCRPHspfSubbasinWateruse() 'Step2 of getting subbasin based water use
            Case 83 : BuildGCRPHspfSubbasinWaterUseTimeseries() 'Step3 of getting subbasin based wateruse into WDM
            Case 9 : ExtractUserSpecifiedConstituents()
            Case 91 : ExtractAllWaterUseSumAnnualCfsFromUCI()
            Case 10 : BuildHydroFrackingTimeseriesMonthlyFixedValues()
            Case 101 : SRBCSumupOSup2010QuaterData()
            Case 102 : SRBCOSup2010WholeYear()
            Case 103 : SRBCOSup2010WholeYearRemoveEmpty()
            Case 104 : SBRCOSup2010DailyWUDataToGCRPDailyTimeseries()
            Case 11 : BuildTwoCountiesWateruseTimeseries()

            Case 112 : HydroFrackingDailyValueSumup()
            Case 113 : HydroFrackingDailyValueCompleteYear()

            Case 12 : TwoCountiesOSup2010DailyWUDataToGCRPDailyTimeseries()
            Case 13 : ProjectionPWSCopyToUSGSWaterUseFiles()
            Case 14 : CreateReachFlowOutputDatasetPlaceHoldersInWDM1s()

            Case 15 : RemoveFrackingDataFrom2025ScenarioWDMs()
            Case 16 : AddNewFrackingDataTo2025ScenarioWDMs()
        End Select
    End Sub

    Private Sub HydroFrackingDailyValueCompleteYear()
        Dim lDataFolder As String = "G:\Admin\EPA_HydroFrac_HSPFEval\WaterUse\"
        Dim lDataFilename As String = IO.Path.Combine(lDataFolder, "DailyFrackingWithdrawals.xls")

        Dim lxlApp As New Excel.Application
        Dim lxlWorkbook As Excel.Workbook = Nothing
        Dim lxlSheet As Excel.Worksheet = Nothing
        Dim lDurations() As String = {"HFSumJan - June", "HFSumJuly - Dec"}
        Dim lDuration As String = ""
        Dim lHFDailyWUs As New atcCollection
        Dim lColumnCount As Integer = 0
        Dim lColSubbasin As Integer = 2
        Dim lColBName As Integer = 3
        Dim lColDailyValueStart As Integer = 7
        Dim lNewDailyValues As New List(Of Double)
        Dim lDailyValuesIter() As Double

        Dim lWriteToWDMs As Boolean = True
        Dim lCreateFullYearExcelSheet As Boolean = False

        lxlWorkbook = lxlApp.Workbooks.Open(lDataFilename)

        'get total number of days
        Dim lDays As Integer
        For Each lDuration In lDurations
            lxlSheet = lxlWorkbook.Worksheets(lDuration)
            With lxlSheet
                lDays += .UsedRange.Columns.Count - 1
            End With
        Next

        'build a list of unique locations
        For Each lDuration In lDurations
            lxlSheet = lxlWorkbook.Worksheets(lDuration)
            With lxlSheet
                lColumnCount = .UsedRange.Columns.Count
                Dim lKey As String
                For lRow As Integer = 2 To .UsedRange.Rows.Count
                    lKey = .Cells(lRow, 1).Value
                    If Not lHFDailyWUs.Keys.Contains(lKey) Then
                        ReDim lDailyValuesIter(lDays - 1)
                        lHFDailyWUs.Add(lKey, lDailyValuesIter)
                    End If
                Next
            End With 'xlsheet
        Next 'lDuration

        'fill in the daily values
        'convert from original gallons per day to cfs
        '1 US gallon = 0.133680556 cubic feet
        Dim lG2CFS As Double = 0.133680556
        For Each lDuration In lDurations
            lxlSheet = lxlWorkbook.Worksheets(lDuration)
            Dim lStartDailyIndex As Integer = 0
            If lDuration.Contains("July") Then
                lStartDailyIndex = 181
            End If
            With lxlSheet
                lColumnCount = .UsedRange.Columns.Count
                Dim lKey As String
                Dim lDailyValue As Double
                For lRow As Integer = 2 To .UsedRange.Rows.Count
                    lKey = .Cells(lRow, 1).Value
                    Dim lDailyValueArray() As Double = lHFDailyWUs.ItemByKey(lKey)
                    For lCol As Integer = 2 To lColumnCount
                        If Not Double.TryParse(.Cells(lRow, lCol).value, lDailyValue) Then lDailyValue = 0
                        If lDailyValue > 0 Then lDailyValue = lDailyValue * lG2CFS / 86400.0
                        lDailyValueArray(lStartDailyIndex + lCol - 2) = lDailyValue
                    Next 'lCol
                Next 'lRow
            End With 'xlsheet
        Next 'lDuration

        If lWriteToWDMs Then 'write daily hydro fracking values to 6000 series of datasets
            Dim lWDMDirPaths() As String = {"G:\Admin\GCRPSusq\RunsWithResvWU2005\parms\", "G:\Admin\GCRPSusq\RunsWithResvWU2005\parms\WDMsWithSRBCWUTsers\"}
            Dim lDateStart As Double = Date2J(1985, 1, 1, 0, 0, 0)
            Dim lDateEnd As Double = Date2J(2005, 12, 31, 24, 0, 0)

            For Each lWDMDirPath As String In lWDMDirPaths
                Dim lSusqTransWDM01 As atcWDM.atcDataSourceWDM = New atcWDM.atcDataSourceWDM()
                If Not lSusqTransWDM01.Open(lWDMDirPath & "SusqTrans01.wdm") Then GoTo ThisWDMDir
                Dim lSusqTransWDM02 As atcWDM.atcDataSourceWDM = New atcWDM.atcDataSourceWDM()
                If Not lSusqTransWDM02.Open(lWDMDirPath & "SusqTrans02.wdm") Then GoTo ThisWDMDir
                Dim lSusqTransWDM03 As atcWDM.atcDataSourceWDM = New atcWDM.atcDataSourceWDM()
                If Not lSusqTransWDM03.Open(lWDMDirPath & "SusqTrans03.wdm") Then GoTo ThisWDMDir
                Dim lSusqTransWDM As atcWDM.atcDataSourceWDM = Nothing
                For Each lKey As String In lHFDailyWUs.Keys
                    Dim m As Match = Regex.Match(lKey, "([0-9]+)\-([0-9]+)", RegexOptions.IgnoreCase)
                    If m.Success Then
                        Dim lBasinName As String = m.Groups(1).Value.Trim
                        Dim lSubbasinId As Integer = Integer.Parse(m.Groups(2).Value.Trim)

                        Dim lTsHFrack2005 As atcTimeseries = GCRPSubbasin.BuildDailyTimeseries("HFRAC", 6000 + lSubbasinId, lHFDailyWUs.ItemByKey(lKey), False, lDateStart, lDateEnd, "R:" & lSubbasinId, "HydroFra")
                        If lTsHFrack2005 Is Nothing Then
                            Logger.Dbg(lKey & "->All Zeros")
                            Continue For 'bypass those all zero arrays
                        End If

                        lSusqTransWDM = Nothing
                        Select Case lBasinName
                            Case "020501"
                                lSusqTransWDM = lSusqTransWDM01
                            Case "020502"
                                lSusqTransWDM = lSusqTransWDM02
                            Case "020503"
                                lSusqTransWDM = lSusqTransWDM03
                        End Select

                        If lSusqTransWDM IsNot Nothing Then
                            'Write Hydro fracking WU timeseries into this WDM
                            If Not lSusqTransWDM.AddDataset(lTsHFrack2005, atcDataSource.EnumExistAction.ExistReplace) Then
                                Logger.Dbg("Add lTsHFrack2005 failed.")
                            End If
                        End If
                    End If
                Next

ThisWDMDir:
                lSusqTransWDM01.Clear()
                lSusqTransWDM01 = Nothing
                lSusqTransWDM02.Clear()
                lSusqTransWDM02 = Nothing
                lSusqTransWDM03.Clear()
                lSusqTransWDM03 = Nothing
                System.GC.Collect()
            Next
        End If

        If lCreateFullYearExcelSheet Then
            'Create a new Sheet
            Dim lFullYearSheet As String = "HFSumFullYear"
            Try
                lxlSheet = lxlWorkbook.Worksheets(lFullYearSheet)
            Catch ex As Exception
                lxlSheet = Nothing
            End Try
            If lxlSheet IsNot Nothing Then
                lxlSheet.Delete()
            End If
            lxlSheet = lxlWorkbook.Worksheets(lDurations(0))
            lxlSheet = lxlWorkbook.Worksheets.Add(lxlSheet)
            With lxlSheet
                .Name = lFullYearSheet
                Dim lxlSheet1 As Excel.Worksheet = lxlWorkbook.Worksheets(lDurations(0))
                Dim lColumnsCount1 As Integer = lxlSheet1.UsedRange.Columns.Count
                lxlSheet1.Range(lxlSheet1.Cells(1, 2), lxlSheet1.Cells(1, lColumnsCount1)).Copy()
                .Range(.Cells(2, 1), .Cells(2, 1)).PasteSpecial(Excel.XlPasteType.xlPasteAll, , , Transpose:=True)
                lxlSheet1 = lxlWorkbook.Worksheets(lDurations(1))
                Dim lColumnsCount2 As Integer = lxlSheet1.UsedRange.Columns.Count
                lxlSheet1.Range(lxlSheet1.Cells(1, 2), lxlSheet1.Cells(1, lColumnsCount2)).Copy()
                .Range(.Cells(lColumnsCount1 - 1 + 2, 1), .Cells(lColumnsCount1 - 1 + 2, 1)).PasteSpecial(Excel.XlPasteType.xlPasteAll, , , Transpose:=True)

                .Cells(1, 1).Value = "Location"
                For I As Integer = 0 To lHFDailyWUs.Count - 1
                    .Cells(1, I + 2).Value = lHFDailyWUs.Keys.Item(I)
                    For d As Integer = 0 To lHFDailyWUs.ItemByIndex(I).Length - 1
                        .Cells(d + 2, I + 2).Value = lHFDailyWUs.ItemByIndex(I)(d)
                    Next
                Next
            End With 'lxlSheet
            lxlWorkbook.Save()
        End If 'lCreateFullYearExcelSheet

        lHFDailyWUs.Clear()

        lxlWorkbook.Close()

        lxlApp.Quit()
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlSheet)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbook)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlApp)

        lxlSheet = Nothing
        lxlWorkbook = Nothing
        lxlApp = Nothing
    End Sub

    Private Sub HydroFrackingDailyValueSumup()
        Dim lDataFolder As String = "G:\Admin\EPA_HydroFrac_HSPFEval\WaterUse\"
        Dim lDataFilename As String = IO.Path.Combine(lDataFolder, "DailyFrackingWithdrawals.xls")

        Dim lxlApp As New Excel.Application
        Dim lxlWorkbook As Excel.Workbook = Nothing
        Dim lxlSheet As Excel.Worksheet = Nothing
        Dim lDailyValuesIter() As Double
        Dim lDuration As String = "Jan - June"
        lDuration = "July - Dec"
        lxlWorkbook = lxlApp.Workbooks.Open(lDataFilename)
        lxlSheet = lxlWorkbook.Worksheets(lDuration)
        Dim lHFDailyWUs As New atcCollection
        With lxlSheet
            Dim lColumnCount As Integer = .UsedRange.Columns.Count
            Dim lColSubbasin As Integer = 2
            Dim lColBName As Integer = 3
            Dim lColDailyValueStart As Integer = 7

            Dim lKeyIsNew As Boolean
            For lRow As Integer = 2 To .UsedRange.Rows.Count
                Dim lKey As String = .Cells(lRow, lColBName).Value & "-" & .Cells(lRow, lColSubbasin).Value
                lKeyIsNew = False
                If Not lHFDailyWUs.Keys.Contains(lKey) Then
                    Dim lNewDailyGallonValues(lColumnCount - lColDailyValueStart) As Double 'zero based
                    lHFDailyWUs.Add(lKey, lNewDailyGallonValues)
                    lDailyValuesIter = lNewDailyGallonValues
                    lKeyIsNew = True
                Else
                    lDailyValuesIter = lHFDailyWUs.ItemByKey(lKey)
                End If

                Dim lValue As Double
                For lCol As Integer = lColDailyValueStart To lColumnCount
                    If Not Double.TryParse(.Cells(lRow, lCol).Value, lValue) Then
                        lValue = 0.0
                    End If
                    If lKeyIsNew Then
                        lDailyValuesIter(lCol - lColDailyValueStart) = lValue
                    Else
                        lDailyValuesIter(lCol - lColDailyValueStart) += lValue
                    End If
                Next
            Next 'lRow

            .Range(.Cells(1, lColDailyValueStart), .Cells(1, lColumnCount)).Copy()
        End With

        'Create a new Sheet
        lxlSheet = lxlWorkbook.Worksheets.Add(lxlSheet)
        With lxlSheet
            .Name = "HFSum" & lDuration
            .Range(.Cells(1, 2), .Cells(1, 2)).PasteSpecial(Excel.XlPasteType.xlPasteAll)
            .Cells(1, 1).Value = "Location"
            For I As Integer = 0 To lHFDailyWUs.Count - 1
                .Cells(I + 2, 1).Value = lHFDailyWUs.Keys.Item(I)
                For d As Integer = 0 To UBound(lHFDailyWUs.ItemByIndex(I))
                    .Cells(I + 2, d + 2).Value = lHFDailyWUs.ItemByIndex(I)(d)
                Next
            Next
        End With

        For Each lDailyValuesIter In lHFDailyWUs
            ReDim lDailyValuesIter(0)
        Next

        lHFDailyWUs.Clear()

        lxlWorkbook.Save()
        lxlWorkbook.Close()

        lxlApp.Quit()
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlSheet)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbook)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlApp)

        lxlSheet = Nothing
        lxlWorkbook = Nothing
        lxlApp = Nothing
    End Sub

    Private Sub BuildTwoCountiesWateruseTimeseries()
        'put the two-counties more specific PWS data into 020501's wdm
        'the data were from pre-calculated Excel file as listed below
        'ground water withdrawal in 8000 series
        'surface water withdrawal in 7000 series
        Dim lGCRPWU2005ParmsWDMDir As String = "G:\Admin\GCRPSusq\RunsWithResvWU2005\parms\WDMsWithSRBCWUTsers\"
        'lGCRPWU2005ParmsWDMDir = "G:\Admin\GCRPSusq\RunsWithResvWU2005\parms\"
        Dim lSusqWDM01 As New atcWDM.atcDataSourceWDM
        If Not lSusqWDM01.Open(lGCRPWU2005ParmsWDMDir & "SusqTrans01.wdm") Then Exit Sub

        Dim lDateStart As Double = Date2J(1985, 1, 1, 0, 0, 0)
        Dim lDateEnd As Double = Date2J(2005, 12, 31, 24, 0, 0)
        Dim lTs As atcTimeseries = Nothing
        Dim lTsGCRPDates As New atcTimeseries(Nothing)
        lTsGCRPDates.Values = NewDates(lDateStart, lDateEnd, atcTimeUnit.TUMonth, 1)

        Dim lTwoCountiesPWSDatasetIdLog As String = lGCRPWU2005ParmsWDMDir & "PWS2010_Brad_SusqCOs_DatasetIds.txt"
        Dim lSW As New StreamWriter(lTwoCountiesPWSDatasetIdLog, False)
        lSW.WriteLine("BName" & vbTab & "Subbasin" & vbTab & "Description" & vbTab & "DatasetId" & vbTab & "Constituent" & vbTab & "WDM")

        Dim lDataFilename As String = "G:\Admin\EPA_HydroFrac_HSPFEval\WaterUse\PWS2010_Bradford_Susquehanna.xls"
        Dim lxlApp As New Excel.Application
        Dim lxlWorkbook As Excel.Workbook = Nothing
        Dim lxlSheet As Excel.Worksheet = Nothing
        lxlWorkbook = lxlApp.Workbooks.Open(lDataFilename)
        lxlSheet = lxlWorkbook.Worksheets("PWS2010CFS")
        With lxlSheet
            Dim lColMonths() As Integer = {3, 4, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15} 'for leap year, 4->5

            Dim lGCRPMonthlyValues As New List(Of Double)
            Dim lTwoCounties2010MonthlyValues(11) As Double

            Dim lDatasetCounter As Integer = 0
            For lRow As Integer = 2 To .UsedRange.Rows.Count 'one timeseries per row
                lGCRPMonthlyValues.Clear()
                lGCRPMonthlyValues.Add(GetNaN)
                Dim lRchres As String = .Cells(lRow, 1).Value
                Dim lLocation As String = "R:" & lRchres
                Dim lDescription As String = .Cells(lRow, 2).Value
                Dim lDatasetId As Integer
                'If lDescription.ToLower.Contains("ground") Then
                '    lDatasetId = 8000
                'Else
                '    lDatasetId = 7000 'surface
                'End If

                lDatasetCounter += 1
                lDatasetId = 4118 + lDatasetCounter
                Dim lConstituent As String
                If lDescription.ToLower.Contains("ground") Then
                    lConstituent = "PWGWC"
                Else
                    lConstituent = "PWSWC"
                End If

                For lYear As Integer = 1985 To 2005
                    If Date.IsLeapYear(lYear) Then
                        lColMonths(1) = 5
                    Else
                        lColMonths(1) = 4
                    End If
                    For I As Integer = 0 To lColMonths.Length - 1
                        lTwoCounties2010MonthlyValues(I) = Double.Parse(.Cells(lRow, lColMonths(I)).Value)
                    Next 'I

                    lGCRPMonthlyValues.AddRange(lTwoCounties2010MonthlyValues)

                Next 'lYear

                'Make New Tser
                lTs = New atcTimeseries(Nothing)
                With lTs
                    .Dates = lTsGCRPDates
                    .SetInterval(atcTimeUnit.TUMonth, 1)
                    .Values = lGCRPMonthlyValues.ToArray()
                    '.Attributes.SetValue("ID", lDatasetId + CInt(lRchres))
                    .Attributes.SetValue("ID", lDatasetId)
                    .Attributes.SetValue("Constituent", lConstituent)
                    .Attributes.SetValue("Location", lLocation)
                    .Attributes.SetValue("Scenario", "PWS2CO")
                End With

                'Write to WDM
                If Not lSusqWDM01.AddDataset(lTs, atcDataSource.EnumExistAction.ExistReplace) Then
                    Logger.Dbg("Writing 020501-" & lRchres & " dataset failed in SusqTrans01.wdm")
                Else
                    lSW.WriteLine("020501" & vbTab & lRchres & vbTab & lDescription & vbTab & lDatasetId & vbTab & lConstituent & vbTab & lSusqWDM01.Specification)
                End If
            Next 'lRow

        End With

        lxlWorkbook.Close()
        lxlApp.Quit()
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlSheet)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbook)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlApp)

        lxlSheet = Nothing
        lxlWorkbook = Nothing
        lxlApp = Nothing

        lSW.Flush()
        lSW.Close()
        lSW = Nothing

        lSusqWDM01.Clear()
        lSusqWDM01 = Nothing

        lTs.Clear()
        lTs = Nothing
        lTsGCRPDates.Clear()
        lTsGCRPDates = Nothing
    End Sub

    Private Sub TwoCountiesOSup2010DailyWUDataToGCRPDailyTimeseries()
        'put 2010 two counties (Bradford and Susq counties in PA, 42) data into 2005 run
        'only replace the Rch 39 in 020501 GCRP subbasin
        'Dim lGCRPWU2005ParmsWDMDir As String = "G:\Admin\GCRPSusq\RunsWithResvWU2005\parms\WDMsWithSRBCWUTsers\"
        Dim lGCRPWU2005ParmsWDMDir As String = "G:\Admin\GCRPSusq\RunsWithResvWU2005\parms\"
        Dim lDataFile As String = "G:\Admin\EPA_HydroFrac_HSPFEval\WaterUse\Susq_Withdraw_Xtab&source_2010(noPWS_Frac)withSubbasin.xls"

        Dim lDateStart As Double = Date2J(1985, 1, 1, 0, 0, 0)
        Dim lDateEnd As Double = Date2J(2005, 12, 31, 24, 0, 0)
        Dim lTs As atcTimeseries = Nothing
        Dim lTsGCRPDates As New atcTimeseries(Nothing)
        lTsGCRPDates.Values = NewDates(lDateStart, lDateEnd, atcTimeUnit.TUDay, 1)

        Dim lSusqWDM01 As New atcWDM.atcDataSourceWDM
        If Not lSusqWDM01.Open(lGCRPWU2005ParmsWDMDir & "SusqTrans01.wdm") Then Exit Sub

        'Dim lSusqWDM02 As New atcWDM.atcDataSourceWDM
        'If Not lSusqWDM02.Open(lGCRPWU2005ParmsWDMDir & "SusqTrans02.wdm") Then Exit Sub
        'Dim lSusqWDM03 As New atcWDM.atcDataSourceWDM
        'If Not lSusqWDM03.Open(lGCRPWU2005ParmsWDMDir & "SusqTrans03.wdm") Then Exit Sub

        Dim lxlApp As New Excel.Application
        Dim lxlWorkbook As Excel.Workbook = Nothing
        Dim lxlSheet As Excel.Worksheet = Nothing
        lxlWorkbook = lxlApp.Workbooks.Open(lDataFile)
        lxlSheet = lxlWorkbook.Worksheets("Rearrange")
        With lxlSheet
            Dim lNumDailyValuesInYear2010 As Integer = 365
            'Dim lDailyDates(lNumDailyValuesInYear2010 - 1) As String 'zero-based
            Dim lIndexFeb28 As Integer
            Dim lIndexMar1 As Integer
            Dim lSubbasinID As Integer
            Dim lFacility As String
            Dim lSource As String
            Dim lLat As Double
            Dim lLong As Double
            Dim lIndustry As String
            Dim mPatternDate As String = "\s*([0-9]+)/([0-9]+)/([0-9]+)$"
            Dim lCellValue As String = ""
            Dim lDataRowStartIndex As Integer = 7
            For lRow As Integer = 1 To .UsedRange.Rows.Count 'first row is header row
                lCellValue = .Cells(lRow, 1).Value
                Dim lMatchDate As Match = Regex.Match(lCellValue, mPatternDate, RegexOptions.IgnoreCase)
                If lMatchDate.Success Then
                    If lMatchDate.Groups(1).Value = "2" AndAlso lMatchDate.Groups(2).Value = "28" Then
                        lIndexFeb28 = lRow - lDataRowStartIndex
                        lIndexMar1 = lIndexFeb28 + 1
                        Exit For
                    End If
                ElseIf lCellValue.ToLower.Contains("subbasin") Then
                    lSubbasinID = .Cells(lRow, 2).Value
                ElseIf lCellValue.ToLower.Contains("facility") Then
                    lFacility = .Cells(lRow, 2).Value
                ElseIf lCellValue.ToLower.Contains("source") Then
                    lSource = .Cells(lRow, 2).Value
                ElseIf lCellValue.ToLower.Contains("lat_") Then
                    lLat = Double.Parse(.Cells(lRow, 2).Value)
                ElseIf lCellValue.ToLower.Contains("long_") Then
                    lLong = Double.Parse(.Cells(lRow, 2).Value)
                ElseIf lCellValue.ToLower.Contains("industry") Then
                    lIndustry = .Cells(lRow, 2).Value
                End If
            Next
            'ReDim lDailyDates(0)

            Dim lGCRPDailyValues As New List(Of Double)
            Dim lTwoCounties2010DailyValues() As Double
            For lCol As Integer = .UsedRange.Columns.Count To .UsedRange.Columns.Count
                Dim lBName As String = "020501"
                'Dim lSBId As String = .Cells(1, lCol).Value.ToString.Split("-")(1)

                'transfer into 1-d array, zero-based, also convert from original gallons per day to cfs
                ReDim lTwoCounties2010DailyValues(lNumDailyValuesInYear2010 - 1)
                For lRow As Integer = lDataRowStartIndex To .UsedRange.Rows.Count
                    lTwoCounties2010DailyValues(lRow - lDataRowStartIndex) = Double.Parse(.Cells(lRow, lCol).Value) * 0.133680556 / 86400 '1 US gallon = 0.133680556 cubic feet
                Next

                lGCRPDailyValues.Clear() 'start fresh
                lGCRPDailyValues.Add(Double.NaN)
                For lYear As Integer = 1985 To 2005
                    If Date.IsLeapYear(lYear) Then
                        Dim lArrPart1(lIndexFeb28) As Double
                        Array.Copy(lTwoCounties2010DailyValues, 0, lArrPart1, 0, 31 + 28)
                        lGCRPDailyValues.AddRange(lArrPart1)

                        lGCRPDailyValues.Add((lTwoCounties2010DailyValues(lIndexFeb28) + lTwoCounties2010DailyValues(lIndexMar1)) / 2.0)

                        Dim lArrPart2(UBound(lTwoCounties2010DailyValues, 1) - lIndexMar1) As Double
                        For I As Integer = lIndexMar1 To UBound(lTwoCounties2010DailyValues, 1)
                            lArrPart2(I - lIndexMar1) = lTwoCounties2010DailyValues(I)
                        Next

                        lGCRPDailyValues.AddRange(lArrPart2)

                        ReDim lArrPart1(0)
                        ReDim lArrPart2(0)

                    Else
                        lGCRPDailyValues.AddRange(lTwoCounties2010DailyValues)
                    End If
                Next 'lYear

                'Create a Timeserie
                lTs = New atcTimeseries(Nothing)
                With lTs
                    .Dates = lTsGCRPDates
                    .SetInterval(atcTimeUnit.TUDay, 1)
                    '.numValues = lTsGCRPDates.numValues
                    .Values = lGCRPDailyValues.ToArray()
                    .Attributes.SetValue("ID", 5000 + CInt(lSubbasinID))
                    .Attributes.SetValue("Constituent", "TOSUP")
                    .Attributes.SetValue("TSTYP", "TOSUP")
                    .Attributes.SetValue("Location", "R:" & lSubbasinID)
                    .Attributes.SetValue("Scenario", "TWOCO")
                End With

                'Write to WDM
                Select Case lBName
                    Case "020501"
                        If Not lSusqWDM01.AddDataset(lTs, atcDataSource.EnumExistAction.ExistReplace) Then
                            Logger.Dbg("Writing " & lBName & "-" & lSubbasinID & " dataset failed in SusqTrans01.wdm")
                        End If
                        'Case "020502"
                        '    If Not lSusqWDM02.AddDataset(lTs, atcDataSource.EnumExistAction.ExistReplace) Then
                        '        Logger.Dbg("Writing " & lBName & "-" & lSBId & " dataset failed in SusqTrans02.wdm")
                        '    End If
                        'Case "020503"
                        '    If Not lSusqWDM03.AddDataset(lTs, atcDataSource.EnumExistAction.ExistReplace) Then
                        '        Logger.Dbg("Writing " & lBName & "-" & lSBId & " dataset failed in SusqTrans03.wdm")
                        '    End If
                End Select
            Next 'lCol
        End With

        lxlWorkbook.Close()
        lxlApp.Quit()
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlSheet)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbook)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlApp)

        lxlSheet = Nothing
        lxlWorkbook = Nothing
        lxlApp = Nothing

        lSusqWDM01.Clear()
        'lSusqWDM02.Clear()
        'lSusqWDM03.Clear()
        lSusqWDM01 = Nothing
        'lSusqWDM02 = Nothing
        'lSusqWDM03 = Nothing
    End Sub

    Private Sub SBRCOSup2010DailyWUDataToGCRPDailyTimeseries()
        'put 2010 SRBC data into 2005 run
        Dim lGCRPWU2005ParmsWDMDir As String = "G:\Admin\GCRPSusq\RunsWithResvWU2005\parms\WDMsWithSRBCWUTsers\"
        Dim lDataFile As String = "G:\Admin\EPA_HydroFrac_HSPFEval\WaterUse\SRBCOsup2010Data\SRBC2010DailyGallonWU.xls"

        Dim lDateStart As Double = Date2J(1985, 1, 1, 0, 0, 0)
        Dim lDateEnd As Double = Date2J(2005, 12, 31, 24, 0, 0)
        Dim lTs As atcTimeseries = Nothing
        Dim lTsGCRPDates As New atcTimeseries(Nothing)
        lTsGCRPDates.Values = NewDates(lDateStart, lDateEnd, atcTimeUnit.TUDay, 1)

        Dim lSusqWDM01 As New atcWDM.atcDataSourceWDM
        If Not lSusqWDM01.Open(lGCRPWU2005ParmsWDMDir & "SusqTrans01.wdm") Then Exit Sub
        Dim lSusqWDM02 As New atcWDM.atcDataSourceWDM
        If Not lSusqWDM02.Open(lGCRPWU2005ParmsWDMDir & "SusqTrans02.wdm") Then Exit Sub
        Dim lSusqWDM03 As New atcWDM.atcDataSourceWDM
        If Not lSusqWDM03.Open(lGCRPWU2005ParmsWDMDir & "SusqTrans03.wdm") Then Exit Sub

        Dim lxlApp As New Excel.Application
        Dim lxlWorkbook As Excel.Workbook = Nothing
        Dim lxlSheet As Excel.Worksheet = Nothing
        lxlWorkbook = lxlApp.Workbooks.Open(lDataFile)
        lxlSheet = lxlWorkbook.Worksheets("RemoveEmpty")
        With lxlSheet
            Dim lNumDailyValuesInYear2010 As Integer = .UsedRange.Rows.Count - 1
            'Dim lDailyDates(lNumDailyValuesInYear2010 - 1) As String 'zero-based
            Dim lArr() As String
            Dim lIndexFeb28 As Integer
            Dim lIndexMar1 As Integer
            For lRow As Integer = 2 To .UsedRange.Rows.Count 'first row is header row
                'lDailyDates(lRow - 2) = .Cells(lRow, 1).Value
                'lArr = lDailyDates(lRow - 2).Split("/")
                lArr = .Cells(lRow, 1).Value.ToString.Split("/")
                If lArr(0) = 2 AndAlso lArr(1) = 28 Then
                    lIndexFeb28 = lRow - 2
                    lIndexMar1 = lIndexFeb28 + 1
                End If
            Next
            'ReDim lDailyDates(0)

            'Dim lDaily2010Values2D(,) As Object = Nothing

            Dim lGCRPDailyValues As New List(Of Double)
            Dim lSBRC2010DailyValues() As Double
            For lCol As Integer = 2 To .UsedRange.Columns.Count
                Dim lBName As String = .Cells(1, lCol).Value.ToString.Split("-")(0)
                Dim lSBId As String = .Cells(1, lCol).Value.ToString.Split("-")(1)
                'lDaily2010Values2D = .Range(.Cells(2, lCol), .Cells(.UsedRange.Rows.Count, 2)).Value
                'transfer into 1-d array, zero-based, also convert from original gallons per day to cfs
                ReDim lSBRC2010DailyValues(lNumDailyValuesInYear2010 - 1)
                For lRow As Integer = 2 To .UsedRange.Rows.Count
                    lSBRC2010DailyValues(lRow - 2) = Double.Parse(.Cells(lRow, lCol).Value) * 0.133680556 / 86400 '1 US gallon = 0.133680556 cubic feet
                Next

                lGCRPDailyValues.Clear() 'start fresh
                lGCRPDailyValues.Add(Double.NaN)
                For lYear As Integer = 1985 To 2005
                    If Date.IsLeapYear(lYear) Then
                        Dim lArrPart1(lIndexFeb28) As Double
                        Array.Copy(lSBRC2010DailyValues, 0, lArrPart1, 0, 31 + 28)
                        lGCRPDailyValues.AddRange(lArrPart1)

                        lGCRPDailyValues.Add((lSBRC2010DailyValues(lIndexFeb28) + lSBRC2010DailyValues(lIndexMar1)) / 2.0)

                        Dim lArrPart2(UBound(lSBRC2010DailyValues, 1) - lIndexMar1) As Double
                        For I As Integer = lIndexMar1 To UBound(lSBRC2010DailyValues, 1)
                            lArrPart2(I - lIndexMar1) = lSBRC2010DailyValues(I)
                        Next

                        lGCRPDailyValues.AddRange(lArrPart2)

                        ReDim lArrPart1(0)
                        ReDim lArrPart2(0)

                    Else
                        lGCRPDailyValues.AddRange(lSBRC2010DailyValues)
                    End If
                Next 'lYear

                'Create a Timeserie
                lTs = New atcTimeseries(Nothing)
                With lTs
                    .Dates = lTsGCRPDates
                    .SetInterval(atcTimeUnit.TUDay, 1)
                    '.numValues = lTsGCRPDates.numValues
                    .Values = lGCRPDailyValues.ToArray()
                    .Attributes.SetValue("ID", 5000 + CInt(lSBId))
                    .Attributes.SetValue("Constituent", "SOSUP")
                    .Attributes.SetValue("Location", "R:" & lSBId)
                    .Attributes.SetValue("Scenario", "SBRCOSup")
                End With

                'Write to WDM
                Select Case lBName
                    Case "020501"
                        If Not lSusqWDM01.AddDataset(lTs, atcDataSource.EnumExistAction.ExistReplace) Then
                            Logger.Dbg("Writing " & lBName & "-" & lSBId & " dataset failed in SusqTrans01.wdm")
                        End If
                    Case "020502"
                        If Not lSusqWDM02.AddDataset(lTs, atcDataSource.EnumExistAction.ExistReplace) Then
                            Logger.Dbg("Writing " & lBName & "-" & lSBId & " dataset failed in SusqTrans02.wdm")
                        End If
                    Case "020503"
                        If Not lSusqWDM03.AddDataset(lTs, atcDataSource.EnumExistAction.ExistReplace) Then
                            Logger.Dbg("Writing " & lBName & "-" & lSBId & " dataset failed in SusqTrans03.wdm")
                        End If
                End Select
            Next 'lCol
        End With

        lxlWorkbook.Close()
        lxlApp.Quit()
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlSheet)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbook)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlApp)

        lxlSheet = Nothing
        lxlWorkbook = Nothing
        lxlApp = Nothing

        lSusqWDM01.Clear()
        lSusqWDM02.Clear()
        lSusqWDM03.Clear()
        lSusqWDM01 = Nothing
        lSusqWDM02 = Nothing
        lSusqWDM03 = Nothing

    End Sub

    Private Sub SRBCOSup2010WholeYearRemoveEmpty()

        'This step go through the resulting whole year daily SRBC OSup water use data
        'and remove those locations that don't have even one day non-zero water use
        'The 'RemoveEmpty' worksheet starts out as a copy of the 'SRBCQuaterSum' sheet
        Dim lDataFile As String = "G:\Admin\EPA_HydroFrac_HSPFEval\WaterUse\SRBCOsup2010Data\SRBC2010DailyGallonWU.xls"
        Dim lxlApp As New Excel.Application
        Dim lxlWorkbook As Excel.Workbook = Nothing
        Dim lxlSheet As Excel.Worksheet = Nothing
        lxlWorkbook = lxlApp.Workbooks.Open(lDataFile)
        lxlSheet = lxlWorkbook.Worksheets("RemoveEmpty")
        With lxlSheet
            Dim lNumColumns As Integer = .UsedRange.Columns.Count
            Dim lNumRows As Integer = .UsedRange.Rows.Count
            For lCol As Integer = 2 To lNumColumns
                Dim lColumnHasNonZeroValues As Boolean = False
                For lRow As Integer = 2 To lNumRows
                    If .Cells(lRow, lCol).Value IsNot Nothing AndAlso Double.Parse(.Cells(lRow, lCol).Value) > 0 Then
                        lColumnHasNonZeroValues = True
                        Exit For
                    ElseIf .Cells(lRow, lCol).value Is Nothing Then
                        .Cells(lRow, lCol).Value = 0
                    End If
                Next
                If Not lColumnHasNonZeroValues Then
                    .Range(.Cells(1, lCol), .Cells(lNumRows, lCol)).ClearContents()
                End If
            Next
        End With

        lxlWorkbook.Save()
        lxlWorkbook.Close()
        lxlApp.Quit()
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlSheet)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbook)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlApp)

        lxlSheet = Nothing
        lxlWorkbook = Nothing
        lxlApp = Nothing

    End Sub

    Private Sub SRBCOSup2010WholeYear()
        'This step pull water use data from the four quarters' files into one complete year's record for all locations
        Dim lDataFolder As String = "G:\Admin\EPA_HydroFrac_HSPFEval\WaterUse\SRBCOsup2010Data\"
        Dim lDatafilenamepart1 As String = "2010_withdraw_q"
        Dim lDatafilenamepart2 As String = "_reprojected_withSubbasins.xls"
        Dim lQuarters() As String = {"1", "2", "3", "4"}
        Dim lxlApp As New Excel.Application
        Dim lxlWorkbook As Excel.Workbook = Nothing
        Dim lxlSheet As Excel.Worksheet = Nothing
        Dim lxlSheetSrc As Excel.Worksheet = Nothing

        'First, build a complete list of locations, SBName-Rchres#
        Dim lSRBCOSupWUs As New atcCollection
        For Each lQuarter As String In lQuarters
            Dim lDataFilename As String = IO.Path.Combine(lDataFolder, lDatafilenamepart1 & lQuarter & lDatafilenamepart2)
            lxlWorkbook = lxlApp.Workbooks.Open(lDataFilename)
            lxlSheet = lxlWorkbook.Worksheets("SRBCQuaterSum")

            With lxlSheet
                For lRow As Integer = 2 To .UsedRange.Rows.Count
                    Dim lKey As String = .Cells(lRow, 1).Value
                    If lSRBCOSupWUs.Keys.Contains(lKey) Then
                        If Not lSRBCOSupWUs.ItemByKey(lKey).Contains(lDataFilename) Then
                            lSRBCOSupWUs.ItemByKey(lKey) &= lDataFilename & ";"
                        End If
                    Else
                        lSRBCOSupWUs.Add(lKey, lDataFilename & ";")
                    End If
                Next
            End With
            lxlWorkbook.Close()
        Next
        lxlApp.Quit()

        'Build a new excel file that contains the full year of record for all locations, column-wise
        Dim lNewExceFileName As String = lDataFolder & "SRBC2010DailyGallonWU.xls"
        If File.Exists(lNewExceFileName) Then
            TryDelete(lNewExceFileName)
        End If
        lxlApp = New Excel.Application
        Dim lxlWorkbookNew As Excel.Workbook = lxlApp.Workbooks.Add()
        lxlWorkbookNew.SaveAs(lNewExceFileName)
        lxlSheet = lxlWorkbookNew.Worksheets.Add()
        lxlSheet.Name = "SRBC2010DailyGallonWU"
        With lxlSheet
            'Set up the date column to the left
            Dim lRowCounts(4) As Integer
            Dim lRowTargets(4) As Integer
            Dim lTargetRow As Integer
            For Each lQuarter As String In lQuarters
                Dim lDataFilename As String = IO.Path.Combine(lDataFolder, lDatafilenamepart1 & lQuarter & lDatafilenamepart2)
                lxlWorkbook = lxlApp.Workbooks.Open(lDataFilename)
                lxlSheetSrc = lxlWorkbook.Worksheets("SRBCQuaterSum")

                With lxlSheetSrc
                    lRowCounts(lQuarter) = .UsedRange.Columns.Count - 1
                    .Range(.Cells(1, 2), .Cells(1, .UsedRange.Columns.Count)).Copy()

                    Select Case lQuarter
                        Case 1
                            lTargetRow = 2
                            lRowTargets(1) = lTargetRow
                        Case 2
                            lTargetRow = lRowCounts(1) + 2
                            lRowTargets(2) = lTargetRow
                        Case 3
                            lTargetRow = lRowCounts(1) + lRowCounts(2) + 2
                            lRowTargets(3) = lTargetRow
                        Case 4
                            lTargetRow = lRowCounts(1) + lRowCounts(2) + lRowCounts(3) + 2
                            lRowTargets(4) = lTargetRow
                    End Select
                End With
                'lxlSheet.Activate()
                .Range(.Cells(lTargetRow, 1), .Cells(lTargetRow, 1)).PasteSpecial(Paste:=Excel.XlPasteType.xlPasteAll, Operation:=Excel.XlPasteSpecialOperation.xlPasteSpecialOperationNone, SkipBlanks:=False, Transpose:=True)
                lxlWorkbook.Close()
            Next
            lxlWorkbookNew.Save()

            'Start copy daily values from multiple files into one column per location
            Dim lColCounter As Integer = 2
            For Each lKeyLocation As String In lSRBCOSupWUs.Keys
                .Cells(1, lColCounter).Value = lKeyLocation 'column header
                Dim lArr() As String = lSRBCOSupWUs.ItemByKey(lKeyLocation).split(";")
                For Each lFile As String In lArr
                    If lFile.Length > 0 Then
                        Dim m As Match = Regex.Match(lFile, _
                                                     lDatafilenamepart1 & "([1-9])" & lDatafilenamepart2, _
                                                     RegexOptions.IgnoreCase)
                        If m.Success Then
                            lTargetRow = lRowTargets(Integer.Parse(m.Groups(1).Value.Trim()))
                            lxlWorkbook = lxlApp.Workbooks.Open(lFile)
                            lxlSheetSrc = lxlWorkbook.Worksheets("SRBCQuaterSum")

                            With lxlSheetSrc
                                For lRow As Integer = 2 To .UsedRange.Rows.Count
                                    If lKeyLocation = .Cells(lRow, 1).Value Then
                                        'found a matching location, copy its row to new target
                                        .Range(.Cells(lRow, 2), .Cells(lRow, .UsedRange.Columns.Count)).Copy()
                                        Exit For
                                    End If
                                Next
                            End With 'lxlSheetSrc

                            .Range(.Cells(lTargetRow, lColCounter), .Cells(lTargetRow, lColCounter)).PasteSpecial(Paste:=Excel.XlPasteType.xlPasteAll, Operation:=Excel.XlPasteSpecialOperation.xlPasteSpecialOperationNone, SkipBlanks:=False, Transpose:=True)
                            lxlWorkbook.Close()
                        End If 'm.Success
                    End If 'lFile.Length > 0
                Next 'lFile

                lColCounter += 1
                lxlWorkbookNew.Save()
            Next 'lKeyLocation
        End With

        lxlWorkbookNew.Save()
        lxlWorkbookNew.Close()
        lxlApp.Quit()
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlSheetSrc)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlSheet)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbook)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbookNew)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlApp)

        lxlSheet = Nothing
        lxlSheetSrc = Nothing
        lxlWorkbook = Nothing
        lxlWorkbookNew = Nothing
        lxlApp = Nothing
    End Sub

    Private Sub SRBCSumupOSup2010QuaterData()
        Dim lDataFolder As String = "G:\Admin\EPA_HydroFrac_HSPFEval\WaterUse\SRBCOsup2010Data\"
        Dim lDatafilenamepart1 As String = "2010_withdraw_q"
        Dim lDatafilenamepart2 As String = "_reprojected_withSubbasins.xls"
        Dim lQuarters() As String = {"1", "2", "3", "4"}
        Dim lxlApp As New Excel.Application
        Dim lxlWorkbook As Excel.Workbook = Nothing
        Dim lxlSheet As Excel.Worksheet = Nothing
        Dim lDailyValuesIter() As Double
        For Each lQuarter As String In lQuarters
            Dim lDataFilename As String = IO.Path.Combine(lDataFolder, lDatafilenamepart1 & lQuarter & lDatafilenamepart2)
            lxlWorkbook = lxlApp.Workbooks.Open(lDataFilename)
            lxlSheet = lxlWorkbook.Worksheets(1)
            Dim lSRBCOSupWUs As New atcCollection
            With lxlSheet 'there are only five columns, subbasinid, ps2000, osup2000, ps2005, osup2005 withdrawal in cfs
                Dim lColumnCount As Integer = .UsedRange.Columns.Count
                Dim lColSubbasin As Integer = lColumnCount - 1
                Dim lKeyIsNew As Boolean
                For lRow As Integer = 2 To .UsedRange.Rows.Count
                    Dim lKey As String = .Cells(lRow, lColumnCount).Value & "-" & .Cells(lRow, lColSubbasin).Value
                    lKeyIsNew = False
                    If Not lSRBCOSupWUs.Keys.Contains(lKey) Then
                        Dim lNewDailyGallonValues(lColumnCount - 2 - 2) As Double
                        lSRBCOSupWUs.Add(lKey, lNewDailyGallonValues)
                        lDailyValuesIter = lNewDailyGallonValues
                        lKeyIsNew = True
                    Else
                        lDailyValuesIter = lSRBCOSupWUs.ItemByKey(lKey)
                    End If

                    Dim lValue As Double
                    For lCol As Integer = 3 To lColumnCount - 2
                        If Not Double.TryParse(.Cells(lRow, lCol).Value, lValue) Then
                            lValue = 0.0
                        End If
                        If lKeyIsNew Then
                            lDailyValuesIter(lCol - 3 + 1) = lValue
                        Else
                            lDailyValuesIter(lCol - 3 + 1) += lValue
                        End If
                    Next
                Next 'lRow
            End With

            'Create a new Sheet
            lxlSheet = lxlWorkbook.Worksheets.Add(lxlSheet)
            With lxlSheet
                .Name = "SRBCQuaterSum"
                For I As Integer = 0 To lSRBCOSupWUs.Count - 1
                    .Cells(I + 1, 1) = lSRBCOSupWUs.Keys.Item(I)
                    For d As Integer = 1 To UBound(lSRBCOSupWUs.ItemByIndex(I))
                        .Cells(I + 1, d + 1).Value = lSRBCOSupWUs.ItemByIndex(I)(d)
                    Next
                Next
            End With

            For Each lDailyValuesIter In lSRBCOSupWUs
                ReDim lDailyValuesIter(0)
            Next

            lSRBCOSupWUs.Clear()

            lxlWorkbook.Save()
            lxlWorkbook.Close()

        Next 'lQuarter
        lxlApp.Quit()
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlSheet)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbook)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlApp)

        lxlSheet = Nothing
        lxlWorkbook = Nothing
        lxlApp = Nothing

    End Sub

    Private Sub BuildHydroFrackingTimeseriesMonthlyFixedValues()
        'The hydro fracking water use is to be used on the water use 2005 scenario, which is considered as 'current' condition
        Dim lHydroFrackingDataFile As String = "G:\Admin\EPA_HydroFrac_HSPFEval\WaterUse\2010_by_sector_47frackingwithdrawals_reprojected_withSubbasins.xls"
        Dim lxlApp As New Excel.Application
        Dim lxlWorkbook As Excel.Workbook = Nothing
        Dim lxlSheet As Excel.Worksheet = Nothing

        Dim lDateStart As Double = Date2J(1985, 1, 1, 0, 0, 0)
        Dim lDateEnd As Double = Date2J(2005, 12, 31, 24, 0, 0)
        Dim lTU As atcTimeUnit = atcTimeUnit.TUMonth

        lxlWorkbook = lxlApp.Workbooks.Open(lHydroFrackingDataFile)
        lxlSheet = lxlWorkbook.Worksheets(1)
        Dim lColFlowCfs As Integer = 6
        Dim lColSubbasin As Integer = 11
        Dim lColBasinName As Integer = 12
        Dim lHFWUs As New atcCollection
        With lxlSheet 'there are only five columns, subbasinid, ps2000, osup2000, ps2005, osup2005 withdrawal in cfs
            For lRow As Integer = 2 To .UsedRange.Rows.Count
                Dim lKey As String = .Cells(lRow, lColBasinName).Value & "-" & .Cells(lRow, lColSubbasin).Value
                If Not lHFWUs.Keys.Contains(lKey) Then
                    lHFWUs.Add(lKey, Double.Parse(.Cells(lRow, lColFlowCfs).Value))
                Else
                    lHFWUs.ItemByKey(lKey) += Double.Parse(.Cells(lRow, lColFlowCfs).Value)
                End If
            Next
        End With

        lxlWorkbook.Close()
        lxlApp.Quit()
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlSheet)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbook)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlApp)

        lxlSheet = Nothing
        lxlWorkbook = Nothing
        lxlApp = Nothing

        Dim lSusqTransWDM01 As atcWDM.atcDataSourceWDM = New atcWDM.atcDataSourceWDM()
        If Not lSusqTransWDM01.Open("G:\Admin\GCRPSusq\RunsWithResvWU2005\parms\SusqTrans01.wdm") Then Exit Sub
        Dim lSusqTransWDM02 As atcWDM.atcDataSourceWDM = New atcWDM.atcDataSourceWDM()
        If Not lSusqTransWDM02.Open("G:\Admin\GCRPSusq\RunsWithResvWU2005\parms\SusqTrans02.wdm") Then Exit Sub
        Dim lSusqTransWDM03 As atcWDM.atcDataSourceWDM = New atcWDM.atcDataSourceWDM()
        If Not lSusqTransWDM03.Open("G:\Admin\GCRPSusq\RunsWithResvWU2005\parms\SusqTrans03.wdm") Then Exit Sub
        Dim lSusqTransWDM As atcWDM.atcDataSourceWDM = Nothing
        For Each lKey As String In lHFWUs.Keys
            Dim m As Match = Regex.Match(lKey, "([0-9]+)\-([0-9]+)", RegexOptions.IgnoreCase)
            If m.Success Then
                Dim lSubbasinId As Integer = Integer.Parse(m.Groups(2).Value.Trim)
                Dim lBasinName As String = m.Groups(1).Value.Trim

                Dim lTsHFrack2005 As atcTimeseries = GCRPSubbasin.BuildWUTimeseries(atcTimeUnit.TUMonth, "HFRAC", 6000 + lSubbasinId, lHFWUs.ItemByKey(lKey), lDateStart, lDateEnd, "R:" & lSubbasinId, "HydroFracking")
                lSusqTransWDM = Nothing
                Select Case lBasinName
                    Case "020501"
                        lSusqTransWDM = lSusqTransWDM01
                    Case "020502"
                        lSusqTransWDM = lSusqTransWDM02
                    Case "020503"
                        lSusqTransWDM = lSusqTransWDM03
                End Select

                If lSusqTransWDM IsNot Nothing Then
                    'Write Hydro fracking WU timeseries into this WDM
                    If Not lSusqTransWDM.AddDataset(lTsHFrack2005, atcDataSource.EnumExistAction.ExistReplace) Then
                        Logger.Dbg("Add lTsHFrack2005 failed.")
                    End If
                End If

            End If
        Next

        'Clean up
        lHFWUs.Clear()
        lHFWUs = Nothing

        lSusqTransWDM01.Clear()
        lSusqTransWDM01 = Nothing
        lSusqTransWDM02.Clear()
        lSusqTransWDM02 = Nothing
        lSusqTransWDM03.Clear()
        lSusqTransWDM03 = Nothing
        System.GC.Collect()
    End Sub

    Private Sub ExtractAllWaterUseSumAnnualCfsFromUCI()
        'Go through the 3 UCIs and go through each timeseries in 
        'the EXT SOURCES block to create SumAnnual CFS output

        Dim lRunPath As String = "G:\Admin\GCRPSusq\RunsWithResvWU2005\"
        Dim lRunBasins() As String = {"020501", "020502", "020503"}

        Dim lExtSourcesLogFile As String = lRunPath & "ExtSources_WSWFr.txt"
        Dim lSW As New StreamWriter(lExtSourcesLogFile, False)

        For Each lRunBasin As String In lRunBasins
            'Dim lUCIFile As String = lRunPath & "Susq" & lRunBasin & ".uci"

            Dim lInputSourceFile As String = lRunPath & "parms\" & "SusqTrans" & lRunBasin.Substring(4, 2) & ".wdm"
            Dim lWDM As New atcWDM.atcDataSourceWDM
            If Not lWDM.Open(lInputSourceFile) Then Continue For

            For Each lTs As atcTimeseries In lWDM.DataSets
                With lTs.Attributes
                    Dim lid As Integer = .GetValue("ID")
                    If lid > 4000 Then
                        Dim lMax As Double = .GetValue("Max")
                        Dim lMin As Double = .GetValue("Min")
                        Dim lMean As Double = .GetValue("Mean")
                        Dim lSum As Double = .GetValue("Sum")
                        Dim lSumAnnual As Double = .GetValue("SumAnnual")
                        Dim lLocation As String = .GetValue("Location")
                        Dim lCons As String = .GetValue("Constituent")
                        Dim lTimeUnit As String = "<unknown>"
                        Select Case .GetValue("tu")
                            Case atcTimeUnit.TUDay : lTimeUnit = "Daily"
                            Case atcTimeUnit.TUMonth : lTimeUnit = "Monthly"
                        End Select
                        lSW.WriteLine(lRunBasin & vbTab & lLocation & vbTab & lid & vbTab & lCons & vbTab & DoubleToString(lSumAnnual) & vbTab & lSum & vbTab & lMax & vbTab & lMin & vbTab & lMean & vbTab & lTimeUnit)
                    End If
                End With
            Next 'lTs
            lSW.WriteLine(" ")
            lSW.Flush()
            lWDM.Clear()
            lWDM = Nothing
        Next 'lRunBasin
        lSW.Flush()
        lSW.Close()
        lSW = Nothing
    End Sub

    Private Sub ExtractUserSpecifiedConstituents()
        'read the constituents from a user-defined configuration file
        'to retrieve from a HSPF simulation
        Dim lConfigFile As String = "G:\Admin\GCRPSusq\GetConstituentsWU2000.xml"
        lConfigFile = "G:\Admin\GCRPSusq\GetConstituentsWU2005.xml"

        Dim lConfigXML As New Xml.XmlDocument()
        lConfigXML.Load(lConfigFile)

        Dim lReportHeader As New StringBuilder
        Dim lReportStr As New StringBuilder
        Dim lStartFolder As String = lConfigXML.DocumentElement("StartFolder").InnerText
        Dim lContributingAreas As XmlElement = lConfigXML.DocumentElement("ContributingAreas")

        Dim lSW As New StreamWriter(IO.Path.Combine(lStartFolder, "ReportUserSpecifiedOutput_WSWFr.txt"), False)

        Dim lSimulations As XmlNodeList = lConfigXML.GetElementsByTagName("Simulation")
        For Each lSimulation As XmlElement In lSimulations
            Dim lName As String = lSimulation.FirstChild.InnerText
            Dim lOperation As String = lSimulation.Item("Operation").InnerText
            Dim lDatasetGroups As XmlNodeList = lSimulation.GetElementsByTagName("DataSets")
            lReportStr.AppendLine(lName)
            lReportHeader.Append("Constituent" & vbTab)
            Dim lHeaderDone As Boolean = False
            For Each lDatasetGroup As XmlElement In lDatasetGroups
                Dim lConstituentCommon As String = lDatasetGroup.GetAttribute("Constituent")
                If lConstituentCommon <> "" Then
                    lReportStr.Append(lConstituentCommon & vbTab)
                End If
                For Each lDataset As XmlElement In lDatasetGroup.GetElementsByTagName("DataSet")
                    Dim lId As Integer = 0
                    Integer.TryParse(lDataset.GetAttribute("ID"), lId)
                    Dim lDatasource As String = lDataset.GetAttribute("History")
                    Dim lLocation As String = lDataset.GetAttribute("Location")
                    Dim lConstituent As String = lDataset.GetAttribute("Constituent")
                    Dim lCArea As Double = 0 'acre
                    For Each lContributingArea As XmlElement In lContributingAreas.ChildNodes
                        With lContributingArea
                            If .GetAttribute("BasinName") = lName AndAlso .GetAttribute("Location") = lLocation Then
                                lCArea = Double.Parse(.GetAttribute("Area"))
                                Exit For
                            End If
                        End With
                    Next

                    Dim m As Match = Regex.Match(lDatasource, _
                    "Read from {([A-Za-z0-9\-]+)}$", _
                    RegexOptions.IgnoreCase)
                    If (m.Success) Then
                        Dim key As String = m.Groups(1).Value
                        lDatasource = lConfigXML.DocumentElement(key).InnerText
                        lDatasource = lDatasource.Replace("{StartFolder}", lStartFolder)
                    End If

                    If Not lHeaderDone Then lReportHeader.Append(lLocation & "_in" & vbTab & lLocation & "_ac-ft" & vbTab)
                    Dim lDepth As Double = -99.9 'in
                    Dim lVolume As Double = -99.9 'ac-ft
                    If lDatasource.ToLower.Contains(".wdm") Then
                        Dim lWDM As New atcWDM.atcDataSourceWDM
                        If lWDM.Open(lDatasource) Then
                            Dim lTs As atcTimeseries = lWDM.DataSets.FindData("ID", lId)(0)
                            'Dim lMeanAnnual As Double = lTs.Attributes.GetValue(lOperation, -99.0)
                            'special case adjustment for water use, which is actually the daily value
                            Dim lMeanAnnual As Double = lTs.Attributes.GetValue("Mean", -99.0)
                            'convert cf per year to ac-ft
                            lVolume = lMeanAnnual * JulianYear * 24.0 * 60 * 60 / 43560.0
                            lDepth = lVolume / lCArea * 12.0
                        End If
                        lWDM.Clear()
                        lWDM = Nothing
                    ElseIf lDatasource.ToLower.Contains(".uci") Then
                        'Dim lMsg As New atcUCI.HspfMsg
                        'lMsg.Open("hspfmsg.mdb")
                        'Dim lHspfUci As New atcUCI.HspfUci
                        'lHspfUci.FastReadUciForStarter(lMsg, lDatasource)

                        If lConstituent.Contains("GENER") Then
                            Dim lCFactorFlow As Double = 0.12787 'from uci *** 1 MGD = 0.12787 ac-ft/hr (flow)

                            Dim lSRUci As New StreamReader(lDatasource)
                            Dim lOneLine As String
                            While Not lSRUci.EndOfStream
                                lOneLine = lSRUci.ReadLine()
                                Dim lDone As Boolean = False
                                If lOneLine.StartsWith("NETWORK") Then
                                    While Not lOneLine.StartsWith("END NETWORK")
                                        lOneLine = lSRUci.ReadLine()
                                        If lOneLine.StartsWith("GENER    1") Then
                                            m = Regex.Match(lOneLine, _
                                                            "SAME RCHRES(\s+[0-9]+\s+)INFLOW IVOL", _
                                                            RegexOptions.IgnoreCase)
                                            If (m.Success) Then
                                                Dim lReach As String = m.Groups(1).Value.Trim()
                                                If "R:" & lReach = lLocation Then
                                                    Dim p As Match = Regex.Match(lOneLine, _
                                                                                 "GENER    1 OUTPUT TIMSER(\s+[0-9\.]+\s+)SAME RCHRES", _
                                                                                 RegexOptions.IgnoreCase)
                                                    If p.Success Then
                                                        If lVolume < 0 Then lVolume = 0
                                                        If lDepth < 0 Then lDepth = 0
                                                        Dim lThisVolume As Double = 0.0
                                                        Dim lThisDepth As Double = 0.0
                                                        If Double.TryParse(p.Groups(1).Value.Trim(), lThisVolume) Then
                                                            lThisVolume *= lCFactorFlow 'convert from MGD to ac-ft/hr
                                                            lThisVolume *= JulianYear * 24.0 'calculate annual total volume ac-ft
                                                            lThisDepth = lThisVolume / lCArea * 12.0
                                                            lVolume += lThisVolume
                                                            lDepth += lThisDepth
                                                        End If
                                                    End If
                                                End If
                                            End If
                                        End If
                                    End While
                                    Exit While 'after this block, simply quit
                                End If
                            End While
                            lSRUci.Close()
                            lSRUci = Nothing
                        End If 'lConstituent.Contains("GENER")
                    End If 'branch wdm vs uci data sources
                    If lDepth > 0 AndAlso lVolume > 0 Then
                        lReportStr.Append(DoubleToString(lDepth) & vbTab & DoubleToString(lVolume) & vbTab)
                    Else
                        lReportStr.Append("None" & vbTab & "None" & vbTab)
                    End If
                Next 'lDataset
                If Not lHeaderDone Then
                    lHeaderDone = True
                    lReportHeader.AppendLine("")
                End If
                lReportStr.AppendLine("")
            Next 'lDatasetGroup
            lSW.WriteLine(lReportHeader.ToString)
            lSW.WriteLine(lReportStr.ToString)
            lSW.Flush()
            lReportHeader.Length = 0
            lReportStr.Length = 0
        Next 'lSimulation

        lConfigXML = Nothing
        lSW.Close()
        lSW = Nothing
    End Sub

    Private Function ContributingArea(ByVal axmlElement As XmlElement, ByVal aBasinName As String, ByVal aLocation As String) As Double
        For Each lContributingArea As XmlElement In axmlElement.ChildNodes
            With lContributingArea
                If .GetAttribute("BasinName") = aBasinName AndAlso .GetAttribute("Location") = aLocation Then
                    Return Double.Parse(.GetAttribute("Area"))
                End If
            End With
        Next
        Return 0
    End Function

    Private Sub BuildGCRPHspfSubbasinWaterUseTimeseries()
        'This routine is to be run after task 82 (SumGCRPHspfSubbasinWateruse) whose output is like, GCRP020503SubbasinWaterUse2000_WSWFr.txt
        'that file is a comma-delimited subbasin and its water use in cfs listing, e.g. GCRP020501SubbasinWaterUse20002005PSW_OSup_WSWFr.xls
        'the PSW_OSup_WSWFr file is constructed by hand by summing up other withdrawal categories of water use besides PS category
        'there are 3 files, one for each of the GCRP runs. In Each file, there are two sheets, 'WaterUse' and 'Note'. Use the WaterUse sheet

        'In this routine, we will build two timeseries for each subbasin, one for PS, the other for the rest
        'from 1/1/1985 to 12/31/2005, monthly timestep

        'Schematic
        'Within each GCRP run
        '   For each subbasin
        '     2 Water uses in 2 years
        'So there will be 3 version of the parm\SusqTrans.wdm, one for each GCRP run
        'dataset id naming convention is as follows
        '2118 -> PWSup 2000
        '3118 -> OSup 2000
        '4118 -> PWSup 2005
        '5118 -> OSup 2005

        'the last action was to rewrite the 2005 wateruse since PA, Bradford and Susq counties's OSUP are zeros
        'this step should have been done in step 81

        'For the projected PWS scenario, use output from task 82, eg GCRP020503SubbasinWaterUse2005_WSWFr_BAU.txt

        Dim lDirGCRPHspfSubbasinWaterUse As String = "G:\Admin\EPA_HydroFrac_HSPFEval\WaterUse\"

        Dim lWUCategory As String = "_WSWFr"
        Dim lProjectionScenario As String = "" '<--This is for running projected WU scenario, if not scenario run, then use empty string
        lProjectionScenario = "_BAU"
        lProjectionScenario = "_EP"
        lProjectionScenario = "_GT"

        Dim lProjectionYear As String = "" '<--This is the projection year, if not scenario run, then use empty string
        lProjectionYear = "2040"
        lProjectionYear = "2025"

        Dim lDateStart As Double = Date2J(1985, 1, 1, 0, 0, 0)
        Dim lDateEnd As Double = Date2J(2005, 12, 31, 24, 0, 0)
        Dim lTU As atcTimeUnit = atcTimeUnit.TUMonth

        Dim lxlApp As New Excel.Application
        Dim lxlWorkbook As Excel.Workbook = Nothing
        Dim lxlSheet As Excel.Worksheet = Nothing

        Dim lGCRPRunNames() As String = {"020501", "020502", "020503"}
        Dim lTsPWSup2000 As atcTimeseries
        Dim lTsOSup2000 As atcTimeseries
        Dim lTsPWSup2005 As atcTimeseries
        Dim lTsOSup2005 As atcTimeseries

        For Each lGCRPRun As String In lGCRPRunNames
            Dim lProjectedScenFoldername As String = lProjectionScenario & lProjectionYear
            If lProjectedScenFoldername.StartsWith("_") Then
                lProjectedScenFoldername = lProjectedScenFoldername.Substring(1) & "\"
            Else
                lProjectedScenFoldername = ""
            End If
            Dim lSusqTransWDMFilename As String = "G:\Admin\GCRPSusq\RunsWithResvWU2005\parms\" & lProjectedScenFoldername & "SusqTrans" & lGCRPRun.Substring(4) & ".wdm"
            Dim lSusqTransWDM As atcWDM.atcDataSourceWDM = New atcWDM.atcDataSourceWDM()
            If Not lSusqTransWDM.Open(lSusqTransWDMFilename) Then Continue For

            Dim lFileWU As String = lDirGCRPHspfSubbasinWaterUse & "GCRP" & lGCRPRun & "SubbasinWaterUse20002005PSW_OSup" & lWUCategory & lProjectionScenario & lProjectionYear & ".xls"
            lxlWorkbook = lxlApp.Workbooks.Open(lFileWU)
            lxlSheet = lxlWorkbook.Worksheets("WaterUse")
            With lxlSheet 'there are only five columns, subbasinid, ps2000, osup2000, ps2005, osup2005 withdrawal in cfs
                For lRow As Integer = 2 To .UsedRange.Rows.Count
                    Dim lSubbasinId As Integer = .Cells(lRow, 1).Value
                    Dim lWUPWSup2000 As Double = .Cells(lRow, 2).Value
                    Dim lWUOSup2000 As Double = .Cells(lRow, 3).Value
                    Dim lWUPWSup2005 As Double = .Cells(lRow, 4).Value
                    Dim lWUOSup2005 As Double = .Cells(lRow, 5).Value

                    'If lWUPWSup2000 > 0 Then
                    '    lTsPWSup2000 = GCRPSubbasin.BuildWUTimeseries(atcTimeUnit.TUMonth, "PWSUP", 2000 + lSubbasinId, lWUPWSup2000, lDateStart, lDateEnd, "R:" & lSubbasinId, "USGS0")
                    'Else
                    '    lTsPWSup2000 = Nothing
                    'End If
                    'If lWUOSup2000 > 0 Then
                    '    lTsOSup2000 = GCRPSubbasin.BuildWUTimeseries(atcTimeUnit.TUMonth, "OSUP", 3000 + lSubbasinId, lWUOSup2000, lDateStart, lDateEnd, "R:" & lSubbasinId, "USGS0")
                    'Else
                    '    lTsOSup2000 = Nothing
                    'End If
                    If lWUPWSup2005 > 0 Then
                        lTsPWSup2005 = GCRPSubbasin.BuildWUTimeseries(atcTimeUnit.TUMonth, "PWSUP", 4000 + lSubbasinId, lWUPWSup2005, lDateStart, lDateEnd, "R:" & lSubbasinId, "USGS5")
                    Else
                        lTsPWSup2005 = Nothing
                    End If
                    'If lWUOSup2005 > 0 Then
                    '    lTsOSup2005 = GCRPSubbasin.BuildWUTimeseries(atcTimeUnit.TUMonth, "OSUP", 5000 + lSubbasinId, lWUOSup2005, lDateStart, lDateEnd, "R:" & lSubbasinId, "USGS5")
                    'Else
                    '    lTsOSup2005 = Nothing
                    'End If

                    With lSusqTransWDM
                        'Write WU timeseries into this WDM
                        'If lTsPWSup2000 Is Nothing Then
                        '    Logger.Dbg("PWSup2000=zero, " & lFileWU)
                        'ElseIf Not .AddDataset(lTsPWSup2000, atcDataSource.EnumExistAction.ExistReplace) Then
                        '    Logger.Dbg("Add lTsPWSup2000 failed.")
                        'End If

                        'If lTsOSup2000 Is Nothing Then
                        '    Logger.Dbg("OSup2000=zero, " & lFileWU)
                        'ElseIf Not .AddDataset(lTsOSup2000, atcDataSource.EnumExistAction.ExistReplace) Then
                        '    Logger.Dbg("Add lTsOSup2000 failed.")
                        'End If

                        If lTsPWSup2005 Is Nothing Then
                            Logger.Dbg("PWSup2005=zero, " & lFileWU)
                        ElseIf Not .AddDataset(lTsPWSup2005, atcDataSource.EnumExistAction.ExistReplace) Then
                            Logger.Dbg("Add lTsPWSup2005 failed.")
                        End If

                        'If lTsOSup2005 Is Nothing Then
                        '    Logger.Dbg("OSup2005=zero, " & lFileWU)
                        'ElseIf Not .AddDataset(lTsOSup2005, atcDataSource.EnumExistAction.ExistReplace) Then
                        '    Logger.Dbg("Add lTsOSup2005 failed.")
                        'End If
                    End With

                Next 'lxlSheet lRow
            End With 'lxlSheet

            lSusqTransWDM.Clear()
            lSusqTransWDM = Nothing
            lxlWorkbook.Close()

            System.GC.Collect()
        Next 'lGCRPRun

        lxlApp.Quit()
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlSheet)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbook)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlApp)

        lxlSheet = Nothing
        lxlWorkbook = Nothing
        lxlApp = Nothing

    End Sub

    Private Sub SumGCRPHspfSubbasinWateruse()
        'This routine is to be run after task task 81 (whose output file is white-space delimited, e.g. GCRP020501byCountyWaterUse2000_WSWFr.txt)
        'basically, to sum up the various categories of wateruse for each subbasin
        'the output file is comma-delimited (e.g. GCRP020501SubbasinWaterUse2000.txt), unit is cfs (converted from task 8 output in Mgd)
        'water use categories and column order are the same as those in the excel files used in
        'task 8 (ConstructGCRPHspfSubbasinBasedWaterUseFile)

        Dim lWUCat As String = "_WSWFr" '<-- need to take care of this, eg WFrTo or WSWFr or ... depending on the first step script
        Dim lProjectionScenario As String = "" '<--this is the projected WU scenario, if not a scenario run, then use empty string
        lProjectionScenario = "_BAU"
        lProjectionScenario = "_EP"
        lProjectionScenario = "_GT"
        'For scenario run, take task 81's output eg GCRP020501byCountyWaterUse2000_WSWFr_BAU.txt

        Dim lProjectionYear As String = "" '<-- projection year, if no scenario, then use empty string
        lProjectionYear = "2040"
        lProjectionYear = "2025"

        Dim lDirGCRPHspfSubbasinWaterUse As String = "G:\Admin\EPA_HydroFrac_HSPFEval\WaterUse\"
        'Dim lYears() As Integer = {2000, 2005}
        Dim lYears() As Integer = {2005} 'For the scenario run, only done for year 2005
        Dim lGCRPRunNames() As String = {"020501", "020502", "020503"}
        '1 million (US gallons per day) = 1.54722865 (cubic foot) per second
        Dim lMgdToCfsFactor As Double = 1.54722865
        Dim lHeaderText As String = ""
        Dim lSR As StreamReader
        For Each lYearToProcess As Integer In lYears
            For Each lGCRPRunName As String In lGCRPRunNames
                Dim lGCRPRun As New GCRPRun()
                Dim lFileSBbyCountyWU As String = lDirGCRPHspfSubbasinWaterUse & "GCRP" & lGCRPRunName & "byCountyWaterUse" & lYearToProcess & lWUCat & lProjectionScenario & lProjectionYear & ".txt"
                Dim lFileSBWU As String = lDirGCRPHspfSubbasinWaterUse & "GCRP" & lGCRPRunName & "SubbasinWaterUse" & lYearToProcess & lWUCat & lProjectionScenario & lProjectionYear & ".txt"

                If Not IO.File.Exists(lFileSBbyCountyWU) Then Continue For

                lSR = New StreamReader(lFileSBbyCountyWU)
                Dim line As String
                Dim lArr() As String
                Dim lWUValueStartingColIndex As Integer = 7
                Dim lSubbasin As GCRPSubbasin
                While Not lSR.EndOfStream
                    line = lSR.ReadLine().Trim() 'The trim is important so as to remove the last delim char
                    lArr = Regex.Split(line, "\s+")
                    If Not IsNumeric(lArr(2)) Then
                        lHeaderText = "Subbasins,"
                        For H As Integer = lWUValueStartingColIndex To lArr.Length - 1
                            lHeaderText &= lArr(H) & ","
                        Next
                        lHeaderText = lHeaderText.TrimEnd(",")
                        Continue While
                    End If

                    lSubbasin = lGCRPRun.Subbasins.ItemByKey(lArr(2)) '2: subbasin id

                    If lSubbasin Is Nothing Then
                        lSubbasin = New GCRPSubbasin()
                        lGCRPRun.Subbasins.Add(lArr(2), lSubbasin)
                    End If
                    With lSubbasin
                        lSubbasin.SubbasinId = Integer.Parse(lArr(2))
                        If .WUYear = 0 Then .WUYear = lYearToProcess 'set the year, which decide which year it is collating
                        If .NumWUs = 0 Then .NumWUs = lArr.Length - lWUValueStartingColIndex 'set the size and initialize value to zeros
                        If Not .WUAreaList.Contains(lArr(3)) Then .WUAreaList.Add(lArr(3)) '3: county fibs code

                        For I As Integer = lWUValueStartingColIndex To lArr.Length - 1
                            If .WUYear = 2000 Then
                                .WaterUses2000.ItemByIndex(I - lWUValueStartingColIndex) += Double.Parse(lArr(I)) * lMgdToCfsFactor
                            ElseIf .WUYear = 2005 Then
                                .WaterUses2005.ItemByIndex(I - lWUValueStartingColIndex) += Double.Parse(lArr(I)) * lMgdToCfsFactor
                            End If
                        Next
                    End With
                End While
                lSR.Close()
                lSR = Nothing

                'write out result
                lGCRPRun.WriteWaterUse(lYearToProcess, lFileSBWU, lHeaderText)
                lGCRPRun.Clear()
                lGCRPRun = Nothing
            Next 'lGCRPRun
        Next 'lYearToProcess
    End Sub

    Private Sub ConstructGCRPHspfSubbasinBasedWaterUseFile()
        '*** distribute Awuds Excel data into GCRP subbasins
        ' output file is white space delimited
        'files involved example:
        'GCRP020501byCounty.txt 'clip county by GCRP subbasin shapefile
        'County.txt 'for querying county area
        'mdco95_CUPct.csv 'consumptive fraction calculated from 1995 data
        'nyco95_CUPct.csv
        'paco95_CUPct.csv
        'DataDictionaryCompare.txt 'matching wateruse categories among 1995, 2000, 2005 to apply the consumptive fraction

        'The wateruse excel files are the source of raw water use data
        'they MUST have the SAME set of wateruse categories in the SAME COLUMN ORDER
        'this same set in the same order is going to be used by task 82 (SumGCRPHspfSubbasinWateruse)
        'mdco2000SelectedWU_WSWFr.xls 'selected original wateruse data 
        'mdco2005SelectedWU_WSWFr.xls

        '!!!SPECIAL ACTION:!!!
        'Need to zero out all OSup categories in PA's Bradford(015) and Susq (115) counties before proceed!!!

        'Two conditions:
        '1. lApplyConsumptivePct: set to true if want to apply consumptive percentages; set to false if not (meaning using the original numbers)
        '2. lDataYear, need to set to one of the years in that list
        '3. WUCat, the category of wateruse data to be distributed, right now, either WSWFr (surface fresh), or WFrTo (total fresh)

        'Latest controls added
        '- ProjectionScenario: BAU, EP, GT
        '- ProjectionYear: 2025, 2040

        Dim lApplyConsumptivePct As Boolean = True '<--- set this
        Dim lAuxFlag As String = ""
        If Not lApplyConsumptivePct Then
            lAuxFlag = "Full"
        End If

        Dim lProjectionScenario As String = "" '<--this is for distributing WU data that are projected in scenario study, if no scenario, then use empty string
        lProjectionScenario = "_BAU"
        lProjectionScenario = "_EP"
        lProjectionScenario = "_GT"

        Dim lProjectionYear As String = "" '<--this is the year of projected water use for public supply
        lProjectionYear = "2040"
        lProjectionYear = "2025"

        Dim lFipsFieldIndexExcel As Integer = 4
        Dim lWUStartColIndexExcel As Integer = 5
        Dim lFipsFieldIndexCSV1995 As Integer = 4

        Dim lAreaType As String = "co" 'county, if huc8, then h8
        'Dim l2000DataElements() As Integer = {7, 8, 9, 11, 12, 13, 14, 17, 20, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 39, 42, 45, 46, 49, 62, 65, 68}
        'Dim l2005DataElements() As Integer = {9, 12, 15, 19, 20, 21, 22, 23, 24, 27, 30, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 46, 47, 48, 49, 53, 54, 55, 56, 57, 58, 59, 60, 63, 66, 69, 72, 75, 99, 102, 105}
        Dim l2000DataElements() As Integer = {5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17} 'only selected fields
        Dim l2005DataElements() As Integer = {5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19} 'only selected fields
        Dim lDataYear() As Integer = {2000, 2005}
        'Dim lStates() As String = {"mdco", "nyco", "paco"}
        Dim lAwudsDataDirectory As String = "G:\Admin\EPA_HydroFrac_HSPFEval\WaterUse\"


        Dim lSubbasinByCountyFiles As New atcCollection
        With lSubbasinByCountyFiles
            .Add("020501", lAwudsDataDirectory & "GCRP020501byCounty.txt")
            .Add("020502", lAwudsDataDirectory & "GCRP020502byCounty.txt")
            .Add("020503", lAwudsDataDirectory & "GCRP020503byCounty.txt")
        End With

        Dim lAwudsDataFile As String = ""
        Dim lxlApp As Excel.Application = Nothing
        Dim lxlWorkbook As Excel.Workbook = Nothing
        Dim lxlWorkbookPA As Excel.Workbook = Nothing
        Dim lxlWorkbookMD As Excel.Workbook = Nothing
        Dim lxlWorkbookNY As Excel.Workbook = Nothing
        Dim lxlSheet As Excel.Worksheet = Nothing

        Dim lStates As New atcCollection
        'With lStates
        '    Dim lNewState As New WUState
        '    lNewState.Name = "Maryland"
        '    lNewState.Abbreviation = "md"
        '    lNewState.Code = "24"
        '    lStates.Add(lNewState.Code, lNewState)

        '    lNewState = New WUState
        '    lNewState.Name = "New York"
        '    lNewState.Abbreviation = "ny"
        '    lNewState.Code = "36"
        '    lStates.Add(lNewState.Code, lNewState)

        '    lNewState = New WUState
        '    lNewState.Name = "Pennsylvania"
        '    lNewState.Abbreviation = "pa"
        '    lNewState.Code = "42"
        '    lStates.Add(lNewState.Code, lNewState)
        'End With

        '*** 
        Dim File1 As String = ""
        Dim File2 As String = lAwudsDataDirectory & "County.txt"
        'construct county fips-area (sq meter) dictionary
        Dim lOneLine As String
        Dim lArrCounty() As String
        Dim lFips As String = ""
        Dim lStateAbbrev As String = ""

        Dim lLinebuilder As New Text.StringBuilder
        Dim lCountyList As New atcCollection()
        ConstructStateCountyList(File2, lStates)

        'open the three Consumptive percentage files
        'these are preformatted to have the same column headings
        'please note that in 1995, LA means livestock animal specialty, but in 2000 and on, it means aquaculture!!!
        'in 2000 and on, LS = 1995's LV

        Dim lWUCat As String = "_WSWFr" '<--this is to differentiate between total fresh water use (WToFr) vs only surface freshwater use (WSWFr)
        For Each lState As WUState In lStates
            Dim lCuPctFilename As String = lAwudsDataDirectory & lState.Abbreviation.ToLower & lAreaType & "95_CUPct.csv"
            Dim lCuPctTable As New atcTableDelimited
            With lCuPctTable
                .Delimiter = ","
                .OpenFile(lCuPctFilename)
                .CurrentRecord = 1
                Dim lFibsFieldIndex As Integer
                Dim lAttStartFieldIndex As Integer
                For I As Integer = 1 To .NumFields
                    If .FieldName(I).ToLower = "fibs" Then lFibsFieldIndex = I
                    If .FieldName(I).ToLower.StartsWith("cupct_") Then
                        lAttStartFieldIndex = I
                        Exit For
                    End If
                Next
                Dim lValue As Double
                While Not .EOF
                    Dim lCounty As WUCounty = lState.Counties.ItemByKey(.Value(lFibsFieldIndex))
                    If lCounty Is Nothing Then
                        lCounty = New WUCounty
                        With lCounty
                            .Fips = lCuPctTable.Value(lFibsFieldIndex)
                            .State = lState
                            .Code = lCuPctTable.Value(lFibsFieldIndex).Substring(2)
                        End With
                        lState.Counties.Add(lCounty.Fips, lCounty)
                    End If
                    For I As Integer = lAttStartFieldIndex To .NumFields
                        If Not lCounty.CUPcts.ContainsAttribute(.FieldName(I).ToUpper) Then
                            If Not Double.TryParse(.Value(I), lValue) Then
                                lValue = GetNaN()
                            End If
                            lCounty.CUPcts.Add(.FieldName(I), lValue)
                        End If
                    Next
                    .MoveNext()
                End While
                .Clear()
            End With 'lCuPctTable
        Next 'lState

        'Set cross-reference of data dictionary for the chosen categories
        Dim lDDTableFilename As String = lAwudsDataDirectory & "DataDictionaryCompare" & lWUCat & ".txt"
        Dim lDD As New DataDictionaries(lDDTableFilename)

        Dim lYearToProcess As Integer = lDataYear(1) '<-- Set this, pick a year to do
        Dim lFile As String = ""

        lxlApp = New Excel.Application()
        lAwudsDataFile = lAwudsDataDirectory & "mdco" & lYearToProcess & "SelectedWU" & lWUCat & lProjectionScenario & lProjectionYear & ".xls"
        lxlWorkbookMD = lxlApp.Workbooks.Open(lAwudsDataFile)
        lAwudsDataFile = lAwudsDataDirectory & "paco" & lYearToProcess & "SelectedWU" & lWUCat & lProjectionScenario & lProjectionYear & ".xls"
        lxlWorkbookPA = lxlApp.Workbooks.Open(lAwudsDataFile)
        lAwudsDataFile = lAwudsDataDirectory & "nyco" & lYearToProcess & "SelectedWU" & lWUCat & lProjectionScenario & lProjectionYear & ".xls"
        lxlWorkbookNY = lxlApp.Workbooks.Open(lAwudsDataFile)

        Dim lNeedToSetHeader As Boolean = True
        For Each lGCRPRun As String In lSubbasinByCountyFiles.Keys
            File1 = lSubbasinByCountyFiles.ItemByKey(lGCRPRun)
            lFile = IO.Path.Combine(IO.Path.GetDirectoryName(File1), "GCRP" & lGCRPRun & "byCountyWaterUse" & lYearToProcess & lWUCat & lAuxFlag & lProjectionScenario & lProjectionYear & ".txt")

            lOneLine = ""
            ReDim lArrCounty(0)
            Dim lArrGCRPSubbasinbyCounty() As String
            lFips = ""
            Dim lCountyArea As Double = 0
            Dim lAreaPartial As Double = 0
            Dim lAreaFraction As Double = 0
            lLinebuilder.Length = 0

            Dim lSWGCRPSubbasinByCountyData As New StreamWriter(lFile, False)
            Dim lSRGCRPSubbasinByCounty As New StreamReader(File1)
            Dim lStateCode As String = ""
            Dim lState As WUState
            Dim lCounty As WUCounty = Nothing
            Dim lCat As String
            Dim lNeedToWriteHeader As Boolean = True

            While Not lSRGCRPSubbasinByCounty.EndOfStream
                lOneLine = lSRGCRPSubbasinByCounty.ReadLine
                lArrGCRPSubbasinbyCounty = Regex.Split(lOneLine, "\s+")

                lFips = lArrGCRPSubbasinbyCounty(3)
                lStateCode = lFips.Substring(0, 2)
                lState = lStates.ItemByKey(lStateCode)
                If lState IsNot Nothing Then
                    lCounty = lState.Counties.ItemByKey(lFips)
                    If lCounty IsNot Nothing Then
                        lCountyArea = lCounty.Area
                    Else
                        lCountyArea = -99.9
                    End If
                End If

                If lCountyArea < 0 Then Continue While
                If Double.TryParse(lArrGCRPSubbasinbyCounty(4), lAreaPartial) Then
                    lAreaFraction = lAreaPartial / lCountyArea
                Else
                    Continue While
                End If

                lLinebuilder.Append(lOneLine & " ")
                lLinebuilder.Append(String.Format("{0:0.0}", lCountyArea) & " ")
                lLinebuilder.Append(String.Format("{0:0.00}", lAreaFraction) & " ")

                'search for data
                Select Case lStateCode
                    Case "24" : lxlWorkbook = lxlWorkbookMD
                    Case "36" : lxlWorkbook = lxlWorkbookNY
                    Case "42" : lxlWorkbook = lxlWorkbookPA
                End Select

                Dim lDataElements() As Integer = Nothing
                If lYearToProcess = 2000 Then
                    lxlSheet = lxlWorkbook.Worksheets("Data")
                    lDataElements = l2000DataElements
                ElseIf lYearToProcess = 2005 Then
                    lDataElements = l2005DataElements
                    lxlSheet = lxlWorkbook.Worksheets("County")
                End If

                With lxlSheet
                    'Get header for WU categories
                    If lNeedToSetHeader Then
                        If lYearToProcess = 2000 AndAlso WUState.WaterUseCategories2000.Count = 0 Then
                            For C As Integer = lWUStartColIndexExcel To .UsedRange.Columns.Count
                                Dim lHeaderCellValue As String = .Cells(1, C).Value
                                If lHeaderCellValue IsNot Nothing AndAlso lHeaderCellValue <> "" Then
                                    If Not WUState.WaterUseCategories2000.Contains(lHeaderCellValue) Then
                                        WUState.WaterUseCategories2000.Add(lHeaderCellValue)
                                    End If
                                End If
                            Next
                        ElseIf lYearToProcess = 2005 AndAlso WUState.WaterUseCategories2005.Count = 0 Then
                            For C As Integer = lWUStartColIndexExcel To .UsedRange.Columns.Count
                                Dim lHeaderCellValue As String = .Cells(1, C).Value
                                If lHeaderCellValue IsNot Nothing AndAlso lHeaderCellValue <> "" Then
                                    If Not WUState.WaterUseCategories2005.Contains(lHeaderCellValue) Then
                                        WUState.WaterUseCategories2005.Add(lHeaderCellValue)
                                    End If
                                End If
                            Next
                        End If

                        lNeedToSetHeader = False
                    End If
                    If lNeedToWriteHeader Then
                        lSWGCRPSubbasinByCountyData.Write("ShapeId MWShapeId Subbasin Fibs Area CArea AreaFrac ")
                        Dim lHeaderArraylist As ArrayList = Nothing
                        If lYearToProcess = 2000 Then
                            lHeaderArraylist = WUState.WaterUseCategories2000
                        ElseIf lYearToProcess = 2005 Then
                            lHeaderArraylist = WUState.WaterUseCategories2005
                        End If
                        If lHeaderArraylist IsNot Nothing Then
                            Dim lHeaderLine As String = ""
                            For Each lWUCategory As String In lHeaderArraylist
                                lHeaderLine &= lWUCategory & " "
                            Next
                            lSWGCRPSubbasinByCountyData.WriteLine(lHeaderLine.Trim())
                        End If
                        lNeedToWriteHeader = False
                    End If

                    For lRow As Integer = 1 To .UsedRange.Rows.Count
                        If lFips = .Cells(lRow, lFipsFieldIndexExcel).Value Then
                            Dim lValue As Double = 0
                            Dim lCuPctValue As Double = 0
                            Dim lHas1995EquivlentCuPctValue As Boolean
                            For I As Integer = 0 To lDataElements.Length - 1
                                'For I As Integer = 1 To .UsedRange.Columns.Count
                                lCat = .Cells(1, lDataElements(I)).Value
                                'lCat = .Cells(1, I).Value
                                lDD.MatchCategory(lYearToProcess, lCat)
                                lHas1995EquivlentCuPctValue = False
                                If lYearToProcess = 2000 Then
                                    If lDD.Cat1995 <> "None" AndAlso lDD.Cat2000 <> "None" Then
                                        lHas1995EquivlentCuPctValue = True
                                    End If
                                ElseIf lYearToProcess = 2005 Then
                                    If lDD.Cat1995 <> "None" AndAlso lDD.Cat2005 <> "None" Then
                                        lHas1995EquivlentCuPctValue = True
                                    End If
                                End If

                                If lApplyConsumptivePct AndAlso lHas1995EquivlentCuPctValue Then
                                    If lCounty.CUPcts.GetDefinedValue(lDD.Cat1995) Is Nothing OrElse Not Double.TryParse(lCounty.CUPcts.GetDefinedValue(lDD.Cat1995).Value.ToString, lCuPctValue) Then
                                        lCuPctValue = 100.0
                                    Else
                                        If Double.IsNaN(lCuPctValue) OrElse lCuPctValue > 100.0 Then
                                            lCuPctValue = 100.0
                                        End If
                                    End If
                                Else
                                    lCuPctValue = 100.0
                                End If

                                lValue = .Cells(lRow, lDataElements(I)).Value * lAreaFraction * lCuPctValue / 100.0
                                lLinebuilder.Append(DoubleToString(lValue).Replace(",", "") & " ")
                            Next
                            Exit For
                        End If
                    Next
                End With

                lSWGCRPSubbasinByCountyData.WriteLine(lLinebuilder.ToString)
                'output file columns
                'first, original columns from the GCRP02050xbyCounty.txt
                'then, followed by county area in sq_m and area fraction of intersection of subbasin and county
                'then, followed by the wateruse categories from the excel files
                'the file is white-space delimited
                lLinebuilder.Length = 0
            End While
            lSRGCRPSubbasinByCounty.Close()
            lSRGCRPSubbasinByCounty = Nothing

            lSWGCRPSubbasinByCountyData.Flush()
            lSWGCRPSubbasinByCountyData.Close()
            lSWGCRPSubbasinByCountyData = Nothing

        Next 'lGCRPRun
        lDD.Clear()
        lxlWorkbookMD.Close()
        lxlWorkbookNY.Close()
        lxlWorkbookPA.Close()
        lxlApp.Quit()
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlSheet)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbook)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbookMD)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbookNY)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbookPA)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlApp)

        lxlSheet = Nothing
        lxlWorkbook = Nothing
        lxlWorkbookMD = Nothing
        lxlWorkbookNY = Nothing
        lxlWorkbookPA = Nothing
        lxlApp = Nothing
    End Sub

    Private Sub ConstructErrorTable()

        Dim lReports As New atcCollection()
        With lReports
            .Add("_Susq020501_", "R69 (Susq020501),Susquehanna River at Danville PA,USGS01540500")
            .Add("_Susq020502_", "R43 (Susq020502),West Branch Susquehanna River at Lewisburg PA,USGS01553500")
            .Add("_Susq020503_", "R86 (Susq020503),Susquehanna River at Marietta PA,USGS01576000")
            .Add("_SusqCalib_", "R10 (Susq02050303),Raystown Branch Juniata River at Saxton PA,USGS01562000")
        End With

        Dim lAreas As New atcCollection() 'acres
        With lAreas
            .Add("_Susq020501_", 7186006)
            .Add("_Susq020502_", 4384719)
            .Add("_Susq020503_", 16554811)
            .Add("_SusqCalib_", 479637.9)
        End With

        Dim lFlowsDepthObs As New atcCollection() 'inch
        With lFlowsDepthObs
            .Add("_Susq020501_", 19.68)
            .Add("_Susq020502_", 21.74)
            .Add("_Susq020503_", 20.23)
            .Add("_SusqCalib_", 17.59)
        End With

        'Dim lRoot As New DirectoryInfo("G:\Admin\HF_CBP\Reports\")
        'Important Note: the CBP results are not from run, but from swapping in their 
        'results into GCRP Run resulting output WDM file and proceed to do a report
        'the simulation total runoff results are from the Expert output file,
        'NOT from the multi-basin balance output file!!!

        'Dim lRoot As New DirectoryInfo("G:\Admin\GCRPSusq\Reports\")
        'Dim lRoot As New DirectoryInfo("G:\Admin\GCRPSusq\ReportsWithReservoirs\")
        'Dim lRoot As New DirectoryInfo("G:\Admin\GCRPSusq\ReportsWithReservoirsWithWU2000\")
        Dim lRoot As New DirectoryInfo("G:\Admin\GCRPSusq\ReportsWithReservoirsWithWU2005_WSWFr\")

        Dim lFiles As FileInfo() = lRoot.GetFiles("*.*")
        Dim lDirs As DirectoryInfo() = lRoot.GetDirectories("*.*")

        'Console.WriteLine("Root Directories")
        Dim lErrorFileName As String = lRoot.FullName & "ErrorTable.txt"
        Dim lSW As New StreamWriter(lErrorFileName, False)

        Dim lDirectoryName As DirectoryInfo
        Dim lReportMultBalanceBasinsPath As String = ""
        Dim lReportDailyMonthly As String = ""
        Dim lReportExpertSys As String = ""
        Dim lKey As String = ""
        For Each lDirectoryName In lDirs
            If lDirectoryName.FullName.Contains("_Susq020501_") Then
                lKey = "_Susq020501_"
                lReportMultBalanceBasinsPath = "Water_Susq020501_Mult_BalanceBasin.txt"
                lReportDailyMonthly = "DailyMonthlyFlowStats-RCH69.txt"
                lReportExpertSys = "ExpertSysStats-susq020501.txt"
            ElseIf lDirectoryName.FullName.Contains("_Susq020502_") Then
                lKey = "_Susq020502_"
                lReportMultBalanceBasinsPath = "Water_Susq020502_Mult_BalanceBasin.txt"
                lReportDailyMonthly = "DailyMonthlyFlowStats-RCH43.txt"
                lReportExpertSys = "ExpertSysStats-susq020502.txt"
            ElseIf lDirectoryName.FullName.Contains("_Susq020503_") Then
                lKey = "_Susq020503_"
                lReportMultBalanceBasinsPath = "Water_Susq020503_Mult_BalanceBasin.txt"
                lReportDailyMonthly = "DailyMonthlyFlowStats-RCH86.txt"
                lReportExpertSys = "ExpertSysStats-susq020503.txt"
            ElseIf lDirectoryName.FullName.Contains("_SusqCalib_") Then
                lKey = "_SusqCalib_"
                lReportMultBalanceBasinsPath = "Water_SusqCalib_Mult_BalanceBasin.txt"
                lReportDailyMonthly = "DailyMonthlyFlowStats-RCH10.txt"
                lReportExpertSys = "ExpertSysStats-susqcalib.txt"
            End If

            lSW.Write(lReports.ItemByKey(lKey) & ",")
            Dim lSR As New StreamReader(IO.Path.Combine(lDirectoryName.FullName, lReportMultBalanceBasinsPath))
            Dim lVolume As Double 'ac-ft
            Dim lDepth As Double 'in
            While Not lSR.EndOfStream
                Dim line As String = lSR.ReadLine()
                If Not line.StartsWith("  OutVolume") Then Continue While
                Dim lArr() As String = line.Split(vbTab)
                If Double.TryParse(lArr(lArr.Length - 1), lVolume) Then
                    lDepth = lVolume / lAreas.ItemByKey(lKey) * 12.0
                    Exit While
                End If
            End While
            lSR.Close()
            lSR = Nothing

            lSW.Write(DoubleToString(lDepth).Replace(",", "") & "," & lFlowsDepthObs.ItemByKey(lKey) & ",")
            Dim lPctChange As Double = (lDepth - lFlowsDepthObs.ItemByKey(lKey)) / lFlowsDepthObs.ItemByKey(lKey) * 100
            lSW.Write(DoubleToString(lPctChange).Replace(",", "") & ",")

            lSR = New StreamReader(IO.Path.Combine(lDirectoryName.FullName, lReportDailyMonthly))
            Dim lDailyR As Double
            Dim lDailyR2 As Double
            Dim lMonthlyR As Double
            Dim lMonthlyR2 As Double
            Dim lDailyNSE As Double
            Dim lMonthlyNSE As Double
            Dim lTimes As Integer = 0
            While Not lSR.EndOfStream
                Dim line As String = lSR.ReadLine()
                If line.StartsWith("             Correlation Coefficient") AndAlso lTimes = 0 Then
                    lTimes += 1
                    Double.TryParse(line.Substring("             Correlation Coefficient".Length), lDailyR)
                End If
                If line.StartsWith("        Coefficient of Determination") AndAlso lTimes = 1 Then

                    Double.TryParse(line.Substring("        Coefficient of Determination".Length), lDailyR2)
                End If
                If line.StartsWith("                Model Fit Efficiency") AndAlso lTimes = 1 Then
                    lTimes += 1
                    Double.TryParse(line.Substring("                Model Fit Efficiency".Length), lDailyNSE)
                End If

                If line.StartsWith("             Correlation Coefficient") AndAlso lTimes = 2 Then

                    Double.TryParse(line.Substring("             Correlation Coefficient".Length), lMonthlyR)
                End If
                If line.StartsWith("        Coefficient of Determination") AndAlso lTimes = 2 Then

                    Double.TryParse(line.Substring("        Coefficient of Determination".Length), lMonthlyR2)
                End If
                If line.StartsWith("                Model Fit Efficiency") AndAlso lTimes = 2 Then

                    Double.TryParse(line.Substring("                Model Fit Efficiency".Length), lMonthlyNSE)

                End If
            End While
            lSR.Close()
            lSR = Nothing

            lSW.Write(DoubleToString(lDailyR).Replace(",", "") & "," & DoubleToString(lDailyR2).Replace(",", "") & ",")
            lSW.Write(DoubleToString(lMonthlyR).Replace(",", "") & "," & DoubleToString(lMonthlyR2).Replace(",", "") & ",")

            lSR = New StreamReader(IO.Path.Combine(lDirectoryName.FullName, lReportExpertSys))
            Dim lPctPeakDiff As Double
            While Not lSR.EndOfStream
                Dim line As String = lSR.ReadLine()
                If line.StartsWith("  Error in average storm peak (%) =") Then
                    line = line.Substring("  Error in average storm peak (%) =".Length)
                    Dim lArr() As String = Regex.Split(line, "\s+")
                    Double.TryParse(lArr(1), lPctPeakDiff)
                    Exit While
                End If
            End While
            lSR.Close()
            lSR = Nothing

            lSW.Write(DoubleToString(lPctPeakDiff).Replace(",", "") & ",")
            lSW.WriteLine(DoubleToString(lDailyNSE).Replace(",", "") & "," & DoubleToString(lMonthlyNSE).Replace(",", ""))
            lSW.Flush()
        Next

        lSW.Close()
        lSW = Nothing
    End Sub

    Private Sub ConstructFTableColumnTimeseries()

        Dim lTsDates As New atcTimeseries(Nothing)
        Dim lDateStart As Double = Date2J(1985, 1, 1, 0, 0, 0)
        Dim lDateEnd As Double = Date2J(2005, 12, 31, 24, 0, 0)
        lTsDates.Values = NewDates(lDateStart, lDateEnd, atcTimeUnit.TUDay, 1)

        Dim lTsDSN1 As New atcTimeseries(Nothing)
        With lTsDSN1
            .Dates = lTsDates
            .numValues = .Dates.numValues
            .SetInterval(atcTimeUnit.TUDay, 1)
            .Attributes.SetValue("ID", 1)
            .Attributes.SetValue("TSTYP", "RCOL")
            .Attributes.SetValue("Constituent", "RCOL")
        End With

        Dim lTsDSN2 As New atcTimeseries(Nothing)
        With lTsDSN2
            .Dates = lTsDates
            .numValues = .Dates.numValues
            .SetInterval(atcTimeUnit.TUDay, 1)
            .Attributes.SetValue("ID", 2)
            .Attributes.SetValue("TSTYP", "RCOL")
            .Attributes.SetValue("Constituent", "RCOL")
        End With

        Dim lTsDSN3 As New atcTimeseries(Nothing)
        With lTsDSN3
            .Dates = lTsDates
            .numValues = .Dates.numValues
            .SetInterval(atcTimeUnit.TUDay, 1)
            .Attributes.SetValue("ID", 3)
            .Attributes.SetValue("TSTYP", "RCOL")
            .Attributes.SetValue("Constituent", "RCOL")
        End With

        Dim lTsDSN4 As New atcTimeseries(Nothing)
        With lTsDSN4
            .Dates = lTsDates
            .numValues = .Dates.numValues
            .SetInterval(atcTimeUnit.TUDay, 1)
            .Attributes.SetValue("ID", 4)
            .Attributes.SetValue("TSTYP", "RCOL")
            .Attributes.SetValue("Constituent", "RCOL")
        End With

        Dim lTsDSN5 As New atcTimeseries(Nothing)
        With lTsDSN5
            .Dates = lTsDates
            .numValues = .Dates.numValues
            .SetInterval(atcTimeUnit.TUDay, 1)
            .Attributes.SetValue("ID", 5)
            .Attributes.SetValue("TSTYP", "RCOL")
            .Attributes.SetValue("Constituent", "RCOL")
        End With

        Dim lTsDSN6 As New atcTimeseries(Nothing)
        With lTsDSN6
            .Dates = lTsDates
            .numValues = .Dates.numValues
            .SetInterval(atcTimeUnit.TUDay, 1)
            .Attributes.SetValue("ID", 6)
            .Attributes.SetValue("TSTYP", "RCOL")
            .Attributes.SetValue("Constituent", "RCOL")
        End With

        Dim lDate(5) As Integer
        Dim lYear As Integer
        Dim lMonth As Integer
        Dim lDay As Integer
        'Construct DSN1 (SJ4_2060, SJ4_2360, 20503, R3, R6)
        'Start with 4
        'change 4-5 05/14 - 05/15
        'change 5-4 11/14 - 11/15
        'no sliding in transition
        With lTsDSN1
            For I As Integer = 0 To .Dates.numValues
                J2Date(.Dates.Value(I), lDate)
                lMonth = lDate(1) : lDay = lDate(2)
                If lMonth < 5 Then
                    .Value(I + 1) = 4.0
                ElseIf lMonth = 5 Then
                    If lDay <= 14 Then
                        .Value(I + 1) = 4.0
                    Else
                        .Value(I + 1) = 5.0
                    End If
                ElseIf lMonth < 11 Then
                    .Value(I + 1) = 5.0
                ElseIf lMonth = 11 Then
                    If lDay <= 14 Then
                        .Value(I + 1) = 5.0
                    Else
                        .Value(I + 1) = 4.0
                    End If
                Else
                    .Value(I + 1) = 4.0
                End If
            Next
        End With

        'Construct DSN2 (SU2_0741, 20501, R35)
        'Start with 4
        'change 4-5 1989/11/1 - 1990/6/1
        'need to slide scale
        Dim lDate1 As Double = Date2J(1989, 11, 1, 0, 0, 0)
        Dim lDate2 As Double = Date2J(1990, 6, 1, 24, 0, 0)
        With lTsDSN2
            For I As Integer = 0 To .Dates.numValues
                If .Dates.Value(I) < lDate1 Then
                    .Value(I + 1) = 4.0
                ElseIf .Dates.Value(I) >= lDate1 AndAlso .Dates.Value(I) <= lDate2 Then
                    'sliding happens here
                    .Value(I + 1) = (.Dates.Value(I) - lDate1) / (lDate2 - lDate1) + 4.0
                Else
                    .Value(I + 1) = 5.0
                End If
            Next
        End With

        'Construct DSN3 (SU3_0240, 20501, R93)
        'Start with 4
        'change 4-5 04/10 - 05/20 '41 days transition
        'change 5-4 12/01 - 12/10 '10 days transition
        'sliding in transition
        With lTsDSN3
            For I As Integer = 0 To .Dates.numValues
                J2Date(.Dates.Value(I), lDate)
                lMonth = lDate(1) : lDay = lDate(2)
                If lMonth < 4 Then
                    .Value(I + 1) = 4.0
                ElseIf lMonth = 4 Then
                    If lDay <= 10 Then
                        .Value(I + 1) = 4.0
                    Else
                        'start sliding 4-5
                        .Value(I + 1) = 4.0 + (lDay - 10 + 1) / 41.0
                    End If
                ElseIf lMonth = 5 Then
                    If lDay <= 20 Then
                        'sliding 4-5
                        .Value(I + 1) = 4.0 + (lDay + 21.0) / 41.0
                    Else
                        .Value(I + 1) = 5.0
                    End If
                ElseIf lMonth < 12 Then
                    .Value(I + 1) = 5.0
                ElseIf lMonth = 12 Then
                    If lDay >= 1 AndAlso lDay <= 10 Then
                        'sliding 5-4
                        .Value(I + 1) = 5.0 - lDay / 10.0
                    Else
                        .Value(I + 1) = 4.0
                    End If
                End If
            Next
        End With

        'Construct DSN4 (SU2_0030, SU2_0291, 20501, R112, R117)
        'Start with 4
        'change 4-5 04/25 - 05/15 '21 days transition
        'change 5-4 11/28 - 12/12 '15 days transition
        'sliding in transition
        With lTsDSN4
            For I As Integer = 0 To .Dates.numValues
                J2Date(.Dates.Value(I), lDate)
                lMonth = lDate(1) : lDay = lDate(2)
                If lMonth < 4 Then
                    .Value(I + 1) = 4.0
                ElseIf lMonth = 4 Then
                    If lDay <= 25 Then
                        .Value(I + 1) = 4.0
                    Else
                        'start sliding 4-5
                        .Value(I + 1) = 4.0 + (lDay - 25 + 1) / 21.0
                    End If
                ElseIf lMonth = 5 Then
                    If lDay <= 15 Then
                        'sliding 4-5
                        .Value(I + 1) = 4.0 + (lDay + 6.0) / 21.0
                    Else
                        .Value(I + 1) = 5.0
                    End If
                ElseIf lMonth < 11 Then
                    .Value(I + 1) = 5.0
                ElseIf lMonth = 11 Then
                    If lDay <= 28 Then
                        .Value(I + 1) = 5.0
                    Else
                        .Value(I + 1) = 5.0 - (lDay - 28 + 1) / 15.0
                    End If
                ElseIf lMonth = 12 Then
                    If lDay >= 1 AndAlso lDay <= 12 Then
                        'sliding 5-4
                        .Value(I + 1) = 5.0 - (lDay + 3.0) / 15.0
                    Else
                        .Value(I + 1) = 4.0
                    End If
                End If
            Next
        End With

        'Construct DSN6 (SW4_1860, 20502, R47)
        'Start with 4
        'change 4-5 04/23 - 05/30 '38 days transition
        'change 5-4 11/15 - 12/01 '17 days transition
        'sliding in transition
        'only through 1996
        With lTsDSN6
            For I As Integer = 0 To .Dates.numValues
                J2Date(.Dates.Value(I), lDate)
                lYear = lDate(0) : lMonth = lDate(1) : lDay = lDate(2)
                If lYear <= 1996 Then
                    If lMonth < 4 Then
                        .Value(I + 1) = 4.0
                    ElseIf lMonth = 4 Then
                        If lDay <= 23 Then
                            .Value(I + 1) = 4.0
                        Else
                            'start sliding 4-5
                            .Value(I + 1) = 4.0 + (lDay - 23.0 + 1) / 38.0
                        End If
                    ElseIf lMonth = 5 Then
                        If lDay <= 30 Then
                            'sliding 4-5
                            .Value(I + 1) = 4.0 + (lDay + 8.0) / 38.0
                        Else
                            .Value(I + 1) = 5.0
                        End If
                    ElseIf lMonth < 11 Then
                        .Value(I + 1) = 5.0
                    ElseIf lMonth = 11 Then
                        If lDay <= 15 Then
                            .Value(I + 1) = 5.0
                        Else
                            .Value(I + 1) = 5.0 - (lDay - 15.0 + 1) / 17.0
                        End If
                    ElseIf lMonth = 12 Then
                        If lDay = 1 Then
                            'sliding 5-4
                            .Value(I + 1) = 5.0 - (lDay + 16.0) / 17.0
                        Else
                            .Value(I + 1) = 4.0
                        End If
                    End If
                Else 'starting from 1997 and on
                    .Value(I + 1) = 4.0
                End If
            Next
        End With

        'Construct DSN5 (SW3_1690_2222, SW3_1690_1660, 20502, R23)
        'Start with 4
        'change 4-6 04/01 - 07/01 '92 days transition
        'change 6-4 10/01 - 12/01 '62 days transition
        'sliding in transition
        'only through 1993

        'start in 1994
        'start with 5
        'change 5-4 02/15 - 03/01 'var days transition
        'change 4-6 04/01 - 05/15 '45 days transition
        'change 6-5 11/15 - 12/01 '17 days transition
        Dim lDayDiff As Double = 0.0
        With lTsDSN5
            For I As Integer = 0 To .Dates.numValues
                J2Date(.Dates.Value(I), lDate)
                lYear = lDate(0) : lMonth = lDate(1) : lDay = lDate(2)
                If lYear <= 1993 Then
                    If lMonth < 4 Then
                        .Value(I + 1) = 4.0
                    ElseIf lMonth = 4 Then
                        If lDay = 1 Then
                            .Value(I + 1) = 4.0
                        Else
                            'start sliding 4-6
                            .Value(I + 1) = 4.0 + lDay * (6.0 - 4.0) / 92.0
                        End If
                    ElseIf lMonth < 7 Then
                        'sliding 4-6
                        .Value(I + 1) = 4.0 + (DateDiff(DateInterval.Day, New Date(lYear, 4, 1), New Date(lYear, lMonth, lDay)) + 1.0) * (6.0 - 4.0) / 92.0
                    ElseIf lMonth = 7 Then
                        .Value(I + 1) = 6.0
                    ElseIf lMonth < 10 Then
                        .Value(I + 1) = 6.0
                    ElseIf lMonth >= 10 AndAlso lMonth < 12 Then
                        If lMonth = 10 AndAlso lDay = 1 Then
                            .Value(I + 1) = 6.0
                        Else
                            .Value(I + 1) = 6.0 - (DateDiff(DateInterval.Day, New Date(lYear, 10, 1), New Date(lYear, lMonth, lDay)) + 1.0) * (6.0 - 4.0) / 62.0
                        End If
                    ElseIf lMonth = 12 Then
                        .Value(I + 1) = 4.0
                    End If
                Else 'lYear >= 1994
                    If lMonth < 2 Then
                        .Value(I + 1) = 5.0
                    ElseIf lMonth = 2 Then
                        If lDay <= 15 Then
                            .Value(I + 1) = 5.0
                        Else
                            'start sliding 5 - 4
                            .Value(I + 1) = 5.0 - (lDay - 15.0 + 1) / (DayMon(lYear, 2) - 15.0 + 1 + 1)
                        End If
                    ElseIf lMonth = 3 Then
                        .Value(I + 1) = 4.0
                    ElseIf lMonth = 4 Then
                        If lDay = 1 Then
                            .Value(I + 1) = 4.0
                        Else
                            'sliding 4 - 6 till 5/15
                            .Value(I + 1) = 4.0 + lDay * (6.0 - 4.0) / 45.0
                        End If
                    ElseIf lMonth = 5 Then
                        If lDay <= 15 Then
                            'sliding 4-6
                            .Value(I + 1) = 4.0 + (lDay + 30.0) * (6.0 - 4.0) / 45.0
                        Else
                            .Value(I + 1) = 6.0
                        End If
                    ElseIf lMonth < 11 Then
                        .Value(I + 1) = 6.0
                    ElseIf lMonth = 11 Then
                        If lDay <= 15 Then
                            .Value(I + 1) = 6.0
                        Else
                            'start sliding 6 - 5 till 12/1
                            .Value(I + 1) = 6.0 - (lDay - 15.0 + 1) / 17.0
                        End If
                    ElseIf lMonth = 12 Then
                        .Value(I + 1) = 5.0
                    End If
                End If
            Next
        End With

        Dim lSusqTransWDMFilename As String = "G:\Admin\GCRPSusq\RunsWithReservoirs\parms\SusqTrans.wdm"
        Dim lSusqTransWDM As New atcWDM.atcDataSourceWDM()
        With lSusqTransWDM
            If Not .Open(lSusqTransWDMFilename) Then Exit Sub

            'Write Variable FTable column timeseries into this WDM
            If Not .AddDataset(lTsDSN1, atcDataSource.EnumExistAction.ExistReplace) Then
                Logger.Dbg("Add TSDSN1 failed.")
            End If
            If Not .AddDataset(lTsDSN2, atcDataSource.EnumExistAction.ExistReplace) Then
                Logger.Dbg("Add TSDSN2 failed.")
            End If
            If Not .AddDataset(lTsDSN3, atcDataSource.EnumExistAction.ExistReplace) Then
                Logger.Dbg("Add TSDSN3 failed.")
            End If
            If Not .AddDataset(lTsDSN4, atcDataSource.EnumExistAction.ExistReplace) Then
                Logger.Dbg("Add TSDSN4 failed.")
            End If
            If Not .AddDataset(lTsDSN5, atcDataSource.EnumExistAction.ExistReplace) Then
                Logger.Dbg("Add TSDSN5 failed.")
            End If
            If Not .AddDataset(lTsDSN6, atcDataSource.EnumExistAction.ExistReplace) Then
                Logger.Dbg("Add TSDSN6 failed.")
            End If
            .Clear()
        End With
        lSusqTransWDM = Nothing
    End Sub

    Private Sub ConstructHuc8BasedWaterUseFile()
        '*** Awuds Excel data
        Dim lExcelFipsFieldIndex As Integer = 4
        Dim l2000DataElements() As Integer = {7, 8, 9, 11, 12, 13, 14, 17, 20, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 39, 42, 45, 46, 49, 62, 65, 68}
        Dim l2005DataElements() As Integer = {9, 12, 15, 19, 20, 21, 22, 23, 24, 27, 30, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 46, 47, 48, 49, 53, 54, 55, 56, 57, 58, 59, 60, 63, 66, 69, 72, 75, 99, 102, 105}
        Dim lDataYear() As Integer = {2000, 2005}
        Dim lStates() As String = {"mdco", "nyco", "paco"}
        Dim lAwudsDataDirectory As String = "G:\Admin\EPA_HydroFrac_HSPFEval\WaterUse\"
        Dim lAwudsDataFile As String = ""
        Dim lxlApp As Excel.Application = Nothing
        Dim lxlWorkbook As Excel.Workbook = Nothing
        Dim lxlWorkbookPA As Excel.Workbook = Nothing
        Dim lxlWorkbookMD As Excel.Workbook = Nothing
        Dim lxlWorkbookNY As Excel.Workbook = Nothing
        Dim lxlSheet As Excel.Worksheet = Nothing
        'Dim lStateList As New atcCollection()
        'With lStateList
        '    .Add("24", "md")
        '    .Add("36", "ny")
        '    .Add("42", "pa")
        'End With

        '*** 
        Dim File1 As String = lAwudsDataDirectory & "HUCbyCounty.txt"
        Dim File2 As String = lAwudsDataDirectory & "County.txt"

        Dim lYearToProcess As Integer = lDataYear(1)
        Dim lFile As String = IO.Path.Combine(IO.Path.GetDirectoryName(File1), "HUCbyCountyWaterUse" & lYearToProcess & ".txt")

        Dim lOneLine As String
        Dim lArrCounty() As String
        Dim lArrHucbyCounty() As String
        Dim lFips As String = ""
        Dim lAreaTotal As Double = 0
        Dim lAreaPartial As Double = 0
        Dim lAreaFraction As Double = 0
        Dim lLinebuilder As New Text.StringBuilder

        'construct county fips-area (sq meter) dictionary
        Dim lCountyList As New atcCollection()
        Dim lSRCounty As New StreamReader(File2)
        While Not lSRCounty.EndOfStream
            lOneLine = lSRCounty.ReadLine()
            lArrCounty = Regex.Split(lOneLine, "\s+")
            lFips = lArrCounty(5)
            Dim lArea As Double = Double.Parse(lArrCounty(13))
            If Not lCountyList.Keys.Contains(lFips) Then
                lCountyList.Add(lFips, lArea)
            End If
        End While
        lSRCounty.Close()
        lSRCounty = Nothing

        lxlApp = New Excel.Application()
        lAwudsDataFile = lAwudsDataDirectory & "mdco" & lYearToProcess & ".xls"
        lxlWorkbookMD = lxlApp.Workbooks.Open(lAwudsDataFile)
        lAwudsDataFile = lAwudsDataDirectory & "paco" & lYearToProcess & ".xls"
        lxlWorkbookPA = lxlApp.Workbooks.Open(lAwudsDataFile)
        lAwudsDataFile = lAwudsDataDirectory & "nyco" & lYearToProcess & ".xls"
        lxlWorkbookNY = lxlApp.Workbooks.Open(lAwudsDataFile)

        Dim lSWHucByCountyData As New StreamWriter(lFile, False)
        Dim lSRHucByCounty As New StreamReader(File1)
        While Not lSRHucByCounty.EndOfStream
            lOneLine = lSRHucByCounty.ReadLine
            lArrHucbyCounty = Regex.Split(lOneLine, "\s+")
            lFips = lArrHucbyCounty(3)
            If lCountyList.Keys.Contains(lFips) Then
                lAreaTotal = lCountyList.ItemByKey(lFips)
            Else
                lAreaTotal = -99.9
            End If
            If lAreaTotal < 0 Then Continue While
            If Double.TryParse(lArrHucbyCounty(4), lAreaPartial) Then
                lAreaFraction = lAreaPartial / lAreaTotal
            Else
                Continue While
            End If

            lLinebuilder.Append(lOneLine & " ")
            lLinebuilder.Append(String.Format("{0:0.0}", lAreaTotal) & " ")
            lLinebuilder.Append(String.Format("{0:0.00}", lAreaFraction) & " ")

            'search for data
            Select Case lFips.Substring(0, 2)
                Case "24" : lxlWorkbook = lxlWorkbookMD
                Case "36" : lxlWorkbook = lxlWorkbookNY
                Case "42" : lxlWorkbook = lxlWorkbookPA
            End Select

            Dim lDataElements() As Integer = Nothing
            If lYearToProcess = 2000 Then
                lxlSheet = lxlWorkbook.Worksheets("Data")
                lDataElements = l2000DataElements
            ElseIf lYearToProcess = 2005 Then
                lDataElements = l2005DataElements
                lxlSheet = lxlWorkbook.Worksheets("County")
            End If

            With lxlSheet
                For lRow As Integer = 1 To .UsedRange.Rows.Count
                    If lFips = .Cells(lRow, lExcelFipsFieldIndex).Value Then
                        Dim lValue As Double = 0
                        For I As Integer = 0 To lDataElements.Length - 1
                            lValue = .Cells(lRow, lDataElements(I)).Value * lAreaFraction
                            lLinebuilder.Append(DoubleToString(lValue) & " ")
                        Next
                        Exit For
                    End If
                Next
            End With

            lSWHucByCountyData.WriteLine(lLinebuilder.ToString)
            lLinebuilder.Length = 0
        End While
        lSRHucByCounty.Close()
        lSRHucByCounty = Nothing

        lSWHucByCountyData.Flush()
        lSWHucByCountyData.Close()
        lSWHucByCountyData = Nothing

        lxlWorkbookMD.Close()
        lxlWorkbookNY.Close()
        lxlWorkbookPA.Close()
        Try
            lxlWorkbook.Close()
        Catch ex As Exception

        End Try
        lxlApp.Quit()
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlSheet)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbook)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbookMD)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbookNY)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbookPA)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlApp)

        lxlSheet = Nothing
        lxlWorkbook = Nothing
        lxlWorkbookMD = Nothing
        lxlWorkbookNY = Nothing
        lxlWorkbookPA = Nothing
        lxlApp = Nothing
    End Sub

    Private Sub ClassifyCalendarYears()
        Dim lUADepth As Double = 0.03719 'To be multiplied by drainage area in square miles to convert to depth inch
        Dim lWorkDirClassifyYear As String = "G:\Admin\EPA_HydroFrac_HSPFEval\ClassifyYears\"
        Dim lDataDir As String = lWorkDirClassifyYear & "NWIS\"
        Dim lWDMFiles As New atcCollection() 'collection of wdm and its drainage area
        With lWDMFiles
            .Add(lDataDir & "danville.wdm", 11220.0)
            .Add(lDataDir & "marietta.wdm", 25990.0)
            .Add(lDataDir & "raystown.wdm", 796.0)
            .Add(lDataDir & "westbrsusq.wdm", 6847.0)
        End With
        Dim lPercentiles() As Integer = {10, 25, 75, 90}
        Dim lClassifyYearLog As String = lWorkDirClassifyYear & "ClassifyYearLog.txt"
        Dim lSW As New StreamWriter(lClassifyYearLog, False)
        Dim lWDMSource As atcWDM.atcDataSourceWDM = Nothing
        For Each lWDMFile As String In lWDMFiles.Keys
            lWDMSource = New atcWDM.atcDataSourceWDM()
            If Not lWDMSource.Open(lWDMFile) Then Continue For
            Dim lConversionFactor As Double = lUADepth / lWDMFiles.ItemByKey(lWDMFile)
            Dim lTs As atcTimeseries = lWDMSource.DataSets(0)
            Dim lCons As String = lTs.Attributes.GetValue("Constituent")
            Dim lTsAnnual As atcTimeseries = Aggregate(lTs, atcTimeUnit.TUYear, 1, atcTran.TranAverSame)
            Dim lTsAnnualDepth As atcTimeseries = Nothing
            lTsAnnualDepth = Aggregate(lTs, atcTimeUnit.TUYear, 1, atcTran.TranSumDiv)

            Dim lUnitAverage As String = ""
            Dim lUnitSum As String = ""
            If lCons.ToLower.Contains("flow") Then
                lTsAnnualDepth = lTsAnnualDepth * lConversionFactor
                lUnitAverage = "(cfs)"
                lUnitSum = "(in)"
            ElseIf lCons.ToLower.Contains("prec") Then
                lUnitAverage = "(in)"
                lUnitSum = "(in)"
            End If

            Dim lPercentilesAnnualAverage As New atcCollection()
            With lPercentilesAnnualAverage
                For Each lPercentile As Integer In lPercentiles
                    .Add(lPercentile, lTsAnnual.Attributes.GetValue("%" & lPercentile.ToString))
                Next
            End With
            Dim lPercentilesAnnualSum As New atcCollection()
            With lPercentilesAnnualSum
                For Each lPercentile As Integer In lPercentiles
                    .Add(lPercentile, lTsAnnualDepth.Attributes.GetValue("%" & lPercentile.ToString))
                Next
            End With
            lSW.WriteLine("Processing Data: " & lTs.Attributes.GetValue("History 1"))
            lSW.WriteLine("Classification based on Annual Average " & lUnitAverage & " and Sum " & lUnitSum & " " & lCons & " percentiles")
            lSW.WriteLine("Percentile,Avg,Sum")
            lSW.WriteLine("%," & lUnitAverage & "," & lUnitSum)
            lSW.WriteLine("-------------------")
            For Each lPct As Integer In lPercentilesAnnualAverage.Keys
                lSW.Write(lPct & ",")
                lSW.Write(String.Format("{0:0.00}", lPercentilesAnnualAverage.ItemByKey(lPct)) & ",")
                lSW.WriteLine(String.Format("{0:0.00}", lPercentilesAnnualSum.ItemByKey(lPct)))
            Next
            Dim lDate(5) As Integer
            Dim lCategory As String = ""
            Dim lValue As Double
            lSW.WriteLine(" ")
            lSW.WriteLine("Year,Avg,Class,Sum,Class")
            lSW.WriteLine("    " & "," & lUnitAverage & ", ," & lUnitSum & ", ,")
            lSW.WriteLine("----,---,-----,---,------")
            For lYCount As Integer = 1 To lTsAnnual.numValues
                J2Date(lTsAnnual.Dates.Value(lYCount - 1), lDate)
                lValue = lTsAnnual.Value(lYCount)
                If lValue <= lPercentilesAnnualAverage.ItemByKey(10) Then
                    lCategory = "Drought"
                ElseIf lValue > lPercentilesAnnualAverage.ItemByKey(10) AndAlso lValue <= lPercentilesAnnualAverage.ItemByKey(25) Then
                    lCategory = "Dry"
                ElseIf lValue > lPercentilesAnnualAverage.ItemByKey(25) AndAlso lValue <= lPercentilesAnnualAverage.ItemByKey(75) Then
                    lCategory = "Normal"
                ElseIf lValue > lPercentilesAnnualAverage.ItemByKey(75) AndAlso lValue <= lPercentilesAnnualAverage.ItemByKey(90) Then
                    lCategory = "Wet"
                ElseIf lValue > lPercentilesAnnualAverage.ItemByKey(90) Then
                    lCategory = "Very Wet"
                End If
                lSW.Write(lDate(0) & "," & String.Format("{0:0.00}", lValue) & "," & lCategory & ",")

                lValue = lTsAnnualDepth.Value(lYCount)
                If lValue <= lPercentilesAnnualSum.ItemByKey(10) Then
                    lCategory = "Drought"
                ElseIf lValue > lPercentilesAnnualSum.ItemByKey(10) AndAlso lValue <= lPercentilesAnnualSum.ItemByKey(25) Then
                    lCategory = "Dry"
                ElseIf lValue > lPercentilesAnnualSum.ItemByKey(25) AndAlso lValue <= lPercentilesAnnualSum.ItemByKey(75) Then
                    lCategory = "Normal"
                ElseIf lValue > lPercentilesAnnualSum.ItemByKey(75) AndAlso lValue <= lPercentilesAnnualSum.ItemByKey(90) Then
                    lCategory = "Wet"
                ElseIf lValue > lPercentilesAnnualSum.ItemByKey(90) Then
                    lCategory = "Very Wet"
                End If
                lSW.WriteLine(String.Format("{0:0.00}", lValue) & "," & lCategory)
            Next
            lWDMSource.Clear()
            lWDMSource = Nothing
            lSW.WriteLine(" ")
            lSW.WriteLine(" ")
            lTsAnnual.Clear() : lTsAnnual = Nothing
            lTsAnnualDepth.Clear() : lTsAnnualDepth = Nothing
            lPercentilesAnnualAverage.Clear() : lPercentilesAnnualAverage = Nothing
            lPercentilesAnnualSum.Clear() : lPercentilesAnnualSum = Nothing
            lSW.Flush()
        Next

        lSW.Close()
        lSW = Nothing
    End Sub

    Private Sub ClassifyCalendarYearsForGraph()
        Dim lUADepth As Double = 0.03719 'To be multiplied by drainage area in square miles to convert to depth inch
        Dim lWorkDirClassifyYear As String = "G:\Admin\EPA_HydroFrac_HSPFEval\ClassifyYears\"
        Dim lDataDir As String = lWorkDirClassifyYear & "NWIS\"
        Dim lWDMFiles As New atcCollection() 'collection of wdm and its drainage area
        With lWDMFiles
            .Add(lDataDir & "danville.wdm", 11220.0)
            .Add(lDataDir & "marietta.wdm", 25990.0)
            .Add(lDataDir & "raystown.wdm", 796.0)
            .Add(lDataDir & "westbrsusq.wdm", 6847.0)
        End With
        Dim lPercentiles() As Integer = {10, 25, 75, 90}
        Dim lClassifyYearLog As String = lWorkDirClassifyYear & "ClassifyYearForGraphLog.txt"
        Dim lDelim As String = ","
        Dim lSW As New StreamWriter(lClassifyYearLog, False)
        Dim lWDMSource As atcWDM.atcDataSourceWDM = Nothing
        For Each lWDMFile As String In lWDMFiles.Keys
            lWDMSource = New atcWDM.atcDataSourceWDM()
            If Not lWDMSource.Open(lWDMFile) Then Continue For
            Dim lConversionFactor As Double = lUADepth / lWDMFiles.ItemByKey(lWDMFile)
            Dim lTs As atcTimeseries = lWDMSource.DataSets(0)
            Dim lCons As String = lTs.Attributes.GetValue("Constituent")
            Dim lTsAnnual As atcTimeseries = Aggregate(lTs, atcTimeUnit.TUYear, 1, atcTran.TranAverSame)
            Dim lTsAnnualDepth As atcTimeseries = Nothing
            lTsAnnualDepth = Aggregate(lTs, atcTimeUnit.TUYear, 1, atcTran.TranSumDiv)

            Dim lUnitAverage As String = ""
            Dim lUnitSum As String = ""
            If lCons.ToLower.Contains("flow") Then
                lTsAnnualDepth = lTsAnnualDepth * lConversionFactor
                lUnitAverage = "(cfs)"
                lUnitSum = "(in)"
            ElseIf lCons.ToLower.Contains("prec") Then
                lUnitAverage = "(in)"
                lUnitSum = "(in)"
            End If

            Dim lPercentilesAnnualAverage As New atcCollection()
            With lPercentilesAnnualAverage
                For Each lPercentile As Integer In lPercentiles
                    .Add(lPercentile, lTsAnnual.Attributes.GetValue("%" & lPercentile.ToString))
                Next
            End With
            Dim lPercentilesAnnualSum As New atcCollection()
            With lPercentilesAnnualSum
                For Each lPercentile As Integer In lPercentiles
                    .Add(lPercentile, lTsAnnualDepth.Attributes.GetValue("%" & lPercentile.ToString))
                Next
            End With
            lSW.WriteLine("Processing Data: " & lTs.Attributes.GetValue("History 1"))
            lSW.WriteLine("Classification based on Annual Average " & lUnitAverage & " and Sum " & lUnitSum & " " & lCons & " percentiles")
            lSW.WriteLine("Percentile,Avg,Sum")
            lSW.WriteLine("%," & lUnitAverage & "," & lUnitSum)
            lSW.WriteLine("-------------------")
            For Each lPct As Integer In lPercentilesAnnualAverage.Keys
                lSW.Write(lPct & ",")
                lSW.Write(String.Format("{0:0.00}", lPercentilesAnnualAverage.ItemByKey(lPct)) & ",")
                lSW.WriteLine(String.Format("{0:0.00}", lPercentilesAnnualSum.ItemByKey(lPct)))
            Next
            Dim lDate(5) As Integer
            Dim lCategory As String = ""
            Dim lValue As Double
            lSW.WriteLine(" ")
            lSW.WriteLine("Listing of Annual Averages " & lUnitAverage & " for different categories")
            lSW.WriteLine("Year,Drought,Dry,Normal,Wet,Very Wet")
            Dim lTsGroupAnnualAvg As New atcTimeseriesGroup()
            Dim lTsAnnAvgDrought As atcTimeseries = lTsAnnual.Clone()
            Dim lTsAnnAvgDry As atcTimeseries = lTsAnnual.Clone()
            Dim lTsAnnAvgNormal As atcTimeseries = lTsAnnual.Clone()
            Dim lTsAnnAvgWet As atcTimeseries = lTsAnnual.Clone()
            Dim lTsAnnAvgVeryWet As atcTimeseries = lTsAnnual.Clone()

            For I As Integer = 1 To lTsAnnual.numValues
                lTsAnnAvgDrought.Value(I) = GetNaN()
                lTsAnnAvgDry.Value(I) = GetNaN()
                lTsAnnAvgNormal.Value(I) = GetNaN()
                lTsAnnAvgWet.Value(I) = GetNaN()
                lTsAnnAvgVeryWet.Value(I) = GetNaN()
            Next
            With lTsGroupAnnualAvg
                .Add(lTsAnnAvgDrought)
                .Add(lTsAnnAvgDry)
                .Add(lTsAnnAvgNormal)
                .Add(lTsAnnAvgWet)
                .Add(lTsAnnAvgVeryWet)
            End With

            lTsAnnAvgDrought.Attributes.SetValue("Point", True)
            lTsAnnAvgDry.Attributes.SetValue("Point", True)
            lTsAnnAvgNormal.Attributes.SetValue("Point", True)
            lTsAnnAvgWet.Attributes.SetValue("Point", True)
            lTsAnnAvgVeryWet.Attributes.SetValue("Point", True)

            lTsAnnAvgDrought.Attributes.SetValue("Class", "Drought")
            lTsAnnAvgDry.Attributes.SetValue("Class", "Dry")
            lTsAnnAvgNormal.Attributes.SetValue("Class", "Normal")
            lTsAnnAvgWet.Attributes.SetValue("Class", "Wet")
            lTsAnnAvgVeryWet.Attributes.SetValue("Class", "Very Wet")

            For lYCount As Integer = 1 To lTsAnnual.numValues
                J2Date(lTsAnnual.Dates.Value(lYCount - 1), lDate)
                lSW.Write(lDate(0) & ",")
                lValue = lTsAnnual.Value(lYCount)
                If lValue <= lPercentilesAnnualAverage.ItemByKey(10) Then
                    lCategory = "Drought"
                    lSW.WriteLine(WriteToColumn(lValue, 1, 5, lDelim))
                    lTsAnnAvgDrought.Value(lYCount) = lValue
                ElseIf lValue > lPercentilesAnnualAverage.ItemByKey(10) AndAlso lValue <= lPercentilesAnnualAverage.ItemByKey(25) Then
                    lCategory = "Dry"
                    lSW.WriteLine(WriteToColumn(lValue, 2, 5, lDelim))
                    lTsAnnAvgDry.Value(lYCount) = lValue
                ElseIf lValue > lPercentilesAnnualAverage.ItemByKey(25) AndAlso lValue <= lPercentilesAnnualAverage.ItemByKey(75) Then
                    lCategory = "Normal"
                    lSW.WriteLine(WriteToColumn(lValue, 3, 5, lDelim))
                    lTsAnnAvgNormal.Value(lYCount) = lValue
                ElseIf lValue > lPercentilesAnnualAverage.ItemByKey(75) AndAlso lValue <= lPercentilesAnnualAverage.ItemByKey(90) Then
                    lCategory = "Wet"
                    lSW.WriteLine(WriteToColumn(lValue, 4, 5, lDelim))
                    lTsAnnAvgWet.Value(lYCount) = lValue
                ElseIf lValue > lPercentilesAnnualAverage.ItemByKey(90) Then
                    lCategory = "Very Wet"
                    lSW.WriteLine(WriteToColumn(lValue, 5, 5, lDelim))
                    lTsAnnAvgVeryWet.Value(lYCount) = lValue
                End If
            Next

            'DisplayTsGraph(lTsGroupAnnualAvg)

            lSW.WriteLine(" ")
            lSW.WriteLine(" ")
            lSW.WriteLine("Listing of Annual Sum " & lUnitSum & " for different categories")
            lSW.WriteLine("Year,Drought,Dry,Normal,Wet,Very Wet")
            For lYCount As Integer = 1 To lTsAnnualDepth.numValues
                J2Date(lTsAnnualDepth.Dates.Value(lYCount - 1), lDate)
                lSW.Write(lDate(0) & ",")
                lValue = lTsAnnualDepth.Value(lYCount)
                If lValue <= lPercentilesAnnualSum.ItemByKey(10) Then
                    lCategory = "Drought"
                    lSW.WriteLine(WriteToColumn(lValue, 1, 5, lDelim))
                ElseIf lValue > lPercentilesAnnualSum.ItemByKey(10) AndAlso lValue <= lPercentilesAnnualSum.ItemByKey(25) Then
                    lCategory = "Dry"
                    lSW.WriteLine(WriteToColumn(lValue, 2, 5, lDelim))
                ElseIf lValue > lPercentilesAnnualSum.ItemByKey(25) AndAlso lValue <= lPercentilesAnnualSum.ItemByKey(75) Then
                    lCategory = "Normal"
                    lSW.WriteLine(WriteToColumn(lValue, 3, 5, lDelim))
                ElseIf lValue > lPercentilesAnnualSum.ItemByKey(75) AndAlso lValue <= lPercentilesAnnualSum.ItemByKey(90) Then
                    lCategory = "Wet"
                    lSW.WriteLine(WriteToColumn(lValue, 4, 5, lDelim))
                ElseIf lValue > lPercentilesAnnualSum.ItemByKey(90) Then
                    lCategory = "Very Wet"
                    lSW.WriteLine(WriteToColumn(lValue, 5, 5, lDelim))
                End If
            Next

            lWDMSource.Clear()
            lWDMSource = Nothing
            lSW.WriteLine(" ")
            lSW.WriteLine(" ")

            lTsAnnAvgDrought.Clear() : lTsAnnAvgDrought = Nothing
            lTsAnnAvgDry.Clear() : lTsAnnAvgDry = Nothing
            lTsAnnAvgNormal.Clear() : lTsAnnAvgNormal = Nothing
            lTsAnnAvgWet.Clear() : lTsAnnAvgWet = Nothing
            lTsAnnAvgVeryWet.Clear() : lTsAnnAvgVeryWet = Nothing
            lTsGroupAnnualAvg.Clear() : lTsGroupAnnualAvg = Nothing

            lTsAnnual.Clear() : lTsAnnual = Nothing
            lTsAnnualDepth.Clear() : lTsAnnualDepth = Nothing
            lPercentilesAnnualAverage.Clear() : lPercentilesAnnualAverage = Nothing
            lPercentilesAnnualSum.Clear() : lPercentilesAnnualSum = Nothing
            lSW.Flush()
        Next

        lSW.Close()
        lSW = Nothing
    End Sub

    Private Sub ClassifyWaterYearsForGraph()
        Dim lUADepth As Double = 0.03719 'To be multiplied by drainage area in square miles to convert to depth inch
        Dim lWorkDirClassifyYear As String = "G:\Admin\EPA_HydroFrac_HSPFEval\ClassifyYears\"
        Dim lDataDir As String = lWorkDirClassifyYear & "NWIS\"
        Dim lWDMFiles As New atcCollection() 'collection of wdm and its drainage area
        With lWDMFiles
            '.Add(lDataDir & "danville.wdm", 11220.0)
            .Add(lDataDir & "marietta.wdm", 25990.0)
            '.Add(lDataDir & "raystown.wdm", 796.0)
            '.Add(lDataDir & "westbrsusq.wdm", 6847.0)
        End With
        Dim lPercentiles() As Integer = {10, 25, 75, 90}
        'Dim lPercentiles() As Integer = {5, 50, 95}
        Dim lClassifyYearLog As String = lWorkDirClassifyYear & "ClassifyWaterYearForGraphLog.txt"
        Dim lDelim As String = ","
        Dim lSW As New StreamWriter(lClassifyYearLog, False)
        Dim lWDMSource As atcWDM.atcDataSourceWDM = Nothing
        Dim lTsWYear As atcTimeseries = Nothing
        For Each lWDMFile As String In lWDMFiles.Keys
            lWDMSource = New atcWDM.atcDataSourceWDM()
            If Not lWDMSource.Open(lWDMFile) Then Continue For
            Dim lConversionFactor As Double = lUADepth / lWDMFiles.ItemByKey(lWDMFile)
            Dim lTs As atcTimeseries = lWDMSource.DataSets(0)
            Dim lCons As String = lTs.Attributes.GetValue("Constituent")

            Dim lDateYearStart As Double = lTs.Dates.Value(0)
            Dim lDateYearEnd As Double = lTs.Dates.Value(lTs.numValues)

            'Should adjust to beginning of a water year (Oct 1)
            '       and    to ending of a water year (Sep 30)
            Dim lDate(5) As Integer
            Dim lSetToYearStart As Boolean = False
            J2Date(lDateYearStart, lDate)
            'If lDate(1) > 1 Then
            '    lSetToYearStart = True
            'Else
            '    If lDate(2) > 1 Then
            '        lSetToYearStart = True
            '    Else
            '        If lDate(3) > 0 Then lSetToYearStart = True
            '    End If
            'End If
            lSetToYearStart = True
            If lSetToYearStart Then
                If lDate(1) < 10 Then
                    lDate(1) = 10
                ElseIf lDate(1) = 10 Then
                    If lDate(2) > 1 Then
                        lDate(0) += 1
                        lDate(1) = 10
                    End If
                Else
                    lDate(0) += 1
                    lDate(1) = 10
                End If

                lDate(2) = 1
                lDate(3) = 0
                lDate(4) = 0
                lDate(5) = 0
                lDateYearStart = Date2J(lDate)
            End If

            'Adjust to end of a year
            Dim lSetToYearEnd As Boolean = False
            J2Date(lDateYearEnd, lDate)
            'If lDate(1) < 12 Then
            '    lSetToYearEnd = True
            'Else
            '    If lDate(2) < 31 Then
            '        lSetToYearEnd = True
            '    Else
            '        If lDate(3) < 24 Then lSetToYearEnd = True
            '    End If
            'End If
            lSetToYearEnd = True
            If lSetToYearEnd Then
                If lDate(1) > 9 Then
                    lDate(1) = 9
                ElseIf lDate(1) = 9 Then
                    If lDate(2) < 30 Then
                        lDate(0) -= 1
                        lDate(1) = 9
                    End If
                Else
                    lDate(0) -= 1
                    lDate(1) = 9
                End If
                lDate(2) = 30
                lDate(3) = 24
                lDate(4) = 0
                lDate(5) = 0
                lDateYearEnd = Date2J(lDate)
            End If

            If lDateYearStart <> lTs.Dates.Value(0) OrElse lDateYearEnd <> lTs.Dates.Value(lTs.numValues) Then
                lTs = SubsetByDate(lTs, lDateYearStart, lDateYearEnd, Nothing)
            End If

            Dim lUnitAverage As String = ""
            Dim lUnitSum As String = ""
            If lCons.ToLower.Contains("flow") Then
                lUnitAverage = "(cfs)"
                lUnitSum = "(in)"
            ElseIf lCons.ToLower.Contains("prec") Then
                lUnitAverage = "(in)"
                lUnitSum = "(in)"
            End If

            'Ask for 7Q10 or 7low10
            Dim l7Q10Flow As Double = lTs.Attributes.GetValue("7Q10")

            Dim lWaterYearsGroup As atcTimeseriesGroup
            Dim lWaterYearSeason As New atcSeasonsWaterYear()
            lWaterYearsGroup = lWaterYearSeason.Split(lTs, Nothing)

            Dim lWYearBeg As Integer
            Dim lWYearEnd As Integer
            Dim lWYearBegDate As Double
            Dim lWYearEndDate As Double
            Dim lTsWaterYearMeans As New atcTimeseries(Nothing)
            lTsWaterYearMeans.Dates = New atcTimeseries(Nothing)
            Dim lWaterYearMeanValues(lWaterYearsGroup.Count) As Double : lWaterYearMeanValues(0) = GetNaN()
            Dim lWaterYearMeansDates(lWaterYearsGroup.Count) As Double : lWaterYearMeansDates(0) = GetNaN()

            For I As Integer = 0 To lWaterYearsGroup.Count - 1
                lTsWYear = lWaterYearsGroup(I)
                lWYearBegDate = lTsWYear.Dates.Value(0)
                lWYearEndDate = lTsWYear.Dates.Value(lTsWYear.numValues)
                J2Date(lWYearBegDate, lDate) : lWYearBeg = lDate(0)
                J2Date(lWYearEndDate, lDate) : lWYearEnd = lDate(0)

                'Set value
                If lWYearBeg = lWYearEnd Then 'not a whole water year
                    lWaterYearMeanValues(I + 1) = GetNaN()
                Else
                    lWaterYearMeanValues(I + 1) = lTsWYear.Attributes.GetValue("Mean")
                End If
                'Set date
                If I = 0 Then
                    lWaterYearMeansDates(I) = Date2J(lWYearEnd, 1, 1, 0, 0, 0)
                Else
                    lWaterYearMeansDates(I) = Date2J(lWYearEnd, 12, 31, 24, 0, 0)
                End If
            Next
            lTsWaterYearMeans.Dates.Values = lWaterYearMeansDates
            lTsWaterYearMeans.Values = lWaterYearMeanValues
            lTsWaterYearMeans.SetInterval(atcTimeUnit.TUYear, 1)

            Dim lPercentilesDaily As New atcCollection()
            Dim lPctStr As String = ""
            With lPercentilesDaily
                For Each lPercentile As Integer In lPercentiles
                    lPctStr = lPercentile.ToString.PadLeft(2, "0")
                    .Add(lPercentile, lTsWaterYearMeans.Attributes.GetValue("%" & lPctStr))
                Next
            End With

            'Dim lExportWaterYearMeansFile As String = "G:\Admin\EPA_HydroFrac_HSPFEval\WaterUse\MeanWaterYearFlowMarietta.txt"
            'lSW = New StreamWriter(lExportWaterYearMeansFile, False)
            'For I As Integer = 0 To lTsWaterYearMeans.numValues
            '    lSW.WriteLine(I & "," & lTsWaterYearMeans.Value(I))
            'Next
            'lSW.Close()

            lSW.WriteLine("Processing Data: " & lTs.Attributes.GetValue("History 1"))
            lSW.WriteLine("Classification based on water year average " & lUnitAverage & " " & lCons & " percentiles")
            lSW.WriteLine("Percentile,Avg")
            lSW.WriteLine("%," & lUnitAverage)
            For Each lPct As Integer In lPercentilesDaily.Keys
                lSW.Write(lPct & ",")
                lSW.WriteLine(String.Format("{0:0.00}", lPercentilesDaily.ItemByKey(lPct)))
            Next

            Dim lCategory As String = ""
            Dim lValue As Double
            lSW.WriteLine(" ")
            lSW.WriteLine("Listing of Water Year Averages " & lUnitAverage & " for different categories")
            lSW.WriteLine("From-To,Drought,Dry,Normal,Wet,Very Wet")
            For Each lTsWYear In lWaterYearsGroup
                J2Date(lTsWYear.Dates.Value(0), lDate) : lWYearBeg = lDate(0)
                J2Date(lTsWYear.Dates.Value(lTsWYear.numValues), lDate) : lWYearEnd = lDate(0)
                lSW.Write(lWYearBeg & "-" & lWYearEnd & lDelim)
                lValue = lTsWYear.Attributes.GetValue("Mean") 'Mean value of the water year
                If lValue <= lPercentilesDaily.ItemByKey(10) Then
                    lCategory = "Drought"
                    lSW.WriteLine(WriteToColumn(lValue, 1, 5, lDelim))
                    'lTsAnnAvgDrought.Value(lYCount) = lValue
                ElseIf lValue > lPercentilesDaily.ItemByKey(10) AndAlso lValue <= lPercentilesDaily.ItemByKey(25) Then
                    lCategory = "Dry"
                    lSW.WriteLine(WriteToColumn(lValue, 2, 5, lDelim))
                    'lTsAnnAvgDry.Value(lYCount) = lValue
                ElseIf lValue > lPercentilesDaily.ItemByKey(25) AndAlso lValue <= lPercentilesDaily.ItemByKey(75) Then
                    lCategory = "Normal"
                    lSW.WriteLine(WriteToColumn(lValue, 3, 5, lDelim))
                    'lTsAnnAvgNormal.Value(lYCount) = lValue
                ElseIf lValue > lPercentilesDaily.ItemByKey(75) AndAlso lValue <= lPercentilesDaily.ItemByKey(90) Then
                    lCategory = "Wet"
                    lSW.WriteLine(WriteToColumn(lValue, 4, 5, lDelim))
                    'lTsAnnAvgWet.Value(lYCount) = lValue
                ElseIf lValue > lPercentilesDaily.ItemByKey(90) Then
                    lCategory = "Very Wet"
                    lSW.WriteLine(WriteToColumn(lValue, 5, 5, lDelim))
                    'lTsAnnAvgVeryWet.Value(lYCount) = lValue
                End If
            Next

            'DisplayTsGraph(lTsGroupAnnualAvg)

            lWDMSource.Clear()
            lWDMSource = Nothing
            lSW.WriteLine(" ")
            lSW.WriteLine(" ")

            lTsWaterYearMeans.Clear() : lTsWaterYearMeans = Nothing
            lPercentilesDaily.Clear() : lPercentilesDaily = Nothing

            ReDim lWaterYearMeanValues(0)
            ReDim lWaterYearMeansDates(0)
            lWaterYearsGroup.Clear() : lWaterYearsGroup = Nothing
            lWaterYearSeason = Nothing
            lSW.Flush()
        Next

        lSW.Close()
        lSW = Nothing
    End Sub

    Private Sub ClassifyWaterYearsPrecipForGraph()
        'Dim lUADepth As Double = 0.03719 'To be multiplied by drainage area in square miles to convert to depth inch
        Dim lWorkDirClassifyYear As String = "G:\Admin\EPA_HydroFrac_HSPFEval\ClassifyYears\"
        Dim lPrecipWDMFile As String = "G:\Admin\GCRPSusq\Runs\parms\Susq_PMETRev_met.wdm"

        Dim lPrecipStnInfoFiles As New atcCollection()
        With lPrecipStnInfoFiles
            .Add("020501", lWorkDirClassifyYear & "PrecipStations020501.txt")
            '.Add("020502", lWorkDirClassifyYear & "PrecipStations020502.txt")
            '.Add("020503", lWorkDirClassifyYear & "PrecipStations020503.txt")
            '.Add("AllGCRP", lWorkDirClassifyYear & "PrecipStationsAllGCRP.txt")
            '.Add("Raystown", lWorkDirClassifyYear & "PrecipStationsRaystown.txt")
        End With

        'Dim lSite As String = "Raystown"
        'lSite = "020501"
        'lSite = "020502"
        'lSite = "020503"
        'lSite = "AllGCRP"

        Dim lPercentiles() As Integer = {10, 25, 75, 90}

        Dim lDelim As String = ","

        Dim lWDMSource As atcWDM.atcDataSourceWDM = Nothing
        lWDMSource = New atcWDM.atcDataSourceWDM()
        If Not lWDMSource.Open(lPrecipWDMFile) Then Exit Sub

        Dim lTsWYear As atcTimeseries = Nothing
        For Each lSite As String In lPrecipStnInfoFiles.Keys

            'open output file
            Dim lClassifyYearLog As String = lWorkDirClassifyYear & "ClassifyWaterYearPrecip" & lSite & ".txt"
            Dim lSW As New StreamWriter(lClassifyYearLog, False)

            'construct dataset id - area pairs
            Dim lPrecipStnsCollection As New atcCollection()
            Dim lInfoFile As String = lPrecipStnInfoFiles.ItemByKey(lSite)
            Dim lSR As New StreamReader(lInfoFile)
            Dim lOneLine As String = ""
            Dim lArr() As String = Nothing
            Dim lTotalArea As Double = 0
            While Not lSR.EndOfStream
                lOneLine = lSR.ReadLine()
                If lOneLine.Trim = "" OrElse lOneLine.StartsWith("#") Then Continue While
                lArr = Regex.Split(lOneLine, "\s+")
                If lPrecipStnsCollection.Keys.Contains(lArr(2)) Then
                    lPrecipStnsCollection.ItemByKey(lArr(2)) += Double.Parse(lArr(3))
                Else
                    lPrecipStnsCollection.Add(lArr(2), Double.Parse(lArr(3)))
                End If
                lTotalArea += Double.Parse(lArr(3))
            End While
            lSR.Close() : lSR = Nothing

            'construct timeseries group
            Dim lTsPrecipGroup As New atcTimeseriesGroup()
            Dim lTs As atcTimeseries = Nothing
            For Each lDsn As String In lPrecipStnsCollection.Keys
                lTs = lWDMSource.DataSets.FindData("ID", Integer.Parse(lDsn))(0)
                lTs = lTs * lPrecipStnsCollection.ItemByKey(lDsn) 'multiply the original rain record with its area
                lTsPrecipGroup.Add(lDsn, lTs)
            Next

            Dim lConversionFactor As Double = 0 'lUADepth / lWDMFiles.ItemByKey(lWDMFile)
            Dim lUnitAverage As String = "(in)"
            Dim lUnitSum As String = "(in)"

            'find common period
            Dim lDateCommonStart As Double = 0
            Dim lDateCommonEnd As Double = 0

            Dim lDateFirstStart As Double = 0
            Dim lDateLastEnd As Double = 0

            If Not CommonDates(lTsPrecipGroup, lDateFirstStart, lDateLastEnd, lDateCommonStart, lDateCommonEnd) Then
                Logger.Dbg("Find common duration problem.")
            End If

            'Should adjust to beginning of a water year (Oct 1)
            '       and    to ending of a water year (Sep 30)
            Dim lDate(5) As Integer
            Dim lSetToYearStart As Boolean = False
            J2Date(lDateCommonStart, lDate)
            'If lDate(1) > 1 Then
            '    lSetToYearStart = True
            'Else
            '    If lDate(2) > 1 Then
            '        lSetToYearStart = True
            '    Else
            '        If lDate(3) > 0 Then lSetToYearStart = True
            '    End If
            'End If
            lSetToYearStart = True
            If lSetToYearStart Then
                If lDate(1) < 10 Then
                    lDate(1) = 10
                ElseIf lDate(1) = 10 Then
                    If lDate(2) > 1 Then
                        lDate(0) += 1
                        lDate(1) = 10
                    End If
                Else
                    lDate(0) += 1
                    lDate(1) = 10
                End If

                lDate(2) = 1
                lDate(3) = 0
                lDate(4) = 0
                lDate(5) = 0
                lDateCommonStart = Date2J(lDate)
            End If

            'Adjust to end of a year
            Dim lSetToYearEnd As Boolean = False
            J2Date(lDateCommonEnd, lDate)
            'If lDate(1) < 12 Then
            '    lSetToYearEnd = True
            'Else
            '    If lDate(2) < 31 Then
            '        lSetToYearEnd = True
            '    Else
            '        If lDate(3) < 24 Then lSetToYearEnd = True
            '    End If
            'End If
            lSetToYearEnd = True
            If lSetToYearEnd Then
                If lDate(1) > 9 Then
                    lDate(1) = 9
                ElseIf lDate(1) = 9 Then
                    If lDate(2) < 30 Then
                        lDate(0) -= 1
                        lDate(1) = 9
                    End If
                Else
                    lDate(0) -= 1
                    lDate(1) = 9
                End If
                lDate(2) = 30
                lDate(3) = 24
                lDate(4) = 0
                lDate(5) = 0
                lDateCommonEnd = Date2J(lDate)
            End If

            Dim lTsPrecipComDurGroup As New atcTimeseriesGroup()
            For Each lTs In lTsPrecipGroup
                lTs = SubsetByDate(lTs, lDateCommonStart, lDateCommonEnd, Nothing)
                lTsPrecipComDurGroup.Add(lTs.Attributes.GetValue("ID"), lTs)
            Next

            'construct one area-weighted average rainfall record
            '  1. sum up
            Dim lTsPrecipAWAvg As atcTimeseries = lTsPrecipComDurGroup(0).Clone
            Dim lTotal As Double
            For H As Integer = 1 To lTsPrecipComDurGroup(0).numValues
                lTotal = 0.0
                For I As Integer = 0 To lTsPrecipComDurGroup.Count - 1
                    lTotal += lTsPrecipComDurGroup(I).Value(H)
                Next
                lTsPrecipAWAvg.Value(H) = lTotal
            Next

            '  2. divide by total area
            'lTsPrecipAWAvg = lTsPrecipAWAvg / lTotalArea
            For H As Integer = 1 To lTsPrecipAWAvg.numValues
                lTsPrecipAWAvg.Value(H) /= lTotalArea
            Next

            'Try free up some memory here
            For Each lTs In lTsPrecipComDurGroup
                lTs.Clear()
            Next
            For Each lTs In lTsPrecipGroup
                lTs.Clear()
            Next
            lTsPrecipGroup.Clear()
            lTsPrecipComDurGroup.Clear()
            System.GC.Collect()
            System.GC.WaitForFullGCComplete()

            Dim lWaterYearsGroup As atcTimeseriesGroup
            Dim lWaterYearSeason As New atcSeasonsWaterYear()
            lWaterYearsGroup = lWaterYearSeason.Split(lTsPrecipAWAvg, Nothing)

            Dim lWYearBeg As Integer
            Dim lWYearEnd As Integer
            Dim lWYearBegDate As Double
            Dim lWYearEndDate As Double
            Dim lTsWaterYearSumRain As New atcTimeseries(Nothing)
            lTsWaterYearSumRain.Dates = New atcTimeseries(Nothing)
            Dim lWaterYearSumValues(lWaterYearsGroup.Count) As Double : lWaterYearSumValues(0) = GetNaN()
            Dim lWaterYearSumDates(lWaterYearsGroup.Count) As Double : lWaterYearSumDates(0) = GetNaN()

            For I As Integer = 0 To lWaterYearsGroup.Count - 1
                lTsWYear = lWaterYearsGroup(I)
                lWYearBegDate = lTsWYear.Dates.Value(0)
                lWYearEndDate = lTsWYear.Dates.Value(lTsWYear.numValues)
                J2Date(lWYearBegDate, lDate) : lWYearBeg = lDate(0)
                J2Date(lWYearEndDate, lDate) : lWYearEnd = lDate(0)

                'Set value
                If lWYearBeg = lWYearEnd Then 'not a whole water year
                    lWaterYearSumValues(I + 1) = GetNaN()
                Else
                    lWaterYearSumValues(I + 1) = lTsWYear.Attributes.GetValue("Sum")
                End If
                'Set date
                If I = 0 Then
                    lWaterYearSumDates(I) = Date2J(lWYearEnd, 1, 1, 0, 0, 0)
                Else
                    lWaterYearSumDates(I) = Date2J(lWYearEnd, 12, 31, 24, 0, 0)
                End If
            Next
            lTsWaterYearSumRain.Dates.Values = lWaterYearSumDates
            lTsWaterYearSumRain.Values = lWaterYearSumValues
            lTsWaterYearSumRain.SetInterval(atcTimeUnit.TUYear, 1)

            Dim lPercentilesDaily As New atcCollection()
            With lPercentilesDaily
                For Each lPercentile As Integer In lPercentiles
                    .Add(lPercentile, lTsWaterYearSumRain.Attributes.GetValue("%" & lPercentile.ToString))
                Next
            End With

            lSW.WriteLine("Processing Data: " & lTs.Attributes.GetValue("History 1"))
            lSW.WriteLine("Classification based on water year sum " & lUnitSum & " precipitation percentiles")
            lSW.WriteLine("Percentile,Sum")
            lSW.WriteLine("%," & lUnitSum)
            For Each lPct As Integer In lPercentilesDaily.Keys
                lSW.Write(lPct & ",")
                lSW.WriteLine(String.Format("{0:0.00}", lPercentilesDaily.ItemByKey(lPct)))
            Next

            Dim lCategory As String = ""
            Dim lValue As Double
            lSW.WriteLine(" ")
            lSW.WriteLine("Listing of Water Year Sum " & lUnitSum & " for different categories")
            lSW.WriteLine("From-To,Drought,Dry,Normal,Wet,Very Wet")
            For Each lTsWYear In lWaterYearsGroup
                J2Date(lTsWYear.Dates.Value(0), lDate) : lWYearBeg = lDate(0)
                J2Date(lTsWYear.Dates.Value(lTsWYear.numValues), lDate) : lWYearEnd = lDate(0)
                lSW.Write(lWYearBeg & "-" & lWYearEnd & lDelim)
                lValue = lTsWYear.Attributes.GetValue("Sum") 'Sum precip value of the water year
                If lValue <= lPercentilesDaily.ItemByKey(10) Then
                    lCategory = "Drought"
                    lSW.WriteLine(WriteToColumn(lValue, 1, 5, lDelim))
                    'lTsAnnAvgDrought.Value(lYCount) = lValue
                ElseIf lValue > lPercentilesDaily.ItemByKey(10) AndAlso lValue <= lPercentilesDaily.ItemByKey(25) Then
                    lCategory = "Dry"
                    lSW.WriteLine(WriteToColumn(lValue, 2, 5, lDelim))
                    'lTsAnnAvgDry.Value(lYCount) = lValue
                ElseIf lValue > lPercentilesDaily.ItemByKey(25) AndAlso lValue <= lPercentilesDaily.ItemByKey(75) Then
                    lCategory = "Normal"
                    lSW.WriteLine(WriteToColumn(lValue, 3, 5, lDelim))
                    'lTsAnnAvgNormal.Value(lYCount) = lValue
                ElseIf lValue > lPercentilesDaily.ItemByKey(75) AndAlso lValue <= lPercentilesDaily.ItemByKey(90) Then
                    lCategory = "Wet"
                    lSW.WriteLine(WriteToColumn(lValue, 4, 5, lDelim))
                    'lTsAnnAvgWet.Value(lYCount) = lValue
                ElseIf lValue > lPercentilesDaily.ItemByKey(90) Then
                    lCategory = "Very Wet"
                    lSW.WriteLine(WriteToColumn(lValue, 5, 5, lDelim))
                    'lTsAnnAvgVeryWet.Value(lYCount) = lValue
                End If
            Next

            'DisplayTsGraph(lTsGroupAnnualAvg)

            lSW.WriteLine(" ")
            lSW.WriteLine(" ")

            lTsWaterYearSumRain.Clear() : lTsWaterYearSumRain = Nothing
            lPercentilesDaily.Clear() : lPercentilesDaily = Nothing

            ReDim lWaterYearSumValues(0)
            ReDim lWaterYearSumDates(0)
            lWaterYearsGroup.Clear() : lWaterYearsGroup = Nothing
            lWaterYearSeason = Nothing
            lTsPrecipGroup.Clear() : lTsPrecipGroup = Nothing

            For Each lTs In lTsPrecipComDurGroup
                lTs.Clear() : lTs = Nothing
            Next
            lTsPrecipComDurGroup.Clear() : lTsPrecipComDurGroup = Nothing

            lSW.Flush()
            lSW.Close()
            lSW = Nothing
        Next 'lSite

        lWDMSource.Clear()
        lWDMSource = Nothing
    End Sub

    Private Function WriteToColumn(ByVal aValue As Double, ByVal aColumn As Integer, ByVal aTotalColumns As Integer, ByVal aDelim As String) As String
        Dim lValue As String = String.Format("{0:0.00}", aValue)
        Dim lOneLine As String = ""
        Dim lThisField As String = ""
        For I As Integer = 1 To aTotalColumns
            lThisField = " "
            If I = aColumn Then lThisField = lValue
            lOneLine &= lThisField & aDelim
        Next
        Return lOneLine
    End Function

    Private Sub DisplayTsGraph(ByVal aDataGroup As atcTimeseriesGroup)
        Dim lGraphForm As New atcGraph.atcGraphForm()
        Dim lZgc As ZedGraphControl = lGraphForm.ZedGraphCtrl
        Dim lGraphTS As New clsGraphTime(aDataGroup, lZgc)
        lGraphForm.Grapher = lGraphTS
        With lGraphForm.Grapher.ZedGraphCtrl.GraphPane
            '.YAxis.Type = AxisType.Log
            'Dim lScaleMin As Double = 10
            '.YAxis.Scale.Min = lScaleMin

            For I As Integer = 0 To aDataGroup.Count - 1
                .CurveList.Item(I).Color = GetCurveColor(aDataGroup(I))
                'With CType(.CurveList.Item(I), LineItem).Symbol
                '    .Type = SymbolType.Circle
                '    .IsVisible = True
                'End With
            Next
            .AxisChange()
        End With
        lGraphForm.Show()
    End Sub

    Private Function GetCurveColor(ByVal aTs As atcTimeseries) As System.Drawing.Color
        Dim lClass As String = aTs.Attributes.GetValue("Class")
        Select Case lClass
            Case "Drought"
                Return Drawing.Color.Magenta
            Case "Dry"
                Return Drawing.Color.DarkOrange
            Case "Normal"
                Return Drawing.Color.Green
            Case "Wet"
                Return Drawing.Color.Cyan
            Case "Very Wet"
                Return Drawing.Color.DarkBlue
        End Select
    End Function

    Private Sub SwapInCBPFlowIntoGCRPRunWDMs()
        Dim lWDMDirCBP As String = "G:\Admin\HF_CBP\CBPResults\"
        Dim lWDMDirGCRP As String = "G:\Admin\HF_CBP\Runs\"

        Dim lWDMFileCBP As String
        'lWDMFileCBP = "SU8_1610_1530.wdm" '020501-R69
        'lWDMFileCBP = "SW7_1640_0003.wdm" '020502-R43
        'lWDMFileCBP = "SL9_2490_2520.wdm" '020503-R86
        'lWDMFileCBP = "SJ4_2660_2360.wdm" '02050303-Calib-R10

        Dim lWDMFilesCBP As New atcCollection()
        With lWDMFilesCBP
            .Add("020501-R69", "SU8_1610_1530.wdm") '020501-R69
            .Add("020502-R43", "SW7_1640_0003.wdm") '020502-R43
            .Add("020503-R86", "SL9_2490_2520.wdm") '020503-R86
            .Add("02050303-Calib-R10", "SJ4_2660_2360.wdm") '02050303-Calib-R10
        End With

        Dim lWDMFileGCRP As String = ""
        Dim lDSNGCRP As Integer = 101
        Dim lDSNCBP As Integer = 111
        Dim lWDMFileHandleCBP As atcWDM.atcDataSourceWDM = Nothing
        Dim lWDMFileHandleGCRP As atcWDM.atcDataSourceWDM = Nothing
        Dim lTSCBP As atcTimeseries = Nothing
        'Dim lTSGCRP As atcTimeseries = Nothing
        For Each lWDMFileCBP In lWDMFilesCBP
            Select Case lWDMFileCBP
                Case "SU8_1610_1530.wdm" '020501-R69
                    lWDMFileGCRP = "Susq020501.wdm"
                    lDSNGCRP = 101
                Case "SW7_1640_0003.wdm" '020502-R43
                    lWDMFileGCRP = "Susq020502.wdm"
                    lDSNGCRP = 102
                Case "SL9_2490_2520.wdm" '020503-R86
                    lWDMFileGCRP = "Susq020503.wdm"
                    lDSNGCRP = 101
                Case "SJ4_2660_2360.wdm" '02050303-Calib-R10
                    lWDMFileGCRP = "SusqCalib.wdm"
                    lDSNGCRP = 101
            End Select

            lWDMFileHandleCBP = New atcWDM.atcDataSourceWDM()
            If lWDMFileHandleCBP.Open(lWDMDirCBP & lWDMFileCBP) Then
                lWDMFileHandleGCRP = New atcWDM.atcDataSourceWDM()
                If lWDMFileHandleGCRP.Open(lWDMDirGCRP & lWDMFileGCRP) Then
                    lTSCBP = lWDMFileHandleCBP.DataSets.FindData("ID", lDSNCBP)(0)
                    If lTSCBP IsNot Nothing Then
                        'lTSGCRP = lWDMFileHandleGCRP.DataSets.FindData("ID", lDSNGCRP)(0)
                        'If lTSGCRP IsNot Nothing Then
                        'End If
                        lTSCBP = Aggregate(lTSCBP, atcTimeUnit.TUDay, 1, atcTran.TranAverSame)
                        lTSCBP.Attributes.SetValue("ID", lDSNGCRP)
                        If lWDMFileHandleGCRP.AddDataset(lTSCBP, atcDataSource.EnumExistAction.ExistReplace) Then Logger.Dbg("Success, replacing flow in " & lWDMFileGCRP & " @ DSN" & lDSNGCRP)
                    End If
                    lWDMFileHandleGCRP.Clear()
                    lWDMFileHandleGCRP = Nothing
                End If
                lWDMFileHandleCBP.Clear()
                lWDMFileHandleCBP = Nothing
            End If
            If lTSCBP IsNot Nothing Then
                lTSCBP.Clear() : lTSCBP = Nothing
            End If
        Next

        Logger.Dbg("Done swapping in CBP flow results into GCRP output WDM files")
    End Sub

    Private Sub DurationPlotGCRPvsCBP()
        Dim lWDMDirGCRP As String = "G:\Admin\GCRPSusq\Runs\"
        Dim lWDMDirGCRPWithReservoir As String = "G:\Admin\GCRPSusq\RunsWithReservoirs\"
        Dim lWDMDirGCRPWithResvWithWU2000 As String = "G:\Admin\GCRPSusq\RunsWithResvWU2000\"
        Dim lWDMDirGCRPWithResvWithWU2005 As String = "G:\Admin\GCRPSusq\RunsWithResvWU2005\"
        Dim lWDMDirCBP As String = "G:\Admin\HF_CBP\Runs\"
        Dim lGraphDir As String = "G:\Admin\EPA_HydroFrac_HSPFEval\Graphs\"

        Dim lWDMFileName As String = ""
        Dim lRuns As New atcCollection()
        With lRuns
            .Add("Susq020501")
            .Add("Susq020502")
            .Add("Susq020503")
            .Add("SusqCalib")
        End With
        Dim lRunsNickNames As New atcCollection()
        With lRunsNickNames
            .Add("Danville")
            .Add("WestBranchLewisburg")
            .Add("Marietta")
            .Add("Raystown")
        End With

        Dim lWDMGCRP As atcWDM.atcDataSourceWDM = Nothing
        Dim lWDMGCRPWithResv As atcWDM.atcDataSourceWDM = Nothing
        Dim lWDMGCRPWithResvWU2000 As atcWDM.atcDataSourceWDM = Nothing
        Dim lWDMGCRPWithResvWU2005 As atcWDM.atcDataSourceWDM = Nothing
        Dim lWDMCBP As atcWDM.atcDataSourceWDM = Nothing

        Dim lTsObsFlowIn As atcTimeseries
        Dim lTsObsFlowCfs As atcTimeseries
        Dim lTsSimFlowGCRPIn As atcTimeseries
        Dim lTsSimFlowGCRPCfs As atcTimeseries
        Dim lTsSimFlowGCRPWithResvIn As atcTimeseries
        Dim lTsSimFlowGCRPWithResvCfs As atcTimeseries
        Dim lTsSimFlowGCRPWithResvWU2000In As atcTimeseries
        Dim lTsSimFlowGCRPWithResvWU2000Cfs As atcTimeseries
        Dim lTsSimFlowGCRPWithResvWU2005In As atcTimeseries
        Dim lTsSimFlowGCRPWithResvWU2005Cfs As atcTimeseries
        Dim lTsSimFlowCBPIn As atcTimeseries
        Dim lTsSimFlowCBPCfs As atcTimeseries
        Dim lDsnObsFlow As Integer = 1 'cfs-daily
        Dim lDsnSimFlow As Integer = 2 'inches-daily
        Dim lDateStart As Double = Date2J(1985, 1, 1, 0, 0, 0)
        Dim lDateEnd As Double = Date2J(2005, 12, 31, 24, 0, 0)

        For Each lRun As String In lRuns
            'open WDM files
            lWDMGCRP = New atcWDM.atcDataSourceWDM()
            If lWDMGCRP.Open(lWDMDirGCRP & lRun & ".wdm") Then
                lWDMCBP = New atcWDM.atcDataSourceWDM()
                If Not lWDMCBP.Open(lWDMDirCBP & lRun & ".wdm") Then
                    lWDMCBP.Clear() : lWDMCBP = Nothing
                    lWDMGCRP.Clear() : lWDMGCRP = Nothing
                    Continue For
                End If
                lWDMGCRPWithResv = New atcWDM.atcDataSourceWDM()
                If Not lWDMGCRPWithResv.Open(lWDMDirGCRPWithReservoir & lRun & ".wdm") Then
                    lWDMCBP.Clear() : lWDMCBP = Nothing
                    lWDMGCRP.Clear() : lWDMGCRP = Nothing
                    lWDMGCRPWithResv.Clear() : lWDMGCRPWithResv = Nothing
                    Continue For
                End If

                lWDMGCRPWithResvWU2000 = New atcWDM.atcDataSourceWDM()
                If Not lWDMGCRPWithResvWU2000.Open(lWDMDirGCRPWithResvWithWU2000 & lRun & ".wdm") Then
                    lWDMCBP.Clear() : lWDMCBP = Nothing
                    lWDMGCRP.Clear() : lWDMGCRP = Nothing
                    lWDMGCRPWithResv.Clear() : lWDMGCRPWithResv = Nothing
                    lWDMGCRPWithResvWU2000.Clear() : lWDMGCRPWithResvWU2000 = Nothing
                    Continue For
                End If

                lWDMGCRPWithResvWU2005 = New atcWDM.atcDataSourceWDM()
                If Not lWDMGCRPWithResvWU2005.Open(lWDMDirGCRPWithResvWithWU2005 & lRun & ".wdm") Then
                    lWDMCBP.Clear() : lWDMCBP = Nothing
                    lWDMGCRP.Clear() : lWDMGCRP = Nothing
                    lWDMGCRPWithResv.Clear() : lWDMGCRPWithResv = Nothing
                    lWDMGCRPWithResvWU2000.Clear() : lWDMGCRPWithResvWU2000 = Nothing
                    lWDMGCRPWithResvWU2005.Clear() : lWDMGCRPWithResvWU2005 = Nothing
                    Continue For
                End If
            End If

            Dim lCons As String = "Flow"
            Dim lNickName As String = ""
            Dim lArea As Double = 0.0
            Select Case lRun
                Case "Susq020501" : lArea = 7186006 '@R69
                Case "Susq020502" : lArea = 4384720 '@R43
                Case "Susq020503" : lArea = 16554812 '@R86, but plus 01 and 02 drainage areas as well
                Case "SusqCalib" : lArea = 479638 '@R10
            End Select
            lNickName = lRunsNickNames.ItemByIndex(lRuns.IndexFromKey(lRun))

            lTsObsFlowCfs = SubsetByDate(lWDMGCRP.DataSets.ItemByKey(lDsnObsFlow), lDateStart, lDateEnd, Nothing)
            lTsObsFlowCfs.Attributes.SetValue("Units", "Flow (cfs)")
            lTsObsFlowCfs.Attributes.SetValue("YAxis", "Left")

            'lTsObsFlowIn = CfsToInches(lTsObsFlowCfs, lArea)
            'lTsObsFlowIn.Attributes.SetValue("Units", "Flow (inches)")
            'lTsObsFlowIn.Attributes.SetValue("YAxis", "Left")

            lTsSimFlowGCRPIn = SubsetByDate(lWDMGCRP.DataSets.ItemByKey(lDsnSimFlow), lDateStart, lDateEnd, Nothing)
            lTsSimFlowGCRPIn.Attributes.SetValue("Units", "Flow (inches)")
            lTsSimFlowGCRPIn.Attributes.SetValue("YAxis", "Left")

            lTsSimFlowGCRPCfs = InchesToCfs(lTsSimFlowGCRPIn, lArea)
            With lTsSimFlowGCRPCfs.Attributes
                .SetValue("Units", "Flow (cfs)")
                .SetValue("YAxis", "Left")
                .SetValue("Scenario", "GCRP")
            End With

            lTsSimFlowGCRPWithResvIn = SubsetByDate(lWDMGCRPWithResv.DataSets.ItemByKey(lDsnSimFlow), lDateStart, lDateEnd, Nothing)
            lTsSimFlowGCRPWithResvIn.Attributes.SetValue("Units", "Flow (inches)")
            lTsSimFlowGCRPWithResvIn.Attributes.SetValue("YAxis", "Left")

            lTsSimFlowGCRPWithResvCfs = InchesToCfs(lTsSimFlowGCRPWithResvIn, lArea)
            With lTsSimFlowGCRPWithResvCfs.Attributes
                .SetValue("Units", "Flow (cfs)")
                .SetValue("YAxis", "Left")
                .SetValue("Scenario", "GCRPWithReservoir")
            End With

            lTsSimFlowGCRPWithResvWU2000In = SubsetByDate(lWDMGCRPWithResvWU2000.DataSets.ItemByKey(lDsnSimFlow), lDateStart, lDateEnd, Nothing)
            lTsSimFlowGCRPWithResvWU2000In.Attributes.SetValue("Units", "Flow (inches)")
            lTsSimFlowGCRPWithResvWU2000In.Attributes.SetValue("YAxis", "Left")

            lTsSimFlowGCRPWithResvWU2000Cfs = InchesToCfs(lTsSimFlowGCRPWithResvWU2000In, lArea)
            With lTsSimFlowGCRPWithResvWU2000Cfs.Attributes
                .SetValue("Units", "Flow (cfs)")
                .SetValue("YAxis", "Left")
                .SetValue("Scenario", "GCRPWithResvWU2000")
            End With

            lTsSimFlowGCRPWithResvWU2005In = SubsetByDate(lWDMGCRPWithResvWU2005.DataSets.ItemByKey(lDsnSimFlow), lDateStart, lDateEnd, Nothing)
            lTsSimFlowGCRPWithResvWU2005In.Attributes.SetValue("Units", "Flow (inches)")
            lTsSimFlowGCRPWithResvWU2005In.Attributes.SetValue("YAxis", "Left")

            lTsSimFlowGCRPWithResvWU2005Cfs = InchesToCfs(lTsSimFlowGCRPWithResvWU2005In, lArea)
            With lTsSimFlowGCRPWithResvWU2005Cfs.Attributes
                .SetValue("Units", "Flow (cfs)")
                .SetValue("YAxis", "Left")
                .SetValue("Scenario", "GCRPWithResvWU2005")
            End With

            lTsSimFlowCBPIn = SubsetByDate(lWDMCBP.DataSets.ItemByKey(lDsnSimFlow), lDateStart, lDateEnd, Nothing)
            lTsSimFlowCBPIn.Attributes.SetValue("Units", "Flow (inches)")
            lTsSimFlowCBPIn.Attributes.SetValue("YAxis", "Left")

            lTsSimFlowCBPCfs = InchesToCfs(lTsSimFlowCBPIn, lArea)
            With lTsSimFlowCBPCfs.Attributes
                .SetValue("Units", "Flow (cfs)")
                .SetValue("YAxis", "Left")
                .SetValue("Scenario", "CBP")
            End With

            Dim lTsGroup As New atcTimeseriesGroup
            lTsGroup.Add(lTsObsFlowCfs)
            lTsGroup.Add(lTsSimFlowCBPCfs)
            lTsGroup.Add(lTsSimFlowGCRPCfs)
            lTsGroup.Add(lTsSimFlowGCRPWithResvCfs)
            lTsGroup.Add(lTsSimFlowGCRPWithResvWU2000Cfs)
            lTsGroup.Add(lTsSimFlowGCRPWithResvWU2005Cfs)

            Dim lSaveIn As String = lGraphDir & lRun & "_" & lNickName & ".png"
            If IO.File.Exists(lSaveIn) Then
                TryDelete(lSaveIn)
            End If
            DisplayDurGraph(lTsGroup, lSaveIn)

            'clean up
            lWDMGCRP.Clear() : lWDMGCRP = Nothing
            lWDMGCRPWithResv.Clear() : lWDMGCRPWithResv = Nothing
            lWDMGCRPWithResvWU2000.Clear() : lWDMGCRPWithResvWU2000 = Nothing
            lWDMGCRPWithResvWU2005.Clear() : lWDMGCRPWithResvWU2005 = Nothing
            lWDMCBP.Clear() : lWDMCBP = Nothing
            lTsObsFlowCfs.Clear()
            lTsSimFlowGCRPCfs.Clear()
            lTsSimFlowGCRPWithResvCfs.Clear()
            lTsSimFlowGCRPWithResvWU2000Cfs.Clear()
            lTsSimFlowGCRPWithResvWU2005Cfs.Clear()

            lTsSimFlowCBPCfs.Clear()
            lTsSimFlowCBPIn.Clear()
            lTsSimFlowGCRPIn.Clear()
            lTsSimFlowGCRPWithResvIn.Clear()
            lTsSimFlowGCRPWithResvWU2000In.Clear()
            lTsSimFlowGCRPWithResvWU2005In.Clear()
        Next

        Logger.Dbg("Done graphing duration plots of GCRP vs CBP vs observed flow in CFS.")
    End Sub

    Private Sub DisplayDurGraph(ByVal aDataGroup As atcTimeseriesGroup, Optional ByVal aSaveIn As String = "")
        Dim lGraphForm As New atcGraph.atcGraphForm()

        Dim lZgc As ZedGraphControl = lGraphForm.ZedGraphCtrl
        Dim lGraphTS As New clsGraphProbability(aDataGroup, lZgc)
        lGraphForm.Grapher = lGraphTS
        With lGraphForm.Grapher.ZedGraphCtrl.GraphPane

            .AxisChange()
            .CurveList.Item(0).Color = Drawing.Color.Blue
            .CurveList.Item(1).Color = Drawing.Color.Red
            .CurveList.Item(2).Color = Drawing.Color.Cyan
            With .Legend.FontSpec
                .IsBold = False
                .Border.IsVisible = False
                .Size = 12
            End With
            .XAxis.Title.Text = "PERCENTAGE OF TIME FLOW WAS EQUALED OR EXCEEDED"
        End With
        lGraphForm.Grapher.ZedGraphCtrl.Refresh()

        If aSaveIn = "" Then
            lGraphForm.Show()
        Else
            lZgc.SaveIn(aSaveIn)
            lZgc.Dispose()
        End If
    End Sub

    ''' <summary>
    ''' The Overall Goal:
    ''' This routine is for creating 3 scenarios of public water supply water use (PWS)
    ''' ie Business-As-Usual, Energy Plus, and Green Technology in the Susq simulations for different years, 2040, 2025 etc
    ''' The provided projection PWS file is: G:\Admin\EPA_HydroFrac_HSPFEval\WaterUse\PWS_Well_Projection\PWS_SBR_projections2040.xls, 
    ''' which contains water use that is 100% consumptive and all from surface fresh water source for the year of 2040; 
    ''' this means no need for adjustment for the goal of this study of focusing only on surface fresh water source for consumptive use
    ''' 
    ''' The idea is to create 3 versions of the following 3 WDMs, one for each of the 3 scenarios above:
    ''' G:\Admin\GCRPSusq\RunsWithResvWU2005\parms\SusqTrans01.wdm
    ''' G:\Admin\GCRPSusq\RunsWithResvWU2005\parms\SusqTrans02.wdm
    ''' G:\Admin\GCRPSusq\RunsWithResvWU2005\parms\SusqTrans03.wdm
    ''' 
    ''' They will be saved in the following folders respectively (for future use):
    ''' G:\Admin\GCRPSusq\RunsWithResvWU2005\parms\BAU2040\ 'business-as-usual
    ''' G:\Admin\GCRPSusq\RunsWithResvWU2005\parms\EP2040\  'energy plus
    ''' G:\Admin\GCRPSusq\RunsWithResvWU2005\parms\GT2040\  'green technology
    ''' 
    ''' In keeping with past effort of distributing USGS water use data, these scenarios' water use will be 
    ''' constructed for the duration of 1985-01-01 ~ 2005-12-31 at monthly timestep and
    ''' converted from the original MGD to CFS
    ''' 
    ''' Specific Action:
    ''' update county's projected PWS water use data in corresponding state water use files, ie
    '''mdco2005SelectedWU_WSWFr_BAU2040.xls (24)
    '''mdco2005SelectedWU_WSWFr_EP2040.xls
    '''mdco2005SelectedWU_WSWFr_GT2040.xls
    '''nyco2005SelectedWU_WSWFr_BAU2040.xls (36)
    '''nyco2005SelectedWU_WSWFr_EP2040.xls
    '''nyco2005SelectedWU_WSWFr_GT2040.xls
    '''paco2005SelectedWU_WSWFr_BAU2040.xls (42)
    '''paco2005SelectedWU_WSWFr_EP2040.xls
    '''paco2005SelectedWU_WSWFr_GT2040.xls
    ''' in the fifth column: PS-WSWFr
    ''' 
    ''' Then, these projected water use data files will be used by steps 81, 82, and 83 to write into WDMs
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub ProjectionPWSCopyToUSGSWaterUseFiles()

        Dim lWaterUseDataDirectory As String = "G:\Admin\EPA_HydroFrac_HSPFEval\WaterUse\"
        Dim lProjectionYear As String = "2040"
        lProjectionYear = "2025"

        Dim lProjectionPWSFilename As String = lWaterUseDataDirectory & "PWS_Well_Projection\PWS_SRB_projections" & lProjectionYear & ".xls"

        Dim lScenario As String = "BAU"
        lScenario = "EP"
        lScenario = "GT"

        Dim lxlApp As Excel.Application = Nothing
        Dim lxlWorkbook As Excel.Workbook = Nothing
        Dim lxlSheet As Excel.Worksheet = Nothing

        Dim lxlWorkbookMd As Excel.Workbook = Nothing
        Dim lxlWorkbookNy As Excel.Workbook = Nothing
        Dim lxlWorkbookPa As Excel.Workbook = Nothing

        Dim lxlSheetMd As Excel.Worksheet = Nothing
        Dim lxlSheetNy As Excel.Worksheet = Nothing
        Dim lxlSheetPa As Excel.Worksheet = Nothing

        Dim lxlSheetTemp As Excel.Worksheet = Nothing

        lxlApp = New Excel.Application()
        lxlWorkbook = lxlApp.Workbooks.Open(lProjectionPWSFilename)
        lxlSheet = lxlWorkbook.Worksheets("Susquehanna Projections")

        lxlWorkbookMd = lxlApp.Workbooks.Open(lWaterUseDataDirectory & "mdco2005SelectedWU_WSWFr_" & lScenario & lProjectionYear & ".xls")
        lxlSheetMd = lxlWorkbookMd.Worksheets("County")

        lxlWorkbookNy = lxlApp.Workbooks.Open(lWaterUseDataDirectory & "nyco2005SelectedWU_WSWFr_" & lScenario & lProjectionYear & ".xls")
        lxlSheetNy = lxlWorkbookNy.Worksheets("County")

        lxlWorkbookPa = lxlApp.Workbooks.Open(lWaterUseDataDirectory & "paco2005SelectedWU_WSWFr_" & lScenario & lProjectionYear & ".xls")
        lxlSheetPa = lxlWorkbookPa.Worksheets("County")

        With lxlSheet
            Dim lProjectionColumn As Integer = 7
            Select Case lScenario
                Case "BAU" : lProjectionColumn = 7
                Case "EP" : lProjectionColumn = 9
                Case "GT" : lProjectionColumn = 11
            End Select
            For lRow As Integer = 3 To .UsedRange.Rows.Count
                Dim lFips As String = .Cells(lRow, 1).Value
                Dim lWUPWSup2005Projected As Double = .Cells(lRow, lProjectionColumn).Value

                Select Case lFips.Substring(0, 2)
                    Case "24"
                        lxlSheetTemp = lxlSheetMd
                    Case "36"
                        lxlSheetTemp = lxlSheetNy
                    Case "42"
                        lxlSheetTemp = lxlSheetPa
                End Select

                For lRowC As Integer = 2 To lxlSheetTemp.UsedRange.Rows.Count
                    If lFips = lxlSheetTemp.Cells(lRowC, 4).Value Then
                        lxlSheetTemp.Cells(lRowC, 5).Value = Math.Round(lWUPWSup2005Projected, 2)
                        Exit For
                    End If
                Next
            Next 'lRow in projection Excel file
        End With 'lxlsheet in projection Excel file

        lxlWorkbook.Close(False)
        lxlWorkbookMd.Close(True)
        lxlWorkbookNy.Close(True)
        lxlWorkbookPa.Close(True)

        'clean up
        lxlApp.Quit()
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlSheet)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbook)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlSheetMd)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbookMd)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlSheetNy)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbookNy)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlSheetPa)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbookPa)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlSheetTemp)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlApp)

        lxlSheet = Nothing
        lxlSheetMd = Nothing
        lxlSheetNy = Nothing
        lxlSheetPa = Nothing
        lxlSheetTemp = Nothing
        lxlWorkbook = Nothing
        lxlWorkbookMd = Nothing
        lxlWorkbookNy = Nothing
        lxlWorkbookPa = Nothing
        lxlApp = Nothing
    End Sub

    Private Sub ConstructStateCountyList(ByVal aCountyFile As String, ByRef aCollection As atcCollection)
        '*** 
        Dim lFilename As String = aCountyFile
        'construct county fips-area (sq meter) dictionary
        Dim lOneLine As String
        Dim lArrCounty() As String
        Dim lFips As String = ""
        Dim lStateAbbrev As String = ""

        Dim lLinebuilder As New Text.StringBuilder
        Dim lCountyList As New atcCollection()
        Dim lSRCounty As New StreamReader(lFilename)
        While Not lSRCounty.EndOfStream
            lOneLine = lSRCounty.ReadLine()
            lArrCounty = Regex.Split(lOneLine, "\s+")
            Dim lState As WUState = aCollection.ItemByKey(lArrCounty(5).Substring(0, 2))
            If lState Is Nothing Then
                lState = New WUState
                With lState
                    .Code = lArrCounty(5).Substring(0, 2)
                    .Abbreviation = lArrCounty(6)
                End With
                aCollection.Add(lState.Code, lState)
            End If
            If Not lState.Counties.Keys.Contains(lArrCounty(5)) Then
                Dim lCounty As New WUCounty
                With lCounty
                    .Fips = lArrCounty(5)
                    .Code = lArrCounty(5).Substring(2) 'the remaining 3 digits
                    .Area = Double.Parse(lArrCounty(13))
                    .State = lState
                End With
                lState.Counties.Add(lCounty.Fips, lCounty)
            End If

        End While
        lSRCounty.Close()
        lSRCounty = Nothing
    End Sub

    ''' <summary>
    ''' This routine is to create dataset place holders in various WDM1 files
    ''' to hold the flow output from every reach in the UCIs for both year 2000 and 2005
    ''' Corresponding output lines are added to various UCIs' EXT TARGETS blocks
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub CreateReachFlowOutputDatasetPlaceHoldersInWDM1s()
        Dim lBaseFolder As String = "G:\Admin\GCRPSusq\RunsWithResvWU"
        Dim lYears() As Integer = {2000, 2005}
        Dim lSusqWDMs() As String = {"Susq020501.wdm", "Susq020502.wdm", "Susq020503.wdm"}
        Dim lSusqRchres() As Integer = {118, 67, 93}
        Dim lBaseID As Integer = 9000
        Dim lCopyFromIDs() As Integer = {101, 102, 101}

        Dim lWDMFilename As String
        Dim lWDMHandle As atcWDM.atcDataSourceWDM

        For Each lYear As Integer In lYears
            For I As Integer = 0 To lSusqWDMs.Length - 1
                lWDMFilename = lBaseFolder & lYear & "\" & lSusqWDMs(I)
                lWDMHandle = New atcWDM.atcDataSourceWDM()
                If lWDMHandle.Open(lWDMFilename) Then
                    'For Debug
                    'Dim lSW As New StreamWriter("C:\Temp\z.txt", False)
                    'For Each lTs As atcTimeseries In lWDMHandle.DataSets
                    '    lSW.WriteLine(lTs.Attributes.GetValue("ID") & vbTab & lTs.Attributes.GetValue("Constituent"))
                    'Next
                    'lSW.Flush()
                    'lSW.Close()
                    'lSW = Nothing

                    Dim lTsCopyFrom As atcTimeseries = lWDMHandle.DataSets.FindData("ID", lCopyFromIDs(I))(0)
                    For J As Integer = 1 To lSusqRchres(I)
                        Dim lTsCopyTo As atcTimeseries = lTsCopyFrom.Clone()
                        lTsCopyTo.Attributes.SetValue("ID", lBaseID + J)
                        lTsCopyTo.Attributes.SetValue("Location", "R:" & J)
                        lTsCopyTo.Attributes.SetValue("Constituent", "FLOW")
                        If Not lWDMHandle.AddDataset(lTsCopyTo, atcDataSource.EnumExistAction.ExistReplace) Then
                            Logger.Dbg("Failed copying: " & lWDMFilename & " R:" & J)
                        End If
                    Next
                    lWDMHandle.Clear()
                    lWDMHandle = Nothing
                End If
            Next 'Susq WDM
        Next 'Next Year
        Logger.Dbg("Done copying.")
    End Sub

    ''' <summary>
    ''' This routine is to remove the previously added HFRAC data so that
    ''' new HFRAC data can be added. These are having dataset ids in the 6000 range
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub RemoveFrackingDataFrom2025ScenarioWDMs()
        Dim lTargetWDMFolder As String = "G:\Admin\GCRPSusq\RunsWithResvWU2005\parms\"
        Dim lProjectionScenario() As String = {"BAU", "EP", "GT"}

        Dim lProjectionYear As String = "2040"
        lProjectionYear = "2025"

        Dim lWDMs() As String = {"SusqTrans01.wdm", "SusqTrans02.wdm", "SusqTrans03.wdm"}
        Dim lWDMHandle As atcWDM.atcDataSourceWDM = Nothing
        For Each lScen As String In lProjectionScenario
            For Each lWDM As String In lWDMs
                Dim lWDMFilename As String = lTargetWDMFolder & lScen & lProjectionYear & "\" & lWDM
                lWDMHandle = New atcWDM.atcDataSourceWDM()
                If lWDMHandle.Open(lWDMFilename) Then
                    Dim lHFRACdatasets As atcTimeseriesGroup = lWDMHandle.DataSets.FindData("Constituent", "HFRAC")
                    For Each lTs As atcDataSet In lHFRACdatasets
                        lWDMHandle.RemoveDataset(lTs)
                    Next
                    lWDMHandle.Clear()
                    lWDMHandle = Nothing
                End If
            Next
        Next

    End Sub

    Private Sub AddNewFrackingDataTo2025ScenarioWDMs()
        Dim lBaseFolder As String = "G:\Admin\GCRPSusq\RunsWithResvWU2005\parms\"
        'Dim lYears() As Integer = {2000, 2005}
        Dim lProjectionYears() As Integer = {2025}

        Dim lWDMs() As String = {"SusqTrans01.wdm", "SusqTrans02.wdm", "SusqTrans03.wdm"}
        Dim lSusqRuns() As String = {"020501", "020502", "020503"}
        Dim lSusqRchres() As Integer = {118, 67, 93}
        Dim lBaseID As Integer = 6000

        Dim lMgY2Cfs As Double = 0.00423617166 '1 million us gallons per year = cubic foot per second

        Dim lDateStart As Double = Date2J(1985, 1, 1, 0, 0, 0)
        Dim lDateEnd As Double = Date2J(2005, 12, 31, 24, 0, 0)

        Dim lNewProjectionFrackingDataFile As String = "G:\Admin\EPA_HydroFrac_HSPFEval\WaterUse\PWS_Well_Projection\FutureFrackingWU_SRB.xls"
        Dim lScenariosWorksheets() As String = {"Business_As_Usual", "Energy_Plus", "Green_Technology"}

        Dim lScenariosFolders() As String = {"BAU", "EP", "GT"}

        Dim lWDMFilename As String
        Dim lWDMHandle As atcWDM.atcDataSourceWDM


        Dim lxlApp As Excel.Application = Nothing
        Dim lxlWorkbook As Excel.Workbook = Nothing
        Dim lxlSheet As Excel.Worksheet = Nothing

        lxlApp = New Excel.Application()
        lxlWorkbook = lxlApp.Workbooks.Open(lNewProjectionFrackingDataFile)

        For Each lProjectionYear As Integer In lProjectionYears
            For Each lScen As String In lScenariosFolders
                Dim lWorkSheetName As String = ""
                Select Case lScen
                    Case "BAU" : lWorkSheetName = "Business_As_Usual"
                    Case "EP" : lWorkSheetName = "Energy_Plus"
                    Case "GT" : lWorkSheetName = "Green_Technology"
                End Select
                lxlSheet = lxlWorkbook.Worksheets(lWorkSheetName)

                For I As Integer = 0 To lSusqRuns.Length - 1
                    lWDMFilename = lBaseFolder & lScen & lProjectionYear & "\" & "SusqTrans" & lSusqRuns(I).Substring(4) & ".wdm"
                    lWDMHandle = New atcWDM.atcDataSourceWDM()
                    If lWDMHandle.Open(lWDMFilename) Then
                        For S As Integer = 1 To lSusqRchres(I)

                            Dim lProj As String = ""
                            Dim lSub As String = ""
                            Dim lHFVal As String = ""

                            For lRow As Integer = 2 To lxlSheet.UsedRange.Rows.Count
                                lProj = lxlSheet.Cells(lRow, 7).Value
                                lSub = lxlSheet.Cells(lRow, 6).Value
                                If lProj = lSusqRuns(I) AndAlso Integer.Parse(lSub) = S Then
                                    lHFVal = lxlSheet.Cells(lRow, 5).Value
                                    Exit For
                                End If
                            Next 'lRow
                            If lHFVal = "" Then
                                lHFVal = "0.0"
                            End If

                            Dim lHFValDouble As Double = Double.Parse(lHFVal) 'Millions Gallon per year
                            Dim lHFValCfs As Double = lHFValDouble * lMgY2Cfs
                            Dim lTsHFrac2025 As atcTimeseries = GCRPSubbasin.BuildWUTimeseries(atcTimeUnit.TUYear, "HFRAC", lBaseID + Integer.Parse(lSub), lHFValCfs, lDateStart, lDateEnd, "R:" & lSub, lScen)

                            If Not lWDMHandle.AddDataset(lTsHFrac2025, atcDataSource.EnumExistAction.ExistReplace) Then
                                Logger.Dbg("Failed writing 2025 HydroFracking for " & lScen & lProjectionYear & ":" & lHFValDouble)
                            End If

                        Next 'subbasin S

                        lWDMHandle.Clear()
                        lWDMHandle = Nothing
                    End If
                Next 'SusqRun

            Next 'lScen
        Next 'lprojectionYear

        lxlWorkbook.Close(False)

        'clean up
        lxlApp.Quit()
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlSheet)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlWorkbook)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(lxlApp)

        lxlSheet = Nothing
        lxlWorkbook = Nothing
        lxlApp = Nothing
        Logger.Dbg("Done writing 2025 HFRAC.")
    End Sub

#Region "UtilityClasses"
    Public Class WUState
        Public Name As String
        Public Code As String
        Public Abbreviation As String
        Public Shared WaterUseCategories2000 As ArrayList
        Public Shared WaterUseCategories2005 As ArrayList

        Public Counties As atcCollection
        Public Sub New()
            Counties = New atcCollection()
            WaterUseCategories2000 = New ArrayList()
            WaterUseCategories2005 = New ArrayList()
        End Sub

    End Class

    Public Class WUCounty
        Public State As WUState
        Public Name As String
        Public Fips As String
        Public Code As String
        Public Area As Double
        Public CUPcts As atcDataAttributes
        Public WaterUses2000 As atcDataAttributes
        Public WaterUses2005 As atcDataAttributes
        Public Sub New()
            CUPcts = New atcDataAttributes()
            WaterUses2000 = New atcDataAttributes()
            WaterUses2005 = New atcDataAttributes()
        End Sub
    End Class

    Public Class DataDictionaries
        Public Specification As String
        Public DDTable As atcTableDelimited
        Public Col1995 As Integer = -99
        Public Col2000 As Integer = -99
        Public Col2005 As Integer = -99
        Public Cat1995 As String
        Public Cat2000 As String
        Public Cat2005 As String
        Public Sub New(Optional ByVal aSpec As String = "", Optional ByVal aDelim As String = ",")
            Specification = aSpec
            If File.Exists(Specification) Then
                DDTable = New atcTableDelimited
                With DDTable
                    .Delimiter = aDelim
                    If Not .OpenFile(Specification) Then
                        Specification = ""
                        DDTable = Nothing
                    Else
                        For I As Integer = 1 To .NumFields
                            Select Case .FieldName(I)
                                Case "1995" : Col1995 = I
                                Case "2000" : Col2000 = I
                                Case "2005" : Col2005 = I
                            End Select
                        Next
                    End If
                End With
            End If
        End Sub
        Public Sub MatchCategory(ByVal aYear As Integer, ByVal aCat As String)
            If DDTable Is Nothing Then
                Cat1995 = "" : Cat2000 = "" : Cat2005 = ""
                Exit Sub
            ElseIf Col1995 < 0 OrElse Col2000 < 0 OrElse Col2005 < 0 Then
                Exit Sub
            End If
            With DDTable
                .CurrentRecord = 1
                Dim lFoundMatch As Boolean = False
                While Not .EOF
                    Select Case aYear
                        Case 1995
                            If .Value(Col1995).ToLower = aCat.ToLower Then
                                Cat1995 = aCat
                                Cat2000 = .Value(Col2000)
                                Cat2005 = .Value(Col2005)
                                lFoundMatch = True
                            End If
                        Case 2000
                            If .Value(Col2000).ToLower = aCat.ToLower Then
                                Cat1995 = .Value(Col1995)
                                Cat2000 = aCat
                                Cat2005 = .Value(Col2005)
                                lFoundMatch = True
                            End If
                        Case 2005
                            If .Value(Col2005).ToLower = aCat.ToLower Then
                                Cat1995 = .Value(Col1995)
                                Cat2000 = .Value(Col2000)
                                Cat2005 = aCat
                                lFoundMatch = True
                            End If
                    End Select
                    If lFoundMatch Then
                        Exit Sub
                    Else
                        Cat1995 = ""
                        Cat2000 = ""
                        Cat2005 = ""
                    End If
                    .MoveNext()
                End While
            End With
        End Sub
        Public Sub Clear()
            If DDTable IsNot Nothing Then
                DDTable.Clear()
                DDTable = Nothing
            End If
        End Sub
    End Class

    Public Class GCRPRun
        Public Subbasins As atcCollection
        Public Sub New()
            Subbasins = New atcCollection()
        End Sub
        Public Sub WriteWaterUse(ByVal aYear As Integer, ByVal aSpec As String, Optional ByVal aHeader As String = "") 'aYear is either 2000 or 2005
            Dim lSW As New StreamWriter(aSpec, False)
            If Not aHeader = "" Then
                lSW.WriteLine(aHeader)
            End If
            For Each lSubbasin As GCRPSubbasin In Subbasins
                lSubbasin.WUYear = aYear
                lSW.WriteLine(lSubbasin.ToString())
            Next
            lSW.Flush()
            lSW.Close()
            lSW = Nothing
        End Sub
        Public Sub Clear()
            For Each lSub As GCRPSubbasin In Subbasins
                lSub.Clear()
            Next
            Subbasins.Clear()
        End Sub
    End Class

    Public Class GCRPSubbasin
        Public SubbasinId As Integer
        Public WUYear As Integer
        Private pNumWUs As Integer
        Public WUAreaList As atcCollection
        Public WaterUses2000 As atcCollection
        Public WaterUses2005 As atcCollection
        'Public WUPWSup2000 As Double
        'Public WUPWSup2005 As Double
        'Public WUOSup2000 As Double
        'Public WUOSup2005 As Double

        Public Property NumWUs() As Integer
            Get
                Return pNumWUs
            End Get
            Set(ByVal value As Integer)
                pNumWUs = value
                If WUYear = 2000 Then
                    WaterUses2000.Clear()
                ElseIf WUYear = 2005 Then
                    WaterUses2005.Clear()
                End If
                For I As Integer = 0 To pNumWUs - 1
                    If WUYear = 2000 Then
                        WaterUses2000.Add(I.ToString, 0.0)
                    ElseIf WUYear = 2005 Then
                        WaterUses2005.Add(I.ToString, 0.0)
                    End If
                Next
            End Set
        End Property
        Public Sub New()
            WUAreaList = New atcCollection()
            WaterUses2000 = New atcCollection() 'keyed on wateruse cat name
            WaterUses2005 = New atcCollection() 'keyed on wateruse cat name
        End Sub

        Public Overrides Function ToString() As String
            Dim lWUs As atcCollection = Nothing
            If WUYear = 2000 Then
                lWUs = WaterUses2000
            ElseIf WUYear = 2005 Then
                lWUs = WaterUses2005
            End If
            If lWUs Is Nothing Then Return ""

            Dim lStr As String = SubbasinId.ToString & ","
            For I As Integer = 0 To lWUs.Count - 1
                lStr &= DoubleToString(lWUs.ItemByIndex(I)).Replace(",", "") & ","
            Next

            Return lStr.TrimEnd(",")
        End Function

        Public Shared Function BuildWUTimeseries(ByVal aTU As atcTimeUnit, _
                                                 ByVal aCon As String, _
                                                 ByVal aId As Integer, _
                                                 ByVal aWUValue As Double, _
                                                 ByVal aStartDate As Double, _
                                                 ByVal aEndDate As Double, _
                                                 Optional ByVal aLocation As String = "", _
                                                 Optional ByVal aScenario As String = "") As atcTimeseries

            Dim lTsDates As New atcTimeseries(Nothing)
            lTsDates.Values = NewDates(aStartDate, aEndDate, aTU, 1)

            Dim lTs As New atcTimeseries(Nothing)
            With lTs
                .Dates = lTsDates
                .numValues = .Dates.numValues
                .SetInterval(aTU, 1)
                .Attributes.SetValue("ID", aId)
                .Attributes.SetValue("TSTYP", aCon)
                .Attributes.SetValue("Constituent", aCon)

                If aLocation <> "" Then .Attributes.SetValue("Location", aLocation)
                If aScenario <> "" Then .Attributes.SetValue("Scenario", aScenario)

                If aWUValue < 0 Then
                    Return Nothing
                End If

                .Value(0) = GetNaN()
                For I As Integer = 1 To .numValues
                    .Value(I) = aWUValue
                Next
            End With

            Return lTs
        End Function

        Public Shared Function BuildDailyTimeseries(ByVal aCon As String, _
                                                 ByVal aId As Integer, _
                                                 ByVal aValues() As Double, _
                                                 ByVal aLeapYear As Boolean, _
                                                 ByVal aStartDate As Double, _
                                                 ByVal aEndDate As Double, _
                                                 Optional ByVal aLocation As String = "", _
                                                 Optional ByVal aScenario As String = "") As atcTimeseries

            'this routine specifically build the daily Hydrofracking water withdrawal timeseries for the 
            'study, it expects a full year (2010 in this case) of daily values

            Dim lTsDates As New atcTimeseries(Nothing)
            lTsDates.Values = NewDates(aStartDate, aEndDate, atcTimeUnit.TUDay, 1)

            Dim lTs As New atcTimeseries(Nothing)
            With lTs
                .Dates = lTsDates
                .numValues = .Dates.numValues
                .SetInterval(atcTimeUnit.TUDay, 1)
                .Attributes.SetValue("ID", aId)
                .Attributes.SetValue("TSTYP", aCon)
                .Attributes.SetValue("Constituent", aCon)

                If aLocation <> "" Then .Attributes.SetValue("Location", aLocation)
                If aScenario <> "" Then .Attributes.SetValue("Scenario", aScenario)

                Dim lAllZeros As Boolean = True
                For I As Integer = 0 To aValues.Length - 1
                    If aValues(I) > 0 Then
                        lAllZeros = False
                        Exit For
                    End If
                Next
                If lAllZeros Then Return Nothing

                Dim lDate(5) As Integer
                Dim lDailyValues As New List(Of Double)
                lDailyValues.Add(GetNaN())

                J2Date(aStartDate, lDate)
                Dim lYearStart As Integer = lDate(0)
                J2Date(aEndDate, lDate)
                Dim lYearEnd As Integer = lDate(0) - 1

                For lYear As Integer = lYearStart To lYearEnd
                    If Date.IsLeapYear(lYear) Then
                        If aLeapYear Then
                            lDailyValues.AddRange(aValues)
                        Else
                            For ld As Integer = 0 To 58
                                lDailyValues.Add(aValues(ld))
                            Next
                            lDailyValues.Add((aValues(58) + aValues(59)) / 2.0)
                            For ld As Integer = 59 To aValues.Length - 1
                                lDailyValues.Add(aValues(ld))
                            Next
                        End If
                    Else 'not a leap year
                        If aLeapYear Then
                            For ld As Integer = 0 To 58
                                lDailyValues.Add(aValues(ld))
                            Next

                            For ld As Integer = 60 To aValues.Length - 1
                                lDailyValues.Add(aValues(ld))
                            Next
                        Else
                            lDailyValues.AddRange(aValues)
                        End If

                    End If
                Next 'lYear

                .Values = lDailyValues.ToArray()
            End With

            Return lTs

        End Function

        Public Sub Clear()
            WUAreaList.Clear()
            WaterUses2000.Clear()
            WaterUses2005.Clear()
            WUAreaList = Nothing
            WaterUses2000 = Nothing
            WaterUses2005 = Nothing
        End Sub
    End Class
#End Region

End Module
