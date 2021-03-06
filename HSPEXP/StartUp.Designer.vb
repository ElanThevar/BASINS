﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class StartUp
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(StartUp))
        Me.cmdStart = New System.Windows.Forms.Button()
        Me.cmdBrowse = New System.Windows.Forms.Button()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.lblRCH = New System.Windows.Forms.Label()
        Me.txtRCH = New System.Windows.Forms.TextBox()
        Me.lblOutReach2 = New System.Windows.Forms.Label()
        Me.cmdEnd = New System.Windows.Forms.Button()
        Me.chkAreaReports = New System.Windows.Forms.CheckBox()
        Me.chkRunHSPF = New System.Windows.Forms.CheckBox()
        Me.pnlHighlight = New System.Windows.Forms.Panel()
        Me.chkAdditionalgraphs = New System.Windows.Forms.CheckBox()
        Me.btn_help = New System.Windows.Forms.Button()
        Me.cmbUCIPath = New System.Windows.Forms.ComboBox()
        Me.DateTimePicker1 = New System.Windows.Forms.DateTimePicker()
        Me.DateTimePicker2 = New System.Windows.Forms.DateTimePicker()
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Plotting = New System.Windows.Forms.GroupBox()
        Me.chkReganGraphs = New System.Windows.Forms.CheckBox()
        Me.ttHSPEXP = New System.Windows.Forms.ToolTip(Me.components)
        Me.chkExpertStats = New System.Windows.Forms.CheckBox()
        Me.chkMultiSim = New System.Windows.Forms.CheckBox()
        Me.chkTotalNitrogen = New System.Windows.Forms.CheckBox()
        Me.chkSedimentBalance = New System.Windows.Forms.CheckBox()
        Me.chkWaterBalance = New System.Windows.Forms.CheckBox()
        Me.chkTotalPhosphorus = New System.Windows.Forms.CheckBox()
        Me.chkBODBalance = New System.Windows.Forms.CheckBox()
        Me.chkGQUAL1 = New System.Windows.Forms.CheckBox()
        Me.chkDO = New System.Windows.Forms.CheckBox()
        Me.chkHeat = New System.Windows.Forms.CheckBox()
        Me.chkGQUAL2 = New System.Windows.Forms.CheckBox()
        Me.chkGQUAL3 = New System.Windows.Forms.CheckBox()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.chkGQUAL4 = New System.Windows.Forms.CheckBox()
        Me.chkGQUAL5 = New System.Windows.Forms.CheckBox()
        Me.chkGQUAL6 = New System.Windows.Forms.CheckBox()
        Me.chkGQUAL7 = New System.Windows.Forms.CheckBox()
        Me.GroupBox3.SuspendLayout()
        Me.Plotting.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.SuspendLayout()
        '
        'cmdStart
        '
        Me.cmdStart.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdStart.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.cmdStart.Enabled = False
        Me.cmdStart.Location = New System.Drawing.Point(326, 594)
        Me.cmdStart.Name = "cmdStart"
        Me.cmdStart.Size = New System.Drawing.Size(75, 23)
        Me.cmdStart.TabIndex = 20
        Me.cmdStart.Text = "Start"
        Me.cmdStart.UseVisualStyleBackColor = True
        '
        'cmdBrowse
        '
        Me.cmdBrowse.AllowDrop = True
        Me.cmdBrowse.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdBrowse.Location = New System.Drawing.Point(401, 52)
        Me.cmdBrowse.Name = "cmdBrowse"
        Me.cmdBrowse.Size = New System.Drawing.Size(75, 23)
        Me.cmdBrowse.TabIndex = 2
        Me.cmdBrowse.Text = "Browse"
        Me.cmdBrowse.UseVisualStyleBackColor = True
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(17, 30)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(140, 13)
        Me.Label1.TabIndex = 2
        Me.Label1.Text = "Browse to the model UCI file"
        '
        'lblRCH
        '
        Me.lblRCH.AutoSize = True
        Me.lblRCH.Enabled = False
        Me.lblRCH.Location = New System.Drawing.Point(20, 537)
        Me.lblRCH.MaximumSize = New System.Drawing.Size(500, 0)
        Me.lblRCH.Name = "lblRCH"
        Me.lblRCH.Size = New System.Drawing.Size(391, 13)
        Me.lblRCH.TabIndex = 7
        Me.lblRCH.Text = "Number(s) of the outlet reach at which you would like constituent balance reports" &
    "."
        '
        'txtRCH
        '
        Me.txtRCH.BackColor = System.Drawing.SystemColors.Window
        Me.txtRCH.Enabled = False
        Me.txtRCH.Location = New System.Drawing.Point(25, 561)
        Me.txtRCH.Name = "txtRCH"
        Me.txtRCH.Size = New System.Drawing.Size(47, 20)
        Me.txtRCH.TabIndex = 19
        Me.ttHSPEXP.SetToolTip(Me.txtRCH, "Enter the location at which you want to generate constituent balance and load all" &
        "ocation reports.")
        '
        'lblOutReach2
        '
        Me.lblOutReach2.AutoSize = True
        Me.lblOutReach2.Enabled = False
        Me.lblOutReach2.Location = New System.Drawing.Point(83, 564)
        Me.lblOutReach2.MaximumSize = New System.Drawing.Size(400, 0)
        Me.lblOutReach2.Name = "lblOutReach2"
        Me.lblOutReach2.Size = New System.Drawing.Size(359, 13)
        Me.lblOutReach2.TabIndex = 18
        Me.lblOutReach2.Text = "NOTE: You can enter multiple reaches, separated by a comma - e.g.: 5, 10"
        '
        'cmdEnd
        '
        Me.cmdEnd.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdEnd.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.cmdEnd.Location = New System.Drawing.Point(407, 594)
        Me.cmdEnd.Name = "cmdEnd"
        Me.cmdEnd.Size = New System.Drawing.Size(75, 23)
        Me.cmdEnd.TabIndex = 21
        Me.cmdEnd.Text = "End"
        Me.cmdEnd.UseVisualStyleBackColor = True
        '
        'chkAreaReports
        '
        Me.chkAreaReports.AutoSize = True
        Me.chkAreaReports.Enabled = False
        Me.chkAreaReports.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.chkAreaReports.Location = New System.Drawing.Point(22, 104)
        Me.chkAreaReports.Name = "chkAreaReports"
        Me.chkAreaReports.Size = New System.Drawing.Size(143, 17)
        Me.chkAreaReports.TabIndex = 4
        Me.chkAreaReports.Text = "Watershed Area Reports"
        Me.ttHSPEXP.SetToolTip(Me.chkAreaReports, "Compute reports of watershed area based on the SCHEMATIC BLOCK.")
        Me.chkAreaReports.UseVisualStyleBackColor = True
        '
        'chkRunHSPF
        '
        Me.chkRunHSPF.AutoSize = True
        Me.chkRunHSPF.Enabled = False
        Me.chkRunHSPF.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.chkRunHSPF.Location = New System.Drawing.Point(22, 81)
        Me.chkRunHSPF.Name = "chkRunHSPF"
        Me.chkRunHSPF.Size = New System.Drawing.Size(77, 17)
        Me.chkRunHSPF.TabIndex = 3
        Me.chkRunHSPF.Text = "Run HSPF"
        Me.ttHSPEXP.SetToolTip(Me.chkRunHSPF, "Makes HSPF Simulation Run.")
        Me.chkRunHSPF.UseVisualStyleBackColor = True
        '
        'pnlHighlight
        '
        Me.pnlHighlight.BackColor = System.Drawing.Color.Red
        Me.pnlHighlight.Enabled = False
        Me.pnlHighlight.Location = New System.Drawing.Point(19, 557)
        Me.pnlHighlight.Name = "pnlHighlight"
        Me.pnlHighlight.Size = New System.Drawing.Size(58, 28)
        Me.pnlHighlight.TabIndex = 17
        '
        'chkAdditionalgraphs
        '
        Me.chkAdditionalgraphs.AutoSize = True
        Me.chkAdditionalgraphs.Enabled = False
        Me.chkAdditionalgraphs.Location = New System.Drawing.Point(3, 19)
        Me.chkAdditionalgraphs.Name = "chkAdditionalgraphs"
        Me.chkAdditionalgraphs.Size = New System.Drawing.Size(286, 17)
        Me.chkAdditionalgraphs.TabIndex = 9
        Me.chkAdditionalgraphs.Text = "Graphs from Graph Specification Files (*.csv and *.json)"
        Me.ttHSPEXP.SetToolTip(Me.chkAdditionalgraphs, "This option requires one or more correctly formatted graph specification file (*." &
        "csv) in the project folder.")
        Me.chkAdditionalgraphs.UseVisualStyleBackColor = True
        '
        'btn_help
        '
        Me.btn_help.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btn_help.Location = New System.Drawing.Point(401, 19)
        Me.btn_help.Name = "btn_help"
        Me.btn_help.Size = New System.Drawing.Size(75, 23)
        Me.btn_help.TabIndex = 0
        Me.btn_help.Text = "Help"
        Me.btn_help.UseVisualStyleBackColor = True
        '
        'cmbUCIPath
        '
        Me.cmbUCIPath.AllowDrop = True
        Me.cmbUCIPath.DisplayMember = """STYLE"""
        Me.cmbUCIPath.DropDownHeight = 150
        Me.cmbUCIPath.DropDownStyle = System.Windows.Forms.ComboBoxStyle.Simple
        Me.cmbUCIPath.FormattingEnabled = True
        Me.cmbUCIPath.IntegralHeight = False
        Me.cmbUCIPath.Location = New System.Drawing.Point(22, 54)
        Me.cmbUCIPath.Name = "cmbUCIPath"
        Me.cmbUCIPath.Size = New System.Drawing.Size(311, 21)
        Me.cmbUCIPath.TabIndex = 1
        Me.cmbUCIPath.ValueMember = """STYLE"""
        '
        'DateTimePicker1
        '
        Me.DateTimePicker1.Enabled = False
        Me.DateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.DateTimePicker1.Location = New System.Drawing.Point(51, 19)
        Me.DateTimePicker1.MaxDate = New Date(2200, 12, 31, 0, 0, 0, 0)
        Me.DateTimePicker1.MinDate = New Date(1800, 1, 1, 0, 0, 0, 0)
        Me.DateTimePicker1.Name = "DateTimePicker1"
        Me.DateTimePicker1.Size = New System.Drawing.Size(96, 20)
        Me.DateTimePicker1.TabIndex = 5
        Me.ttHSPEXP.SetToolTip(Me.DateTimePicker1, "Start date of the analysis. It can be after the model simulation start date.")
        Me.DateTimePicker1.Value = New Date(1996, 1, 1, 0, 0, 0, 0)
        '
        'DateTimePicker2
        '
        Me.DateTimePicker2.Enabled = False
        Me.DateTimePicker2.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.DateTimePicker2.Location = New System.Drawing.Point(204, 19)
        Me.DateTimePicker2.Name = "DateTimePicker2"
        Me.DateTimePicker2.Size = New System.Drawing.Size(96, 20)
        Me.DateTimePicker2.TabIndex = 6
        Me.ttHSPEXP.SetToolTip(Me.DateTimePicker2, "End date of the analysis. It can be before the end of the simulation period.")
        Me.DateTimePicker2.Value = New Date(2009, 12, 31, 0, 0, 0, 0)
        '
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.Label3)
        Me.GroupBox3.Controls.Add(Me.Label2)
        Me.GroupBox3.Controls.Add(Me.DateTimePicker1)
        Me.GroupBox3.Controls.Add(Me.DateTimePicker2)
        Me.GroupBox3.Location = New System.Drawing.Point(18, 132)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(442, 51)
        Me.GroupBox3.TabIndex = 47
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "Analysis Period"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(161, 23)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(26, 13)
        Me.Label3.TabIndex = 48
        Me.Label3.Text = "End"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(15, 23)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(29, 13)
        Me.Label2.TabIndex = 47
        Me.Label2.Text = "Start"
        '
        'Plotting
        '
        Me.Plotting.Controls.Add(Me.chkReganGraphs)
        Me.Plotting.Controls.Add(Me.chkAdditionalgraphs)
        Me.Plotting.Location = New System.Drawing.Point(21, 265)
        Me.Plotting.Name = "Plotting"
        Me.Plotting.Size = New System.Drawing.Size(438, 69)
        Me.Plotting.TabIndex = 48
        Me.Plotting.TabStop = False
        Me.Plotting.Text = "Graph"
        '
        'chkReganGraphs
        '
        Me.chkReganGraphs.AutoSize = True
        Me.chkReganGraphs.Location = New System.Drawing.Point(3, 42)
        Me.chkReganGraphs.Name = "chkReganGraphs"
        Me.chkReganGraphs.Size = New System.Drawing.Size(84, 17)
        Me.chkReganGraphs.TabIndex = 10
        Me.chkReganGraphs.Text = "Regan Plots"
        Me.ttHSPEXP.SetToolTip(Me.chkReganGraphs, "Make sure that PLANK section is active and models sre output in binary files at m" &
        "onthly, or shorter intervals.")
        Me.chkReganGraphs.UseVisualStyleBackColor = True
        '
        'chkExpertStats
        '
        Me.chkExpertStats.AutoSize = True
        Me.chkExpertStats.Enabled = False
        Me.chkExpertStats.Location = New System.Drawing.Point(24, 232)
        Me.chkExpertStats.Name = "chkExpertStats"
        Me.chkExpertStats.Size = New System.Drawing.Size(228, 17)
        Me.chkExpertStats.TabIndex = 8
        Me.chkExpertStats.Text = "Hydrology Calibration Statistics and Graphs"
        Me.ttHSPEXP.SetToolTip(Me.chkExpertStats, "Make sure that the basins specification file (EXS) file is available for the cali" &
        "bration location in the project folder.")
        Me.chkExpertStats.UseVisualStyleBackColor = True
        '
        'chkMultiSim
        '
        Me.chkMultiSim.AutoSize = True
        Me.chkMultiSim.Location = New System.Drawing.Point(24, 200)
        Me.chkMultiSim.Name = "chkMultiSim"
        Me.chkMultiSim.Size = New System.Drawing.Size(144, 17)
        Me.chkMultiSim.TabIndex = 7
        Me.chkMultiSim.Text = "Multi Simulation Manager"
        Me.ttHSPEXP.SetToolTip(Me.chkMultiSim, "This option requires a specificion file for multiple simulations. If not availabl" &
        "e, a default specification file will be generated,")
        Me.chkMultiSim.UseVisualStyleBackColor = True
        '
        'chkTotalNitrogen
        '
        Me.chkTotalNitrogen.AutoSize = True
        Me.chkTotalNitrogen.Enabled = False
        Me.chkTotalNitrogen.Location = New System.Drawing.Point(6, 65)
        Me.chkTotalNitrogen.Name = "chkTotalNitrogen"
        Me.chkTotalNitrogen.Size = New System.Drawing.Size(93, 17)
        Me.chkTotalNitrogen.TabIndex = 13
        Me.chkTotalNitrogen.Text = "Total Nitrogen"
        Me.chkTotalNitrogen.UseVisualStyleBackColor = True
        '
        'chkSedimentBalance
        '
        Me.chkSedimentBalance.AutoSize = True
        Me.chkSedimentBalance.Enabled = False
        Me.chkSedimentBalance.Location = New System.Drawing.Point(6, 42)
        Me.chkSedimentBalance.Name = "chkSedimentBalance"
        Me.chkSedimentBalance.Size = New System.Drawing.Size(70, 17)
        Me.chkSedimentBalance.TabIndex = 12
        Me.chkSedimentBalance.Text = "Sediment"
        Me.chkSedimentBalance.UseVisualStyleBackColor = True
        '
        'chkWaterBalance
        '
        Me.chkWaterBalance.AutoSize = True
        Me.chkWaterBalance.Enabled = False
        Me.chkWaterBalance.Location = New System.Drawing.Point(6, 19)
        Me.chkWaterBalance.Name = "chkWaterBalance"
        Me.chkWaterBalance.Size = New System.Drawing.Size(55, 17)
        Me.chkWaterBalance.TabIndex = 11
        Me.chkWaterBalance.Text = "Water"
        Me.chkWaterBalance.UseVisualStyleBackColor = True
        '
        'chkTotalPhosphorus
        '
        Me.chkTotalPhosphorus.AutoSize = True
        Me.chkTotalPhosphorus.Enabled = False
        Me.chkTotalPhosphorus.Location = New System.Drawing.Point(6, 88)
        Me.chkTotalPhosphorus.Name = "chkTotalPhosphorus"
        Me.chkTotalPhosphorus.Size = New System.Drawing.Size(109, 17)
        Me.chkTotalPhosphorus.TabIndex = 14
        Me.chkTotalPhosphorus.Text = "Total Phosphorus"
        Me.chkTotalPhosphorus.UseVisualStyleBackColor = True
        '
        'chkBODBalance
        '
        Me.chkBODBalance.AutoSize = True
        Me.chkBODBalance.Enabled = False
        Me.chkBODBalance.Location = New System.Drawing.Point(6, 111)
        Me.chkBODBalance.Name = "chkBODBalance"
        Me.chkBODBalance.Size = New System.Drawing.Size(80, 17)
        Me.chkBODBalance.TabIndex = 15
        Me.chkBODBalance.Text = "BOD-Labile"
        Me.chkBODBalance.UseVisualStyleBackColor = True
        '
        'chkGQUAL1
        '
        Me.chkGQUAL1.AutoSize = True
        Me.chkGQUAL1.Location = New System.Drawing.Point(246, 19)
        Me.chkGQUAL1.Name = "chkGQUAL1"
        Me.chkGQUAL1.Size = New System.Drawing.Size(69, 17)
        Me.chkGQUAL1.TabIndex = 18
        Me.chkGQUAL1.Text = "GQUAL1"
        Me.chkGQUAL1.UseVisualStyleBackColor = True
        '
        'chkDO
        '
        Me.chkDO.AutoSize = True
        Me.chkDO.Location = New System.Drawing.Point(6, 134)
        Me.chkDO.Name = "chkDO"
        Me.chkDO.Size = New System.Drawing.Size(111, 17)
        Me.chkDO.TabIndex = 16
        Me.chkDO.Text = "Dissolved Oxygen"
        Me.chkDO.UseVisualStyleBackColor = True
        '
        'chkHeat
        '
        Me.chkHeat.AutoSize = True
        Me.chkHeat.Location = New System.Drawing.Point(6, 155)
        Me.chkHeat.Name = "chkHeat"
        Me.chkHeat.Size = New System.Drawing.Size(49, 17)
        Me.chkHeat.TabIndex = 17
        Me.chkHeat.Text = "Heat"
        Me.chkHeat.UseVisualStyleBackColor = True
        '
        'chkGQUAL2
        '
        Me.chkGQUAL2.AutoSize = True
        Me.chkGQUAL2.Location = New System.Drawing.Point(246, 42)
        Me.chkGQUAL2.Name = "chkGQUAL2"
        Me.chkGQUAL2.Size = New System.Drawing.Size(69, 17)
        Me.chkGQUAL2.TabIndex = 19
        Me.chkGQUAL2.Text = "GQUAL2"
        Me.chkGQUAL2.UseVisualStyleBackColor = True
        '
        'chkGQUAL3
        '
        Me.chkGQUAL3.AutoSize = True
        Me.chkGQUAL3.Location = New System.Drawing.Point(246, 65)
        Me.chkGQUAL3.Name = "chkGQUAL3"
        Me.chkGQUAL3.Size = New System.Drawing.Size(69, 17)
        Me.chkGQUAL3.TabIndex = 20
        Me.chkGQUAL3.Text = "GQUAL3"
        Me.chkGQUAL3.UseVisualStyleBackColor = True
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.chkGQUAL7)
        Me.GroupBox2.Controls.Add(Me.chkGQUAL6)
        Me.GroupBox2.Controls.Add(Me.chkGQUAL5)
        Me.GroupBox2.Controls.Add(Me.chkGQUAL4)
        Me.GroupBox2.Controls.Add(Me.chkGQUAL3)
        Me.GroupBox2.Controls.Add(Me.chkGQUAL2)
        Me.GroupBox2.Controls.Add(Me.chkHeat)
        Me.GroupBox2.Controls.Add(Me.chkDO)
        Me.GroupBox2.Controls.Add(Me.chkGQUAL1)
        Me.GroupBox2.Controls.Add(Me.chkBODBalance)
        Me.GroupBox2.Controls.Add(Me.chkTotalPhosphorus)
        Me.GroupBox2.Controls.Add(Me.chkWaterBalance)
        Me.GroupBox2.Controls.Add(Me.chkSedimentBalance)
        Me.GroupBox2.Controls.Add(Me.chkTotalNitrogen)
        Me.GroupBox2.Location = New System.Drawing.Point(18, 343)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(442, 179)
        Me.GroupBox2.TabIndex = 10
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Constituent Balance Reports"
        Me.ttHSPEXP.SetToolTip(Me.GroupBox2, "This following reports require binary output to be at a monthly or shorter interv" &
        "al.")
        '
        'chkGQUAL4
        '
        Me.chkGQUAL4.AutoSize = True
        Me.chkGQUAL4.Location = New System.Drawing.Point(246, 88)
        Me.chkGQUAL4.Name = "chkGQUAL4"
        Me.chkGQUAL4.Size = New System.Drawing.Size(69, 17)
        Me.chkGQUAL4.TabIndex = 21
        Me.chkGQUAL4.Text = "GQUAL4"
        Me.chkGQUAL4.UseVisualStyleBackColor = True
        '
        'chkGQUAL5
        '
        Me.chkGQUAL5.AutoSize = True
        Me.chkGQUAL5.Location = New System.Drawing.Point(246, 111)
        Me.chkGQUAL5.Name = "chkGQUAL5"
        Me.chkGQUAL5.Size = New System.Drawing.Size(69, 17)
        Me.chkGQUAL5.TabIndex = 22
        Me.chkGQUAL5.Text = "GQUAL5"
        Me.chkGQUAL5.UseVisualStyleBackColor = True
        '
        'chkGQUAL6
        '
        Me.chkGQUAL6.AutoSize = True
        Me.chkGQUAL6.Location = New System.Drawing.Point(246, 134)
        Me.chkGQUAL6.Name = "chkGQUAL6"
        Me.chkGQUAL6.Size = New System.Drawing.Size(69, 17)
        Me.chkGQUAL6.TabIndex = 23
        Me.chkGQUAL6.Text = "GQUAL6"
        Me.chkGQUAL6.UseVisualStyleBackColor = True
        '
        'chkGQUAL7
        '
        Me.chkGQUAL7.AutoSize = True
        Me.chkGQUAL7.Location = New System.Drawing.Point(246, 155)
        Me.chkGQUAL7.Name = "chkGQUAL7"
        Me.chkGQUAL7.Size = New System.Drawing.Size(69, 17)
        Me.chkGQUAL7.TabIndex = 24
        Me.chkGQUAL7.Text = "GQUAL7"
        Me.chkGQUAL7.UseVisualStyleBackColor = True
        '
        'StartUp
        '
        Me.AcceptButton = Me.cmdStart
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.cmdEnd
        Me.ClientSize = New System.Drawing.Size(493, 634)
        Me.Controls.Add(Me.chkMultiSim)
        Me.Controls.Add(Me.chkExpertStats)
        Me.Controls.Add(Me.Plotting)
        Me.Controls.Add(Me.GroupBox3)
        Me.Controls.Add(Me.cmbUCIPath)
        Me.Controls.Add(Me.lblRCH)
        Me.Controls.Add(Me.txtRCH)
        Me.Controls.Add(Me.lblOutReach2)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.pnlHighlight)
        Me.Controls.Add(Me.btn_help)
        Me.Controls.Add(Me.chkRunHSPF)
        Me.Controls.Add(Me.chkAreaReports)
        Me.Controls.Add(Me.cmdEnd)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.cmdBrowse)
        Me.Controls.Add(Me.cmdStart)
        Me.HelpButton = True
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "StartUp"
        Me.Padding = New System.Windows.Forms.Padding(5)
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "HSPEXP+ "
        Me.TransparencyKey = System.Drawing.SystemColors.ActiveBorder
        Me.GroupBox3.ResumeLayout(False)
        Me.GroupBox3.PerformLayout()
        Me.Plotting.ResumeLayout(False)
        Me.Plotting.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents cmdStart As System.Windows.Forms.Button
    Friend WithEvents cmdBrowse As System.Windows.Forms.Button
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents lblRCH As System.Windows.Forms.Label
    Friend WithEvents txtRCH As System.Windows.Forms.TextBox
    Friend WithEvents lblOutReach2 As System.Windows.Forms.Label
    Friend WithEvents cmdEnd As System.Windows.Forms.Button
    Friend WithEvents chkAreaReports As System.Windows.Forms.CheckBox
    Friend WithEvents chkRunHSPF As System.Windows.Forms.CheckBox
    Friend WithEvents pnlHighlight As System.Windows.Forms.Panel
    Friend WithEvents chkAdditionalgraphs As System.Windows.Forms.CheckBox
    Friend WithEvents btn_help As System.Windows.Forms.Button
    Friend WithEvents cmbUCIPath As System.Windows.Forms.ComboBox
    Friend WithEvents DateTimePicker1 As System.Windows.Forms.DateTimePicker
    Friend WithEvents DateTimePicker2 As System.Windows.Forms.DateTimePicker
    Friend WithEvents GroupBox3 As System.Windows.Forms.GroupBox
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents Plotting As GroupBox
    Friend WithEvents chkReganGraphs As CheckBox
    Friend WithEvents ttHSPEXP As ToolTip
    Friend WithEvents chkExpertStats As CheckBox
    Friend WithEvents chkMultiSim As CheckBox
    Friend WithEvents chkTotalNitrogen As CheckBox
    Friend WithEvents chkSedimentBalance As CheckBox
    Friend WithEvents chkWaterBalance As CheckBox
    Friend WithEvents chkTotalPhosphorus As CheckBox
    Friend WithEvents chkBODBalance As CheckBox
    Friend WithEvents chkGQUAL1 As CheckBox
    Friend WithEvents chkDO As CheckBox
    Friend WithEvents chkHeat As CheckBox
    Friend WithEvents chkGQUAL2 As CheckBox
    Friend WithEvents chkGQUAL3 As CheckBox
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents chkGQUAL7 As CheckBox
    Friend WithEvents chkGQUAL6 As CheckBox
    Friend WithEvents chkGQUAL5 As CheckBox
    Friend WithEvents chkGQUAL4 As CheckBox
End Class
