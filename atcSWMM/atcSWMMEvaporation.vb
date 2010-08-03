﻿Imports System.Collections.ObjectModel
Imports System.IO
Imports MapWinUtility
Imports atcUtility
Imports System.Text
Imports System.Text.RegularExpressions

Public Class atcSWMMEvaporation
    Implements IBlock

    Private pName As String = "[EVAPORATION]"
    Private pSWMMProject As atcSWMMProject
    Public Timeseries As atcData.atcTimeseries

    Property Name() As String Implements IBlock.Name
        Get
            Return pName
        End Get
        Set(ByVal value As String)
            pName = value
        End Set
    End Property

    Public Sub New(ByVal aSWMMProject As atcSWMMProject)
        pSWMMProject = aSWMMProject
    End Sub

    Public Sub New(ByVal aSWMMPRoject As atcSWMMProject, ByVal aContents As String)
        pSWMMProject = aSWMMPRoject
        FromString(aContents)
    End Sub

    Public Sub FromString(ByVal aContents As String) Implements IBlock.FromString
        Timeseries = New atcData.atcTimeseries(pSWMMProject)
        Timeseries.Attributes.SetValue("Location", "")
        Timeseries.Attributes.SetValue("Constituent", "PEVT")
        Timeseries.ValuesNeedToBeRead = True

        'TODO: populate Timeseries
        'Need to do a delayed action here

        'Break it up into multiple lines
        Dim lLines() As String = aContents.Split(vbCrLf)
        Dim lWord As String = "TIMESERIES"
        Dim laTSFile As String = String.Empty
        For I As Integer = 0 To lLines.Length - 1
            If Not lLines(I).Trim().StartsWith(";") Then
                laTSFile = lLines(I).Trim().Substring(lWord.Length).Trim()
                'Assuming there is only one TS for Evap
                If laTSFile.Length > 0 Then
                    Timeseries.Attributes.SetValue("Scenario", laTSFile)
                    Timeseries.Attributes.SetValue("Location", pSWMMProject.FilterFileName(laTSFile.TrimEnd("E")))
                    Exit For
                End If
            End If
        Next
    End Sub

    Public Overrides Function ToString() As String
        Dim lSB As New StringBuilder
        If Timeseries IsNot Nothing Then
            lSB.Append(pName & vbCrLf & _
                       ";;Type       Parameters" & vbCrLf & _
                       ";;---------- ----------" & vbCrLf)

            lSB.Append(StrPad("TIMESERIES", 12, " ", False))
            lSB.Append(" ")
            lSB.Append(StrPad(Timeseries.Attributes.GetValue("Location") & ":E", 10, " ", False))
            lSB.Append(" ")
            lSB.Append(vbCrLf)
        End If
        Return lSB.ToString
    End Function

    Public Shared Function TimeSeriesHeaderToString() As String
        Return "[TIMESERIES]" & vbCrLf & _
               ";;Name           Date       Time       Value     " & vbCrLf & _
               ";;-------------- ---------- ---------- ----------"
    End Function

    Public Sub TimeSeriesToStream(ByVal aSW As IO.StreamWriter)
        If Timeseries IsNot Nothing Then
            aSW.Write(";EVAPORATION" & vbCrLf)
            pSWMMProject.TimeSeriesToStream(Timeseries, Timeseries.Attributes.GetValue("Location") & ":E", aSW)
        End If
    End Sub

    Public Function TimeSeriesFileNamesToString() As String
        Dim lSB As New StringBuilder

        Dim lFileName As String = ""
        If Timeseries IsNot Nothing Then
            lFileName = PathNameOnly(Me.pSWMMProject.FileName) & g_PathChar & Timeseries.Attributes.GetValue("Location") & "E.DAT"
            lSB.Append(StrPad(Timeseries.Attributes.GetValue("Location") & ":E", 16, " ", False))
            lSB.AppendLine(" FILE " & lFileName)
        End If

        Return lSB.ToString
    End Function

    Public Function TimeSeriesToFile() As Boolean
        If Timeseries IsNot Nothing Then
            Dim lFileName As String = PathNameOnly(Me.pSWMMProject.FileName) & g_PathChar & Timeseries.Attributes.GetValue("Location") & "E.DAT"
            Dim lSB As New StringBuilder
            lSB.Append(Me.pSWMMProject.TimeSeriesToString(Timeseries, Timeseries.Attributes.GetValue("Location") & ":E", "PEVT"))
            SaveFileString(lFileName, lSB.ToString)
            Return True
        Else
            Return False
        End If
    End Function

    Public Sub TimeSeriesFromFile(ByVal aFilename As String, ByVal aTS As atcData.atcTimeseries)
        If Not aTS.ValuesNeedToBeRead Then
            Exit Sub
        End If
        Dim lStn As String = aTS.Attributes.GetValue("Location")
        Dim lDates As New List(Of Double)
        Dim lValues As New List(Of Double)

        'Set up common date array

        Dim lSR As System.IO.StreamReader = New System.IO.StreamReader(aFilename)
        While Not lSR.EndOfStream
            Dim line As String = lSR.ReadLine()
            Dim lItems() As String = Regex.Split(line.Trim(), "\s+")
            Dim lDateParts() As String = lItems(0).Split("/")
            Dim lTimeParts() As String = lItems(1).Split(":")

            'If lItems(0) <> lStn Then
            '    Continue While
            'End If
            'SWMM5 denote a day has 0 hour to 23 hour, but atcTimeseries denote a day as 1 ~ 24 hour
            Dim ldate As Double = Jday(Integer.Parse(lDateParts(2)), Integer.Parse(lDateParts(0)), Integer.Parse(lDateParts(1)), Integer.Parse(lTimeParts(0)) + 1, Integer.Parse(lTimeParts(1)), 0)
            lDates.Add(ldate)
            lValues.Add(Double.Parse(lItems(lItems.Length - 1)))
        End While

        aTS.ValuesNeedToBeRead = False

        Dim lDates1 As New atcData.atcTimeseries(Nothing)
        lDates1.numValues = lDates.Count
        lDates1.Values = lDates.ToArray()

        aTS.numValues = lDates.Count
        aTS.Dates = lDates1
        aTS.Values = lValues.ToArray
        lSR.Close()
    End Sub

End Class

