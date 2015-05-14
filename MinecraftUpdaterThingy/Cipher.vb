Imports System.Text
Imports System.Management
Imports System.Security.Cryptography
Imports System.IO
Imports System.Text.RegularExpressions

Namespace MinecraftUpdaterThingy

    '///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    '                                                             ATTENTION                                                            //
    '                                            --!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!--                                           //
    '                                                        WARNING NOT SECURE                                                        //
    '                                                                                                                                  //
    '      To secure this programs encyption                                                                                           //
    '      change the string in line                                                                                                   //
    '      string builtString = "" + builder.ToString(); // Change the "" as needed. (Strongest change)                                //
    '      or                                                                                                                          //
    '      private const string initVector = "" + "8dfn27c6vhd81j9s"; // Change the "" as needed. Uses only up to 16 characters.       //
    '      to somthing else other than                                                                                                 //
    '      "" to somthing like "mycustomsalt"                                                                                          //
    '                                                                                                                                  //
    '                                                        WARNING NOT SECURE                                                        //
    '                                            --!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!--                                           //
    '                                                                                                                                  //
    '///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    '//////////////////////////////////
    ' Start - Encrypt Section
    Class otherCipher
        '//////////////////////////////////
        ' Start - Get Machine Id for Encrypt
        Friend Shared Function machineIDLookup() As String
            'Edit hardware here (if you know what your doing) to make this a different encryption id.
            Dim theHardware As String()() = {New String() {"Win32_BaseBoard", "SerialNumber"}, New String() {"Win32_Processor", "ProcessorId"}}
            Dim x As Integer = theHardware.Length
            Dim builder As New StringBuilder()
            For Each entry As String() In theHardware
                Try
                    Dim searcher As New ManagementObjectSearcher("SELECT * FROM " & entry(0))
                    For Each item As ManagementObject In searcher.[Get]()
                        Dim obj As [Object] = item(entry(1))
                        builder.Append(Convert.ToString(obj))
                    Next
                Catch ex As Exception
                    If ex.Message = "Not found " Then
                        x -= 1
                        If x <= 0 Then
                            Throw New System.Exception("Cipher: Could not detect a number to use from any of the system devices.")
                        End If
                    Else
                        Throw New System.Exception(ex.Message)
                    End If
                End Try
            Next
            Dim builtString As String = "" & builder.ToString()
            ' Change the "" as needed. (Strongest change) 
            builtString = Regex.Replace(builtString, "[^a-z0-9A-Z]", "")
            builtString = builtString.ToLower()
            Return builtString
        End Function
        ' End
        '//////////////////////////////////

        '//////////////////////////////////
        ' Start - Encrypt Code
        ' This constant string is used as a "salt" value for the PasswordDeriveBytes function calls.
        ' This size of the IV (in bytes) must = (keysize / 8).  Default keysize is 256, so the IV must be
        ' 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.
        Private Const initVector As String = "Ilikepieand81235" & "8dfn27c6vhd81j9s"
        ' Change the "" as needed. Uses only up to 16 characters.
        Private Const vectorInt As Integer = 16
        ' Max Character length of string. Don't change unless you know what your doing.
        ' This constant is used to determine the keysize of the encryption algorithm.
        Private Const keysize As Integer = 256

        Friend Shared Function Encrypt(plainText As String, passPhrase As String) As String
            Try
                Dim initVectorBytes As Byte() = Encoding.UTF8.GetBytes(initVector.Substring(0, vectorInt))
                Dim plainTextBytes As Byte() = Encoding.UTF8.GetBytes(plainText)
                Dim password As New PasswordDeriveBytes(passPhrase, Nothing)
                Dim keyBytes As Byte() = password.GetBytes(keysize \ 8)
                Dim symmetricKey As New RijndaelManaged()
                symmetricKey.Mode = CipherMode.CBC
                Dim encryptor As ICryptoTransform = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes)
                Dim cipherTextBytes As Byte()
                Using memoryStream As New MemoryStream()
                    Using cryptoStream As New CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write)
                        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length)
                        cryptoStream.FlushFinalBlock()
                        cipherTextBytes = memoryStream.ToArray()
                    End Using
                End Using
                Return Convert.ToBase64String(cipherTextBytes)
            Catch ex As Exception
                System.Windows.Forms.MessageBox.Show("Something went wrong with the Encryption Code. Deleting the User Data to atempt to prevent further issues." & vbLf & vbLf & "Error:" & vbLf & """" & ex.Message & """", "Encryption Error")
                'AtomLauncher.userData.Clear()
                'atomFileData.saveDictonary(atomFileData.config("dataLocation") + atomFileData.config("userDataName"), AtomLauncher.userData, True)
                Return ""
            End Try
        End Function

        Friend Shared Function Decrypt(cipherText As String, passPhrase As String) As String
            Try
                Dim initVectorBytes As Byte() = Encoding.ASCII.GetBytes(initVector.Substring(0, vectorInt))
                Dim cipherTextBytes As Byte() = Convert.FromBase64String(cipherText)
                Dim password As New PasswordDeriveBytes(passPhrase, Nothing)
                Dim keyBytes As Byte() = password.GetBytes(keysize \ 8)
                Dim symmetricKey As New RijndaelManaged()
                symmetricKey.Mode = CipherMode.CBC
                Dim decryptor As ICryptoTransform = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes)
                Dim plainTextBytes As Byte()
                Dim decryptedByteCount As Integer
                Using memoryStream As New MemoryStream(cipherTextBytes)
                    Using cryptoStream As New CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read)
                        plainTextBytes = New Byte(cipherTextBytes.Length - 1) {}
                        decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length)
                    End Using
                End Using
                Return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount)
            Catch ex As Exception
                System.Windows.Forms.MessageBox.Show("Something went wrong with the Decryption Code. Deleting the User Data to atempt to prevent further issues." & vbLf & vbLf & "Error:" & vbLf & """" & ex.Message & """", "Decryption Error")
                'AtomLauncher.userData.Clear()
                'atomFileData.saveDictonary(atomFileData.config("dataLocation") + atomFileData.config("userDataName"), AtomLauncher.userData, True)
                Return ""
            End Try
        End Function
        ' End
        '//////////////////////////////////
    End Class
    ' End
    '//////////////////////////////////
End Namespace