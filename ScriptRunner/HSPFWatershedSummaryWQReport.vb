Imports System
Imports atcUtility
Imports atcData
Imports atcWDM
Imports atcHspfBinOut
Imports MapWinUtility
Imports atcGraph
Imports ZedGraph

Imports MapWindow.Interfaces
Imports System.Collections.Specialized

Module HSPFWatershedSummaryWQReport 
    Private pTestPath As String
    Private pBaseName As String
    Private pDrive As String = "D:" 'C: in CA
    Private pSummaryType As String

    Private Sub Initialize()
        Dim lTestName As String = "upatoi"
        'Dim lTestName As String = "addtrail"
        Select Case lTestName
            Case "upatoi"
                pTestPath = pDrive & "\Basins\modelout\Upatoi"
                pBaseName = "upatoi"
            Case "addtrail"
                pTestPath = pDrive & "\Basins\modelout\UpatoiScen"
                pBaseName = "addtrail"
        End Select

        pSummaryType = "Sediment"
        'pSummaryType = "Water"
        'pSummaryType = "BOD"
        'pSummaryType = "DO"
        'pSummaryType = "FColi"
        'pSummaryType = "Lead"
        'pSummaryType = "NH3"
        'pSummaryType = "NH4"
        'pSummaryType = "NO3"
        'pSummaryType = "OrganicN"
        'pSummaryType = "OrganicP"
        'pSummaryType = "PO4"
        'pSummaryType = "TotalN"
        'pSummaryType = "TotalP"
        'pSummaryType = "WaterTemp"
        'pSummaryType = "Zinc"
    End Sub

    Public Sub ScriptMain(ByRef aMapWin As IMapWin)
        Initialize()
        ChDriveDir(pTestPath)

        'open uci file
        Dim lMsg As New atcUCI.HspfMsg("hspfmsg.mdb")
        Dim lHspfUci As New atcUCI.HspfUci
        lHspfUci.FastReadUciForStarter(lMsg, pBaseName & ".uci")

        'open HBN file
        Dim lHspfBinFileName As String = pTestPath & "\" & pBaseName & ".hbn"
        Dim lHspfBinDataSource As New atcTimeseriesFileHspfBinOut()
        lHspfBinDataSource.Open(lHspfBinFileName)
        Dim lHspfBinFileInfo As System.IO.FileInfo = New System.IO.FileInfo(lHspfBinFileName)

        'constituent balance
        Dim lOperationTypes As New atcCollection
        lOperationTypes.Add("P:", "PERLND")
        lOperationTypes.Add("I:", "IMPLND")
        lOperationTypes.Add("R:", "RCHRES")
        Dim lLocations As atcCollection = lHspfBinDataSource.DataSets.SortedAttributeValues("Location")

        Dim lString As Text.StringBuilder = HspfSupport.ConstituentBalance.Report _
                (lHspfUci, pSummaryType, lOperationTypes, pBaseName, _
                 lHspfBinDataSource, lLocations, lHspfBinFileInfo.LastWriteTime)
        Dim lOutFileName As String = "outfiles\" & pSummaryType & "_" & "ConstituentBalance.txt"
        SaveFileString(lOutFileName, lString.ToString)

        'watershed summary
        lString = HspfSupport.WatershedSummary.Report(lHspfUci, lHspfBinDataSource, lHspfBinFileInfo.LastWriteTime, pSummaryType)
        lOutFileName = "outfiles\" & pSummaryType & "_" & "WatershedSummary.txt"
        SaveFileString(lOutFileName, lString.ToString)
        lString = Nothing
    End Sub
End Module
