Imports MapWindow.Interfaces
Imports MapWinUtility
Imports System
Imports System.Data
Imports System.Collections.ObjectModel
Imports Microsoft.VisualBasic

Imports atcUtility
Imports atcData
Imports atcWDM
Imports atcTimeseriesBinary
Imports atcList
Imports atcTimeseriesMath.atcTimeseriesMath
Imports SwatObject

Module SWATRunner
    Private pStartYear As Integer = 0 '1960
    Private pNumYears As Integer = 12
    Private pRefreshDB As Boolean = False ' make a copy of the SWATInput database
    Private pUserInteractiveUpdate As Boolean = True 'False - use these defaults
    Private pOutputSummarize As Boolean = True
    Private pInputSummarize As Boolean = True
    Private pCropChangeSummarize As Boolean = True
    Private pChangeCropAreas As Boolean = True
    Private pRunModel As Boolean = True
    Private pScenario As String = "RevCrop"
    Private pDrive As String = "S:"
    Private pBaseFolder As String = pDrive & "\Scratch\UMRB\baseline90"
    Private pSWATGDB As String = "c:\Program Files\SWAT 2005 Editor\Databases\SWAT2005.mdb"
    Private pOutGDB As String = "baseline90.mdb"
    Private pInputFolder As String
    Private pOutGDBFolder As String
    Private pOutputFolder As String
    Private pReportsFolder As String
    Private pLogsFolder As String
    Private pParmChangesTextfile As String = "SWATParmChanges.txt"
    Friend pFormat As String = "###,##0.00"
    'Private pSWATExe As String = pOutputFolder & "\swat2005.exe" 'local copy with input data
    Private pSWATExe As String = "C:\Program Files\SWAT 2005 Editor\swat2005.exe"

    Public Sub ScriptMain(ByRef aMapWin As IMapWin)

        Dim lContinue As Boolean = True
        If pUserInteractiveUpdate Then
            'these defaults are overwritten by registry values set by most recent run
            Dim lUserParms As New atcCollection
            With lUserParms
                .Add("Start Year", pStartYear)
                .Add("Number of Years", pNumYears)
                .Add("Run Model", pRunModel)

                .Add("RefreshDB", pRefreshDB)
                .Add("OutputSummarize", pOutputSummarize)
                .Add("InputSummarize", pInputSummarize)
                .Add("ChangeCropAreas", pChangeCropAreas)
                .Add("CropChangeSummarize", pCropChangeSummarize)
                .Add("Scenario", pScenario)
                .Add("Drive", pDrive)
                .Add("BaseFolder", pBaseFolder)
                .Add("SWATGDB", pSWATGDB)
                .Add("OutGDB", pOutGDB)
                .Add("ParmChangesTextfile", pParmChangesTextfile)
                .Add("SWATExe", pSWATExe)
            End With
            Dim lAsk As New frmArgs
            lContinue = lAsk.AskUser("User Specified Parameters", lUserParms)
            If lContinue Then
                With lUserParms
                    pStartYear = .ItemByKey("Start Year")
                    pNumYears = .ItemByKey("Number of Years")
                    pRunModel = .ItemByKey("Run Model")

                    pRefreshDB = .ItemByKey("RefreshDB")
                    pOutputSummarize = .ItemByKey("OutputSummarize")
                    pInputSummarize = .ItemByKey("InputSummarize")
                    pCropChangeSummarize = .ItemByKey("CropChangeSummarize")
                    pChangeCropAreas = .ItemByKey("ChangeCropAreas")
                    pScenario = .ItemByKey("Scenario")
                    pDrive = .ItemByKey("Drive")
                    pBaseFolder = .ItemByKey("BaseFolder")
                    pSWATGDB = .ItemByKey("SWATGDB")
                    pOutGDB = .ItemByKey("OutGDB")
                    pParmChangesTextfile = .ItemByKey("ParmChangesTextfile")
                    pSWATExe = .ItemByKey("SWATExe")
                End With
            End If
        End If

        If lContinue Then
            Dim lLogFileName As String = Logger.FileName

            pInputFolder = IO.Path.Combine(pBaseFolder, "Scenarios" & IO.Path.DirectorySeparatorChar & pScenario)
            pOutGDBFolder = IO.Path.Combine(pInputFolder, "TablesIn")
            pOutputFolder = IO.Path.Combine(pInputFolder, "TxtInOut")
            pReportsFolder = IO.Path.Combine(pInputFolder, "TablesOut")
            pLogsFolder = IO.Path.Combine(pInputFolder, "logs")

            ChDriveDir(pInputFolder)

            'log for swat runner
            Logger.StartToFile(IO.Path.Combine(pLogsFolder, "SWATRunner.log"), , , True)

            Dim lOutGDB As String = IO.Path.Combine(pOutGDBFolder, pOutGDB)
            If pRefreshDB OrElse Not IO.File.Exists(lOutGDB) Then 'copy the entire input parameter database for this new scenario
                If IO.File.Exists(lOutGDB) Then
                    Logger.Dbg("DeleteExisting " & lOutGDB)
                    IO.File.Delete(lOutGDB)
                End If
                IO.Directory.CreateDirectory(IO.Path.GetDirectoryName(lOutGDB))
                IO.File.Copy(IO.Path.Combine(pBaseFolder, pOutGDB), lOutGDB)
                Logger.Dbg("Copied " & lOutGDB & " from " & pBaseFolder)
            End If

            Logger.Dbg("InitializeSwatInput")
            Dim lSwatInput As New SwatInput(pSWATGDB, lOutGDB, pBaseFolder, pScenario)

            If pStartYear > 0 Then lSwatInput.UpdateInputDB("CIO", "OBJECTID", 1, "IYR", pStartYear)
            If pNumYears > 0 Then lSwatInput.UpdateInputDB("CIO", "OBJECTID", 1, "NBYR", pNumYears)
            lSwatInput.UpdateInputDB("CIO", "OBJECTID", 1, "IPRINT", 2)
            lSwatInput.CIO.PrintHru = False

            If pInputSummarize Then
                SummarizeInput(lSwatInput)
            End If

            Dim lTotalArea As Double = 0.0
            Dim lTotalAreaNotConverted As Double = 0.0
            Dim lTotalAreaConverted As Double = 0.0
            Dim lTotalAreaCornFut As Double = 0.0
            Dim lTotalAreaCornNow As Double = 0.0
            Dim lCropChangesSummaryFilename As String = IO.Path.Combine(pLogsFolder, "CropChanges.txt")
            Dim lCropChangesHruFilename As String = IO.Path.Combine(pLogsFolder, "CropHruChanges.txt")
            Dim lCropConversions As New CropConversions

            If pCropChangeSummarize Then
                SummarizeCropChange(lSwatInput, _
                                    lCropConversions, _
                                    lCropChangesSummaryFilename, _
                                    lCropChangesHruFilename, _
                                    lTotalArea, _
                                    lTotalAreaNotConverted, _
                                    lTotalAreaConverted, _
                                    lTotalAreaCornFut, _
                                    lTotalAreaCornNow)
            Else
                For Each lString As String In LinesInFile(lCropChangesSummaryFilename)
                    If lString.StartsWith("Total") Then
                        Dim lFields() As String = lString.Split(vbTab)
                        Double.TryParse(lFields(3), lTotalArea)
                        Double.TryParse(lFields(4), lTotalAreaCornNow)
                        Double.TryParse(lFields(5), lTotalAreaConverted)
                        Double.TryParse(lFields(6), lTotalAreaNotConverted)
                        Double.TryParse(lFields(7), lTotalAreaCornFut)
                    End If
                Next
            End If

            If pChangeCropAreas Then
                ChangeHRUfractions(lSwatInput, lCropConversions, lCropChangesHruFilename)
            End If

            If IO.File.Exists(pParmChangesTextfile) Then
                Logger.Dbg("SWATPreprocess-UpdateParametersAsRequested")
                For Each lString As String In LinesInFile(pParmChangesTextfile)
                    Dim lParms() As String = lString.Split(";")
                    lSwatInput.UpdateInputDB(lParms(0).Trim, lParms(1).Trim, lParms(2).Trim, lParms(3).Trim, lParms(4).Trim)
                Next
            End If

            If pRunModel Then
                lSwatInput.SaveAllTextInput()
                LaunchProgram(pSWATExe, pOutputFolder)
            End If

            If pOutputSummarize Then
                SummarizeOutputs()
            End If

            'back to basins log
            Logger.StartToFile(lLogFileName, True, False, True)
        End If
    End Sub

    Private Sub SummarizeInput(ByVal aSwatInput As SwatInput)
        Logger.Dbg("SWATSummarizeInput")
        Dim lUniqueLandUses As DataTable = aSwatInput.Hru.UniqueValues("LandUse")
        Dim lStreamWriter As New IO.StreamWriter(IO.Path.Combine(pLogsFolder, "LandUses.txt"))
        For Each lLandUse As DataRow In lUniqueLandUses.Rows
            lStreamWriter.WriteLine(lLandUse.Item(0).ToString)
        Next
        lStreamWriter.Close()

        Dim lLandUSeTable As DataTable = AggregateCrops(aSwatInput.SubBsn.TableWithArea("LandUse"))
        SaveFileString(IO.Path.Combine(pLogsFolder, "AreaLandUseReport.txt"), _
                       SWATArea.Report(lLandUSeTable))
        SaveFileString(IO.Path.Combine(pLogsFolder, "AreaSoilReport.txt"), _
                       SWATArea.Report(aSwatInput.SubBsn.TableWithArea("Soil")))
        SaveFileString(IO.Path.Combine(pLogsFolder, "AreaSlopeCodeReport.txt"), _
                       SWATArea.Report(aSwatInput.SubBsn.TableWithArea("Slope_Cd")))
    End Sub

    Private Sub SummarizeCropChange(ByVal aSwatInput As SwatInput, _
                                    ByVal aCropConversions As CropConversions, _
                                    ByVal aCropChangesSummaryFilename As String, _
                                    ByVal aCropChangesHruFilename As String, _
                                    ByRef aTotalArea As Double, _
                                    ByRef aTotalAreaNotConverted As Double, _
                                    ByRef aTotalAreaConverted As Double, _
                                    ByRef aTotalAreaCornFut As Double, _
                                    ByRef aTotalAreaCornNow As Double)

        aTotalArea = 0.0
        aTotalAreaNotConverted = 0.0
        aTotalAreaConverted = 0.0
        aTotalAreaCornFut = 0.0
        aTotalAreaCornNow = 0.0

        Dim lSummaryWriter As New IO.StreamWriter(aCropChangesSummaryFilename)
        lSummaryWriter.WriteLine("FrmCrp" & vbTab & "ToCrp" & vbTab _
                               & "FrcChg".PadLeft(12) & vbTab _
                               & "Area".PadLeft(12) & vbTab _
                               & "AreaNow".PadLeft(12) & vbTab _
                               & "AreaChange".PadLeft(12) & vbTab _
                               & "AreaSkip".PadLeft(12) & vbTab _
                               & "AreaFuture".PadLeft(12) & vbTab _
                               & "CntPot".PadLeft(8) & vbTab & "CntAct".PadLeft(8))

        Dim lHruWriter As New IO.StreamWriter(aCropChangesHruFilename)
        lHruWriter.WriteLine("FrmCrp" & vbTab & "ToCrp" & vbTab _
                           & "SubId" & vbTab & "Soil" & vbTab & "Slope" & vbTab _
                           & "FrcChg".PadLeft(12) & vbTab _
                           & "Area".PadLeft(12) & vbTab _
                           & "AreaNow".PadLeft(12) & vbTab _
                           & "AreaChange".PadLeft(12) & vbTab _
                           & "AreaSkip".PadLeft(12) & vbTab _
                           & "AreaFuture".PadLeft(12))

        Dim lTotalPotentialChangeCount As Integer = 0
        Dim lTotalActualChangeCount As Integer = 0
        Dim lUniqueLandUses As DataTable = aSwatInput.Hru.UniqueValues("LandUse")
        For Each lLandUse As DataRow In lUniqueLandUses.Rows
            Dim lLandUseName As String = lLandUse.Item(0).ToString
            Logger.Dbg("Process " & lLandUseName)
            Dim lLandUSeConvertsTo As String = ""
            Dim lPotentialChangedHrus As DataTable = aSwatInput.QueryInputDB("Select * FROM(hru) WHERE LANDUSE='" & lLandUseName & "';")
            Dim lConvertFractionNet As Double = 0
            Dim lCornFractionAfter As Double = 0
            Dim lCornFractionBefore As Double = 0
            If aCropConversions.Contains(lLandUseName) Then
                Dim lCornConversion As CropConversion = aCropConversions.Item(lLandUseName)
                Dim lCornConvertTo As CropConversion = aCropConversions.Item(lCornConversion.NameConvertsTo)
                If lCornConvertTo.Fraction > lCornConversion.Fraction Then
                    lConvertFractionNet = lCornConvertTo.Fraction - lCornConversion.Fraction
                End If
                lLandUSeConvertsTo = lCornConversion.NameConvertsTo
                lCornFractionAfter = lCornConvertTo.Fraction
                lCornFractionBefore = lCornConversion.Fraction
            End If
            Dim lChangedHruCount As Integer = 0
            Dim lCropArea As Double = 0.0
            Dim lCropAreaNotConverted As Double = 0.0
            Dim lCropAreaConverted As Double = 0.0
            Dim lCropAreaCornNow As Double = 0.0
            Dim lCropAreaCornFut As Double = 0.0
            For Each lPotentialChangedHru As DataRow In lPotentialChangedHrus.Rows
                Dim lHruItem As New SwatInput.clsHruItem(lPotentialChangedHru)
                With lHruItem
                    Dim lSubBasinArea As Double = aSwatInput.QueryInputDB("Select SUB_KM FROM(sub) WHERE SUBBASIN=" & .SUBBASIN & ";").Rows(0).Item(0)
                    Dim lHruChangeTo As DataTable = aSwatInput.QueryInputDB("Select * FROM(hru) WHERE LANDUSE='" & lLandUSeConvertsTo & "' AND SOIL='" & .SOIL & "' AND SLOPE_CD='" & .SLOPE_CD & "' AND SUBBASIN=" & .SUBBASIN & ";")
                    Dim lHruArea As Double = lSubBasinArea * .HRU_FR
                    Dim lHruAreaPotentialConvert As Double = lHruArea * lConvertFractionNet
                    Dim lHruAreaNotConverted As Double = 0.0
                    Dim lHruAreaConverted As Double = 0.0
                    Dim lHruAreaCornNow As Double = lHruArea * lCornFractionBefore
                    Dim lHruAreaCornFut As Double = 0.0
                    If lHruChangeTo.Rows.Count > 0 Then
                        lHruAreaConverted = lHruAreaPotentialConvert
                        lChangedHruCount += 1
                        lHruAreaCornFut = lCornFractionAfter * lHruArea
                    Else 'no conversion
                        lHruAreaNotConverted = lHruAreaPotentialConvert
                        lHruAreaCornFut = lCornFractionBefore * lHruArea
                    End If
                    lHruWriter.WriteLine(lLandUseName & vbTab & lLandUSeConvertsTo & vbTab _
                                       & .SUBBASIN & vbTab & .SOIL & vbTab & .SLOPE_CD & vbTab _
                                       & DoubleToString(lConvertFractionNet, 12, pFormat, , , 10).PadLeft(12) & vbTab _
                                       & DoubleToString(lHruArea, 12, pFormat, , , 10).PadLeft(12) & vbTab _
                                       & DoubleToString(lHruAreaCornNow, 12, pFormat, , , 10).PadLeft(12) & vbTab _
                                       & DoubleToString(lHruAreaConverted, 12, pFormat, , , 10).PadLeft(12) & vbTab _
                                       & DoubleToString(lHruAreaNotConverted, 12, pFormat, , , 10).PadLeft(12) & vbTab _
                                       & DoubleToString(lHruAreaCornFut, 12, pFormat, , , 10).PadLeft(12))
                    lCropArea += lHruArea
                    lCropAreaConverted += lHruAreaConverted
                    lCropAreaNotConverted += lHruAreaNotConverted
                    lCropAreaCornNow += lHruAreaCornNow
                    lCropAreaCornFut += lHruAreaCornFut
                End With
            Next
            lSummaryWriter.WriteLine(lLandUseName & vbTab & lLandUSeConvertsTo & vbTab _
                                   & DoubleToString(lConvertFractionNet, 12, pFormat, , , 10).PadLeft(12) & vbTab _
                                   & DoubleToString(lCropArea, 12, pFormat, , , 10).PadLeft(12) & vbTab _
                                   & DoubleToString(lCropAreaCornNow, 12, pFormat, , , 10).PadLeft(12) & vbTab _
                                   & DoubleToString(lCropAreaConverted, 12, pFormat, , , 10).PadLeft(12) & vbTab _
                                   & DoubleToString(lCropAreaNotConverted, 12, pFormat, , , 10).PadLeft(12) & vbTab _
                                   & DoubleToString(lCropAreaCornFut, 12, pFormat, , , 10).PadLeft(12) & vbTab _
                                   & lPotentialChangedHrus.Rows.Count.ToString.PadLeft(8) & vbTab _
                                   & lChangedHruCount.ToString.PadLeft(8))
            aTotalArea += lCropArea
            aTotalAreaConverted += lCropAreaConverted
            aTotalAreaNotConverted += lCropAreaNotConverted
            aTotalAreaCornNow += lCropAreaCornNow
            aTotalAreaCornFut += lCropAreaCornFut
            lTotalPotentialChangeCount += lPotentialChangedHrus.Rows.Count
            lTotalActualChangeCount += lChangedHruCount
            lHruWriter.Flush()
        Next
        lHruWriter.Close()
        lSummaryWriter.WriteLine()
        lSummaryWriter.WriteLine("Total" & vbTab & Space(6) & vbTab & Space(12) & vbTab _
                               & DoubleToString(aTotalArea, 12, pFormat, , , 10).PadLeft(12) & vbTab _
                               & DoubleToString(aTotalAreaCornNow, 12, pFormat, , , 10).PadLeft(12) & vbTab _
                               & DoubleToString(aTotalAreaConverted, 12, pFormat, , , 10).PadLeft(12) & vbTab _
                               & DoubleToString(aTotalAreaNotConverted, 12, pFormat, , , 10).PadLeft(12) & vbTab _
                               & DoubleToString(aTotalAreaCornFut, 12, pFormat, , , 10).PadLeft(12) & vbTab _
                               & lTotalPotentialChangeCount.ToString.PadLeft(8) & vbTab _
                               & lTotalActualChangeCount.ToString.PadLeft(8))
        lSummaryWriter.Flush()
        lSummaryWriter.Close()
        Logger.Dbg("AreaTotal " & aTotalArea & " Converted " & aTotalAreaConverted & " NotTotal " & aTotalAreaNotConverted & " CornTotal " & aTotalAreaCornFut)
    End Sub

    Private Sub ChangeHRUfractions(ByVal aSwatInput As SwatInput, _
                                   ByVal aCornConversions As CropConversions, _
                                   ByVal aHruChangesFilename As String)
        Dim lConvertFractionOfAvailable As Double = 0.5 'TODO: compute from desired acres of corn vs. lTotalAreaCornFut or lTotalAreaConverted
        Dim lAreaChange As Double
        Dim lFractionOfSubbasinToChange As Double
        Dim lNumChangesMade As Integer = 0
        For Each lString As String In LinesInFile(aHruChangesFilename)
            Dim lFields() As String = lString.Split(vbTab)
            If Double.TryParse(lFields(8), lAreaChange) AndAlso lAreaChange > 0 Then
                Dim lLandUseName As String = lFields(0)
                Dim lCornConversion As CropConversion = aCornConversions.Item(lLandUseName)
                Dim lHruToChangeFrom As DataTable = aSwatInput.QueryInputDB("Select * FROM(hru) WHERE LANDUSE='" & lLandUseName & "' AND SOIL='" & lFields(3) & "' AND SLOPE_CD='" & lFields(4) & "' AND SUBBASIN=" & lFields(2) & ";")
                Dim lHruChangeTo As DataTable = aSwatInput.QueryInputDB("Select * FROM(hru) WHERE LANDUSE='" & lCornConversion.NameConvertsTo & "' AND SOIL='" & lFields(3) & "' AND SLOPE_CD='" & lFields(4) & "' AND SUBBASIN=" & lFields(2) & ";")
                If lHruToChangeFrom.Rows.Count > 0 AndAlso lHruChangeTo.Rows.Count > 0 Then
                    With lHruToChangeFrom.Rows(0) 'remove fraction of this land use
                        lFractionOfSubbasinToChange = .Item("HRU_FR") * lConvertFractionOfAvailable
                        aSwatInput.UpdateInputDB("hru", "OID", .Item(0), "HRU_FR", .Item("HRU_FR") - lFractionOfSubbasinToChange)
                    End With
                    With lHruChangeTo.Rows(0) 'add fraction of this land use
                        aSwatInput.UpdateInputDB("hru", "OID", .Item(0), "HRU_FR", .Item("HRU_FR") + lFractionOfSubbasinToChange)
                    End With
                    lNumChangesMade += 1
                End If
            End If
        Next
        Logger.Dbg("Moved fraction of subbasin from " & lNumChangesMade & " HRUs.")
    End Sub

    Private Sub SummarizeOutputs()
        MkDirPath(IO.Path.GetFullPath(pReportsFolder))
        Dim lOutputRch As New atcTimeseriesSWAT.atcTimeseriesSWAT
        With lOutputRch
            .Open(IO.Path.Combine(pOutputFolder, "output.rch"))
            Logger.Dbg("OutputRchTimserCount " & .DataSets.Count)
            WriteDatasets(IO.Path.Combine(pReportsFolder, "rch"), .DataSets)
        End With

        Dim lOutputSub As New atcTimeseriesSWAT.atcTimeseriesSWAT
        With lOutputSub
            .Open(IO.Path.Combine(pOutputFolder, "output.sub"))
            Logger.Dbg("OutputSubTimserCount " & .DataSets.Count)
            WriteDatasets(IO.Path.Combine(pReportsFolder, "sub"), .DataSets)
        End With

        Dim lOutputFields As New atcData.atcDataAttributes
        'TODO:add nutrient fields
        'AREAkm2  PRECIPmm SNOFALLmm SNOMELTmm     IRRmm     PETmm      ETmm SW_INITmm  SW_ENDmm    PERCmm GW_RCHGmm DA_RCHGmm   REVAPmm  SA_IRRmm  DA_IRRmm   SA_STmm   DA_STmmSURQ_GENmmSURQ_CNTmm   TLOSSmm    LATQmm    GW_Qmm    WYLDmm   DAILYCN 
        'TMP_AVdgC TMP_MXdgC TMP_MNdgCSOL_TMPdgCSOLARMJ/m2  
        'SYLDt/ha  USLEt/ha
        'N_APPkg/haP_APPkg/haNAUTOkg/haPAUTOkg/ha NGRZkg/ha PGRZkg/haNCFRTkg/haPCFRTkg/haNRAINkg/ha NFIXkg/ha F-MNkg/ha A-MNkg/ha A-SNkg/ha F-MPkg/haAO-LPkg/ha L-APkg/ha A-SPkg/ha 
        'DNITkg/ha  NUPkg/ha  PUPkg/ha ORGNkg/ha ORGPkg/ha SEDPkg/ha
        'NSURQkg/haNLATQkg/ha NO3Lkg/haNO3GWkg/ha SOLPkg/ha P_GWkg/ha    W_STRS  TMP_STRS    N_STRS    P_STRS  
        'BIOMt/ha       LAI   YLDt/ha   BACTPct  BACTLPct
        lOutputFields.SetValue("FieldName", "AREAkm2;YLDt/ha")
        Dim lOutputHru As New atcTimeseriesSWAT.atcTimeseriesSWAT
        With lOutputHru
            '.Open(IO.Path.Combine(pOutputFolder, "tab.hru"), lOutputFields)
            .Open(IO.Path.Combine(pOutputFolder, "output.hru"), lOutputFields)
            Logger.Dbg("OutputHruTimserCount " & .DataSets.Count)
            WriteDatasets(IO.Path.Combine(pReportsFolder, "hru"), .DataSets)
        End With

        Dim lSubBasin2HUC8 As New atcCollection
        Dim lSubBasin2HUC8Table As New atcTableDelimited
        With lSubBasin2HUC8Table
            .Delimiter = ","
            .NumHeaderRows = 0
            .OpenFile(IO.Path.Combine(pBaseFolder, "flowfig.csv"))
            For lRowIndex As Integer = 1 To .NumRecords
                lSubBasin2HUC8.Add(.Value(5), .Value(7))
                .MoveNext()
            Next
            .Clear()
        End With

        Logger.Dbg("SwatSummaryReports")
        'TODO be sure appropriate attributes are written
        Dim lDisplayAttributesSave As ArrayList = atcDataManager.DisplayAttributes.Clone
        Dim lDisplayAttributes As String() = {"Location", "Mean", "Max", "Min", "Sum", "Constituent"}
        With atcDataManager.DisplayAttributes
            .Clear()
            .AddRange(lDisplayAttributes)
        End With

        Dim lSubTimseriesGroup As New atcDataSourceTimeseriesBinary
        lSubTimseriesGroup.Open(IO.Path.Combine(pReportsFolder, "sub.tsbin"))
        WriteSubSummary(pReportsFolder, lSubTimseriesGroup.DataSets, lSubBasin2HUC8)
        Logger.Dbg("Report Load Done")

        Dim lRchTimseriesGroup As New atcDataSourceTimeseriesBinary
        lRchTimseriesGroup.Open(IO.Path.Combine(pReportsFolder, "rch.tsbin"))
        Dim lReachOutputFileName As String = IO.Path.Combine(pReportsFolder, "ReachSummary.txt")
        WriteReachSummary(pReportsFolder, lRchTimseriesGroup.DataSets, lSubBasin2HUC8, lReachOutputFileName)
        Logger.Dbg("Report Reach Done")
        Dim lReachOutputTable As New atcTableDelimited
        With lReachOutputTable
            .Delimiter = vbTab
            .OpenFile(lReachOutputFileName)
        End With

        With atcDataManager.DisplayAttributes
            .Clear()
            .AddRange(lDisplayAttributesSave)
        End With

        Dim lHruTimseriesGroup As New atcDataSourceTimeseriesBinary
        lHruTimseriesGroup.Open(IO.Path.Combine(pReportsFolder, "hru.tsbin"))
        WriteYieldSummary(pReportsFolder, lHruTimseriesGroup.DataSets)

        Logger.Dbg("SwatPostProcessingDone")
    End Sub

    Private Sub WriteDatasets(ByVal aFileName As String, ByVal aDatasets As atcDataGroup)
        Dim lDataTarget As New atcDataSourceTimeseriesBinary ' atcDataSourceWDM
        Dim lFileName As String = aFileName & ".tsbin" 'lDataTarget.Filter.?) Then
        TryDelete(lFileName)
        If lDataTarget.Open(lFileName) Then
            lDataTarget.AddDatasets(aDatasets)
        End If
    End Sub

    Private Sub WriteSubSummary(ByVal aOutputFolder As String, ByVal aTimeseriesGroup As atcDataGroup, ByVal aSubBasin2HUC8 As atcCollection)
        Dim lConsNitr As String() = {"ORGN", "NSURQ", "LATNO3", "GWNO3"}
        Dim lConsPhos As String() = {"ORGP", "SOLP", "SEDP"}
        Dim lConsOtherOut As String() = {"AREA", "WYLD", "SYLD"}
        Dim lConsToOutput As New ArrayList
        With lConsToOutput
            .AddRange(lConsNitr)
            .AddRange(lConsPhos)
            .AddRange(lConsOtherOut)
        End With

        Dim lSBHuc8 As New Text.StringBuilder
        lSBHuc8.AppendLine("SubID" & vbTab & "HUC8" & vbTab & "LocalArea".PadLeft(12) _
                                                    & vbTab & "N_Unit_Load".PadLeft(12) & vbTab & "N_HUC_Load".PadLeft(12) _
                                                    & vbTab & "P_Unit_Load".PadLeft(12) & vbTab & "P_HUC_Load".PadLeft(12))
        lSBHuc8.AppendLine(vbTab & vbTab & "km2".PadLeft(12) _
                                 & vbTab & "kg/ha".PadLeft(12) & vbTab & "kg".PadLeft(12) _
                                 & vbTab & "kg/ha".PadLeft(12) & vbTab & "kg".PadLeft(12))

        For lIndex As Integer = 1 To aSubBasin2HUC8.Count
            Dim lHuc8 As String = aSubBasin2HUC8.ItemByKey(lIndex.ToString)
            lSBHuc8.Append(lIndex & vbTab & lHuc8)
            Dim lSubData As atcDataGroup = aTimeseriesGroup.FindData("Location", "BIGSUB" & lIndex.ToString.PadLeft(4))
            Dim lSubDataToList As New atcDataGroup
            Dim lTimserNitr As New atcDataGroup
            Dim lTimserPhos As New atcDataGroup
            Dim lHucAreaFactor As Double = 0.0

            For Each lDataSet As atcTimeseries In lSubData
                Dim lCons As String = lDataSet.Attributes.GetValue("Constituent")
                If lConsToOutput.Contains(lCons) Then
                    lSubDataToList.Add(lDataSet)
                    If lCons = "AREA" Then
                        Dim lHucArea As Double = lDataSet.Attributes.GetDefinedValue("Mean").Value
                        lSBHuc8.Append(vbTab & DecimalAlign(lHucArea))
                        lHucAreaFactor = lHucArea * 100 'km -> ha
                    End If
                    If Array.IndexOf(lConsNitr, lCons) > -1 Then
                        lTimserNitr.Add(lDataSet)
                    End If
                    If Array.IndexOf(lConsPhos, lCons) > -1 Then
                        lTimserPhos.Add(lDataSet)
                    End If
                End If
            Next

            Dim lTimserNitrUnitLoad As atcTimeseries = Compute("Add", lTimserNitr)
            lTimserNitrUnitLoad.Attributes.SetValue("Constituent", "UnitN_Load")
            lSubDataToList.Add(lTimserNitrUnitLoad)
            lSBHuc8.Append(vbTab & DecimalAlign(lTimserNitrUnitLoad.Attributes.GetDefinedValue("Mean").Value))
            Dim lTimserNitrHucLoad As atcTimeseries = Compute("Multiply", lTimserNitrUnitLoad, lHucAreaFactor)
            lTimserNitrHucLoad.Attributes.SetValue("Constituent", "HucN_Load")
            lSubDataToList.Add(lTimserNitrHucLoad)
            lSBHuc8.Append(vbTab & DecimalAlign(lTimserNitrHucLoad.Attributes.GetDefinedValue("Mean").Value))

            Dim lTimserPhosUnitLoad As atcTimeseries = Compute("Add", lTimserPhos)
            lTimserPhosUnitLoad.Attributes.SetValue("Constituent", "UnitP_Load")
            lSubDataToList.Add(lTimserPhosUnitLoad)
            lSBHuc8.Append(vbTab & DecimalAlign(lTimserPhosUnitLoad.Attributes.GetDefinedValue("Mean").Value))
            Dim lTimserPhosHucLoad As atcTimeseries = Compute("Multiply", lTimserPhosUnitLoad, lHucAreaFactor)
            lTimserPhosHucLoad.Attributes.SetValue("Constituent", "HucP_Load")
            lSubDataToList.Add(lTimserPhosHucLoad)
            lSBHuc8.Append(vbTab & DecimalAlign(lTimserPhosHucLoad.Attributes.GetDefinedValue("Mean").Value))
            lSBHuc8.AppendLine()

            Dim lOutputFilenameHuc As String = lHuc8 & "_" & lIndex & "_Sub.txt"
            Dim lList As New atcListPlugin
            'TODO: just output year
            lList.Save(lSubDataToList, IO.Path.Combine(aOutputFolder, lOutputFilenameHuc))
        Next
        SaveFileString(IO.Path.Combine(aOutputFolder, "SubBasinSummary.txt"), lSBHuc8.ToString)
    End Sub

    Private Sub WriteReachSummary(ByVal aOutputFolder As String, ByVal aTimeseriesGroup As atcDataGroup, ByVal aSubBasin2HUC8 As atcCollection, ByVal aOutputFileName As String)
        Dim lConsNitrOut As String() = {"NH4_OUT", "NO2_OUT", "NO3_OUT", "ORGN_OUT"}
        Dim lConsNitrIn As String() = {"NH4_IN", "NO2_IN", "NO3_IN", "ORGN_IN"}
        Dim lConsPhosOut As String() = {"ORGP_OUT", "MINP_OUT"}
        Dim lConsPhosIn As String() = {"ORGP_IN", "MINP_IN"}
        Dim lConsOtherOut As String() = {"AREA", "FLOW_OUT", "SED_OUT"}
        Dim lConsToOutput As New ArrayList
        With lConsToOutput
            .AddRange(lConsNitrOut)
            .AddRange(lConsNitrIn)
            .AddRange(lConsPhosOut)
            .AddRange(lConsPhosIn)
            .AddRange(lConsOtherOut)
        End With

        Dim lSBHuc8 As New Text.StringBuilder
        lSBHuc8.AppendLine("SubID" & vbTab & "HUC8" & vbTab & "CumArea".PadLeft(12) _
                                                    & vbTab & "N_Total_In".PadLeft(16) & vbTab & "N_Total_Out".PadLeft(16) _
                                                    & vbTab & "P_Total_In".PadLeft(16) & vbTab & "P_Total_Out".PadLeft(16))
        lSBHuc8.AppendLine(vbTab & vbTab & "km2".PadLeft(12) _
                                 & vbTab & "kg".PadLeft(16) & vbTab & "kg".PadLeft(16) _
                                 & vbTab & "kg".PadLeft(16) & vbTab & "kg".PadLeft(16))

        For lIndex As Integer = 1 To aSubBasin2HUC8.Count
            Dim lHuc8 As String = aSubBasin2HUC8.ItemByKey(lIndex.ToString)
            lSBHuc8.Append(lIndex & vbTab & lHuc8)
            Dim lReachData As atcDataGroup = aTimeseriesGroup.FindData("Location", "REACH" & lIndex.ToString.PadLeft(5))
            Dim lReachDataToList As New atcDataGroup
            Dim lTimserNitrOut As New atcDataGroup
            Dim lTimserNitrIn As New atcDataGroup
            Dim lTimserPhosOut As New atcDataGroup
            Dim lTimserPhosIn As New atcDataGroup
            Dim lAreaFactor As Double = 1.0

            For Each lDataSet As atcTimeseries In lReachData
                Dim lCons As String = lDataSet.Attributes.GetValue("Constituent")
                If lConsToOutput.Contains(lCons) Then
                    lReachDataToList.Add(lDataSet)
                    If lCons = "AREA" Then
                        Dim lHucCumArea As Double = lDataSet.Attributes.GetDefinedValue("Mean").Value
                        lSBHuc8.Append(vbTab & DecimalAlign(lHucCumArea))
                        lAreaFactor = lHucCumArea * 100 'km -> ha
                    End If
                    If Array.IndexOf(lConsNitrOut, lCons) > -1 Then
                        lTimserNitrOut.Add(lDataSet)
                    End If
                    If Array.IndexOf(lConsNitrIn, lCons) > -1 Then
                        lTimserNitrIn.Add(lDataSet)
                    End If
                    If Array.IndexOf(lConsPhosOut, lCons) > -1 Then
                        lTimserPhosOut.Add(lDataSet)
                    End If
                    If Array.IndexOf(lConsPhosIn, lCons) > -1 Then
                        lTimserPhosIn.Add(lDataSet)
                    End If
                End If
            Next

            Dim lTimserNitrTotalIn As atcTimeseries = Compute("Add", lTimserNitrIn)
            lTimserNitrTotalIn.Attributes.SetValue("Constituent", "TotalN_In")
            lReachDataToList.Add(lTimserNitrTotalIn)
            lSBHuc8.Append(vbTab & DecimalAlign(lTimserNitrTotalIn.Attributes.GetDefinedValue("Mean").Value, 16))
            Dim lTimserNitrTotalOut As atcTimeseries = Compute("Add", lTimserNitrOut)
            lTimserNitrTotalOut.Attributes.SetValue("Constituent", "TotalN_Out")
            lReachDataToList.Add(lTimserNitrTotalOut)
            lSBHuc8.Append(vbTab & DecimalAlign(lTimserNitrTotalOut.Attributes.GetDefinedValue("Mean").Value, 16))

            Dim lTimserPhosTotalIn As atcTimeseries = Compute("Add", lTimserPhosIn)
            lTimserPhosTotalIn.Attributes.SetValue("Constituent", "TotalP_In")
            lReachDataToList.Add(lTimserPhosTotalIn)
            lSBHuc8.Append(vbTab & DecimalAlign(lTimserPhosTotalIn.Attributes.GetDefinedValue("Mean").Value, 16))
            Dim lTimserPhosTotalOut As atcTimeseries = Compute("Add", lTimserPhosOut)
            lTimserPhosTotalOut.Attributes.SetValue("Constituent", "TotalP_Out")
            lReachDataToList.Add(lTimserPhosTotalOut)
            lSBHuc8.Append(vbTab & DecimalAlign(lTimserPhosTotalOut.Attributes.GetDefinedValue("Mean").Value, 16))
            lSBHuc8.AppendLine()

            Dim lOutputFilenameHuc As String = lHuc8 & "_" & lIndex & ".txt"
            Dim lList As New atcListPlugin
            'TODO: just output year
            lList.Save(lReachDataToList, IO.Path.Combine(aOutputFolder, lOutputFilenameHuc))
        Next
        SaveFileString(aOutputFileName, lSBHuc8.ToString)
    End Sub

    Private Sub WriteYieldSummary(ByVal aOutputFolder As String, ByVal aTimeseriesGroup As atcDataGroup)
        Dim lCropIds As New atcCollection
        With lCropIds
            .Add("CORN") : .Add("CCCC") : .Add("CSC1") : .Add("CSS1") : .Add("CCS1")
        End With

        Dim lTab As String = vbTab
        Dim lFieldWidth As Integer = 12
        Dim lSigDigits As Integer = 8
        Dim lArea As Double = 0.0
        Dim lUnitYield As Double = 0.0

        Dim lSBAreaDebug As New Text.StringBuilder
        lSBAreaDebug.AppendLine("SubId" & lTab & _
                                "HruId" & lTab & _
                                "Crop" & lTab & _
                                "Area".PadLeft(lFieldWidth) & lTab & _
                                "Fraction".PadLeft(lFieldWidth))
        Dim lSBDebug As New Text.StringBuilder
        lSBDebug.AppendLine("SubId" & lTab & _
                            "Crop" & lTab & _
                            "Year" & lTab & _
                            "Area".PadLeft(lFieldWidth) & lTab & _
                            "UnitYield".PadLeft(lFieldWidth) & lTab & _
                            "Yield".PadLeft(lFieldWidth))
        Dim lSBAnnual As New Text.StringBuilder
        lSBAnnual.AppendLine("SubId" & lTab & _
                             "Year" & lTab & _
                             "Area".PadLeft(lFieldWidth) & lTab & _
                             "CornArea".PadLeft(lFieldWidth) & lTab & _
                             "%".PadLeft(lFieldWidth) & lTab & _
                             "UnitYield".PadLeft(lFieldWidth) & lTab & _
                             "Yield".PadLeft(lFieldWidth))
        Dim lSBAverage As New Text.StringBuilder
        lSBAverage.AppendLine("SubId" & lTab & _
                              "Area".PadLeft(lFieldWidth) & lTab & _
                              "CornArea".PadLeft(lFieldWidth) & lTab & _
                              "%".PadLeft(lFieldWidth) & lTab & _
                              "UnitYield".PadLeft(lFieldWidth) & lTab & _
                              "Yield".PadLeft(lFieldWidth))
        Dim lSBTotal As New Text.StringBuilder
        lSBTotal.AppendLine("Area".PadLeft(lFieldWidth) & lTab & _
                            "CornArea".PadLeft(lFieldWidth) & lTab & _
                            "%".PadLeft(lFieldWidth) & lTab & _
                            "UnitYield".PadLeft(lFieldWidth) & lTab & _
                            "Yield".PadLeft(lFieldWidth))

        Dim lAreaGroup As atcDataGroup = aTimeseriesGroup.FindData("Constituent", "AREA")
        Dim lSubIds As atcCollection = lAreaGroup.SortedAttributeValues("SubId")
        Dim lSubIdAreas As New atcCollection
        For Each lSubId As String In lSubIds
            Dim lAreaSubIdTotal As Double = 0.0
            Dim lSubIdDataGroup As atcDataGroup = lAreaGroup.FindData("SubId", lSubId)
            Dim lHruIds As atcCollection = lSubIdDataGroup.SortedAttributeValues("HruId")
            Dim lAreaStrings As New atcCollection
            For Each lHruId As String In lHruIds
                Dim lHruIdDataGroup As atcDataGroup = lSubIdDataGroup.FindData("HruId", lHruId)
                Dim lAreaUsed As Boolean = False
                For Each lAreaTimeseries As atcTimeseries In lHruIdDataGroup
                    lArea = lAreaTimeseries.Value(1)
                    If Double.IsNaN(lArea) Then
                        'skip
                    ElseIf Not lAreaUsed Then
                        lAreaStrings.Add(lHruId, lSubId & lTab & _
                                                 lAreaTimeseries.Attributes.GetValue("HruId") & lTab & _
                                                 lAreaTimeseries.Attributes.GetValue("CropId") & lTab & _
                                                 DecimalAlign(lArea, , , lSigDigits))
                        lAreaSubIdTotal += lArea
                        lAreaUsed = True
                    Else
                        Logger.Dbg("Problem " & lHruId)
                    End If
                Next
            Next
            For Each lAreaString As String In lAreaStrings
                lArea = lAreaString.Substring(lAreaString.LastIndexOf(lTab))
                lSBAreaDebug.AppendLine(lAreaString & lTab & DecimalAlign(lArea / lAreaSubIdTotal, , 10, lSigDigits))
            Next
            lSubIdAreas.Add(lSubId, lAreaSubIdTotal)
        Next
        SaveFileString(IO.Path.Combine(aOutputFolder, "Area.txt"), lSBAreaDebug.ToString)

        Dim lMatchingDataGroup As atcDataGroup = aTimeseriesGroup.FindData("CropId", lCropIds)
        Dim lTimserBase As atcTimeseries = lMatchingDataGroup.Item(0)
        Dim lDateBase(5) As Integer
        J2Date(lTimserBase.Dates.Value(0), lDateBase)
        Dim lNumValues As Integer = lTimserBase.numValues

        Dim lAreaAllTotal As Double = 0.0
        Dim lAreaTotal As Double = 0.0
        Dim lYieldTotal As Double = 0.0
        For Each lSubId As String In lSubIds
            Dim lSubIdDataGroup As atcDataGroup = lMatchingDataGroup.FindData("SubId", lSubId)
            Dim lLocationIdsInSub As atcCollection = lSubIdDataGroup.SortedAttributeValues("Location")
            Dim lYieldSum As Double = 0.0
            Dim lAreaSum As Double = 0.0
            Dim lYear As Integer = lDateBase(0)
            Dim lSubIdArea As Double = lSubIdAreas.ItemByKey(lSubId)
            lAreaAllTotal += lSubIdArea
            For lYearIndex As Integer = 1 To lNumValues
                Dim lAreaSub As Double = 0
                Dim lYieldSub As Double = 0
                For Each lLocationId As String In lLocationIdsInSub
                    Dim lLocationIdDataGroup As atcDataGroup = lSubIdDataGroup.FindData("Location", lLocationId)
                    If lLocationIdDataGroup.Count = 2 Then
                        Dim lAreaTimser As atcTimeseries = lLocationIdDataGroup.FindData("Constituent", "Area").Item(0)
                        Dim lYieldTimser As atcTimeseries = lLocationIdDataGroup.FindData("Constituent", "Yld").Item(0)

                        If lYearIndex <= lAreaTimser.numValues Then
                            lArea = lAreaTimser.Value(lYearIndex)
                            lUnitYield = lYieldTimser.Value(lYearIndex)
                        Else
                            lArea = Double.NaN
                            lUnitYield = Double.NaN
                        End If
                        Dim lYield As Double = lUnitYield * lArea
                        lSBDebug.AppendLine(lSubId.Trim & lTab & _
                                            lLocationId & lTab & _
                                            lYear & lTab & _
                                            DecimalAlign(lArea) & lTab & _
                                            DecimalAlign(lUnitYield) & lTab & _
                                            DecimalAlign(lYield))
                        If Not Double.IsNaN(lArea) Then
                            lAreaSub += lArea
                            lYieldSub += lYield
                        End If
                    Else
                        Logger.Dbg("Problem:" & lLocationIdDataGroup.Count)
                    End If
                Next
                lSBAnnual.AppendLine(lSubId.Trim & lTab & _
                                     lYear & lTab & _
                                     DecimalAlign(lSubIdArea) & lTab & _
                                     DecimalAlign(lAreaSub) & lTab & _
                                     DecimalAlign(100 * lAreaSub / lSubIdArea, , 1) & lTab & _
                                     DecimalAlign(lYieldSub / lAreaSub) & lTab & _
                                     DecimalAlign(lYieldSub))
                lYieldSum += lYieldSub
                lAreaSum += lAreaSub
                lYear += 1
            Next
            Dim lAreaAvg As Double = lAreaSum / lNumValues
            Dim lYieldAvg As Double = lYieldSum / lNumValues
            lSBAverage.AppendLine(lSubId.Trim & lTab & _
                                  DecimalAlign(lSubIdArea) & lTab & _
                                  DecimalAlign(lAreaAvg) & lTab & _
                                  DecimalAlign(100 * lAreaAvg / lSubIdArea, , 1) & lTab & _
                                  DecimalAlign(lYieldAvg / lAreaAvg) & lTab & _
                                  DecimalAlign(lYieldAvg))
            lAreaTotal += lAreaAvg
            lYieldTotal += lYieldAvg
        Next
        lSBTotal.AppendLine(DecimalAlign(lAreaAllTotal) & lTab & _
                            DecimalAlign(lAreaTotal) & lTab & _
                            DecimalAlign(100 * lAreaTotal / lAreaAllTotal, , 1) & lTab & _
                            DecimalAlign(lYieldTotal / lAreaTotal) & lTab & _
                            DecimalAlign(lYieldTotal))
        SaveFileString(IO.Path.Combine(aOutputFolder, "Debug.txt"), lSBDebug.ToString)
        SaveFileString(IO.Path.Combine(aOutputFolder, "Annual.txt"), lSBAnnual.ToString)
        SaveFileString(IO.Path.Combine(aOutputFolder, "Average.txt"), lSBAverage.ToString)
        SaveFileString(IO.Path.Combine(aOutputFolder, "Total.txt"), lSBTotal.ToString)
    End Sub
End Module

Module SWATArea
    Public Function Report(ByVal aReportTable As DataTable) As String

        With aReportTable
            Dim lAreaTotals(.Columns.Count) As Double
            Dim lSb As New Text.StringBuilder
            Dim lStr As String = ""
            For lColumnIndex As Integer = 0 To .Columns.Count - 1
                lStr &= .Columns(lColumnIndex).ColumnName.PadLeft(12) & vbTab
            Next
            lSb.AppendLine(lStr.TrimEnd(vbTab))
            For lRowIndex As Integer = 0 To .Rows.Count - 1
                Dim lReportRow As DataRow = .Rows(lRowIndex)
                With lReportRow
                    lStr = .Item(0).ToString.PadLeft(12) & vbTab
                    For lColumnIndex As Integer = 1 To .ItemArray.GetUpperBound(0)
                        lStr &= DoubleToString(.Item(lColumnIndex), 12, pFormat, , , 10).PadLeft(12) & vbTab
                        lAreaTotals(lColumnIndex) += .Item(lColumnIndex)
                    Next
                End With
                lSb.AppendLine(lStr.TrimEnd(vbTab))
            Next
            lStr = "Totals".PadLeft(12) & vbTab
            For lColumnIndex As Integer = 1 To .Columns.Count - 1
                lStr &= DoubleToString(lAreaTotals(lColumnIndex), 12, pFormat, , , 10).PadLeft(12) & vbTab
            Next
            lSb.AppendLine(lStr.TrimEnd(vbTab))
            Logger.Dbg("AreaTotalReportComplete " & lAreaTotals(1))
            Return lSb.ToString
        End With
    End Function

    Public Function AggregateCrops(ByVal aInputTable As DataTable) As DataTable
        Dim lCornConversions As New CropConversions
        Dim lArea As Double = 0.0

        Dim lOutputTable As DataTable = aInputTable.Copy
        Dim lCornColumnIndex As Integer = lOutputTable.Columns.Count
        lOutputTable.Columns.Add("CORN")
        Dim lSoybColumnIndex As Integer = lOutputTable.Columns.Count
        lOutputTable.Columns.Add("SOYB")

        For Each lRow As DataRow In lOutputTable.Rows
            lRow(lCornColumnIndex) = 0.0
            lRow(lSoybColumnIndex) = 0.0
            For lColumnIndex As Integer = 2 To lOutputTable.Columns.Count - 2
                Dim lColumnName As String = lOutputTable.Columns(lColumnIndex).ColumnName
                If lCornConversions.Contains(lColumnName) Then
                    Dim lCornConversion As CropConversion = lCornConversions.Item(lColumnName)
                    lArea = lRow(lColumnIndex)
                    lRow(lCornColumnIndex) += lArea * lCornConversion.Fraction
                    lRow(lSoybColumnIndex) += lArea * (1 - lCornConversion.Fraction)
                End If
            Next
        Next
        Return lOutputTable
    End Function

    Friend Class CropConversions
        Inherits KeyedCollection(Of String, CropConversion)
        Protected Overrides Function GetKeyForItem(ByVal aParm As CropConversion) As String
            Return aParm.Name
        End Function

        Public Sub New()
            Me.Add(New CropConversion("AGRR", 0.0, "CRP"))
            Me.Add(New CropConversion("ALFA", 0.0, "CRP"))
            Me.Add(New CropConversion("HAY", 0.0, "CRP"))
            Me.Add(New CropConversion("PAST", 0.0, "CRP"))
            Me.Add(New CropConversion("RNGE", 0.0, "CRP"))
            Me.Add(New CropConversion("CRP", 1.0, "CRP"))
            'Me.Add(New CornConversion("CCCC", 1.0, "CCCC"))
            ''Me.Add(New CornConversion("CCS1", 0.66667, "CCCC"))
            'Me.Add(New CornConversion("CSC1", 0.5, "CCCC")) 'TODO: check
            ''Me.Add(New CornConversion("CSS1", 0.33333, "CCCC"))
            ''Me.Add(New CornConversion("SCC1", 0.66667, "CCCC"))
            'Me.Add(New CornConversion("SCS1", 0.5, "CCCC"))  'TODO: check
            ''Me.Add(New CornConversion("SSC1", 0.33333, "CCCC"))
            ''Me.Add(New CornConversion("SSSC", 0.0, "CCCC"))
            'Me.Add(New CornConversion("AGRR", 0.0, "CCCC"))
            ''Me.Add(New CornConversion("CRP", 0.0, "CCCC"))
            'Me.Add(New CornConversion("HAY", 0.0, "CCCC"))
        End Sub
    End Class

    Friend Class CropConversion
        Public Name As String
        Public Fraction As Double
        Public NameConvertsTo As String

        Public Sub New(ByVal aName As String, ByVal aFraction As Double, ByVal aNameConvertsTo As String)
            Name = aName
            Fraction = aFraction
            NameConvertsTo = aNameConvertsTo
        End Sub
    End Class
End Module
