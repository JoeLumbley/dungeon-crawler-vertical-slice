Imports System.Drawing.Drawing2D

Public Class Form1

    Private ReadOnly gameTimer As New Timer()

    Private heroPos As PointF
    Private bossPos As PointF

    Private heroAttackAnim As Double = 0

    Private bossHealth As Integer = 500
    Private bossMaxHealth As Integer = 500

    Private bossFlashTimer As Double

    Private ReadOnly damageTexts As New List(Of DamageText)

    Private rand As New Random()

    'Private attackCooldown As Double


    Private heroTarget As PointF

    Private heroHealth As Integer = 100
    Private heroMaxHealth As Integer = 100

    Private Const HeroSpeed As Single = 200.0F

    Private bossAttackTimer As Double

    Private ReadOnly fireballs As New List(Of Fireball)





    'Private heroAttackCooldown As Double

    'Private Const AttackInterval As Double = 0.5

    'Private Const MeleeRange As Single = 85.0F

    'Private playerTargetingBoss As Boolean



    Private Const HeroRadius As Single = 20.0F
    Private Const BossRadius As Single = 60.0F
    Private Const MeleeRange As Single = 70.0F
    Private Const HallwayHalfWidth As Single = 180.0F




    Private ReadOnly CombatDistance As Single =
    HeroRadius + BossRadius + MeleeRange

    Private pendingAttack As Boolean





    Private attackCooldown As Double

    Private Const AttackCooldownDuration As Double = 0.25












    Public Sub New()

        InitializeComponent()

        DoubleBuffered = True

        ' we draw background


        ClientSize = New Size(900, 700)

        Text = "Dungeon Combat Prototype"

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

    End Sub

    Private Sub GameLoop(
        sender As Object,
        e As EventArgs)

        Dim dt As Double = 0.016

        UpdateGame(dt)

        Invalidate()

    End Sub

    Private Sub UpdateGame(dt As Double)




        UpdateHeroMovement(dt)

        UpdateCombat()

        UpdateBossAI(dt)

        UpdateFireballs(dt)


        ResolveBossCollision()


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

    'Protected Overrides Sub OnMouseDown(
    '    e As MouseEventArgs)

    '    MyBase.OnMouseDown(e)

    '    If e.Button <> MouseButtons.Left Then Return

    '    If bossHealth <= 0 Then Return

    '    If BossBounds.Contains(e.Location) Then

    '        AttackBoss()

    '    End If

    'End Sub



    'Protected Overrides Sub OnMouseDown(
    'e As MouseEventArgs)

    '    MyBase.OnMouseDown(e)

    '    If e.Button = MouseButtons.Left Then

    '        ' If bossHealth > 0 AndAlso
    '        'BossBounds.Contains(e.Location) Then

    '        '     AttackBoss()

    '        ' End If

    '        If bossHealth > 0 AndAlso
    '           BossBounds.Contains(e.Location) Then

    '            playerTargetingBoss = True

    '        End If


    '    End If


    '    If e.Button = MouseButtons.Right Then

    '        'heroTarget = e.Location

    '        heroTarget = e.Location

    '        playerTargetingBoss = False


    '    End If

    'End Sub

    Protected Overrides Sub OnMouseDown(
    e As MouseEventArgs)

        MyBase.OnMouseDown(e)

        If e.Button <> MouseButtons.Left Then Return

        If heroHealth <= 0 Then Return

        If bossHealth > 0 AndAlso
       BossBounds.Contains(e.Location) Then

            HandleBossClick()

        Else

            pendingAttack = False
            heroTarget = e.Location

        End If

    End Sub

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

            'pendingAttack = True
            If Not pendingAttack Then
                pendingAttack = True
            End If
        End If

    End Sub





















    'Private Sub UpdateCombat(dt As Double)

    '    If bossHealth <= 0 Then
    '        playerTargetingBoss = False
    '        Return
    '    End If

    '    If Not playerTargetingBoss Then Return

    '    Dim dx As Single =
    '    bossPos.X - heroPos.X

    '    Dim dy As Single =
    '    bossPos.Y - heroPos.Y

    '    Dim distance As Single =
    '    CSng(Math.Sqrt(dx * dx + dy * dy))

    '    'If distance > MeleeRange Then

    '    '    heroTarget = bossPos

    '    'Else

    '    '    heroAttackCooldown -= dt

    '    '    If heroAttackCooldown <= 0 Then

    '    '        heroAttackCooldown = AttackInterval

    '    '        AttackBoss()

    '    '    End If

    '    'End If


    '    If distance > CombatDistance Then

    '        Dim nx As Single = dx / distance
    '        Dim ny As Single = dy / distance

    '        heroTarget = New PointF(
    '            bossPos.X - nx * CombatDistance,
    '            bossPos.Y - ny * CombatDistance)

    '    Else

    '        heroAttackCooldown -= dt

    '        If heroAttackCooldown <= 0 Then

    '            heroAttackCooldown = AttackInterval

    '            AttackBoss()

    '        End If

    '    End If

    'End Sub




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

        If distance > CombatDistance Then

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










    'Private Sub UpdateHeroMovement(dt As Double)

    '    'Dim dx As Single =
    '    'heroTarget.X - heroPos.X

    '    'Dim dy As Single =
    '    'heroTarget.Y - heroPos.Y

    '    Dim target As PointF = heroTarget

    '    If playerTargetingBoss Then

    '        Dim bossDx =
    '    bossPos.X - heroPos.X

    '        Dim bossDy =
    '    bossPos.Y - heroPos.Y

    '        Dim bossDistance =
    '    CSng(Math.Sqrt(
    '        bossDx * bossDx +
    '        bossDy * bossDy))

    '        If bossDistance <= MeleeRange Then

    '            Return

    '        End If

    '    End If

    '    Dim dx As Single =
    'target.X - heroPos.X

    '    Dim dy As Single =
    'target.Y - heroPos.Y












    '    Dim distance As Single =
    '    CSng(Math.Sqrt(dx * dx + dy * dy))

    '    If distance < 2 Then Return

    '    Dim moveDistance As Single =
    '    HeroSpeed * CSng(dt)

    '    If moveDistance > distance Then

    '        heroPos = heroTarget
    '        Return

    '    End If

    '    heroPos.X += dx / distance * moveDistance
    '    heroPos.Y += dy / distance * moveDistance


    '    'ResolveBossCollision()

    'End Sub

    Private Sub UpdateHeroMovement(dt As Double)

        Dim dx As Single =
        heroTarget.X - heroPos.X

        Dim dy As Single =
        heroTarget.Y - heroPos.Y

        Dim distance As Single =
        CSng(Math.Sqrt(dx * dx + dy * dy))

        If distance < 2 Then
            ClampHeroToHallway()
            Return
        End If

        Dim moveDistance As Single =
        HeroSpeed * CSng(dt)

        If moveDistance > distance Then
            heroPos = heroTarget
        Else
            heroPos.X += dx / distance * moveDistance
            heroPos.Y += dy / distance * moveDistance
        End If

        ClampHeroToHallway()

        ResolveBossCollision()
        ClampHeroToHallway()

    End Sub

    Private Sub ClampHeroToHallway()

        Dim leftWallEdge As Single =
            ClientSize.Width / 2.0F - HallwayHalfWidth

        Dim rightWallEdge As Single =
            ClientSize.Width / 2.0F + HallwayHalfWidth

        heroPos.X = Math.Max(
            leftWallEdge + HeroRadius,
            Math.Min(rightWallEdge - HeroRadius, heroPos.X))

    End Sub





























    Private Sub ResolveBossCollision()

        Dim dx As Single =
        heroPos.X - bossPos.X

        Dim dy As Single =
        heroPos.Y - bossPos.Y

        Dim distance As Single =
        CSng(Math.Sqrt(dx * dx + dy * dy))

        Dim minimumDistance As Single =
        HeroRadius + BossRadius

        If distance <= 0 Then Return

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

        If bossHealth <= 0 Then Return

        bossAttackTimer += dt

        If bossAttackTimer >= 2.0 Then

            bossAttackTimer = 0

            LaunchFireball()

        End If

    End Sub


    Private Sub LaunchFireball()

        Dim dx As Single =
        heroPos.X - bossPos.X

        Dim dy As Single =
        heroPos.Y - bossPos.Y

        Dim distance As Single =
        CSng(Math.Sqrt(dx * dx + dy * dy))

        If distance <= 0 Then Return

        Dim speed As Single = 250.0F

        fireballs.Add(
        New Fireball With {
            .Position = bossPos,
            .Velocity = New PointF(
                dx / distance * speed,
                dy / distance * speed)
        })

        ' audioPlayer.Play("fireball.wav")

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

                'heroHealth -= 10
                heroHealth -= 10

                If heroHealth < 0 Then
                    heroHealth = 0
                End If

                fireballs.RemoveAt(i)

                Continue For

            End If

            If fireball.Position.X < -50 OrElse
           fireball.Position.X > ClientSize.Width + 50 OrElse
           fireball.Position.Y < -50 OrElse
           fireball.Position.Y > ClientSize.Height + 50 Then

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

        'If heroAttackAnim > 0 Then Return
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

        damageTexts.Add(
            New DamageText With {
                .X = bossPos.X,
                .Y = bossPos.Y - 40,
                .Value = damage,
                .Lifetime = 1.0
            })

        ' audioPlayer.Play("SwordHit.wav")

    End Sub

    Private ReadOnly Property BossBounds As RectangleF

        Get

            Return New RectangleF(
                bossPos.X - 60,
                bossPos.Y - 60,
                120,
                120)

        End Get

    End Property

    Protected Overrides Sub OnPaint(
        e As PaintEventArgs)

        MyBase.OnPaint(e)

        Dim g = e.Graphics

        g.SmoothingMode = SmoothingMode.AntiAlias

        DrawBackground(g)

        DrawCombatIndicator(g)

        DrawMoveTarget(g)

        DrawBossRange(g)

        ' Draw Boss Collision Ring
        g.DrawEllipse(
            Pens.Red,
            bossPos.X - BossRadius,
            bossPos.Y - BossRadius,
            BossRadius * 2,
            BossRadius * 2)

        DrawBoss(g)

        DrawFireballs(g)

        ' Draw Hero Collision Ring
        g.DrawEllipse(
            Pens.Lime,
            heroPos.X - HeroRadius,
            heroPos.Y - HeroRadius,
            HeroRadius * 2,
            HeroRadius * 2)

        DrawHero(g)

        DrawPlayerHealthBar(g)

        DrawBossHealthBar(g)

        DrawDamageNumbers(g)

        DrawInstructions(g)

    End Sub

    Private Sub DrawBackground(
        g As Graphics)

        g.Clear(Color.DeepPink)

        Using hallwayBrush As New SolidBrush(
            Color.FromArgb(45, 45, 45))

            g.FillRectangle(
                hallwayBrush,
                ClientSize.Width \ 2 - 180,
                0,
                360,
                ClientSize.Height)

        End Using

        Using wallBrush As New SolidBrush(
            Color.FromArgb(32, 32, 32))

            g.FillRectangle(
                wallBrush,
                0,
                0,
                ClientSize.Width \ 2 - 180,
                ClientSize.Height)

            g.FillRectangle(
                wallBrush,
                ClientSize.Width \ 2 + 180,
                0,
                ClientSize.Width,
                ClientSize.Height)

        End Using

        For y As Integer = 0 To ClientSize.Height Step 40

            g.DrawLine(
                Pens.DimGray,
                ClientSize.Width \ 2 - 180,
                y,
                ClientSize.Width \ 2 + 180,
                y)

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

        'Using brush As New SolidBrush(
        '    Color.DeepSkyBlue)

        '    g.FillEllipse(
        '        brush,
        '        rect)

        'End Using

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

        If bossFlashTimer > 0 Then

            brushColor = Color.White

        Else

            brushColor = Color.DarkRed

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

        Const width As Integer = 400
        Const height As Integer = 24

        Dim x =
            (ClientSize.Width - width) \ 2

        Dim y As Integer = 20

        g.FillRectangle(
            Brushes.Black,
            x - 2,
            y - 2,
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
                CSng(x + width / 2 - sz.Width / 2),
                CSng(y + 3))

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
            10)

            g.DrawString(
                "Click the boss to attack",
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

            Using f As New Font(
                "Segoe UI",
                24,
                FontStyle.Bold)

                g.DrawString(
                    "YOU DIED",
                    f,
                    Brushes.Red,
                    320,
                    320)

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

            g.DrawEllipse(
                Pens.Yellow,
                fireball.Position.X - r,
                fireball.Position.Y - r,
                r * 2,
                r * 2)

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







    'Private Sub DrawMoveTarget(
    'g As Graphics)

    '    g.DrawEllipse(
    '        Pens.Yellow,
    '        heroTarget.X - 8,
    '        heroTarget.Y - 8,
    '        16,
    '        16)

    'End Sub

    Private Sub DrawMoveTarget(
    g As Graphics)

        If pendingAttack Then Return

        g.DrawEllipse(
        Pens.Yellow,
        heroTarget.X - 8,
        heroTarget.Y - 8,
        16,
        16)

    End Sub


    Private Sub DrawCombatIndicator(
    g As Graphics)

        'If Not playerTargetingBoss Then Return
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



    Private Sub DrawBossRange(
    g As Graphics)

        g.DrawEllipse(
        Pens.DarkOrange,
        bossPos.X - MeleeRange,
        bossPos.Y - MeleeRange,
        MeleeRange * 2,
        MeleeRange * 2)

    End Sub














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