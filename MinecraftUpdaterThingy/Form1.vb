Imports System.IO
Imports System.Net
Imports MinecraftUpdaterThingy.MinecraftUpdaterThingy
Imports System.Text
Imports Newtonsoft.Json
Public Class Form1
    Dim _LauncherPath As String
    Dim _Version As String
    Dim AppPath As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
    Private Shared mcAccessToken As String
    Private Shared mcClientToken As String
    Private Shared mcUUID As String
    Private Shared mcUsername As String
    Private Shared mcEpw As String
    Private WithEvents bgw As New System.ComponentModel.BackgroundWorker

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Me.Hide()
        Console.Show()
        If bgw.IsBusy = False Then bgw.RunWorkerAsync()
        Do Until bgw.IsBusy = False
            Threading.Thread.Sleep(100)
            Application.DoEvents()
        Loop

    End Sub
    Private Sub StartEXE() Handles bgw.DoWork
        Try

            CheckUpdates()
            Dim JavaHome As String = IO.Path.Combine(My.Settings.LauncherPath, "jre")
            Dim p As New System.Diagnostics.Process
            Dim appdata As String = AppPath & "\" & My.Settings.Version
            Dim arguments As String = "-XX:ParallelGCThreads=4 -XX:UseSSE=4 -XX:+AggressiveOpts -XX:+UseConcMarkSweepGC -XX:+UseParNewGC -XX:MaxPermSize=128m -Xmx2048M -Xms512M -XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump -Djava.library.path=bin/natives -jar " & IO.Path.Combine(appdata, "minecraft.exe") & " --noupdate"
            If My.Settings.Version.Substring(2, 1) >= 6 Then
                arguments = LaunchV6Plus(256, IO.Path.Combine(appdata, "natives"), IO.Path.Combine(appdata, "libraries"), appdata, JavaHome, My.Settings.Version)
                UpdateTextBoxColor(arguments, Color.Blue)
            End If
            With p.StartInfo
                .EnvironmentVariables.Remove("APPDATA")
                .EnvironmentVariables.Add("APPDATA", appdata)
                .UseShellExecute = False
                .FileName = IO.Path.Combine(JavaHome, "bin/java.exe")
                .Arguments = arguments
                .WorkingDirectory = IO.Path.Combine(appdata)
                .RedirectStandardError = True
                .RedirectStandardOutput = True
                .CreateNoWindow = True

            End With
            p.EnableRaisingEvents = True
            Application.DoEvents()
            AddHandler p.ErrorDataReceived, AddressOf p_DataReceivedHandler
            'AddHandler p.OutputDataReceived, AddressOf p_DataReceivedHandler
            AddHandler p.Exited, AddressOf p_exited
            p.Start()
            p.BeginErrorReadLine()
            p.BeginOutputReadLine()

            'p.BeginOutputReadLine()

            'Trace.WriteLine(p.StandardOutput.ReadToEnd)
            'Trace.WriteLine(p.StandardError.ReadToEnd)
            'Do Until p.HasExited = True
            '    Threading.Thread.Sleep(100)
            '    Application.DoEvents()
            'Loop
            ' Throw New System.Exception("test")
        Catch ex As Exception
            UpdateTextBoxColor(ex.ToString, Color.Red)

        End Try
    End Sub
    Delegate Sub showFormDelg()
    Public showFormDel As showFormDelg = New showFormDelg(AddressOf ShowForm)
    Private Sub ShowForm()
        If Me.InvokeRequired = True Then
            BeginInvoke(showFormDel)
        Else
            Me.Show()
        End If
    End Sub
    Public Sub p_exited()
        ShowForm()
    End Sub
    Public Sub UpdateTextBoxColor(Text As String, Color As Color)
        If Me.InvokeRequired = True Then
            Me.Invoke(myColorDel, {Text, Color})
        Else
            Console.RichTextBox1.SelectionColor = Color
            Console.RichTextBox1.AppendText(Text & Environment.NewLine)
            Console.RichTextBox1.ScrollToCaret()
            Console.RichTextBox1.SelectionColor = ForeColor
        End If
    End Sub
    Delegate Sub UpdateTextBoxDelg(text As String)
    Delegate Sub UpdateTextBoxColorDelg(text As String, color As Color)
    Public myDelegate As UpdateTextBoxDelg = New UpdateTextBoxDelg(AddressOf UpdateTextBox)
    Public myColorDel As UpdateTextBoxColorDelg = New UpdateTextBoxColorDelg(AddressOf UpdateTextBoxColor)
    Public Sub UpdateTextBox(text As String)
        UpdateTextBoxColor(text, Color.Black)
    End Sub

    Public Sub p_DataReceivedHandler(ByVal sender As Object, ByVal e As DataReceivedEventArgs)
        If Me.InvokeRequired = True Then
            Me.Invoke(myDelegate, e.Data)
        Else
            UpdateTextBox(e.Data)
        End If
    End Sub

    Private Function LaunchV6Plus(ByVal MaxPermSize As Integer, ByVal NativesPath As String, ByVal LibraryPath As String, ByVal UserHome As String, ByVal JavaPath As String, ByVal MCVersion As String) As String
        If txtPW.Text = "" Then Throw New Exception("No login info...")
        If txtPW.Text <> mcEpw Then
            mcEpw = otherCipher.Encrypt(txtPW.Text, otherCipher.machineIDLookup())
            txtPW.Text = mcEpw
        End If


        setSaveData(txtUN.Text, mcEpw)
        setSession(txtUN.Text, mcEpw)
        Dim s As New System.Text.StringBuilder
        s.Append("-XX:ParallelGCThreads=4 -XX:UseSSE=4 -XX:+AggressiveOpts -XX:+CICompilerCountPerCPU -XX:+TieredCompilation -XX:+UseConcMarkSweepGC -XX:+UseParNewGC -XX:PermSize=256m -Xmx2048M -Xms512M -XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump -Dlog4j.configurationFile=" & IO.Path.Combine(UserHome, "debugsettings.xml") & " ")
        s.Append("-Djava.library.path=" & NativesPath & " -Dorg.lwjgl.librarypath=" & NativesPath & " -Dnet.java.games.input.librarypath=" & NativesPath & " -Djava.net.preferIPv4Stack=true -Duser.home=" & IO.Path.Combine(UserHome, "minecraft") & " -cp ;")
        s.Append(IO.Path.Combine(UserHome, MCVersion & "\" & MCVersion & ".jar"))
        For Each File As String In IO.Directory.GetFiles(LibraryPath, "*.*", SearchOption.AllDirectories)
            s.Append(";" & File.Trim(vbCrLf))
        Next
        s.Append(" net.minecraft.launchwrapper.Launch ")
        s.Append(" --username " & mcUsername & " --version " & MCVersion & " --gameDir " & IO.Path.Combine(UserHome, "minecraft") & " --assetsDir assets --assetIndex " & MCVersion)
        s.Append(" --uuid " & mcUUID)
        s.Append(" --accessToken " & mcAccessToken & " --userProperties {} --userType mojang  --tweakClass cpw.mods.fml.common.launcher.FMLTweaker ")
        's.Append(" --assetsDir " & assets)
        '"C:\Program Files\Java\jre8\bin\javaw.exe" -Xms256M -Xmx2816M -XX:PermSize=256m -Djava.library.path=C:\Users\derek\Desktop\FTB\direwolf20_17\natives -Dorg.lwjgl.librarypath=C:\Users\derek\Desktop\FTB\direwolf20_17\natives -Dnet.java.games.input.librarypath=C:\Users\derek\Desktop\FTB\direwolf20_17\natives -Duser.home=C:\Users\derek\Desktop\FTB\direwolf20_17 -XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump -Djava.net.preferIPv4Stack=true -cp ;C:\Users\derek\Desktop\FTB\libraries\net\minecraftforge\forge\1.7.10-10.13.2.1231\forge-1.7.10-10.13.2.1231-universal.jar;C:\Users\derek\Desktop\FTB\libraries\net\minecraft\launchwrapper\1.11\launchwrapper-1.11.jar;C:\Users\derek\Desktop\FTB\libraries\org\ow2\asm\asm-all\5.0.3\asm-all-5.0.3.jar;C:\Users\derek\Desktop\FTB\libraries\com\typesafe\akka\akka-actor_2.11\2.3.3\akka-actor_2.11-2.3.3.jar;C:\Users\derek\Desktop\FTB\libraries\com\typesafe\config\1.2.1\config-1.2.1.jar;C:\Users\derek\Desktop\FTB\libraries\org\scala-lang\scala-actors-migration_2.11\1.1.0\scala-actors-migration_2.11-1.1.0.jar;C:\Users\derek\Desktop\FTB\libraries\org\scala-lang\scala-compiler\2.11.1\scala-compiler-2.11.1.jar;C:\Users\derek\Desktop\FTB\libraries\org\scala-lang\plugins\scala-continuations-library_2.11\1.0.2\scala-continuations-library_2.11-1.0.2.jar;C:\Users\derek\Desktop\FTB\libraries\org\scala-lang\plugins\scala-continuations-plugin_2.11.1\1.0.2\scala-continuations-plugin_2.11.1-1.0.2.jar;C:\Users\derek\Desktop\FTB\libraries\org\scala-lang\scala-library\2.11.1\scala-library-2.11.1.jar;C:\Users\derek\Desktop\FTB\libraries\org\scala-lang\scala-parser-combinators_2.11\1.0.1\scala-parser-combinators_2.11-1.0.1.jar;C:\Users\derek\Desktop\FTB\libraries\org\scala-lang\scala-reflect\2.11.1\scala-reflect-2.11.1.jar;C:\Users\derek\Desktop\FTB\libraries\org\scala-lang\scala-swing_2.11\1.0.1\scala-swing_2.11-1.0.1.jar;C:\Users\derek\Desktop\FTB\libraries\org\scala-lang\scala-xml_2.11\1.0.2\scala-xml_2.11-1.0.2.jar;C:\Users\derek\Desktop\FTB\libraries\lzma\lzma\0.0.1\lzma-0.0.1.jar;C:\Users\derek\Desktop\FTB\versions\1.7.10\1.7.10.jar;C:\Users\derek\Desktop\FTB\libraries\com\mojang\realms\1.3.5\realms-1.3.5.jar;C:\Users\derek\Desktop\FTB\libraries\org\apache\commons\commons-compress\1.8.1\commons-compress-1.8.1.jar;C:\Users\derek\Desktop\FTB\libraries\org\apache\httpcomponents\httpclient\4.3.3\httpclient-4.3.3.jar;C:\Users\derek\Desktop\FTB\libraries\commons-logging\commons-logging\1.1.3\commons-logging-1.1.3.jar;C:\Users\derek\Desktop\FTB\libraries\org\apache\httpcomponents\httpcore\4.3.2\httpcore-4.3.2.jar;C:\Users\derek\Desktop\FTB\libraries\java3d\vecmath\1.3.1\vecmath-1.3.1.jar;C:\Users\derek\Desktop\FTB\libraries\net\sf\trove4j\trove4j\3.0.3\trove4j-3.0.3.jar;C:\Users\derek\Desktop\FTB\libraries\com\ibm\icu\icu4j-core-mojang\51.2\icu4j-core-mojang-51.2.jar;C:\Users\derek\Desktop\FTB\libraries\net\sf\jopt-simple\jopt-simple\4.5\jopt-simple-4.5.jar;C:\Users\derek\Desktop\FTB\libraries\com\paulscode\codecjorbis\20101023\codecjorbis-20101023.jar;C:\Users\derek\Desktop\FTB\libraries\com\paulscode\codecwav\20101023\codecwav-20101023.jar;C:\Users\derek\Desktop\FTB\libraries\com\paulscode\libraryjavasound\20101123\libraryjavasound-20101123.jar;C:\Users\derek\Desktop\FTB\libraries\com\paulscode\librarylwjglopenal\20100824\librarylwjglopenal-20100824.jar;C:\Users\derek\Desktop\FTB\libraries\com\paulscode\soundsystem\20120107\soundsystem-20120107.jar;C:\Users\derek\Desktop\FTB\libraries\io\netty\netty-all\4.0.10.Final\netty-all-4.0.10.Final.jar;C:\Users\derek\Desktop\FTB\libraries\com\google\guava\guava\16.0\guava-16.0.jar;C:\Users\derek\Desktop\FTB\libraries\org\apache\commons\commons-lang3\3.2.1\commons-lang3-3.2.1.jar;C:\Users\derek\Desktop\FTB\libraries\commons-io\commons-io\2.4\commons-io-2.4.jar;C:\Users\derek\Desktop\FTB\libraries\commons-codec\commons-codec\1.9\commons-codec-1.9.jar;C:\Users\derek\Desktop\FTB\libraries\net\java\jinput\jinput\2.0.5\jinput-2.0.5.jar;C:\Users\derek\Desktop\FTB\libraries\net\java\jutils\jutils\1.0.0\jutils-1.0.0.jar;C:\Users\derek\Desktop\FTB\libraries\com\google\code\gson\gson\2.2.4\gson-2.2.4.jar;C:\Users\derek\Desktop\FTB\libraries\com\mojang\authlib\1.5.16\authlib-1.5.16.jar;C:\Users\derek\Desktop\FTB\libraries\org\apache\logging\log4j\log4j-api\2.0-beta9\log4j-api-2.0-beta9.jar;C:\Users\derek\Desktop\FTB\libraries\org\apache\logging\log4j\log4j-core\2.0-beta9\log4j-core-2.0-beta9.jar;C:\Users\derek\Desktop\FTB\libraries\org\lwjgl\lwjgl\lwjgl\2.9.1\lwjgl-2.9.1.jar;C:\Users\derek\Desktop\FTB\libraries\org\lwjgl\lwjgl\lwjgl_util\2.9.1\lwjgl_util-2.9.1.jar;C:\Users\derek\Desktop\FTB\libraries\org\lwjgl\lwjgl\lwjgl-platform\2.9.1\lwjgl-platform-2.9.1.jar;C:\Users\derek\Desktop\FTB\libraries\net\java\jinput\jinput-platform\2.0.5\jinput-platform-2.0.5.jar;C:\Users\derek\Desktop\FTB\libraries\tv\twitch\twitch\5.16\twitch-5.16.jar;C:\Users\derek\Desktop\FTB\libraries\tv\twitch\twitch-platform\5.16\twitch-platform-5.16.jar;C:\Users\derek\Desktop\FTB\libraries\tv\twitch\twitch-external-platform\4.5\twitch-external-platform-4.5.jar net.minecraft.launchwrapper.Launch --username Gelix --version 1.7.10 --gameDir C:\Users\derek\Desktop\FTB\direwolf20_17\minecraft --assetsDir C:\Users\derek\Desktop\FTB\assets --assetIndex 1.7.10 --uuid d6165bb9af3d4ef3adc0ddbd93eff86a --accessToken 1663c936b2744d7b9143d63a6edc1f9f --userProperties {} --userType mojang --tweakClass cpw.mods.fml.common.launcher.FMLTweaker --width 1920 --height 1040
        Return s.ToString
    End Function
    Private Shared Function setSession(Optional inputUsername As String = "", Optional inputPassword As String = "") As String
        Dim status As String = "Successful"
        Dim responsePayload = ""
        Try
            Dim placedPassword As String = ""
            If inputPassword <> "" Then
                placedPassword = otherCipher.Decrypt(inputPassword, otherCipher.machineIDLookup())
            End If

            Dim request As HttpWebRequest = DirectCast(WebRequest.Create("https://authserver.mojang.com/authenticate"), HttpWebRequest)
            'Start WebRequest
            ' request.UserAgent = atomDownloading.userAgentVer
            request.Method = "POST"
            'Method type, POST
            'Object to Upload
            ' optional /                                           //This seems to be required for minecraft despite them saying its optional.
            '          /
            ' -------- / So far this is the only encountered value
            ' -------- / This number might be increased by the vanilla client in the future
            '          /
            ' Can be an email address or player name for unmigrated accounts
            'clientToken = "TOKEN"     // Client Identifier: optional
            Dim json As String = Newtonsoft.Json.JsonConvert.SerializeObject(New With { _
             Key .agent = New With { _
              Key .name = "Minecraft", _
              Key .version = 1 _
             }, _
             Key .username = inputUsername, _
             Key .password = placedPassword _
            })
            Dim uploadBytes As Byte() = Encoding.UTF8.GetBytes(json)
            'Convert UploadObject to ByteArray
            request.ContentType = "application/json"
            'Set Client Header ContentType to "application/json"
            request.ContentLength = uploadBytes.Length
            'Set Client Header ContentLength to size of upload
            Using dataStream As Stream = request.GetRequestStream()
                'Start/Close Upload
                'Upload the ByteArray
                dataStream.Write(uploadBytes, 0, uploadBytes.Length)
            End Using
            Using response As WebResponse = request.GetResponse()
                'Start/Close Download
                Using dataStream As Stream = response.GetResponseStream()
                    'Start/Close Download Content
                    Using reader As New StreamReader(dataStream)
                        'Start/Close Reading the Stream
                        'Save Downloaded Content
                        responsePayload = reader.ReadToEnd()
                    End Using
                End Using
            End Using
            Dim responseJson As Newtonsoft.Json.Linq.JObject = JsonConvert.DeserializeObject(responsePayload)

            'Convert string to dynamic josn object
            responseJson.SelectToken("accessToken")
            If responseJson.SelectToken("accessToken") IsNot Nothing Then
                'Detect if this is an error Payload
                mcAccessToken = responseJson.SelectToken("accessToken")
                'Assign Access Token
                mcClientToken = responseJson.SelectToken("clientToken")
                'Assign Client Token
                If responseJson.SelectToken("selectedProfile.id") IsNot Nothing Then
                    'Detect if this is an error Payload
                    mcUUID = responseJson.SelectToken("selectedProfile.id")
                    'Assign User ID
                    'Assign Selected Profile Name
                    mcUsername = responseJson.SelectToken("selectedProfile.name")
                Else
                    status = "Error: WebPayLoad: Missing UUID and Username"
                End If
            ElseIf responseJson.SelectToken("errorMessage") IsNot Nothing Then
                status = "Error: WebPayLoad: " & Convert.ToString(responseJson.SelectToken("errorMessage"))
            Else
                status = "Error: WebPayLoad: Had an error and the payload was empty."
            End If
        Catch webEx As WebException
            Try
                Using dataStream As Stream = webEx.Response.GetResponseStream()
                    'Start/Close Download Content
                    Using reader As New StreamReader(dataStream)
                        'Start/Close Reading the Stream
                        'Save Downloaded Content
                        responsePayload = reader.ReadToEnd()
                    End Using
                End Using
                Dim responseJson As Newtonsoft.Json.Linq.JObject = JsonConvert.DeserializeObject(responsePayload)
                'Convert string to dynamic josn object
                status = ("Web Ex: " & Convert.ToString(responseJson.SelectToken("errorMessage")) & " - ") + webEx.Message
            Catch
                status = "Web Ex: " + webEx.Message
            End Try
        Catch ex As Exception
            If Not System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() Then
                status = "Error: Internet Disconnected. Use Offline Mode in order to play without internet."
            Else
                status = "Error: Web: " + ex.Message
            End If
        End Try
        Return status
    End Function
    ''' <summary>
    ''' Save the login data.
    ''' </summary>
    ''' <param name="username">Input the users username</param>
    ''' <param name="password">Input the users password</param>
    ''' <returns>Status of exceptions or success</returns>
    Private Shared Function setSaveData(username As String, AccessToken As String) As String
        Dim status As String = "Successful"
        Try
            My.Settings.Username = username
            My.Settings.Password = AccessToken
            My.Settings.Save()
        Catch ex As Exception

            status = "Error: Launcher Data: " + ex.Message

        End Try
        Return status
    End Function
    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        GetSettings()
    End Sub

    Private Sub GetSettings()
        My.Application.SaveMySettingsOnExit = True
        If My.Settings.LauncherPath = "" Then My.Settings.LauncherPath = New IO.FileInfo(Application.ExecutablePath).DirectoryName
        If My.Settings.Version = "" Then My.Settings.Version = "1.5.1"
        My.Settings.LauncherPath = New IO.FileInfo(Application.ExecutablePath).DirectoryName
        If My.Settings.LauncherPath.Contains("MinecraftUpdaterThingy") Then My.Settings.LauncherPath = "C:\Users\derek\Dropbox\Mitch Shared"
        Me.TextBox3.Text = My.Settings.LauncherPath
        If My.Settings.Password <> "" Then
            Me.txtPW.Text = My.Settings.Password
            mcEpw = My.Settings.Password
        End If
        If My.Settings.Username <> "" Then
            Me.txtUN.Text = My.Settings.Username
        End If

        Dim dirs As String() = IO.Directory.GetDirectories(My.Settings.LauncherPath, "*.*", SearchOption.TopDirectoryOnly)
        For Each d As String In dirs
            If System.Text.RegularExpressions.Regex.Match(d, ".*\d\.\d\.\d.*").Success = True Then
                ComboBox1.Items.Add(d.Substring(My.Settings.LauncherPath.Length + 1))
            End If

        Next
        ComboBox1.SelectedIndex() = ComboBox1.Items.Count - 1
        My.Settings.Version = ComboBox1.SelectedItem.ToString
    End Sub

    Private Sub CheckUpdates()
        Dim dbPath As String = IO.Path.Combine(My.Settings.LauncherPath, My.Settings.Version)
        Dim appVersionPath As String = IO.Path.Combine(AppPath, My.Settings.Version)
        Dim SourceFiles As New Generic.List(Of IO.FileInfo)
        For Each file As String In IO.Directory.GetFiles(dbPath, "*.*", IO.SearchOption.AllDirectories)
            Dim fi As New IO.FileInfo(file)
            SourceFiles.Add(fi)
            Dim DestPath As String = fi.DirectoryName.Substring(dbPath.Length)
            If CompareFiles(fi.FullName, appVersionPath & DestPath & "\" & fi.Name) = False Then
                If IO.Directory.Exists(appVersionPath & DestPath) = False Then
                    IO.Directory.CreateDirectory(appVersionPath & DestPath)
                End If
                If DestPath.Contains("rei_minimap") Then

                Else
                    fi.CopyTo(appVersionPath & DestPath & "\" & fi.Name, True)
                End If

            End If
        Next
        For Each File As String In IO.Directory.GetFiles(appVersionPath, "*.*", SearchOption.AllDirectories)
            Dim fi As New FileInfo(File)
            Dim found As Boolean = False
            For Each fi2 As FileInfo In SourceFiles
                If fi.Name = fi2.Name Then
                    found = True
                    Exit For
                End If
            Next
            If found = False Then
                If fi.DirectoryName.ToLower.Contains("assets") Or fi.DirectoryName.ToLower.Contains("mods") Or fi.DirectoryName.ToLower.Contains("libraries") Or fi.DirectoryName.ToLower.Contains("natives") Then
                    fi.Delete()
                    UpdateTextBoxColor("deleting: " & fi.FullName, Color.Orange)
                End If

            End If
        Next

        If IO.File.Exists(IO.Path.Combine(appVersionPath, ".minecraft\lastlogin")) = False Then
            If IO.File.Exists(IO.Path.Combine(AppPath, ".minecraft\lastlogin")) = True Then IO.File.Copy(IO.Path.Combine(AppPath, ".minecraft\lastlogin"), IO.Path.Combine(appVersionPath, ".minecraft\lastlogin"))
        End If
    End Sub
    Private Function CompareFiles(ByVal filePathOne As String, ByVal filePathTwo As String) As Boolean

        Dim fileOneByte As Integer
        Dim fileTwoByte As Integer

        Dim fileOneStream As FileStream
        Dim fileTwoStream As FileStream

        ' If user has selected the same file as file one and file two....
        If (filePathOne = filePathTwo) Then
            ' Files are the same.
            ' Me.ResultsRichTextBox.Text = "Files are the same; file one is file two."
            Return True
        End If
        If IO.File.Exists(filePathTwo) = False Then Return False
        ' Open a FileStream for each file.
        fileOneStream = New FileStream(filePathOne, FileMode.Open)
        fileTwoStream = New FileStream(filePathTwo, FileMode.Open)

        ' If the files are not the same length...
        If (fileOneStream.Length <> fileTwoStream.Length) Then
            fileOneStream.Close()
            fileTwoStream.Close()
            ' File's are not equal.
            'Me.ResultsRichTextBox.Text = "Files are not the same length; they are not equal."
            Return False
        End If

        Dim areFilesEqual As Boolean = True

        ' Loop through bytes in the files until
        '  a byte in file one <> a byte in file two
        ' OR
        '  end of the file one is reached.
        Do
            ' Read one byte from each file.
            fileOneByte = fileOneStream.ReadByte()
            fileTwoByte = fileTwoStream.ReadByte()
            If fileOneByte <> fileTwoByte Then
                ' Files are not equal; byte in file one <> byte in file two.
                ' Me.ResultsRichTextBox.Text = "Files are not equal; contents are different."
                areFilesEqual = False
                Exit Do
            End If
        Loop While (fileOneByte <> -1)

        ' Close the FileStreams.
        fileOneStream.Close()
        fileTwoStream.Close()

        Return areFilesEqual

    End Function

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        Me.Enabled = False
        If IO.Directory.Exists(IO.Path.Combine(AppPath, My.Settings.Version)) Then IO.Directory.Delete(IO.Path.Combine(AppPath, My.Settings.Version), True)
        My.Settings.Reset()
        My.Settings.Save()
        GetSettings()
        Me.Enabled = True
    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Me.Enabled = False
        CheckUpdates()
        Me.Enabled = True
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(sender As System.Object, e As System.EventArgs) Handles ComboBox1.SelectedIndexChanged
        My.Settings.Version = ComboBox1.SelectedItem.ToString
        If My.Settings.Version.Substring(2, 1) >= 6 Then
            txtPW.Show()
            txtUN.Show()
            lblUsername.Show()
            lblPassword.Show()
        Else
            txtPW.Hide()
            txtUN.Hide()
            lblUsername.Hide()
            lblPassword.Hide()
        End If
    End Sub
End Class
