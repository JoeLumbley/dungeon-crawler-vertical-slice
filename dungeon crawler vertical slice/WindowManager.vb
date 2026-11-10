
Imports Microsoft.Win32
Imports System.Runtime.InteropServices

Public Class WindowManager

    Private ReadOnly hostForm As Form

    Private minimumSize As New Size(255, 255)
    Private hdSize As New Size(1280, 720)
    Private fhdSize As New Size(1920, 1080)


    Private Const DWMWA_USE_IMMERSIVE_DARK_MODE As Integer = 20

    Public Sub New(form As Form)

        hostForm = form

    End Sub

    Public Sub Initialize()

        hostForm.Text = "Dungeon Combat Prototype"
        hostForm.BackColor = Color.Black

        hostForm.StartPosition = FormStartPosition.CenterScreen
        hostForm.MinimumSize = minimumSize
        hostForm.Size = hdSize

        ApplySystemTheme()

        CenterWindow()

    End Sub

    Public Sub ApplySystemTheme()

        ApplyDarkTitleBar(IsDarkMode())

    End Sub

    Private Function IsDarkMode() As Boolean

        Try

            Using key As RegistryKey =
                Registry.CurrentUser.OpenSubKey(
                    "Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")

                If key Is Nothing Then Return False

                Dim value As Integer =
                    CInt(key.GetValue("AppsUseLightTheme", 1))

                Return value = 0

            End Using

        Catch

            Return False

        End Try

    End Function

    <DllImport("dwmapi.dll")>
    Private Shared Function DwmSetWindowAttribute(
        hwnd As IntPtr,
        attribute As Integer,
        ByRef value As Integer,
        valueSize As Integer) As Integer
    End Function

    Private Sub ApplyDarkTitleBar(isDark As Boolean)

        If Environment.OSVersion.Version.Major < 10 Then
            Return
        End If

        Dim value As Integer = If(isDark, 1, 0)

        DwmSetWindowAttribute(
            hostForm.Handle,
            DWMWA_USE_IMMERSIVE_DARK_MODE,
            value,
            Marshal.SizeOf(Of Integer)())

    End Sub

    'Public Sub ToggleFullscreen()

    '    ' Is the window in fullscreen mode?
    '    If hostForm.FormBorderStyle = FormBorderStyle.None Then
    '        ' Yes, the window is in fullscreen mode.

    '        ' Switch to windowed mode.
    '        hostForm.FormBorderStyle = FormBorderStyle.Sizable
    '        hostForm.WindowState = FormWindowState.Normal
    '        hostForm.Size = hdSize

    '        CenterWindow()

    '    Else
    '        ' No, the window is NOT in fullscreen mode.

    '        hostForm.FormBorderStyle = FormBorderStyle.Sizable
    '        hostForm.WindowState = FormWindowState.Normal
    '        hostForm.Size = hdSize

    '        CenterWindow()

    '        ' Switch to fullscreen mode.
    '        hostForm.FormBorderStyle = FormBorderStyle.None
    '        hostForm.WindowState = FormWindowState.Maximized

    '    End If

    'End Sub



    Private previousBounds As Rectangle

    Public Sub ToggleFullscreen()

        If hostForm.FormBorderStyle = FormBorderStyle.None Then

            ' Restore windowed mode.
            hostForm.WindowState = FormWindowState.Normal
            hostForm.FormBorderStyle = FormBorderStyle.Sizable
            hostForm.Bounds = previousBounds

        Else

            ' Save current windowed size and position.
            previousBounds = hostForm.Bounds

            ' Enter fullscreen.
            hostForm.WindowState = FormWindowState.Normal
            hostForm.FormBorderStyle = FormBorderStyle.None
            hostForm.Bounds = Screen.FromControl(hostForm).Bounds

        End If

    End Sub











    Private Sub CenterWindow()

        Dim bounds =
            Screen.PrimaryScreen.WorkingArea

        hostForm.Location =
            New Point(
                (bounds.Width - hostForm.Width) \ 2,
                (bounds.Height - hostForm.Height) \ 2)

    End Sub

End Class



