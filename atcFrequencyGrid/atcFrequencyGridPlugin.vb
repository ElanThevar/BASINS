Imports atcUtility
Imports atcData

Public Class atcFrequencyGridPlugin
    Inherits atcData.atcDataDisplay

    Public Overrides ReadOnly Property Name() As String
        Get
            Return "Analysis::Frequency Grid"
        End Get
    End Property

    Public Overrides Function Show(ByVal aDataManager As atcData.atcDataManager, _
                     Optional ByVal aDataGroup As atcData.atcDataGroup = Nothing) As Object
        Dim lDataGroup As atcDataGroup = aDataGroup
        If lDataGroup Is Nothing Then
            lDataGroup = New atcDataGroup
        End If

        Dim lForm As New frmDisplayFrequencyGrid(aDataManager, lDataGroup)
        If Not (lDataGroup Is Nothing) AndAlso lDataGroup.Count > 0 AndAlso Not lForm.IsDisposed Then
            lForm.Show()
            Return lForm
        Else
            Return Nothing
        End If
    End Function

    Public Overrides Sub Save(ByVal aDataManager As atcDataManager, _
                              ByVal aDataGroup As atcDataGroup, _
                              ByVal aFileName As String, _
                              ByVal ParamArray aOptions() As String)

        Dim lForm As New frmDisplayFrequencyGrid(aDataManager, aDataGroup)

        For Each lOption As String In aOptions
            Select Case lOption
                Case "SwapRowsColumns"
                    lForm.SwapRowsColumns = True
                Case "Low"
                    lForm.HighDisplay = False
                Case "High"
                    lForm.HighDisplay = True
            End Select
        Next

        SaveFileString(aFileName, lForm.ToString)
    End Sub

End Class
