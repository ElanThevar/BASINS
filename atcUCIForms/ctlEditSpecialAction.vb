Imports System.Drawing
Imports MapWinUtility
Imports atcUCI
Imports atcControls
Imports System.Collections.ObjectModel

Public Class ctlEditSpecialAction
    Implements ctlEdit

    Dim pSpecialActionBlk As HspfSpecialActionBlk
    Dim pChanged As Boolean
    Dim PreviousTab As Integer = 0
    Public Event Change(ByVal aChange As Boolean) Implements ctlEdit.Change
    Public ReadOnly Property Caption() As String Implements ctlEdit.Caption
        Get
            Return "Edit Special Actions Block"
        End Get
    End Property

    Public Property Changed() As Boolean Implements ctlEdit.Changed
        Get
            Return pChanged
        End Get
        Set(ByVal aChanged As Boolean)
            If aChanged <> pChanged Then
                pChanged = aChanged
                RaiseEvent Change(aChanged)
            End If
        End Set
    End Property
    Private Sub Display()
        DisplayRecords()
        DisplayCounts()
    End Sub
    Private Sub DisplayCounts()
        Dim ac, dc, unc, uqc, cc, i As Integer

        ac = 0
        dc = 0
        unc = 0
        uqc = 0
        cc = 0

        With atcgrid0
            For i = 1 To .Source.Rows - 1   'top header row was being counted so subtracted 1
                Select Case .Source.CellValue(i, 0)
                    Case "Action" : ac += 1
                    Case "Distribute" : dc += 1
                    Case "User Defn Name" : unc += 1
                    Case "User Defn Quan" : uqc += 1
                    Case "Condition" : cc += 1
                End Select
            Next
            lblcounts.Text = "Records: " & (.Source.Rows - 1) & ", Actions: " & (ac) & ", Distributes: " & (dc) & ", User Define Names: " & (unc) & ", User Define Quans: " & (uqc) & ", Conditions: " & (cc)
        End With
    End Sub
    Private Sub DisplayRecords()

        Dim lRecordType As String
        With pSpecialActionBlk.Records
            For lIndex As Integer = 1 To .Count
                lRecordType = pSpecialActionBlk.HspfSpecialRecordName(.Item(lIndex).SpecType)
                atcgrid0.Source.CellValue(lIndex, 0) = lRecordType
                atcgrid0.Source.CellValue(lIndex, 1) = .Item(lIndex).Text
            Next
        End With

        atcgrid0.SizeAllColumnsToContents()
        atcgrid0.Refresh()

    End Sub

    Private Sub PutRecsToFrontTab(ByVal itab As Integer)
        Dim rowcount, i, j As Integer
        Dim newText, ctemp As String

        If itab = 1 Then
            'action type records
            rowcount = 0
            With atcgrid1.Source
                For i = 1 To atcgrid0.Source.Rows
                    If atcgrid0.Source.CellValue(i, 0) = "Action" Then
                        'get next record from this tab
                        rowcount = rowcount + 1
                        newText = "   "
                        newText = BlankPad(newText & .CellValue(rowcount, 0), 8)
                        ctemp = "   "
                        RSet(ctemp, Len(.CellValue(rowcount, 1)))
                        newText = newText & ctemp
                        If Len(.CellValue(rowcount, 2)) = 0 Then
                            newText = newText & "    "
                        ElseIf .CellValue(rowcount, 2) = 0 Then
                            newText = newText & "    "
                        Else
                            ctemp = "   "
                            RSet(ctemp, Len(.CellValue(rowcount, 2)))
                            newText = newText & ctemp
                        End If
                        newText = BlankPad(newText & .CellValue(rowcount, 3), 17)
                        If Len(.CellValue(rowcount, 4)) = 0 Then
                            newText = newText & "   "
                        ElseIf .CellValue(rowcount, 4) = 0 Then
                            newText = newText & "   "
                        Else
                            ctemp = "   "
                            RSet(ctemp, Len(.CellValue(rowcount, 4)))
                            newText = newText & ctemp
                        End If
                        If Len(.CellValue(rowcount, 5)) = 0 Then
                            newText = newText & "    "
                        ElseIf .CellValue(rowcount, 5) = 0 Then
                            newText = newText & "    "
                        Else
                            newText = BlankPad(newText & .CellValue(rowcount, 5), 24)
                        End If
                        If Len(.CellValue(rowcount, 6)) = 0 Then
                            newText = newText & "   "
                        ElseIf .CellValue(rowcount, 6) = 0 Then
                            newText = newText & "   "
                        Else
                            ctemp = "   "
                            RSet(ctemp, Len(.CellValue(rowcount, 6)))
                            newText = newText & ctemp
                        End If
                        If Len(.CellValue(rowcount, 7)) = 0 Then
                            newText = newText & "   "
                        ElseIf .CellValue(rowcount, 7) = 0 Then
                            newText = newText & "   "
                        Else
                            ctemp = "   "
                            RSet(ctemp, Len(.CellValue(rowcount, 7)))
                            newText = newText & ctemp
                        End If
                        If Len(.CellValue(rowcount, 8)) = 0 Then
                            newText = newText & "   "
                        ElseIf .CellValue(rowcount, 8) = 0 Then
                            newText = newText & "   "
                        Else
                            ctemp = "   "
                            RSet(ctemp, Len(.CellValue(rowcount, 8)))
                            newText = newText & ctemp
                        End If
                        If Len(.CellValue(rowcount, 9)) = 0 Then
                            newText = newText & "   "
                        ElseIf .CellValue(rowcount, 9) = 0 Then
                            newText = newText & "   "
                        Else
                            ctemp = "   "
                            RSet(ctemp, Len(.CellValue(rowcount, 9)))
                            newText = newText & ctemp
                        End If
                        If Len(.CellValue(rowcount, 10)) = 0 Then
                            newText = newText & "  "
                        ElseIf .CellValue(rowcount, 10) = 0 Then
                            newText = newText & "  "
                        Else
                            ctemp = "  "
                            RSet(ctemp, Len(.CellValue(rowcount, 10)))
                            newText = newText & ctemp
                        End If
                        ctemp = "  "
                        RSet(ctemp, Len(.CellValue(rowcount, 11)))
                        newText = newText & ctemp & "  "
                        If IsNumeric(.CellValue(rowcount, 12)) Then
                            newText = BlankPad(newText & .CellValue(rowcount, 12), 57) 'addr
                        Else
                            newText = BlankPad(newText & .CellValue(rowcount, 12), 48) 'vname
                            newText = BlankPad(newText & .CellValue(rowcount, 13), 51)
                            newText = BlankPad(newText & .CellValue(rowcount, 14), 54)
                            newText = BlankPad(newText & .CellValue(rowcount, 15), 57)
                        End If
                        newText = BlankPad(newText & .CellValue(rowcount, 16), 60)
                        If IsNumeric(.CellValue(rowcount, 17)) Then
                            ctemp = "          "
                            RSet(ctemp, Len(.CellValue(rowcount, 17)))
                            newText = newText & ctemp 'value
                        Else
                            newText = BlankPad(newText & .CellValue(rowcount, 17), 70) 'quan
                        End If
                        newText = newText & " "
                        newText = BlankPad(newText & .CellValue(rowcount, 18), 73)
                        If .CellValue(rowcount, 19) = 0 Then
                            newText = newText & "    "
                        Else
                            ctemp = "    "
                            RSet(ctemp, Len(.CellValue(rowcount, 19)))
                            newText = newText & ctemp
                        End If
                        If .CellValue(rowcount, 20) = 0 Then
                            newText = newText & "   "
                        Else
                            ctemp = "   "
                            RSet(ctemp, Len(.CellValue(rowcount, 20)))
                            newText = newText & ctemp
                        End If
                        atcgrid0.Source.CellValue(i, 1) = newText
                    End If
                Next i
            End With
        ElseIf itab = 2 Then
            'distribute records
            rowcount = 0
            With atcgrid2.Source
                For i = 1 To .Rows
                    If atcgrid0.Source.CellValue(i, 0) = "Distribute" Then
                        'get next record from this tab
                        rowcount = rowcount + 1
                        newText = "  DISTRB"
                        ctemp = "   "
                        RSet(ctemp, Len(.CellValue(rowcount, 0)))
                        newText = newText & ctemp
                        ctemp = "    "
                        RSet(ctemp, Len(.CellValue(rowcount, 1)))
                        newText = newText & ctemp & " "
                        newText = BlankPad(newText & .CellValue(rowcount, 2), 18)
                        ctemp = "    "
                        RSet(ctemp, Len(.CellValue(rowcount, 3)))
                        newText = newText & ctemp & " "
                        newText = BlankPad(newText & .CellValue(rowcount, 4), 30)
                        For j = 1 To 10
                            ctemp = "     "
                            RSet(ctemp, Len(.CellValue(rowcount, 4 + j)))
                            newText = newText & ctemp
                        Next j
                        atcgrid0.Source.CellValue(i, 1) = newText
                    End If
                Next i
            End With
        ElseIf itab = 3 Then
            'User Defn Name records
            rowcount = 0
            With atcgrid3.Source
                For i = 1 To atcgrid0.Source.Rows
                    If atcgrid0.Source.CellValue(i, 0) = "User Defn Name" Then
                        'get next record from this tab
                        rowcount = rowcount + 1
                        newText = "  UVNAME  "
                        newText = BlankPad(newText & .CellValue(rowcount, 0), 16)
                        ctemp = "   "
                        RSet(ctemp, Len(.CellValue(rowcount, 1)))
                        newText = newText & ctemp & " "
                        If IsNumeric(.CellValue(rowcount, 2)) Then
                            newText = BlankPad(newText & .CellValue(rowcount, 2), 35) 'addr
                        Else
                            newText = BlankPad(newText & .CellValue(rowcount, 2), 26) 'vname
                            newText = BlankPad(newText & .CellValue(rowcount, 3), 29)
                            newText = BlankPad(newText & .CellValue(rowcount, 4), 32)
                            newText = BlankPad(newText & .CellValue(rowcount, 5), 35)
                        End If
                        newText = newText & " "
                        ctemp = "     "
                        RSet(ctemp, Len(.CellValue(rowcount, 6)))
                        newText = newText & ctemp & " "
                        newText = BlankPad(newText & .CellValue(rowcount, 7), 46)
                        newText = newText & "    "
                        If IsNumeric(.CellValue(rowcount, 8)) Then
                            newText = BlankPad(newText & .CellValue(rowcount, 8), 65) 'addr
                        Else
                            newText = BlankPad(newText & .CellValue(rowcount, 8), 56) 'vname
                            newText = BlankPad(newText & .CellValue(rowcount, 9), 59)
                            newText = BlankPad(newText & .CellValue(rowcount, 10), 62)
                            newText = BlankPad(newText & .CellValue(rowcount, 11), 65)
                        End If
                        newText = newText & " "
                        ctemp = "     "
                        RSet(ctemp, Len(.CellValue(rowcount, 12)))
                        newText = newText & ctemp & " "
                        newText = BlankPad(newText & .CellValue(rowcount, 13), 76)
                        atcgrid0.Source.CellValue(i, 1) = newText
                    End If
                Next i
            End With
        ElseIf itab = 4 Then
            'User Defn Quan records
            rowcount = 0
            With atcgrid4.Source
                For i = 1 To atcgrid0.Source.Rows
                    If atcgrid0.Source.CellValue(i, 0) = "User Defn Quan" Then
                        'get next record from this tab
                        rowcount = rowcount + 1
                        newText = "  UVQUAN "
                        newText = BlankPad(newText & .CellValue(rowcount, 0), 16)
                        newText = BlankPad(newText & .CellValue(rowcount, 1), 23)
                        ctemp = "   "
                        RSet(ctemp, Len(.CellValue(rowcount, 2)))
                        newText = newText & ctemp & " "
                        If IsNumeric(.CellValue(rowcount, 3)) Then
                            newText = BlankPad(newText & .CellValue(rowcount, 3), 42) 'addr
                        Else
                            newText = BlankPad(newText & .CellValue(rowcount, 3), 33) 'vname
                            newText = BlankPad(newText & .CellValue(rowcount, 4), 36)
                            newText = BlankPad(newText & .CellValue(rowcount, 5), 39)
                            newText = BlankPad(newText & .CellValue(rowcount, 6), 42)
                        End If
                        ctemp = "   "
                        RSet(ctemp, Len(.CellValue(rowcount, 7)))
                        newText = newText & ctemp
                        ctemp = "          "
                        RSet(ctemp, Len(.CellValue(rowcount, 8)))
                        newText = newText & ctemp & " "
                        newText = BlankPad(newText & .CellValue(rowcount, 9), 58)
                        ctemp = "   "
                        RSet(ctemp, Len(.CellValue(rowcount, 10)))
                        newText = newText & ctemp & " "
                        newText = BlankPad(newText & .CellValue(rowcount, 11), 64)
                        ctemp = "   "
                        RSet(ctemp, Len(.CellValue(rowcount, 12)))
                        newText = newText & ctemp & " "
                        newText = newText & .CellValue(rowcount, 13)
                        atcgrid0.Source.CellValue(i, 1) = newText
                    End If
                Next i
            End With
        ElseIf itab = 5 Then
            'conditional records
            rowcount = 0
            With atcgrid5.Source
                For i = 1 To atcgrid0.Source.Rows
                    If atcgrid0.Source.CellValue(i, 0) = "Condition" Then
                        'get next record from this tab
                        rowcount = rowcount + 1
                        atcgrid0.Source.CellValue(i, 1) = .CellValue(rowcount, 0)
                    End If
                Next i
            End With
        End If
    End Sub
    Private Function BlankPad(ByVal ctxt As String, ByVal ilen As Integer)
        'pad a string to be the desired length
        Dim i, j As Integer
        If Len(ctxt) > ilen Then
            BlankPad = Mid(ctxt, 1, ilen)
        ElseIf Len(ctxt) < ilen Then
            j = ilen - Len(ctxt)
            BlankPad = ctxt
            For i = 1 To j
                BlankPad = BlankPad & " "
            Next
        Else
            BlankPad = ctxt
        End If
    End Function

    Public Sub Add() Implements ctlEdit.Add

        Changed = True
    End Sub

    Public Sub Help() Implements ctlEdit.Help
        'TODO: add this code
    End Sub

    Public Sub Remove() Implements ctlEdit.Remove

    End Sub

    Public Property Data() As Object Implements ctlEdit.Data
        Get
            Return pSpecialActionBlk
        End Get


        Set(ByVal aHspfSpecialAction As Object)
            pSpecialActionBlk = aHspfSpecialAction
            atcgrid0.Source = New atcControls.atcGridSource
            With atcgrid0
                .Clear()
                .AllowHorizontalScrolling = False
                .AllowNewValidValues = True
                .Visible = True
            End With

            With atcgrid0.Source
                .CellValue(0, 0) = "Type"
                .CellValue(0, 1) = "Text"

                For lCol As Integer = 0 To 1
                    .CellColor(0, lCol) = SystemColors.ControlLight
                Next

            End With

            atcgrid0.SizeAllColumnsToContents()
            atcgrid0.Refresh()
            'action records

            atcgrid1.Source = New atcControls.atcGridSource
            With atcgrid1
                .Clear()
                .AllowHorizontalScrolling = False
                .AllowNewValidValues = True
                .Visible = True
            End With

            With atcgrid1.Source
                .CellValue(0, 0) = "OpTyp"
                .CellValue(0, 1) = "OpFst"
                .CellValue(0, 2) = "OpLst"
                .CellValue(0, 3) = "Dc"
                .CellValue(0, 4) = "Ds"
                .CellValue(0, 5) = "Yr"
                .CellValue(0, 6) = "Mo"
                .CellValue(0, 7) = "Dy"
                .CellValue(0, 8) = "Hr"
                .CellValue(0, 9) = "Mn"
                .CellValue(0, 10) = "DsInd"
                .CellValue(0, 11) = "Typ"
                .CellValue(0, 12) = "Vname/Addr"
                .CellValue(0, 13) = "Sub1"
                .CellValue(0, 14) = "Sub2"
                .CellValue(0, 15) = "Sub3"
                .CellValue(0, 16) = "ActCod"
                .CellValue(0, 17) = "Value/Uvquan"
                .CellValue(0, 18) = "Tc"
                .CellValue(0, 19) = "Ts"
                .CellValue(0, 20) = "Num"

                For lCol As Integer = 0 To 20
                    .CellColor(0, lCol) = SystemColors.ControlLight
                Next

            End With

            atcgrid1.SizeAllColumnsToContents()
            atcgrid1.Refresh()

            'distributes
            atcgrid2.Source = New atcControls.atcGridSource
            With atcgrid2
                .Clear()
                .AllowHorizontalScrolling = False
                .AllowNewValidValues = True
                .Visible = True
            End With

            With atcgrid2.Source
                .CellValue(0, 0) = "DSInd"
                .CellValue(0, 1) = "Count"
                .CellValue(0, 2) = "CTCode"
                .CellValue(0, 3) = "TStep"
                .CellValue(0, 4) = "DefFg"
                .CellValue(0, 5) = "Frac1"
                .CellValue(0, 6) = "Frac2"
                .CellValue(0, 7) = "Frac3"
                .CellValue(0, 8) = "Frac4"
                .CellValue(0, 9) = "Frac5"
                .CellValue(0, 10) = "Frac6"
                .CellValue(0, 11) = "Frac7"
                .CellValue(0, 12) = "Frac8"
                .CellValue(0, 13) = "Frac9"
                .CellValue(0, 14) = "Frac10"

                For lCol As Integer = 0 To 14
                    .CellColor(0, lCol) = SystemColors.ControlLight
                Next

            End With

            atcgrid2.SizeAllColumnsToContents()
            atcgrid2.Refresh()

            'uvnames

            atcgrid3.Source = New atcControls.atcGridSource
            With atcgrid3
                .Clear()
                .AllowHorizontalScrolling = False
                .AllowNewValidValues = True
                .Visible = True
            End With

            With atcgrid3.Source
                .CellValue(0, 0) = "UVName"
                .CellValue(0, 1) = "VCount"
                .CellValue(0, 2) = "VName/Addr"
                .CellValue(0, 3) = "Sub1"
                .CellValue(0, 4) = "Sub2"
                .CellValue(0, 5) = "Sub3"
                .CellValue(0, 6) = "Frac"
                .CellValue(0, 7) = "ActCd"
                .CellValue(0, 8) = "VName/Addr"
                .CellValue(0, 9) = "Sub1"
                .CellValue(0, 10) = "Sub2"
                .CellValue(0, 11) = "Sub3"
                .CellValue(0, 12) = "Frac"
                .CellValue(0, 13) = "ActCd"

                For lCol As Integer = 0 To 13
                    .CellColor(0, lCol) = SystemColors.ControlLight
                Next

            End With

            atcgrid3.SizeAllColumnsToContents()
            atcgrid3.Refresh()

            'User Defn Quan

            atcgrid4.Source = New atcControls.atcGridSource
            With atcgrid4
                .Clear()
                .AllowHorizontalScrolling = False
                .AllowNewValidValues = True
                .Visible = True
            End With

            With atcgrid4.Source
                .CellValue(0, 0) = "UVQNam"
                .CellValue(0, 1) = "OpTyp"
                .CellValue(0, 2) = "OpNo"
                .CellValue(0, 3) = "VName/Addr"
                .CellValue(0, 4) = "Sub1"
                .CellValue(0, 5) = "Sub2"
                .CellValue(0, 6) = "Sub3"
                .CellValue(0, 7) = "Typ"
                .CellValue(0, 8) = "Mult"
                .CellValue(0, 9) = "LagCode"
                .CellValue(0, 10) = "LagStep"
                .CellValue(0, 11) = "AgCode"
                .CellValue(0, 12) = "AgStep"
                .CellValue(0, 13) = "Tran"

                For lCol As Integer = 0 To 13
                    .CellColor(0, lCol) = SystemColors.ControlLight
                Next

            End With

            atcgrid4.SizeAllColumnsToContents()
            atcgrid4.Refresh()

            'Conditionals

            atcgrid5.Source = New atcControls.atcGridSource
            With atcgrid5
                .Clear()
                .AllowHorizontalScrolling = False
                .AllowNewValidValues = True
                .Visible = True
            End With

            With atcgrid5.Source
                .CellValue(0, 0) = "Text"
                .CellColor(0, 0) = SystemColors.ControlLight
            End With

            atcgrid5.Refresh()

            Display()
        End Set
    End Property

    Public Sub New(ByVal aHspfSpecialAction As Object, ByVal aParent As Windows.Forms.Form, ByVal aTag As String)

        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Data = aHspfSpecialAction
    End Sub

    Public Sub tabSpecial_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tabSpecial.Click
        Dim i As Integer
        Dim newText As String

        If tabSpecial.SelectedIndex <> PreviousTab And PreviousTab <> 0 Then
            'changed tab, put previous tab recs back to first tab
            PutRecsToFrontTab(PreviousTab)
            atcgrid0.Refresh()
        End If
        If tabSpecial.SelectedIndex <> PreviousTab Then
            'now load records for this tab
            If tabSpecial.SelectedIndex = 1 Then
                'action type records
                With atcgrid1.Source
                    .Columns = 21
                    .Rows = 1
                    For i = 1 To atcgrid0.Source.Rows
                        If atcgrid0.Source.CellValue(i, 0) = "Action" Then
                            .Rows = .Rows + 1
                            newText = Mid(atcgrid0.Source.CellValue(i, 1), 3)
                            .CellValue(.Rows - 1, 0) = Trim(Mid(newText, 1, 6))
                            .CellValue(.Rows - 1, 1) = Mid(newText, 7, 3)
                            If Mid(newText, 10, 4) = "    " Then
                                .CellValue(.Rows - 1, 2) = 0
                            Else
                                .CellValue(.Rows - 1, 2) = Mid(newText, 10, 4)
                            End If
                            .CellValue(.Rows - 1, 3) = Mid(newText, 14, 2)
                            If Mid(newText, 16, 3) = "   " Then
                                .CellValue(.Rows - 1, 4) = 0
                            Else
                                .CellValue(.Rows - 1, 4) = Mid(newText, 16, 3)
                            End If
                            If Mid(newText, 19, 4) = "    " Then
                                .CellValue(.Rows - 1, 5) = 0
                            Else
                                .CellValue(.Rows - 1, 5) = Mid(newText, 19, 4)
                            End If
                            If Mid(newText, 23, 3) = "   " Then
                                .CellValue(.Rows - 1, 6) = 0
                            Else
                                .CellValue(.Rows - 1, 6) = Mid(newText, 23, 3)
                            End If
                            If Mid(newText, 26, 3) = "   " Then
                                .CellValue(.Rows - 1, 7) = 0
                            Else
                                .CellValue(.Rows - 1, 7) = Mid(newText, 26, 3)
                            End If
                            If Mid(newText, 29, 3) = "   " Then
                                .CellValue(.Rows - 1, 8) = 0
                            Else
                                .CellValue(.Rows - 1, 8) = Mid(newText, 29, 3)
                            End If
                            If Mid(newText, 32, 3) = "   " Then
                                .CellValue(.Rows - 1, 9) = 0
                            Else
                                .CellValue(.Rows - 1, 9) = Mid(newText, 32, 3)
                            End If
                            If Mid(newText, 35, 2) = "  " Then
                                .CellValue(.Rows - 1, 10) = 0
                            Else
                                .CellValue(.Rows - 1, 10) = Mid(newText, 35, 2)
                            End If
                            .CellValue(.Rows - 1, 11) = Mid(newText, 37, 2)
                            'determine if vname or addr
                            If IsNumeric(Mid(newText, 41, 8)) Then
                                .CellValue(.Rows - 1, 12) = Mid(newText, 41, 8) 'addr
                                .CellValue(.Rows - 1, 13) = ""
                                .CellValue(.Rows - 1, 14) = ""
                                .CellValue(.Rows - 1, 15) = ""
                            Else
                                .CellValue(.Rows - 1, 12) = Mid(newText, 41, 6)
                                .CellValue(.Rows - 1, 13) = Mid(newText, 47, 3)
                                .CellValue(.Rows - 1, 14) = Mid(newText, 50, 3)
                                .CellValue(.Rows - 1, 15) = Mid(newText, 53, 3)
                            End If
                            .CellValue(.Rows - 1, 16) = Mid(newText, 56, 3)
                            'determine if value or uvquan
                            If IsNumeric(Mid(newText, 59, 10)) Then
                                .CellValue(.Rows - 1, 17) = Trim(Mid(newText, 59, 10)) 'value
                            Else
                                .CellValue(.Rows - 1, 17) = Mid(newText, 63, 6) 'quan
                            End If
                            .CellValue(.Rows - 1, 18) = Mid(newText, 70, 2)
                            If Len(Trim(Mid(newText, 73, 3))) = 0 Or Len(newText) < 73 Then
                                .CellValue(.Rows - 1, 19) = 0
                            Else
                                .CellValue(.Rows - 1, 19) = Mid(newText, 73, 3)
                            End If
                            If Mid(newText, 76, 3) = "   " Or Len(newText) < 76 Then
                                .CellValue(.Rows - 1, 20) = 0
                            Else
                                .CellValue(.Rows - 1, 20) = Mid(newText, 76, 3)
                            End If
                        End If
                    Next i

                    For j As Integer = 0 To .Columns - 1
                        For k As Integer = 1 To .Rows - 1
                            .CellEditable(k, j) = True
                        Next
                    Next

                    atcgrid1.SizeAllColumnsToContents()
                    atcgrid1.Refresh()
                End With
            ElseIf tabSpecial.SelectedIndex = 2 Then
                'distributes
                With atcgrid2.Source
                    .Columns = 15
                    .Rows = 1
                    For i = 1 To atcgrid0.Source.Rows
                        If atcgrid0.Source.CellValue(i, 0) = "Distribute" Then
                            .Rows = .Rows + 1
                            newText = Mid(atcgrid0.Source.CellValue(i, 1), 3)
                            .CellValue(.Rows - 1, 0) = Mid(newText, 7, 3)
                            .CellValue(.Rows - 1, 1) = Mid(newText, 11, 3)
                            .CellValue(.Rows - 1, 2) = Mid(newText, 15, 2)
                            .CellValue(.Rows - 1, 3) = Mid(newText, 18, 3)
                            .CellValue(.Rows - 1, 4) = Mid(newText, 22, 5)
                            .CellValue(.Rows - 1, 5) = Mid(newText, 29, 5)
                            .CellValue(.Rows - 1, 6) = Mid(newText, 34, 5)
                            .CellValue(.Rows - 1, 7) = Mid(newText, 39, 5)
                            .CellValue(.Rows - 1, 8) = Mid(newText, 44, 5)
                            .CellValue(.Rows - 1, 9) = Mid(newText, 49, 5)
                            .CellValue(.Rows - 1, 10) = Mid(newText, 54, 5)
                            .CellValue(.Rows - 1, 11) = Mid(newText, 59, 5)
                            .CellValue(.Rows - 1, 12) = Mid(newText, 64, 5)
                            .CellValue(.Rows - 1, 13) = Mid(newText, 69, 5)
                            .CellValue(.Rows - 1, 14) = Mid(newText, 74, 5)
                        End If
                    Next i
                    If .Rows > 1 Then
                        .Rows = .Rows - 1
                    End If
                    atcgrid2.SizeAllColumnsToContents()
                End With
            ElseIf tabSpecial.SelectedIndex = 3 Then
                'uvname
                With atcgrid3.Source
                    .Columns = 14
                    .Rows = 1
                    For i = 1 To atcgrid0.Source.Rows
                        If atcgrid0.Source.CellValue(i, 0) = "User Defn Name" Then
                            .Rows = .Rows + 1
                            newText = Mid(atcgrid0.Source.CellValue(i, 1), 3)
                            .CellValue(.Rows - 1, 0) = Mid(newText, 9, 6)
                            .CellValue(.Rows - 1, 1) = Mid(newText, 15, 3)
                            .CellValue(.Rows - 1, 2) = Mid(newText, 19, 6)
                            .CellValue(.Rows - 1, 3) = Mid(newText, 25, 3)
                            .CellValue(.Rows - 1, 4) = Mid(newText, 28, 3)
                            .CellValue(.Rows - 1, 5) = Mid(newText, 31, 3)
                            .CellValue(.Rows - 1, 6) = Mid(newText, 35, 5)
                            .CellValue(.Rows - 1, 7) = Mid(newText, 41, 4)
                            .CellValue(.Rows - 1, 8) = Mid(newText, 49, 6)
                            .CellValue(.Rows - 1, 9) = Mid(newText, 55, 3)
                            .CellValue(.Rows - 1, 10) = Mid(newText, 58, 3)
                            .CellValue(.Rows - 1, 11) = Mid(newText, 61, 3)
                            If Mid(newText, 65, 5) = "     " Then
                                .CellValue(.Rows - 1, 12) = 1
                            Else
                                .CellValue(.Rows - 1, 12) = Mid(newText, 65, 5)
                            End If
                            .CellValue(.Rows - 1, 13) = Mid(newText, 71, 4)
                        End If
                    Next i
                    atcgrid3.SizeAllColumnsToContents()
                End With
            ElseIf tabSpecial.SelectedIndex = 4 Then
                'User Defn Quan
                With atcgrid4.Source
                    .Columns = 14
                    .Rows = 1
                    For i = 1 To atcgrid0.Source.Rows
                        If atcgrid0.Source.CellValue(i, 0) = "User Defn Quan" Then
                            .Rows = .Rows + 1
                            newText = Mid(atcgrid0.Source.CellValue(i, 1), 3)
                            .CellValue(.Rows - 1, 0) = Mid(newText, 8, 6)
                            .CellValue(.Rows - 1, 1) = Mid(newText, 15, 6)
                            .CellValue(.Rows - 1, 2) = Mid(newText, 22, 3)
                            .CellValue(.Rows - 1, 3) = Mid(newText, 26, 6)
                            .CellValue(.Rows - 1, 4) = Mid(newText, 33, 3)
                            .CellValue(.Rows - 1, 5) = Mid(newText, 36, 3)
                            .CellValue(.Rows - 1, 6) = Mid(newText, 39, 3)
                            .CellValue(.Rows - 1, 7) = Mid(newText, 41, 3)
                            If Mid(newText, 44, 10) = "          " Then
                                .CellValue(.Rows - 1, 8) = 1.0#
                            Else
                                .CellValue(.Rows - 1, 8) = Mid(newText, 44, 10)
                            End If
                            .CellValue(.Rows - 1, 9) = Mid(newText, 55, 2)
                            If Mid(newText, 57, 3) = "   " Then
                                .CellValue(.Rows - 1, 10) = 1
                            Else
                                .CellValue(.Rows - 1, 10) = Mid(newText, 57, 3)
                            End If
                            .CellValue(.Rows - 1, 11) = Mid(newText, 61, 2)
                            If Mid(newText, 63, 3) = "   " Then
                                .CellValue(.Rows - 1, 12) = 1
                            Else
                                .CellValue(.Rows - 1, 12) = Mid(newText, 63, 3)
                            End If
                            .CellValue(.Rows - 1, 13) = Mid(newText, 67, 4)
                        End If
                    Next i
                    atcgrid4.SizeAllColumnsToContents()
                End With
            ElseIf tabSpecial.SelectedIndex = 5 Then
                'conditionals
                With atcgrid5.Source
                    .Columns = 1
                    .Rows = 2
                    For i = 1 To atcgrid0.Source.Rows
                        If atcgrid0.Source.CellValue(i, 0) = "Condition" Then
                            .CellValue(.Rows - 1, 0) = atcgrid0.Source.CellValue(i, 1)
                        End If
                    Next i
                    'atcgrid5.SizeAllColumnsToContents()
                    atcgrid5.Refresh()
                End With
            End If
        End If
        PreviousTab = tabSpecial.SelectedIndex
    End Sub

    Public Sub Save() Implements ctlEdit.Save
        Dim mySpecialRecord As HspfSpecialRecord
        Dim lOper As Integer

        PutRecsToFrontTab(tabSpecial.SelectedIndex)
        atcgrid0.Refresh()

        With pSpecialActionBlk.Records
            Do Until .Count = 0
                .Remove(1)
            Loop
            For lOper = 1 To atcgrid0.Source.Rows - 1
                mySpecialRecord = New HspfSpecialRecord
                mySpecialRecord.Text = atcgrid0.Source.CellValue(lOper, 1)
                If atcgrid0.Source.CellValue(lOper, 0) = "Comment" Then
                    mySpecialRecord.SpecType = 6
                ElseIf atcgrid0.Source.CellValue(lOper, 0) = "Condition" Then
                    mySpecialRecord.SpecType = 5
                ElseIf atcgrid0.Source.CellValue(lOper, 0) = "Distribute" Then
                    mySpecialRecord.SpecType = 2
                ElseIf atcgrid0.Source.CellValue(lOper, 0) = "User Defn Name" Then
                    mySpecialRecord.SpecType = 3
                ElseIf atcgrid0.Source.CellValue(lOper, 0) = "User Defn Quan" Then
                    mySpecialRecord.SpecType = 4
                Else
                    mySpecialRecord.SpecType = 1
                End If
                pSpecialActionBlk.Records.Add(mySpecialRecord)
            Next
        End With

    End Sub

End Class
