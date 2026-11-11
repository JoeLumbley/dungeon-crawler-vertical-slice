Imports System.Drawing.Drawing2D
Imports System.Globalization
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks

Public Class Form1


    Private ReadOnly gameTimer As New Timer()

    Private heroPos As PointF
    Private bossPos As PointF

    Private heroAttackAnim As Double = 0

    Private bossHealth As Integer = 500
    Private bossMaxHealth As Integer = 500

    Private bossFlashTimer As Double
    Private bossHealthBarTimer As Double
    Private heroFlashTimer As Double

    Private ReadOnly damageTexts As New List(Of DamageText)

    Private rand As New Random()



    Private heroTarget As PointF

    Private heroHealth As Integer = 100
    Private heroMaxHealth As Integer = 100

    Private Const HeroSpeed As Single = 200.0F



    Private Const HeroAttackRange As Single = 50.0F


    Private Const HeroRadius As Single = 20.0F
    Private HeroLightRadius As Single = 260.0F
    Private Const BossRadius As Single = 60.0F
    Private Const HallwayHalfWidth As Single = 180.0F
    Private Const GridSize As Single = 40.0F


    Private Const DefaultBossFireballRange As Single = 135.0F
    Private Const DefaultBossPreferredDistance As Single = 75.0F
    Private Const DefaultBossSightRange As Single = 175.0F
    Private Const DefaultBossMeleeRange As Single = BossRadius + 10

    Private bossFireballRange As Single = DefaultBossFireballRange
    Private bossPreferredDistance As Single = DefaultBossPreferredDistance
    Private bossSightRange As Single = DefaultBossSightRange
    Private bossMeleeReach As Single = DefaultBossMeleeRange


    Private Const BossMoveSpeed As Single = 90.0F
    Private Const BossAttackInterval As Double = 1.25
    Private Const FireballSpeed As Single = 350.0F
    Private Const FireballDamage As Integer = 15
    Private Const BossMeleeDamage As Integer = 25
    Private Const BossKnockbackDistance As Single = 300.0F
    Private Const BossMeleeCooldown As Double = 1.0

    Private bossAttackTimer As Double
    Private bossMeleeTimer As Double
    Private bossActive As Boolean
    Private activeBossRangeHandle As BossRangeHandle


    Private showBossRanges As Boolean = True

    Private ReadOnly Property ResetButtonBounds As Rectangle
        Get
            Return New Rectangle(
                (ClientSize.Width - 160) \ 2,
                ClientSize.Height \ 2 + 70,
                160,
                40)
        End Get
    End Property

    Private Enum BossRangeHandle
        None
        Fireball
        PreferredDistance
        Sight
        Melee
    End Enum

    Private ReadOnly fireballs As New List(Of Fireball)



    Private mapEditorActive As Boolean
    Private selectedMapType As MapObjectType = MapObjectType.Wall
    Private selectedMapObject As MapObject
    Private isResizingMapObject As Boolean
    Private resizeStart As PointF
    Private resizeOriginalBounds As RectangleF
    Private editorPan As PointF
    Private editorZoom As Single = 1.0F
    Private ReadOnly mapObjects As New List(Of MapObject)

    'Private ReadOnly Property CombatDistance As Single
    '    Get
    '        Return HeroRadius + BossRadius + bossMeleeRange
    '    End Get
    'End Property

    Private ReadOnly Property CombatDistance As Single
        Get
            Return HeroRadius + BossRadius + HeroAttackRange
        End Get
    End Property


    Private ReadOnly Property BossMeleeDisplayRange As Single
        Get
            Return HeroRadius + BossRadius + bossMeleeReach
        End Get
    End Property



    Private pendingAttack As Boolean

    Private attackCooldown As Double

    Private Const AttackCooldownDuration As Double = 0.25

    Private HeroCollisionRingPen As New Pen(Color.Lime, 2)
    Private BossCollisionRingPen As New Pen(Color.Red, 2)
    Private MoveTargetPen As New Pen(Color.Yellow, 2)
    Private BossRangePen As New Pen(Color.DarkOrange, 2)
    Private BossPreferredDistancePen As New Pen(Color.Yellow, 2)
    Private BossSightRangePen As New Pen(Color.Purple, 2)
    Private BossMeleeRangePen As New Pen(Color.Red, 2)

    Private BossSightRangeBrush = New SolidBrush(Color.FromArgb(16, Color.Purple))


    Private windowManager As WindowManager

    Private fullscreenKeyDown As Boolean






    Private ReadOnly editorButtonFont As New Font(
    "Segoe UI Semibold",
    9.5F,
    FontStyle.Regular)

    Private ReadOnly editorInstructionFont As New Font(
    "Consolas Bold",
    12.0F,
    FontStyle.Regular)

    Private ReadOnly editorHeaderFont As New Font(
    "Segoe UI",
    12.0F,
    FontStyle.Bold)



























    Protected Overrides Async Sub OnLoad(e As EventArgs)

        MyBase.OnLoad(e)

        SetStyle(
            ControlStyles.AllPaintingInWmPaint Or
            ControlStyles.UserPaint Or
            ControlStyles.OptimizedDoubleBuffer,
            True)

        DoubleBuffered = True
        KeyPreview = True


        windowManager = New WindowManager(Me)
        windowManager.Initialize()


        heroPos = New PointF(
            ClientSize.Width \ 2,
            ClientSize.Height - 120)

        heroTarget = heroPos

        bossPos = New PointF(
            ClientSize.Width \ 2,
            120)

        gameTimer.Interval = 16

        AddHandler gameTimer.Tick,
            AddressOf GameLoop

        gameTimer.Start()

        Dim mapPath As String = Path.Combine(Application.StartupPath, "map.dat")

        If Not File.Exists(mapPath) Then
            Try
                File.WriteAllText(mapPath, My.Resources.Resource1.map)
            Catch ex As IOException
                MessageBox.Show(Me, $"Unable to create the default map: {ex.Message}",
                                "Map Load", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End Try
        End If

        Await Task.Delay(100)
        LoadMapFile(mapPath)


        mapPath = Path.Combine(Application.StartupPath, "map2.dat")

        If Not File.Exists(mapPath) Then
            Try
                File.WriteAllText(mapPath, My.Resources.Resource1.map2)
            Catch ex As IOException
                MessageBox.Show(Me, $"Unable to create map2: {ex.Message}",
                                "Map Load", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End Try
        End If


    End Sub

    Private Sub ResetGame()

        For Each mapObject In mapObjects
            If mapObject.ObjectType = MapObjectType.Hero Then
                heroPos = New PointF(
                    mapObject.Bounds.X + mapObject.Bounds.Width / 2.0F,
                    mapObject.Bounds.Y + mapObject.Bounds.Height / 2.0F)
                heroTarget = heroPos
            ElseIf mapObject.ObjectType = MapObjectType.Boss Then
                bossPos = New PointF(
                    mapObject.Bounds.X + mapObject.Bounds.Width / 2.0F,
                    mapObject.Bounds.Y + mapObject.Bounds.Height / 2.0F)
            End If
        Next

        heroHealth = heroMaxHealth
        bossHealth = bossMaxHealth
        bossActive = False
        bossAttackTimer = 0
        attackCooldown = 0
        heroAttackAnim = 0
        heroFlashTimer = 0
        bossFlashTimer = 0
        bossHealthBarTimer = 0
        bossMeleeTimer = 0
        pendingAttack = False
        activeBossRangeHandle = BossRangeHandle.None
        fireballs.Clear()
        damageTexts.Clear()

        Invalidate()

    End Sub

    Private ReadOnly Property ViewportOffset As PointF
        Get
            If mapEditorActive Then Return editorPan

            Return GameplayViewportOffset
        End Get
    End Property

    Private ReadOnly Property GameplayViewportOffset As PointF
        Get
            Return New PointF(
                heroPos.X - ClientSize.Width / 2.0F,
                heroPos.Y - ClientSize.Height / 2.0F)
        End Get
    End Property

    Private Function ScreenToWorld(screenPoint As Point) As PointF
        Dim view = ViewportOffset
        Dim zoom As Single = If(mapEditorActive, editorZoom, 1.0F)

        Return New PointF(
            view.X + screenPoint.X / zoom,
            view.Y + screenPoint.Y / zoom)
    End Function

    Private Function SnapToGrid(value As Single) As Single
        Return CSng(Math.Floor(value / GridSize) * GridSize)
    End Function

    Private Function SnapPoint(point As PointF) As PointF
        Return New PointF(SnapToGrid(point.X), SnapToGrid(point.Y))
    End Function

    Private Function MapObjectAt(worldPoint As PointF) As MapObject
        For i As Integer = mapObjects.Count - 1 To 0 Step -1
            If mapObjects(i).Bounds.Contains(worldPoint) Then
                Return mapObjects(i)
            End If
        Next

        Return Nothing
    End Function

    Private Sub GameLoop(
        sender As Object,
        e As EventArgs)

        Dim dt As Double = 0.016

        UpdateGame(dt)

        Invalidate()

    End Sub

    Private Sub UpdateGame(dt As Double)

        If mapEditorActive Then Return


        UpdateHeroMovement(dt)

        UpdateCombat()

        UpdateBossAI(dt)

        UpdateFireballs(dt)



        If attackCooldown > 0 Then

            attackCooldown -= dt

            If attackCooldown < 0 Then
                attackCooldown = 0
            End If

        End If

        If heroAttackAnim > 0 Then

            heroAttackAnim -= dt

        End If

        If bossFlashTimer > 0 Then

            bossFlashTimer -= dt

        End If

        If bossHealthBarTimer > 0 Then
            bossHealthBarTimer -= dt
        End If

        If heroFlashTimer > 0 Then
            heroFlashTimer -= dt
        End If

        For i As Integer =
            damageTexts.Count - 1 To 0 Step -1

            Dim dmg = damageTexts(i)

            dmg.Y -= CSng(50 * dt)

            dmg.Lifetime -= dt

            If dmg.Lifetime <= 0 Then

                damageTexts.RemoveAt(i)

            End If

        Next

    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)

        MyBase.OnMouseDown(e)

        If mapEditorActive Then
            HandleEditorMouseDown(e)
            Return
        End If

        If e.Button = MouseButtons.Left AndAlso
       (heroHealth <= 0 OrElse bossHealth <= 0) AndAlso
       ResetButtonBounds.Contains(e.Location) Then

            ResetGame()
            Return

        End If

        If e.Button <> MouseButtons.Left Then Return

        Dim worldPoint As PointF = ScreenToWorld(e.Location)

        If heroHealth <= 0 Then Return

        If bossHealth > 0 AndAlso
       BossBounds.Contains(worldPoint) Then

            HandleBossClick()

        Else

            pendingAttack = False
            heroTarget = worldPoint

        End If

    End Sub


    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)

        MyBase.OnMouseMove(e)

        If activeBossRangeHandle <> BossRangeHandle.None Then

            UpdateBossRangeHandle(ScreenToWorld(e.Location))
            Invalidate()
            Return

        End If

        If mapEditorActive AndAlso
       isResizingMapObject AndAlso
       selectedMapObject IsNot Nothing Then

            Dim current As PointF =
            SnapPoint(ScreenToWorld(e.Location))

            Dim left As Single =
            Math.Min(resizeStart.X, current.X)

            Dim top As Single =
            Math.Min(resizeStart.Y, current.Y)

            Dim right As Single =
            Math.Max(resizeStart.X + GridSize,
                     current.X + GridSize)

            Dim bottom As Single =
            Math.Max(resizeStart.Y + GridSize,
                     current.Y + GridSize)

            selectedMapObject.Bounds = New RectangleF(
            left,
            top,
            Math.Max(GridSize, right - left),
            Math.Max(GridSize, bottom - top))

            If selectedMapObject.ObjectType = MapObjectType.Hero OrElse
           selectedMapObject.ObjectType = MapObjectType.Boss Then

                Dim placementCenter As New PointF(
                selectedMapObject.Bounds.X + selectedMapObject.Bounds.Width / 2.0F,
                selectedMapObject.Bounds.Y + selectedMapObject.Bounds.Height / 2.0F)

                If selectedMapObject.ObjectType = MapObjectType.Hero Then

                    heroPos = placementCenter
                    heroTarget = heroPos

                Else

                    bossPos = placementCenter

                End If

            End If

            Invalidate()

        End If

    End Sub


    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)

        If mapEditorActive AndAlso e.Button = MouseButtons.Left Then
            isResizingMapObject = False
        End If

        If e.Button = MouseButtons.Left Then
            activeBossRangeHandle = BossRangeHandle.None
        End If

    End Sub


    Private Function GetHandlePosition(
    radius As Single,
    angleDegrees As Single) As PointF

        Dim radians As Double =
        angleDegrees * Math.PI / 180.0

        Return New PointF(
        bossPos.X + radius * CSng(Math.Cos(radians)),
        bossPos.Y + radius * CSng(Math.Sin(radians)))

    End Function









    'Private Function TryGetBossRangeHandle(
    '    worldPoint As PointF,
    '    ByRef rangeHandle As BossRangeHandle) As Boolean

    '    Const handleRadius As Single = 12.0F
    '    Dim handleDistanceSquared As Single = handleRadius * handleRadius

    '    Dim fireballHandle As New PointF(
    '        bossPos.X + bossFireballRange,
    '        bossPos.Y)
    '    If DistanceSquared(worldPoint, fireballHandle) <= handleDistanceSquared Then
    '        rangeHandle = BossRangeHandle.Fireball
    '        Return True
    '    End If

    '    'Dim meleeHandle As New PointF(
    '    '    bossPos.X + bossMeleeReach,
    '    '    bossPos.Y)

    '    Dim meleeHandle As New PointF(
    '    bossPos.X + BossMeleeDisplayRange,
    '    bossPos.Y)

    '    If DistanceSquared(worldPoint, meleeHandle) <= handleDistanceSquared Then
    '        rangeHandle = BossRangeHandle.Melee
    '        Return True
    '    End If

    '    Dim preferredHandle As New PointF(
    '        bossPos.X + bossPreferredDistance,
    '        bossPos.Y)
    '    If DistanceSquared(worldPoint, preferredHandle) <= handleDistanceSquared Then
    '        rangeHandle = BossRangeHandle.PreferredDistance
    '        Return True
    '    End If

    '    Dim sightHandle As New PointF(
    '        bossPos.X + bossSightRange,
    '        bossPos.Y)
    '    If DistanceSquared(worldPoint, sightHandle) <= handleDistanceSquared Then
    '        rangeHandle = BossRangeHandle.Sight
    '        Return True
    '    End If

    '    rangeHandle = BossRangeHandle.None
    '    Return False

    'End Function

    Private Function TryGetBossRangeHandle(
    worldPoint As PointF,
    ByRef rangeHandle As BossRangeHandle) As Boolean

        Const handleRadius As Single = 12.0F

        Dim handleDistanceSquared As Single =
        handleRadius * handleRadius

        Dim sightHandle As PointF =
        GetHandlePosition(
            bossSightRange,
            270.0F)

        If DistanceSquared(worldPoint, sightHandle) <= handleDistanceSquared Then

            rangeHandle = BossRangeHandle.Sight
            Return True

        End If

        Dim fireballHandle As PointF =
        GetHandlePosition(
            bossFireballRange,
            0.0F)

        If DistanceSquared(worldPoint, fireballHandle) <= handleDistanceSquared Then

            rangeHandle = BossRangeHandle.Fireball
            Return True

        End If

        Dim preferredHandle As PointF =
        GetHandlePosition(
            bossPreferredDistance,
            180.0F)

        If DistanceSquared(worldPoint, preferredHandle) <= handleDistanceSquared Then

            rangeHandle = BossRangeHandle.PreferredDistance
            Return True

        End If

        Dim meleeHandle As PointF =
        GetHandlePosition(
            BossMeleeDisplayRange,
            90.0F)

        If DistanceSquared(worldPoint, meleeHandle) <= handleDistanceSquared Then

            rangeHandle = BossRangeHandle.Melee
            Return True

        End If

        rangeHandle = BossRangeHandle.None

        Return False

    End Function


























    Private Sub UpdateBossRangeHandle(worldPoint As PointF)

        Dim dx As Single = worldPoint.X - bossPos.X
        Dim dy As Single = worldPoint.Y - bossPos.Y
        Dim radius As Single = CSng(Math.Sqrt(dx * dx + dy * dy))
        radius = Math.Max(25.0F, Math.Min(2000.0F, radius))

        Select Case activeBossRangeHandle
            Case BossRangeHandle.Fireball
                bossFireballRange = radius
            Case BossRangeHandle.PreferredDistance
                bossPreferredDistance = radius
            Case BossRangeHandle.Sight
                bossSightRange = radius
                'Case BossRangeHandle.Melee
                '    bossMeleeReach = radius
                'Case BossRangeHandle.Melee

                '    bossMeleeReach =
                '        Math.Max(
                '            0.0F,
                '            radius - (HeroRadius + BossRadius))

            Case BossRangeHandle.Melee

                bossMeleeReach =
                    Math.Max(
                        5.0F,
                        radius - (HeroRadius + BossRadius))
        End Select

    End Sub

    Private Function DistanceSquared(first As PointF, second As PointF) As Single

        Dim dx As Single = first.X - second.X
        Dim dy As Single = first.Y - second.Y
        Return dx * dx + dy * dy

    End Function

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)


        If e.KeyCode = Keys.F OrElse e.KeyCode = Keys.F11 Then
            If fullscreenKeyDown Then Return

            fullscreenKeyDown = True

            windowManager.ToggleFullscreen()
            Return
        End If


        If e.Control AndAlso e.KeyCode = Keys.H Then

            showBossRanges = Not showBossRanges

            If Not showBossRanges Then
                activeBossRangeHandle = BossRangeHandle.None
            End If

            Invalidate()
            Return

        End If

        If e.KeyCode = Keys.E Then
            If Not mapEditorActive Then
                editorPan = GameplayViewportOffset
            End If

            mapEditorActive = Not mapEditorActive
            isResizingMapObject = False
            selectedMapObject = Nothing
            Invalidate()
            Return
        End If

        If e.Control AndAlso e.KeyCode = Keys.S Then
            SaveMap()
            Return

        End If

        If e.Control AndAlso e.KeyCode = Keys.O Then
            OpenMap()
            Return

        End If

        If Not mapEditorActive Then Return

        Select Case e.KeyCode
            Case Keys.Add, Keys.Oemplus
                SetEditorZoom(editorZoom * 1.25F)
            Case Keys.Subtract, Keys.OemMinus
                SetEditorZoom(editorZoom / 1.25F)
            Case Keys.D0, Keys.NumPad0
                SetEditorZoom(1.0F)
            Case Keys.Left
                editorPan.X -= GridSize
            Case Keys.Right
                editorPan.X += GridSize
            Case Keys.Up
                editorPan.Y -= GridSize
            Case Keys.Down
                editorPan.Y += GridSize
            Case Keys.Delete
                If selectedMapObject IsNot Nothing Then
                    mapObjects.Remove(selectedMapObject)
                    selectedMapObject = Nothing
                End If


        End Select


        Invalidate()
    End Sub

    Private Sub SaveMap()

        Using dialog As New SaveFileDialog With {
            .InitialDirectory = Application.StartupPath,
            .Filter = "Map data (*.dat)|*.dat|All files (*.*)|*.*",
            .DefaultExt = "dat",
            .AddExtension = True,
            .FileName = "map.dat"
        }
            If dialog.ShowDialog(Me) <> DialogResult.OK Then Return

            Try
                EnsureSingleHeroPlacement()

                Using writer As New StreamWriter(dialog.FileName, False)
                    For Each mapObject In mapObjects
                        writer.WriteLine(String.Join("|", {
                            mapObject.ObjectType.ToString(),
                            mapObject.Bounds.X.ToString(CultureInfo.InvariantCulture),
                            mapObject.Bounds.Y.ToString(CultureInfo.InvariantCulture),
                            mapObject.Bounds.Width.ToString(CultureInfo.InvariantCulture),
                            mapObject.Bounds.Height.ToString(CultureInfo.InvariantCulture)}))
                    Next

                    writer.WriteLine(String.Join("|", {
                        "Setting",
                        "BossFireballRange",
                        bossFireballRange.ToString(CultureInfo.InvariantCulture)}))
                    writer.WriteLine(String.Join("|", {
                        "Setting",
                        "BossPreferredDistance",
                        bossPreferredDistance.ToString(CultureInfo.InvariantCulture)}))
                    writer.WriteLine(String.Join("|", {
                        "Setting",
                        "BossSightRange",
                        bossSightRange.ToString(CultureInfo.InvariantCulture)}))
                    writer.WriteLine(String.Join("|", {
                        "Setting",
                        "BossMeleeReach",
                        bossMeleeReach.ToString(CultureInfo.InvariantCulture)}))

                    '"BossMeleeRange",
                    'bossMeleeReach.ToString(CultureInfo.InvariantCulture)}))

                End Using
            Catch ex As IOException
                MessageBox.Show(Me, $"Unable to save the map: {ex.Message}",
                                "Save Map", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using

    End Sub

    Private Sub OpenMap()

        Using dialog As New OpenFileDialog With {
            .InitialDirectory = Application.StartupPath,
            .Filter = "Map data (*.dat)|*.dat|All files (*.*)|*.*",
            .DefaultExt = "dat",
            .CheckFileExists = True,
            .FileName = "map.dat"
        }
            If dialog.ShowDialog(Me) <> DialogResult.OK Then Return

            LoadMapFile(dialog.FileName)
        End Using

    End Sub

    Private Sub LoadMapFile(filePath As String)

        Dim loadedObjects As New List(Of MapObject)

        Try
            bossFireballRange = DefaultBossFireballRange
            bossPreferredDistance = DefaultBossPreferredDistance
            bossSightRange = DefaultBossSightRange
            bossMeleeReach = DefaultBossMeleeRange

            For Each line As String In File.ReadLines(filePath)
                If String.IsNullOrWhiteSpace(line) Then Continue For

                Dim fields() As String = line.Split("|"c)

                If fields.Length = 3 AndAlso fields(0) = "Setting" Then
                    Dim settingValue As Single
                    If Single.TryParse(fields(2), NumberStyles.Float,
                                       CultureInfo.InvariantCulture, settingValue) Then
                        Select Case fields(1)
                            Case "BossFireballRange"
                                bossFireballRange = Math.Max(25.0F, Math.Min(2000.0F, settingValue))
                            Case "BossPreferredDistance"
                                bossPreferredDistance = Math.Max(25.0F, Math.Min(2000.0F, settingValue))
                            Case "BossSightRange"
                                bossSightRange = Math.Max(25.0F, Math.Min(2000.0F, settingValue))
                                'Case "BossMeleeRange"
                                '    bossMeleeReach = Math.Max(25.0F, Math.Min(2000.0F, settingValue))

                            Case "BossMeleeRange",
                                 "BossMeleeReach"

                                bossMeleeReach =
                                    Math.Max(
                                        5.0F,
                                        Math.Min(2000.0F, settingValue))

                        End Select
                    End If

                    Continue For
                End If

                If fields.Length <> 5 Then Continue For

                Dim objectType As MapObjectType
                Dim x As Single
                Dim y As Single
                Dim width As Single
                Dim height As Single

                If Not [Enum].TryParse(fields(0), objectType) OrElse
                   Not Single.TryParse(fields(1), NumberStyles.Float, CultureInfo.InvariantCulture, x) OrElse
                   Not Single.TryParse(fields(2), NumberStyles.Float, CultureInfo.InvariantCulture, y) OrElse
                   Not Single.TryParse(fields(3), NumberStyles.Float, CultureInfo.InvariantCulture, width) OrElse
                   Not Single.TryParse(fields(4), NumberStyles.Float, CultureInfo.InvariantCulture, height) Then
                    Continue For
                End If

                If width <= 0 OrElse height <= 0 Then Continue For

                loadedObjects.Add(New MapObject With {
                    .ObjectType = objectType,
                    .Bounds = New RectangleF(x, y, width, height)
                })
            Next

            mapObjects.Clear()
            mapObjects.AddRange(loadedObjects)
            EnsureSingleHeroPlacement()
            bossActive = False
            bossAttackTimer = 0

            For Each mapObject In mapObjects
                If mapObject.ObjectType = MapObjectType.Hero Then
                    heroPos = New PointF(
                        mapObject.Bounds.X + mapObject.Bounds.Width / 2.0F,
                        mapObject.Bounds.Y + mapObject.Bounds.Height / 2.0F)
                    heroTarget = heroPos
                ElseIf mapObject.ObjectType = MapObjectType.Boss Then
                    bossPos = New PointF(
                        mapObject.Bounds.X + mapObject.Bounds.Width / 2.0F,
                        mapObject.Bounds.Y + mapObject.Bounds.Height / 2.0F)
                End If
            Next

            selectedMapObject = Nothing
            isResizingMapObject = False
            Invalidate()
        Catch ex As IOException
            MessageBox.Show(Me, $"Unable to open the map: {ex.Message}",
                            "Open Map", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

    End Sub

    Private Sub EnsureSingleHeroPlacement()

        Dim heroFound As Boolean = False
        Dim bossFound As Boolean = False

        For i As Integer = mapObjects.Count - 1 To 0 Step -1
            If mapObjects(i).ObjectType = MapObjectType.Hero Then
                If heroFound Then
                    mapObjects.RemoveAt(i)
                Else
                    heroFound = True
                End If
            ElseIf mapObjects(i).ObjectType = MapObjectType.Boss Then
                If bossFound Then
                    mapObjects.RemoveAt(i)
                Else
                    bossFound = True
                End If
            End If
        Next

    End Sub

    Private Sub SetEditorZoom(newZoom As Single)

        Dim clampedZoom As Single = Math.Max(0.25F, Math.Min(4.0F, newZoom))
        Dim centerWorld As New PointF(
            editorPan.X + ClientSize.Width / (2.0F * editorZoom),
            editorPan.Y + ClientSize.Height / (2.0F * editorZoom))

        editorZoom = clampedZoom
        editorPan = New PointF(
            centerWorld.X - ClientSize.Width / (2.0F * editorZoom),
            centerWorld.Y - ClientSize.Height / (2.0F * editorZoom))

    End Sub


    Private Sub HandleEditorMouseDown(e As MouseEventArgs)

        If e.Button = MouseButtons.Right Then

            Dim objectToRemove = MapObjectAt(ScreenToWorld(e.Location))

            If objectToRemove IsNot Nothing Then
                mapObjects.Remove(objectToRemove)
            End If

            If selectedMapObject Is objectToRemove Then
                selectedMapObject = Nothing
            End If

            Invalidate()
            Return

        End If

        If e.Button <> MouseButtons.Left Then Return

        Dim worldPoint As PointF = ScreenToWorld(e.Location)

        ' =====================================================
        ' Boss range handles get first chance at the click
        ' =====================================================

        If showBossRanges Then

            Dim rangeHandle As BossRangeHandle

            If TryGetBossRangeHandle(worldPoint, rangeHandle) Then

                activeBossRangeHandle = rangeHandle
                isResizingMapObject = False

                Invalidate()
                Return

            End If

        End If

        ' =====================================================
        ' Toolbar buttons
        ' =====================================================

        Dim toolbarType As MapObjectType

        If TryGetToolbarType(e.Location, toolbarType) Then

            selectedMapType = toolbarType
            selectedMapObject = Nothing

            Invalidate()
            Return

        End If

        ' =====================================================
        ' Normal editor behavior
        ' =====================================================

        Dim snappedPoint As PointF = SnapPoint(worldPoint)

        Dim existing As MapObject =
        MapObjectAt(worldPoint)

        If selectedMapType = MapObjectType.Hero OrElse
       selectedMapType = MapObjectType.Boss Then

            For i As Integer = mapObjects.Count - 1 To 0 Step -1

                If mapObjects(i).ObjectType = selectedMapType Then
                    mapObjects.RemoveAt(i)
                End If

            Next

            Dim placementObject As New MapObject With {
                .ObjectType = selectedMapType,
                .Bounds = New RectangleF(
                    snappedPoint.X,
                    snappedPoint.Y,
                    GridSize,
                    GridSize)
            }

            mapObjects.Add(placementObject)

            selectedMapObject = placementObject

            Dim placementCenter As New PointF(
            snappedPoint.X + GridSize / 2.0F,
            snappedPoint.Y + GridSize / 2.0F)

            If selectedMapType = MapObjectType.Hero Then

                heroPos = placementCenter
                heroTarget = heroPos

            Else

                bossPos = placementCenter

            End If

            resizeStart = snappedPoint
            resizeOriginalBounds = placementObject.Bounds
            isResizingMapObject = True

        ElseIf existing IsNot Nothing AndAlso
          (selectedMapType = MapObjectType.Floor OrElse
           existing.ObjectType <> MapObjectType.Floor) Then

            selectedMapObject = existing

            resizeStart = snappedPoint
            resizeOriginalBounds = existing.Bounds
            isResizingMapObject = True

        Else

            Dim newObject As New MapObject With {
                .ObjectType = selectedMapType,
                .Bounds = New RectangleF(
                    snappedPoint.X,
                    snappedPoint.Y,
                    GridSize,
                    GridSize)
            }

            mapObjects.Add(newObject)

            selectedMapObject = newObject

            resizeStart = snappedPoint
            resizeOriginalBounds = newObject.Bounds
            isResizingMapObject = True

        End If

        Invalidate()

    End Sub


    'Private Function TryGetToolbarType(point As Point, ByRef objectType As MapObjectType) As Boolean
    '    For i As Integer = 0 To 5
    '        Dim button As New Rectangle(8 + i * 104, 8, 96, 32)
    '        If button.Contains(point) Then
    '            objectType = CType(i, MapObjectType)
    '            Return True
    '        End If
    '    Next

    '    Return False
    'End Function

    Private Function TryGetToolbarType(
    point As Point,
    ByRef objectType As MapObjectType) As Boolean

        For i As Integer = 0 To 6

            Dim button As New Rectangle(
                18 + i * 104,
                80,
                96,
                32)

            If button.Contains(point) Then

                objectType = CType(i, MapObjectType)

                Return True

            End If

        Next

        Return False

    End Function











































    Private Sub HandleBossClick()

        Dim dx As Single =
        bossPos.X - heroPos.X

        Dim dy As Single =
        bossPos.Y - heroPos.Y

        Dim distance As Single =
        CSng(Math.Sqrt(dx * dx + dy * dy))

        If distance <= CombatDistance Then

            AttackBoss()

        Else

            If Not pendingAttack Then
                pendingAttack = True
            End If
        End If

    End Sub

    Private Sub UpdateCombat()

        If Not pendingAttack Then Return

        If bossHealth <= 0 Then

            pendingAttack = False
            Return

        End If

        Dim dx As Single =
        bossPos.X - heroPos.X

        Dim dy As Single =
        bossPos.Y - heroPos.Y

        Dim distance As Single =
        CSng(Math.Sqrt(dx * dx + dy * dy))

        ' If the hero is too far from the boss, move them closer to the boss to attack
        If distance > CombatDistance + 2 Then

            Dim nx As Single = dx / distance
            Dim ny As Single = dy / distance

            heroTarget = New PointF(
            bossPos.X - nx * CombatDistance,
            bossPos.Y - ny * CombatDistance)

            Return

        End If

        AttackBoss()

        heroTarget = heroPos
        pendingAttack = False

    End Sub

    Private Sub UpdateHeroMovement(dt As Double)

        Dim dx As Single =
        heroTarget.X - heroPos.X

        Dim dy As Single =
        heroTarget.Y - heroPos.Y

        Dim distance As Single =
        CSng(Math.Sqrt(dx * dx + dy * dy))

        Dim moveDistance As Single =
        HeroSpeed * CSng(dt)

        If moveDistance > distance Then
            heroPos = heroTarget
        Else
            heroPos.X += dx / distance * moveDistance
            heroPos.Y += dy / distance * moveDistance
        End If

        ResolveBossCollision()
        ResolveMapObjectCollisions()

    End Sub


    'Private Sub ResolveBossCollision()

    '    Dim dx As Single =
    '    heroPos.X - bossPos.X

    '    Dim dy As Single =
    '    heroPos.Y - bossPos.Y

    '    Dim distance As Single =
    '    CSng(Math.Sqrt(dx * dx + dy * dy))

    '    Dim minimumDistance As Single =
    '    HeroRadius + BossRadius

    '    If distance <= 0 Then Return

    '    If distance < minimumDistance Then

    '        Dim pushOut As Single =
    '        minimumDistance - distance

    '        Dim nx As Single = dx / distance
    '        Dim ny As Single = dy / distance

    '        heroPos.X += nx * pushOut
    '        heroPos.Y += ny * pushOut

    '    End If

    'End Sub


    Private Sub ResolveBossCollision()

        Dim dx As Single =
        heroPos.X - bossPos.X

        Dim dy As Single =
        heroPos.Y - bossPos.Y

        Dim distance As Single =
        CSng(Math.Sqrt(dx * dx + dy * dy))

        Dim minimumDistance As Single =
        HeroRadius + BossRadius

        If distance <= 0 Then

            heroPos.Y += minimumDistance

            Return

        End If

        If distance < minimumDistance Then

            Dim pushOut As Single =
            minimumDistance - distance

            Dim nx As Single = dx / distance
            Dim ny As Single = dy / distance

            heroPos.X += nx * pushOut
            heroPos.Y += ny * pushOut

        End If

    End Sub



































    Private Sub UpdateBossAI(dt As Double)

        If bossHealth <= 0 OrElse heroHealth <= 0 Then Return

        Dim dx As Single = heroPos.X - bossPos.X
        Dim dy As Single = heroPos.Y - bossPos.Y
        Dim distance As Single = CSng(Math.Sqrt(dx * dx + dy * dy))

        If Not bossActive Then
            If distance > bossSightRange Then Return

            bossActive = True
        End If

        If distance > 0 Then
            Dim moveDirectionX As Single
            Dim moveDirectionY As Single

            ' Determine movement direction based on distance
            If distance > bossFireballRange Then
                ' Move towards the hero if outside fireball range
                moveDirectionX = dx / distance
                moveDirectionY = dy / distance
            ElseIf distance < bossPreferredDistance Then
                ' Move away from the hero if too close
                moveDirectionX = -dx / distance
                moveDirectionY = -dy / distance
            Else
                ' Stay in place if within preferred distance
                moveDirectionX = 0
                moveDirectionY = 0
            End If

            bossPos.X += moveDirectionX * BossMoveSpeed * CSng(dt)
            bossPos.Y += moveDirectionY * BossMoveSpeed * CSng(dt)

            ResolveBossMapObjectCollisions()
        End If

        If distance <= bossFireballRange AndAlso distance > 0 Then
            bossAttackTimer += dt
        Else
            bossAttackTimer = 0
        End If

        If bossAttackTimer >= BossAttackInterval Then

            bossAttackTimer = 0

            LaunchFireball()

        End If

        If bossMeleeTimer > 0 Then
            bossMeleeTimer -= dt
        End If

        TryBossMeleeAttack()

    End Sub

    'Private Sub TryBossMeleeAttack()

    '    If bossMeleeTimer > 0 OrElse heroHealth <= 0 Then Return

    '    Dim dx As Single = heroPos.X - bossPos.X
    '    Dim dy As Single = heroPos.Y - bossPos.Y
    '    Dim distance As Single = CSng(Math.Sqrt(dx * dx + dy * dy))




    '    'If distance > bossMeleeRange Then Return

    '    Dim effectiveMeleeRange As Single =
    '        HeroRadius + BossRadius + bossMeleeRange

    '    If distance > effectiveMeleeRange Then Return




    '    bossMeleeTimer = BossMeleeCooldown
    '    heroHealth -= BossMeleeDamage
    '    heroFlashTimer = 0.15

    '    damageTexts.Add(New DamageText With {
    '        .X = heroPos.X - 10,
    '        .Y = heroPos.Y - 30,
    '        .Value = BossMeleeDamage,
    '        .Lifetime = 1.0
    '    })

    '    If distance > 0 Then
    '        heroPos.X += dx / distance * BossKnockbackDistance
    '        heroPos.Y += dy / distance * BossKnockbackDistance
    '    Else
    '        heroPos.Y += BossKnockbackDistance
    '    End If

    '    heroTarget = heroPos
    '    ResolveBossCollision()
    '    ResolveMapObjectCollisions()

    '    If heroHealth < 0 Then
    '        heroHealth = 0
    '    End If

    'End Sub



    Private Sub TryBossMeleeAttack()

        If bossMeleeTimer > 0 OrElse heroHealth <= 0 Then Return

        Dim dx As Single = heroPos.X - bossPos.X
        Dim dy As Single = heroPos.Y - bossPos.Y
        Dim distance As Single = CSng(Math.Sqrt(dx * dx + dy * dy))

        'Dim effectiveMeleeRange As Single =
        'HeroRadius + BossRadius + bossMeleeReach

        'If distance > effectiveMeleeRange Then Return



        If distance > BossMeleeDisplayRange Then Return



        bossMeleeTimer = BossMeleeCooldown

        heroHealth -= BossMeleeDamage
        heroFlashTimer = 0.15

        damageTexts.Add(New DamageText With {
            .X = heroPos.X - 10,
            .Y = heroPos.Y - 30,
            .Value = BossMeleeDamage,
            .Lifetime = 1.0
        })

        If distance > 0 Then
            heroPos.X += dx / distance * BossKnockbackDistance
            heroPos.Y += dy / distance * BossKnockbackDistance
        Else
            heroPos.Y += BossKnockbackDistance
        End If

        heroTarget = heroPos

        ResolveBossCollision()
        ResolveMapObjectCollisions()

        If heroHealth < 0 Then
            heroHealth = 0
        End If

    End Sub






















    Private Sub ResolveBossMapObjectCollisions()

        For Each mapObject In mapObjects
            If mapObject.ObjectType = MapObjectType.Floor OrElse
               mapObject.ObjectType = MapObjectType.Hero OrElse
               mapObject.ObjectType = MapObjectType.Boss OrElse
               mapObject.ObjectType = MapObjectType.Light Then


                Continue For
            End If

            If mapObject.ObjectType = MapObjectType.Column Then
                ResolveBossColumnCollision(mapObject.Bounds)
                Continue For
            End If

            Dim expanded As New RectangleF(
                mapObject.Bounds.X - BossRadius,
                mapObject.Bounds.Y - BossRadius,
                mapObject.Bounds.Width + BossRadius * 2,
                mapObject.Bounds.Height + BossRadius * 2)

            If Not expanded.Contains(bossPos) Then Continue For

            Dim pushLeft As Single = bossPos.X - expanded.Left
            Dim pushRight As Single = expanded.Right - bossPos.X
            Dim pushTop As Single = bossPos.Y - expanded.Top
            Dim pushBottom As Single = expanded.Bottom - bossPos.Y
            Dim smallestPush As Single = Math.Min(Math.Min(pushLeft, pushRight), Math.Min(pushTop, pushBottom))

            If smallestPush = pushLeft Then
                bossPos.X = expanded.Left
            ElseIf smallestPush = pushRight Then
                bossPos.X = expanded.Right
            ElseIf smallestPush = pushTop Then
                bossPos.Y = expanded.Top
            Else
                bossPos.Y = expanded.Bottom
            End If
        Next

    End Sub


    Private Sub LaunchFireball()

        Dim dx As Single =
        heroPos.X - bossPos.X

        Dim dy As Single =
        heroPos.Y - bossPos.Y

        Dim distance As Single =
        CSng(Math.Sqrt(dx * dx + dy * dy))

        If distance <= 0 Then Return

        fireballs.Add(
        New Fireball With {
            .Position = bossPos,
            .Velocity = New PointF(
                dx / distance * FireballSpeed,
                dy / distance * FireballSpeed)
        })

        ' audioPlayer.Play("fireball.wav")

    End Sub

    Private Sub ResolveBossColumnCollision(bounds As RectangleF)

        Dim centerX As Single = bounds.X + bounds.Width / 2.0F
        Dim centerY As Single = bounds.Y + bounds.Height / 2.0F
        Dim radiusX As Single = Math.Max(1.0F, bounds.Width / 2.0F + BossRadius)
        Dim radiusY As Single = Math.Max(1.0F, bounds.Height / 2.0F + BossRadius)
        Dim normalizedX As Single = (bossPos.X - centerX) / radiusX
        Dim normalizedY As Single = (bossPos.Y - centerY) / radiusY
        Dim distanceSquared As Single = normalizedX * normalizedX + normalizedY * normalizedY

        If distanceSquared > 1.0F Then Return

        If distanceSquared < 0.0001F Then
            bossPos.Y = centerY - radiusY
            Return
        End If

        Dim scale As Single = CSng(1.0 / Math.Sqrt(distanceSquared))
        bossPos.X = centerX + normalizedX * radiusX * scale
        bossPos.Y = centerY + normalizedY * radiusY * scale

    End Sub


    Private Sub UpdateFireballs(dt As Double)

        For i As Integer =
        fireballs.Count - 1 To 0 Step -1

            Dim fireball = fireballs(i)

            fireball.Position = New PointF(
            fireball.Position.X +
            fireball.Velocity.X *
            CSng(dt),
            fireball.Position.Y +
            fireball.Velocity.Y *
            CSng(dt))

            If HitHero(fireball) Then

                heroHealth -= FireballDamage
                heroFlashTimer = 0.15

                damageTexts.Add(New DamageText With {
                    .X = heroPos.X - 10,
                    .Y = heroPos.Y - 30,
                    .Value = FireballDamage,
                    .Lifetime = 1.0
                })

                If heroHealth < 0 Then
                    heroHealth = 0
                End If

                fireballs.RemoveAt(i)

                Continue For

            End If

            ' Fireball cleanup 
            Dim dx As Single =
                fireball.Position.X - bossPos.X

            Dim dy As Single =
                fireball.Position.Y - bossPos.Y

            Dim distance As Single =
                CSng(Math.Sqrt(dx * dx + dy * dy))

            If distance <= 0 Then Continue For

            If distance > bossFireballRange Then

                fireballs.RemoveAt(i)

            End If


        Next


    End Sub

    Private Function HitHero(
    fireball As Fireball) As Boolean

        Dim dx =
        fireball.Position.X - heroPos.X

        Dim dy =
        fireball.Position.Y - heroPos.Y

        Dim distance =
        Math.Sqrt(dx * dx + dy * dy)

        Return distance < 30

    End Function


    Private Sub AttackBoss()

        If attackCooldown > 0 Then Return
        attackCooldown = AttackCooldownDuration

        ' lunge toward the boss
        Dim dx As Single =
            bossPos.X - heroPos.X

        Dim dy As Single =
            bossPos.Y - heroPos.Y

        Dim dist As Single =
            CSng(Math.Sqrt(dx * dx + dy * dy))

        If dist > 0 Then

            heroPos.X += dx / dist * 5
            heroPos.Y += dy / dist * 5

        End If


        heroAttackAnim = 0.15



        Dim damage As Integer =
            rand.Next(8, 16)

        bossHealth -= damage

        If bossHealth < 0 Then
            bossHealth = 0
        End If

        heroAttackAnim = 0.15

        bossFlashTimer = 0.15
        bossHealthBarTimer = 2.0

        damageTexts.Add(
            New DamageText With {
                .X = bossPos.X,
                .Y = bossPos.Y - 40,
                .Value = damage,
                .Lifetime = 1.0
            })

        ' audioPlayer.Play("SwordHit.wav")

    End Sub

    'Private ReadOnly Property BossBounds As RectangleF

    '    Get

    '        Return New RectangleF(
    '            bossPos.X - 60,
    '            bossPos.Y - 60,
    '            120,
    '            120)

    '    End Get

    'End Property


    Private ReadOnly Property BossBounds As RectangleF
        Get

            Return New RectangleF(
                bossPos.X - BossRadius,
                bossPos.Y - BossRadius,
                BossRadius * 2,
                BossRadius * 2)

        End Get
    End Property




    Protected Overrides Sub OnPaint(
        e As PaintEventArgs)

        MyBase.OnPaint(e)

        Dim g = e.Graphics

        g.SmoothingMode = SmoothingMode.HighQuality
        g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAlias
        g.InterpolationMode = InterpolationMode.HighQualityBicubic
        g.CompositingQuality = CompositingQuality.AssumeLinear
        g.PixelOffsetMode = PixelOffsetMode.HighQuality



        Dim view = ViewportOffset


        If mapEditorActive Then
            g.Transform = New Matrix(
                editorZoom,
                0,
                0,
                editorZoom,
                -view.X * editorZoom,
                -view.Y * editorZoom)
        Else
            g.TranslateTransform(-view.X, -view.Y)
        End If

        DrawBackground(g)

        If mapEditorActive Then
            DrawEditorGrid(g, view)


            DrawMapObjects(g)


            'DrawMapLights(g)








            DrawBoss(g)


            DrawHero(g)



            DrawBossRange(g)


            g.ResetTransform()




            DrawEditorOverlay(g)




            Return
        End If

        DrawMapObjects(g)

        'DrawMapLights(g)


        DrawCombatIndicator(g)

        DrawBoss(g)

        'DrawBossRadius(g)

        DrawBossRange(g)

        DrawBossHealthBar(g)


        DrawHeroLight(g)

        DrawHero(g)

        DrawFireballs(g)

        DrawMoveTarget(g)


        DrawDamageNumbers(g)

        ' Draw Hero Collision Ring
        g.DrawEllipse(
            HeroCollisionRingPen,
            heroPos.X - HeroRadius,
            heroPos.Y - HeroRadius,
            HeroRadius * 2,
            HeroRadius * 2)

        g.ResetTransform()

        DrawPlayerHealthBar(g)

        DrawInstructions(g)

    End Sub

    Private Sub ResolveMapObjectCollisions()

        For Each mapObject In mapObjects
            If mapObject.ObjectType = MapObjectType.Floor OrElse
               mapObject.ObjectType = MapObjectType.Hero OrElse
               mapObject.ObjectType = MapObjectType.Boss OrElse
               mapObject.ObjectType = MapObjectType.Light Then

                Continue For

            End If

            If mapObject.ObjectType = MapObjectType.Column Then
                ResolveColumnCollision(mapObject.Bounds)
                Continue For
            End If

            Dim expanded As New RectangleF(
                mapObject.Bounds.X - HeroRadius,
                mapObject.Bounds.Y - HeroRadius,
                mapObject.Bounds.Width + HeroRadius * 2,
                mapObject.Bounds.Height + HeroRadius * 2)

            If Not expanded.Contains(heroPos) Then Continue For

            Dim pushLeft As Single = heroPos.X - expanded.Left
            Dim pushRight As Single = expanded.Right - heroPos.X
            Dim pushTop As Single = heroPos.Y - expanded.Top
            Dim pushBottom As Single = expanded.Bottom - heroPos.Y
            Dim smallestPush As Single = Math.Min(Math.Min(pushLeft, pushRight), Math.Min(pushTop, pushBottom))

            If smallestPush = pushLeft Then
                heroPos.X = expanded.Left
            ElseIf smallestPush = pushRight Then
                heroPos.X = expanded.Right
            ElseIf smallestPush = pushTop Then
                heroPos.Y = expanded.Top
            Else
                heroPos.Y = expanded.Bottom
            End If
        Next

    End Sub

    Private Sub ResolveColumnCollision(bounds As RectangleF)

        Dim centerX As Single = bounds.X + bounds.Width / 2.0F
        Dim centerY As Single = bounds.Y + bounds.Height / 2.0F
        Dim radiusX As Single = Math.Max(1.0F, bounds.Width / 2.0F + HeroRadius)
        Dim radiusY As Single = Math.Max(1.0F, bounds.Height / 2.0F + HeroRadius)
        Dim normalizedX As Single = (heroPos.X - centerX) / radiusX
        Dim normalizedY As Single = (heroPos.Y - centerY) / radiusY
        Dim distanceSquared As Single = normalizedX * normalizedX + normalizedY * normalizedY

        If distanceSquared > 1.0F Then Return

        If distanceSquared < 0.0001F Then
            heroPos.Y = centerY - radiusY
            Return
        End If

        Dim scale As Single = CSng(1.0 / Math.Sqrt(distanceSquared))
        heroPos.X = centerX + normalizedX * radiusX * scale
        heroPos.Y = centerY + normalizedY * radiusY * scale

    End Sub

    Private Sub DrawBackground(
        g As Graphics)

        g.Clear(Color.Black)


    End Sub

    Private Sub DrawEditorGrid(g As Graphics, view As PointF)

        Using gridPen As New Pen(Color.FromArgb(70, Color.White))
            Dim startX As Single = SnapToGrid(view.X) - GridSize
            Dim endX As Single = view.X + ClientSize.Width / editorZoom + GridSize
            Dim startY As Single = SnapToGrid(view.Y) - GridSize
            Dim endY As Single = view.Y + ClientSize.Height / editorZoom + GridSize

            For x As Single = startX To endX Step GridSize
                g.DrawLine(gridPen, x, startY, x, endY)
            Next

            For y As Single = startY To endY Step GridSize
                g.DrawLine(gridPen, startX, y, endX, y)
            Next
        End Using

    End Sub

    'Private Sub DrawMapObjects(g As Graphics)

    '    'For Each mapObject In mapObjects
    '    '    If mapObject.ObjectType = MapObjectType.Floor OrElse
    '    '       mapObject.ObjectType = MapObjectType.Hero OrElse
    '    '       mapObject.ObjectType = MapObjectType.Boss Then
    '    '        DrawMapObject(g, mapObject)
    '    '    End If
    '    'Next

    '    'For Each mapObject In mapObjects
    '    '    If mapObject.ObjectType <> MapObjectType.Floor AndAlso
    '    '       mapObject.ObjectType <> MapObjectType.Hero AndAlso
    '    '       mapObject.ObjectType <> MapObjectType.Boss Then
    '    '        DrawMapObject(g, mapObject)
    '    '    End If
    '    'Next
    '    For Each mapObject In mapObjects

    '        If mapObject.ObjectType = MapObjectType.Floor Then
    '            DrawMapObject(g, mapObject)
    '        End If

    '    Next

    '    For Each mapObject In mapObjects

    '        Select Case mapObject.ObjectType

    '            Case MapObjectType.Wall,
    '                 MapObjectType.Door,
    '                 MapObjectType.Column

    '                DrawMapObject(g, mapObject)

    '        End Select

    '    Next
    'End Sub



    Private Sub DrawMapObjects(g As Graphics)

        ' Floor first.
        For Each mapObject In mapObjects

            If mapObject.ObjectType = MapObjectType.Floor Then
                DrawMapObject(g, mapObject)
            End If

        Next

        ' Lights above floor.
        DrawMapLights(g)

        ' Walls, doors, columns.
        For Each mapObject In mapObjects

            Select Case mapObject.ObjectType

                Case MapObjectType.Wall,
                 MapObjectType.Door,
                 MapObjectType.Column

                    DrawMapObject(g, mapObject)

            End Select

        Next

    End Sub






























    'Private Sub DrawMapObject(g As Graphics, mapObject As MapObject)

    '    Dim fillColor As Color
    '    Select Case mapObject.ObjectType
    '        Case MapObjectType.Floor
    '            fillColor = Color.FromArgb(75, 75, 75)
    '        Case MapObjectType.Wall
    '            fillColor = Color.FromArgb(64, 64, 64)
    '        Case MapObjectType.Column
    '            fillColor = Color.FromArgb(64, 64, 64)

    '        Case MapObjectType.Light

    '            fillColor = Color.Gold

    '        Case Else
    '            If mapEditorActive Then
    '                fillColor = Color.Goldenrod

    '            End If
    '    End Select

    '    If mapObject.ObjectType = MapObjectType.Light Then

    '        Using p As New Pen(Color.Yellow, 2)

    '            g.DrawEllipse(
    '        p,
    '        mapObject.Bounds)

    '        End Using

    '        Return

    '    End If





    '    Using brush As New SolidBrush(fillColor)
    '        If mapObject.ObjectType = MapObjectType.Column Then
    '            g.FillEllipse(brush, mapObject.Bounds)
    '        Else
    '            g.FillRectangle(brush, mapObject.Bounds)
    '        End If
    '    End Using


    '    If mapObject Is selectedMapObject Then
    '        Using selectionPen As New Pen(Color.Lime, 3)
    '            If mapObject.ObjectType = MapObjectType.Column OrElse
    '           mapObject.ObjectType = MapObjectType.Hero OrElse
    '           mapObject.ObjectType = MapObjectType.Boss Then
    '                g.DrawEllipse(selectionPen, mapObject.Bounds)
    '            Else
    '                g.DrawRectangle(selectionPen, mapObject.Bounds.X, mapObject.Bounds.Y,
    '                                mapObject.Bounds.Width, mapObject.Bounds.Height)
    '            End If
    '        End Using
    '    End If

    'End Sub




    Private Sub DrawMapObject(
    g As Graphics,
    mapObject As MapObject)

        Dim fillColor As Color

        Select Case mapObject.ObjectType

            Case MapObjectType.Floor
                fillColor = Color.FromArgb(75, 75, 75)

            Case MapObjectType.Wall
                fillColor = Color.FromArgb(64, 64, 64)

            Case MapObjectType.Column
                fillColor = Color.FromArgb(55, 55, 55)

            Case MapObjectType.Light
                fillColor = Color.Gold

            Case Else

                If mapEditorActive Then
                    fillColor = Color.Goldenrod
                Else
                    fillColor = Color.Transparent
                End If

        End Select

        If mapObject.ObjectType = MapObjectType.Column Then

            Using brush As New SolidBrush(fillColor)
                g.FillEllipse(brush, mapObject.Bounds)
            End Using

        ElseIf mapObject.ObjectType = MapObjectType.Light Then

            If mapEditorActive Then

                Using brush As New SolidBrush(Color.Gold)

                    g.FillEllipse(
                    brush,
                    mapObject.Bounds)

                End Using

                Using p As New Pen(Color.Yellow, 2)

                    g.DrawEllipse(
                    p,
                    mapObject.Bounds)

                End Using

            End If

        Else

            Using brush As New SolidBrush(fillColor)

                g.FillRectangle(
                brush,
                mapObject.Bounds)

            End Using

        End If

        If mapObject Is selectedMapObject Then

            Using selectionPen As New Pen(Color.Lime, 3)

                If mapObject.ObjectType = MapObjectType.Column OrElse
               mapObject.ObjectType = MapObjectType.Hero OrElse
               mapObject.ObjectType = MapObjectType.Boss OrElse
               mapObject.ObjectType = MapObjectType.Light Then

                    g.DrawEllipse(
                    selectionPen,
                    mapObject.Bounds)

                Else

                    g.DrawRectangle(
                    selectionPen,
                    mapObject.Bounds.X,
                    mapObject.Bounds.Y,
                    mapObject.Bounds.Width,
                    mapObject.Bounds.Height)

                End If

            End Using

        End If

    End Sub






































    'Private Sub DrawMapLights(g As Graphics)

    '    For Each mapObject In mapObjects

    '        If mapObject.ObjectType <> MapObjectType.Light Then
    '            Continue For
    '        End If

    '        Dim center As New PointF(
    '        mapObject.Bounds.X + mapObject.Bounds.Width / 2.0F,
    '        mapObject.Bounds.Y + mapObject.Bounds.Height / 2.0F)

    '        Dim radius As Single = mapObject.LightRadius

    '        Dim lightBounds As New RectangleF(
    '        center.X - radius,
    '        center.Y - radius,
    '        radius * 2,
    '        radius * 2)

    '        Using path As New GraphicsPath()

    '            path.AddEllipse(lightBounds)

    '            Using brush As New PathGradientBrush(path)

    '                brush.CenterColor =
    '                Color.FromArgb(180,
    '                               mapObject.LightColor)

    '                brush.SurroundColors =
    '                {Color.FromArgb(0,
    '                                 mapObject.LightColor)}

    '                g.FillPath(brush, path)

    '            End Using

    '        End Using

    '    Next

    'End Sub


    Private Sub DrawMapLights(g As Graphics)

        For Each mapObject In mapObjects

            If mapObject.ObjectType <> MapObjectType.Light Then
                Continue For
            End If

            Dim center As New PointF(
            mapObject.Bounds.X + mapObject.Bounds.Width / 2.0F,
            mapObject.Bounds.Y + mapObject.Bounds.Height / 2.0F)

            Dim radius As Single = 180.0F

            Dim lightBounds As New RectangleF(
            center.X - radius,
            center.Y - radius,
            radius * 2,
            radius * 2)

            Using path As New GraphicsPath()

                path.AddEllipse(lightBounds)

                Using brush As New PathGradientBrush(path)

                    brush.CenterColor =
                    Color.FromArgb(
                        128,
                        Color.White)

                    brush.SurroundColors =
                    {
                        Color.FromArgb(
                            0,
                            Color.White)
                    }

                    g.FillPath(
                    brush,
                    path)

                End Using

            End Using

        Next

    End Sub















    Private Sub DrawDarkness(g As Graphics)

        ' Draw darkness over the whole screen first.
        Using darknessBrush As New SolidBrush(Color.FromArgb(220, Color.Black))
            g.FillRectangle(darknessBrush, ClientRectangle)
        End Using

    End Sub


    Private Sub DrawHeroLight(g As Graphics)

        Dim lightBounds As New RectangleF(
        heroPos.X - HeroLightRadius - HeroLightRadius \ 15,
        heroPos.Y - HeroLightRadius,
        HeroLightRadius * 2,
        HeroLightRadius * 2)


        Using path As New GraphicsPath()

            path.AddEllipse(lightBounds)

            Using lightBrush As New PathGradientBrush(path)

                Dim blend As New ColorBlend()

                blend.Positions = New Single() {
                    0.0F,   ' center
                    0.8F,   ' middle
                    1.0F    ' edge
                }

                blend.Colors = New Color() {
                    Color.FromArgb(255, Color.Black),      ' fully visible
                    Color.FromArgb(180, Color.Black),    ' partially dark
                    Color.FromArgb(0, Color.Black)     ' full darkness
                }

                lightBrush.InterpolationColors = blend

                g.FillPath(lightBrush, path)

            End Using

        End Using

    End Sub


    Private Sub DrawEditorOverlay(g As Graphics)


        ' Draw editor header
        g.DrawString(
            "EDITOR MODE",
            editorHeaderFont,
            Brushes.Gold,
            10,
            10)


        ' Draw instructions
        g.DrawString(
            "Click: Create   Click and Drag: Resize   Arrows: Pan   +/-: Zoom    0: Reset Zoom   Right-Click/Delete: Remove   Ctrl+S: Save   Ctrl+O: Open   E: Gameplay",
            editorInstructionFont,
            Brushes.WhiteSmoke,
            8,
            42)


        ' Draw toolbar background
        Using toolbarBrush As New SolidBrush(
        Color.FromArgb(220, 20, 20, 20))

            g.FillRectangle(
                toolbarBrush,
                10,
                73,
                744,
                48)

        End Using


        ' Draw toolbar buttons
        For i As Integer = 0 To 6

            Dim button As New Rectangle(
                18 + i * 104,
                80,
                96,
                32)

            Dim buttonColor As Color =
            If(selectedMapType = CType(i, MapObjectType),
               Color.DarkGreen,
               Color.DimGray)

            Using brush As New SolidBrush(buttonColor)


                ' Draw button background
                g.FillRectangle(
                brush,
                button)

            End Using


            ' Draw button border
            g.DrawRectangle(
                Pens.White,
                button)

            Dim label As String =
            [Enum].GetName(
                GetType(MapObjectType),
                CType(i, MapObjectType))

            Dim sf As New StringFormat With {
            .Alignment = StringAlignment.Center,
            .LineAlignment = StringAlignment.Center
        }

            ' Draw button label text centered within the button
            g.DrawString(
            label,
            editorButtonFont,
            Brushes.White,
            button,
            sf)

        Next

    End Sub












































    Private Sub DrawHero(
        g As Graphics)

        Dim size As Integer = 40

        If heroAttackAnim > 0 Then

            size = 55

        End If

        Dim rect As New RectangleF(
            heroPos.X - size / 2,
            heroPos.Y - size / 2,
            size,
            size)

        Dim heroColor As Color
        If heroHealth <= 0 Then
            heroColor = Color.Gray
        ElseIf heroFlashTimer > 0 Then
            heroColor = Color.White
        Else
            heroColor = Color.DeepSkyBlue
        End If

        Using brush As New SolidBrush(heroColor)

            g.FillEllipse(
                brush,
                rect)

        End Using

        'g.DrawEllipse(
        '    Pens.White,
        '    rect.X,
        '    rect.Y,
        '    rect.Width,
        '    rect.Height)

    End Sub


    Private Sub DrawBoss(
        g As Graphics)

        Dim brushColor As Color


        If bossHealth <= 0 Then

            brushColor = Color.Gray

        Else

            If bossFlashTimer > 0 Then

                brushColor = Color.White

            Else

                brushColor = Color.DarkRed

            End If


        End If


        Using brush As New SolidBrush(
            brushColor)

            g.FillEllipse(
                brush,
                BossBounds)

        End Using

        g.DrawEllipse(
            Pens.Red,
            BossBounds.X,
            BossBounds.Y,
            BossBounds.Width,
            BossBounds.Height)

        Using f As New Font(
            "Segoe UI",
            12,
            FontStyle.Bold)

            Dim text = "THE BIG RED ONE"

            Dim sz =
                g.MeasureString(text, f)

            g.DrawString(
                text,
                f,
                Brushes.OrangeRed,
                bossPos.X - sz.Width / 2,
                bossPos.Y + 75)

        End Using

    End Sub

    Private Sub DrawBossHealthBar(
        g As Graphics)

        If bossHealthBarTimer <= 0 Then Return

        Const width As Integer = 160
        Const height As Integer = 14

        Dim x As Single = bossPos.X - width / 2.0F
        Dim y As Single = bossPos.Y - BossRadius - height - 8

        g.FillRectangle(
            Brushes.Black,
            x - 2.0F,
            y - 2.0F,
            width + 4,
            height + 4)

        Dim pct As Single =
            CSng(bossHealth) /
            bossMaxHealth

        g.FillRectangle(
            Brushes.DarkRed,
            x,
            y,
            width,
            height)

        g.FillRectangle(
            Brushes.Red,
            x,
            y,
            width * pct,
            height)

        g.DrawRectangle(
            Pens.White,
            x,
            y,
            width,
            height)

        Dim hpText =
            $"{bossHealth}/{bossMaxHealth}"

        Using f As New Font(
            "Segoe UI",
            9,
            FontStyle.Bold)

            Dim sz =
                g.MeasureString(
                    hpText,
                    f)

            g.DrawString(
                hpText,
                f,
                Brushes.White,
                x + width / 2.0F - sz.Width / 2.0F,
                y - 2.0F)

        End Using

    End Sub

    Private Sub DrawDamageNumbers(
        g As Graphics)

        Using f As New Font(
            "Segoe UI",
            14,
            FontStyle.Bold)

            For Each dmg In damageTexts

                g.DrawString(
                    dmg.Value.ToString(),
                    f,
                    Brushes.Gold,
                    dmg.X,
                    dmg.Y)

            Next

        End Using

    End Sub











    Private Sub DrawInstructions(
        g As Graphics)

        Using f As New Font(
            "Segoe UI",
            15)

            g.DrawString(
                "Click To - Move to or Attack   CTRL+H - Hide/Show boss ranges   E - Editor",
                f,
                Brushes.White,
                20,
                ClientSize.Height - 40)

        End Using

        If bossHealth <= 0 Then

            Using f As New Font(
                "Segoe UI",
                24,
                FontStyle.Bold)

                Dim txt As String =
                    "BOSS DEFEATED"

                Dim sz =
                    g.MeasureString(txt, f)

                g.DrawString(
                    txt,
                    f,
                    Brushes.Gold,
                    CSng(ClientSize.Width / 2 - sz.Width / 2),
                    CSng(ClientSize.Height / 2))

            End Using

        End If



        If heroHealth <= 0 Then


            Using f As New Font("Segoe UI", 24, FontStyle.Bold)

                Using sf As New StringFormat()
                    sf.Alignment = StringAlignment.Center
                    sf.LineAlignment = StringAlignment.Center

                    g.DrawString(
                        "YOU DIED",
                        f,
                        Brushes.Red,
                        New RectangleF(0, 0, ClientSize.Width, ClientSize.Height),
                        sf)
                End Using

            End Using

        End If

        If heroHealth <= 0 OrElse bossHealth <= 0 Then
            Using buttonBrush As New SolidBrush(Color.FromArgb(220, 45, 45, 45))
                g.FillRectangle(buttonBrush, ResetButtonBounds)
            End Using

            g.DrawRectangle(Pens.White, ResetButtonBounds)

            Using buttonFont As New Font("Segoe UI", 12, FontStyle.Bold)
                Using sf As New StringFormat()
                    sf.Alignment = StringAlignment.Center
                    sf.LineAlignment = StringAlignment.Center
                    g.DrawString("RESET", buttonFont, Brushes.White,
                                 ResetButtonBounds, sf)
                End Using
            End Using
        End If

    End Sub














    Private Sub DrawFireballs(
    g As Graphics)

        For Each fireball In fireballs

            Dim r As Single = fireball.Radius

            Using brush As New SolidBrush(
                Color.Orange)

                g.FillEllipse(
                    brush,
                    fireball.Position.X - r,
                    fireball.Position.Y - r,
                    r * 2,
                    r * 2)

            End Using

            'g.DrawEllipse(
            '    Pens.Yellow,
            '    fireball.Position.X - r,
            '    fireball.Position.Y - r,
            '    r * 2,
            '    r * 2)

        Next

    End Sub

    Private Sub DrawPlayerHealthBar(
    g As Graphics)

        Const width As Integer = 220
        Const height As Integer = 22

        Dim x As Integer = 20
        Dim y As Integer = 20

        Dim pct As Single =
            CSng(heroHealth) /
            heroMaxHealth

        g.FillRectangle(
            Brushes.DarkRed,
            x,
            y,
            width,
            height)

        g.FillRectangle(
            Brushes.LimeGreen,
            x,
            y,
            width * pct,
            height)

        g.DrawRectangle(
            Pens.White,
            x,
            y,
            width,
            height)

    End Sub


    Private Sub DrawMoveTarget(
    g As Graphics)

        If pendingAttack Then Return

        g.DrawEllipse(
        MoveTargetPen,
        heroTarget.X - 8,
        heroTarget.Y - 8,
        16,
        16)

    End Sub


    Private Sub DrawCombatIndicator(
    g As Graphics)

        If Not pendingAttack Then Return

        Using pen As New Pen(
            Color.Yellow,
            2)

            g.DrawLine(
                pen,
                heroPos,
                bossPos)

        End Using

    End Sub




    Private Sub DrawBossRadius(
    g As Graphics)

        g.DrawEllipse(
                BossMeleeRangePen,
                bossPos.X - BossRadius,
                bossPos.Y - BossRadius,
                BossRadius * 2,
                BossRadius * 2)



    End Sub


    Private Sub DrawBossRange(
    g As Graphics)



        'If Not mapEditorActive Then Return
        If Not showBossRanges Then Return


        g.FillEllipse(
                BossSightRangeBrush,
                bossPos.X - bossSightRange,
                bossPos.Y - bossSightRange,
                bossSightRange * 2,
                bossSightRange * 2)

        g.DrawEllipse(
                BossSightRangePen,
                bossPos.X - bossSightRange,
                bossPos.Y - bossSightRange,
                bossSightRange * 2,
                bossSightRange * 2)


        If mapEditorActive Then

            'DrawBossRangeHandle(g, bossSightRange, BossSightRangePen.Color)
            DrawBossRangeHandle(
                g,
                bossSightRange,
                270.0F,
                BossSightRangePen.Color)

            DrawCenteredBossRangeLabel(g, "Sight Range", bossSightRange)


        End If








        Dim BossFireballRangeBrush = New SolidBrush(Color.FromArgb(16, Color.DarkOrange))

        g.FillEllipse(
        BossFireballRangeBrush,
        bossPos.X - bossFireballRange,
        bossPos.Y - bossFireballRange,
        bossFireballRange * 2,
        bossFireballRange * 2)

        g.DrawEllipse(
        BossRangePen,
        bossPos.X - bossFireballRange,
        bossPos.Y - bossFireballRange,
        bossFireballRange * 2,
        bossFireballRange * 2)



        If mapEditorActive Then
            'DrawBossRangeHandle(g, bossFireballRange, BossRangePen.Color)
            DrawBossRangeHandle(
                g,
                bossFireballRange,
                0.0F,
                BossRangePen.Color)


            DrawCenteredBossRangeLabel(g, "Fireball Range", bossFireballRange)



        End If








        ' 
        Using bossPreferredDistanceBrush As New SolidBrush(Color.FromArgb(16, Color.Yellow))
            g.FillEllipse(
                bossPreferredDistanceBrush,
                bossPos.X - bossPreferredDistance,
                bossPos.Y - bossPreferredDistance,
                bossPreferredDistance * 2,
                bossPreferredDistance * 2)
        End Using

        g.DrawEllipse(
        BossPreferredDistancePen,
        bossPos.X - bossPreferredDistance,
        bossPos.Y - bossPreferredDistance,
        bossPreferredDistance * 2,
        bossPreferredDistance * 2)



        If mapEditorActive Then
            'DrawBossRangeHandle(g, bossPreferredDistance, BossPreferredDistancePen.Color)
            DrawBossRangeHandle(
                g,
                bossPreferredDistance,
                180.0F,
                BossPreferredDistancePen.Color)

            DrawCenteredBossRangeLabel(g, "Preferred Distance", bossPreferredDistance)




        End If








        ' Melee
        'Using bossMeleeRangeBrush As New SolidBrush(Color.FromArgb(16, Color.Red))
        '    g.FillEllipse(
        '        bossMeleeRangeBrush,
        '        bossPos.X - bossMeleeRange,
        '        bossPos.Y - bossMeleeRange,
        '        bossMeleeRange * 2,
        '        bossMeleeRange * 2)
        'End Using

        'g.DrawEllipse(
        '    BossMeleeRangePen,
        '    bossPos.X - bossMeleeRange,
        '    bossPos.Y - bossMeleeRange,
        '    bossMeleeRange * 2,
        '    bossMeleeRange * 2)

        Dim displayedMeleeRange As Single =
        HeroRadius + BossRadius + bossMeleeReach

        'Using bossMeleeRangeBrush As New SolidBrush(Color.FromArgb(16, Color.Red))

        '    g.FillEllipse(
        '        bossMeleeRangeBrush,
        '        bossPos.X - displayedMeleeRange,
        '        bossPos.Y - displayedMeleeRange,
        '        displayedMeleeRange * 2,
        '        displayedMeleeRange * 2)

        'End Using

        'g.DrawEllipse(
        '    BossMeleeRangePen,
        '    bossPos.X - displayedMeleeRange,
        '    bossPos.Y - displayedMeleeRange,
        '    displayedMeleeRange * 2,
        '    displayedMeleeRange * 2)


        ' Melee
        Using bossMeleeRangeBrush As New SolidBrush(
            Color.FromArgb(16, Color.Red))

            g.FillEllipse(
                bossMeleeRangeBrush,
                bossPos.X - BossMeleeDisplayRange,
                bossPos.Y - BossMeleeDisplayRange,
                BossMeleeDisplayRange * 2,
                BossMeleeDisplayRange * 2)

        End Using

        g.DrawEllipse(
            BossMeleeRangePen,
            bossPos.X - BossMeleeDisplayRange,
            bossPos.Y - BossMeleeDisplayRange,
            BossMeleeDisplayRange * 2,
            BossMeleeDisplayRange * 2)



        If mapEditorActive Then

            'DrawBossRangeHandle(
            'g,
            'BossMeleeDisplayRange,
            'BossMeleeRangePen.Color)

            DrawBossRangeHandle(
                g,
                BossMeleeDisplayRange,
                90.0F,
                BossMeleeRangePen.Color)

            DrawCenteredBossRangeLabel(
                g,
                $"Melee Reach ({CInt(bossMeleeReach)})",
                BossMeleeDisplayRange)



        End If



        'DrawBossRangeHandle(g, bossMeleeReach, BossMeleeRangePen.Color)
        'DrawCenteredBossRangeLabel(g, "Melee Range", bossMeleeReach)


    End Sub

    'Private Sub DrawBossRangeHandle(
    '    g As Graphics,
    '    radius As Single,
    '    color As Color)

    '    Const handleSize As Single = 14.0F
    '    Dim handleBounds As New RectangleF(
    '        bossPos.X + radius - handleSize / 2.0F,
    '        bossPos.Y - handleSize / 2.0F,
    '        handleSize,
    '        handleSize)

    '    Using brush As New SolidBrush(color)
    '        g.FillEllipse(brush, handleBounds)
    '    End Using

    '    g.DrawEllipse(Pens.White, handleBounds)

    'End Sub

    Private Sub DrawBossRangeHandle(
    g As Graphics,
    radius As Single,
    angleDegrees As Single,
    color As Color)

        Const handleSize As Single = 14.0F

        Dim position As PointF =
        GetHandlePosition(
            radius,
            angleDegrees)

        Dim handleBounds As New RectangleF(
        position.X - handleSize / 2.0F,
        position.Y - handleSize / 2.0F,
        handleSize,
        handleSize)

        Using brush As New SolidBrush(color)
            g.FillEllipse(brush, handleBounds)
        End Using

        g.DrawEllipse(Pens.White, handleBounds)

    End Sub











    Private Sub DrawCenteredBossRangeLabel(
        g As Graphics,
        label As String,
        radius As Single)

        Using font As New Font("Segoe UI", 10)
            Dim textSize As SizeF = g.MeasureString(label, font)
            g.DrawString(
                label,
                font,
                Brushes.White,
                bossPos.X - textSize.Width / 2.0F,
                bossPos.Y - radius - textSize.Height - 4.0F)
        End Using

    End Sub

    Private Sub Form1_Resize(sender As Object, e As EventArgs) Handles Me.Resize

        HeroLightRadius = ClientSize.Width



    End Sub



    ' ============================================================
    '  SYSTEM THEME CHANGE DETECTION
    '
    '  Windows sends WM_SETTINGCHANGE whenever a system setting
    '  changes. We use this notification to detect Light/Dark
    '  theme changes and update the title bar appearance without
    '  requiring the application to restart.
    ' ============================================================
    Protected Overrides Sub WndProc(ByRef m As Message)

        Const WM_SETTINGCHANGE As Integer = &H1A

        If m.Msg = WM_SETTINGCHANGE Then
            windowManager?.ApplySystemTheme()
        End If

        MyBase.WndProc(m)


    End Sub


    Private Sub Form1_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        If e.KeyCode = Keys.F OrElse e.KeyCode = Keys.F11 Then
            fullscreenKeyDown = False
        End If


    End Sub

    Private Sub Form1_Shown(sender As Object, e As EventArgs) Handles Me.Shown

        Me.WindowState = FormWindowState.Maximized

    End Sub





End Class




'Public Enum MapObjectType
'    Floor
'    Wall
'    Door
'    Column
'    Hero
'    Boss
'End Enum

Public Enum MapObjectType
    Floor
    Wall
    Door
    Column
    Hero
    Boss
    Light
End Enum

'Public Class MapObject

'    Public Property ObjectType As MapObjectType

'    Public Property Bounds As RectangleF

'End Class

Public Class MapObject

    Public Property ObjectType As MapObjectType

    Public Property Bounds As RectangleF

    Public Property LightRadius As Single = 200.0F

    Public Property LightColor As Color = Color.Gold

End Class










Public Class DamageText

    Public Property X As Single

    Public Property Y As Single

    Public Property Value As Integer

    Public Property Lifetime As Double

End Class


Public Class Fireball

    Public Property Position As PointF

    Public Property Velocity As PointF

    Public Property Radius As Single = 10

End Class