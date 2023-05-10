Imports System.Net
Imports System.Xml
Imports System.IO
Imports System.Text
Imports Newtonsoft.Json
Imports System.Globalization
Imports System.Collections.Specialized
Imports RestSharp
Imports System.Security.Cryptography

Public Class cls_LDV_AMZ
    Private endpoint_url As String = "https:" & "//sandbox.sellingpartnerapi-eu.amazon.com"

    Private IAMUserAccessKey As String = "Your AWS Access Key Here"
    Private IAMUserSecretKey As String = "Your AWS Secret Key Here"
    '*** La durata di refresh_token è annuale
    Private RefreshToken As String = "Your Refresh Token"

    Private LWAClinetId As String = "Your LWA ClientId"
    Private LWAClientSecret As String = "Your LWA Client Secret"

    Public access_token As New cls_access_token

    Public Class cls_access_token
        Public Property access_token As String
        Public Property refresh_token As String
        Public Property token_type As String
        Public Property expires_in As Integer
    End Class

    Public Class QueryStringBuilder
        Public Shared Function BuildQuery(ByVal nvc As NameValueCollection) As String
            Return String.Join("&", nvc.AllKeys.[Select](Function(key) String.Format("{0}={1}", System.Net.WebUtility.UrlEncode(key), System.Net.WebUtility.UrlEncode(nvc(key)))))
        End Function
    End Class


    Private Function SendRequest(ByVal url As System.Uri,
                                 ByVal contentType As String, ByVal jsonString As String, ByVal method As String,
                                 ByVal user As String, ByVal password As String,
                                 ByRef response As String) As Boolean
        Try
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                Function(se As Object, cert As System.Security.Cryptography.X509Certificates.X509Certificate,
                         chain As System.Security.Cryptography.X509Certificates.X509Chain, sslerror As System.Net.Security.SslPolicyErrors) True
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12

            Dim request As WebRequest
            Dim jsonDataBytes As Byte() = Encoding.UTF8.GetBytes(jsonString)
            Dim credenziali = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("AWS " & user & ":" & password))

            request = WebRequest.Create(url)
            request.Method = method
            request.ContentLength = jsonDataBytes.Length
            request.ContentType = contentType

            If user <> "" Then
                request.Headers.Add("x-amz-access-token", access_token.access_token)
                request.Headers.Add("X-Amz-Content-Sha256", "beaead3198f7da1e70d03ab969765e0821b24fc913697e929e726aeaebf0eba3")
                request.Headers.Add("X-Amz-Date", "20220927T090611Z")
                request.Headers.Add("Authorization", "AWS4-HMAC-SHA256 Credential=AWSAccessKey/20220927/eu-west-1/execute-api/aws4_request, SignedHeaders=content-type;host;x-amz-access-token;x-amz-content-sha256;x-amz-date, Signature=331d734c58b5d98360bcaca6f4abf77042514df2df9758be59c07485c84f109b")

                'request.Headers.Add("Authorization", credenziali)
                'request.Credentials = New NetworkCredential(user, password)
            End If

            Using requestStream = request.GetRequestStream
                requestStream.Write(jsonDataBytes, 0, jsonDataBytes.Length)
                requestStream.Close()

                Using responseStream = request.GetResponse.GetResponseStream
                    Using reader As New StreamReader(responseStream)
                        response = reader.ReadToEnd()
                    End Using
                End Using
            End Using
        Catch ex As Exception
            Debug.Print(ex.Message.ToString)
            Return False
        End Try

        Return True
    End Function
    Public Function getToken() As Boolean
        Dim url As System.Uri = New Uri("https:" & "//api.amazon.co.uk/auth/o2/token")
        Dim response As String = ""
        Dim GetParameters As NameValueCollection
        Dim request, grant_type As String

        Try
            grant_type = "refresh_token"

            GetParameters = New NameValueCollection From {
                    {"grant_type", grant_type},
                    {"refresh_token", RefreshToken},
                    {"client_id", LWAClinetId},
                    {"client_secret", LWAClientSecret}
                }
            request = QueryStringBuilder.BuildQuery(GetParameters)

            SendRequest(url, "application/x-www-form-urlencoded", request, "POST", "", "", response)

            Debug.Print(response)

            access_token = Newtonsoft.Json.JsonConvert.DeserializeObject(Of cls_access_token)(response)
        Catch ex As Exception
            Return False
        End Try

        Return True
    End Function

    Private Function ToDateSQL(ByVal s As String, Optional ByVal WithTime As Boolean = False) As String
        If s = "" Then
            s = "null"
        Else
            If WithTime Then
                s = String.Format("{0:yyyy-MM-dd HH:mm:ss}", Convert.ToDateTime(s))
                s = s.Replace(".", ":")
            Else
                s = String.Format("{0:yyyy-MM-dd}", Convert.ToDateTime(s))
            End If
        End If

        Return s
    End Function

    Private Shared Function HmacSHA256(ByVal data As String, ByVal key As Byte()) As Byte()
        Dim kha As KeyedHashAlgorithm = New HMACSHA256(key)
        kha.Initialize()

        Return kha.ComputeHash(Encoding.UTF8.GetBytes(data))
    End Function

    Private Shared Function vbSHA256(ByVal data As String) As String
        Dim hash = New System.Security.Cryptography.SHA256Managed().ComputeHash(System.Text.Encoding.UTF8.GetBytes(data))

        Return BitConverter.ToString(hash).Replace("-", "").ToLower
    End Function

    Private Shared Function getSignatureKey(ByVal stringToSign As String, ByVal key As String, ByVal dateStamp As String, ByVal regionName As String, ByVal serviceName As String)
        Dim kSecret As Byte() = Encoding.UTF8.GetBytes(("AWS4" & key))
        Dim kDate As Byte() = HmacSHA256(dateStamp, kSecret)
        Dim kRegion As Byte() = HmacSHA256(regionName, kDate)
        Dim kService As Byte() = HmacSHA256(serviceName, kRegion)
        Dim kSigning As Byte() = HmacSHA256("aws4_request", kService)

        Return ToHex(HmacSHA256(stringToSign, kSigning))
    End Function

    Public Shared Function ToHex(ByVal data As Byte()) As String
        Dim sb As StringBuilder = New StringBuilder()

        For i As Integer = 0 To data.Length - 1
            sb.Append(data(i).ToString("x2", CultureInfo.InvariantCulture))
        Next

        Return sb.ToString()
    End Function

    Private Function get_Amz_Date() As String
        Dim dt As Date = DateAdd(DateInterval.Hour, -2, Now())
        Dim hh As Integer = dt.Hour
        Dim mm As Integer = dt.Minute
        Dim ss As Integer = dt.Second
        Dim gg As String = ToDateSQL(dt.ToShortDateString).Replace("-", "")

        Dim Amz_Date As String = gg & "T" & hh.ToString("00") & mm.ToString("00") & ss.ToString("00") & "Z"

        Return Amz_Date
    End Function

    Public Sub getRates()
        Dim client = New RestClient("https:" & "//sandbox.sellingpartnerapi-eu.amazon.com/shipping/v2/shipments/rates")
        Dim Signature, signing_key, body, canonicalHeaders, requestHashedPayload, canonicalRequest, requestHashedCanonicalRequest, stringToSign, credentialScopeStr As String
        Dim Amz_Date As String = get_Amz_Date()

        client.Timeout = -1

        Dim request = New RestRequest(Method.POST)

        body = "{""shipTo"":{""name"":""A3"",""addressLine1"":""SWA Test Account"",""addressLine2"":""SWA Test Account"",""addressLine3"":""SWA Test Account"",""stateOrRegion"":"""",""postalCode"":""DN1 1QZ"",""city"":""Doncaster"",""countryCode"":""GB"",""email"":""test+test@amazon.com"",""phoneNumber"":""444-444-4444""},""shipFrom"":{""name"":""A1"",""addressLine1"":""4 Neal Street"",""stateOrRegion"":"""",""postalCode"":""WC2H 9QL"",""city"":""London"",""countryCode"":""GB"",""email"":""test+test@amazon.com"",""phoneNumber"":""444-444-4444""},""packages"":[{""dimensions"":{""length"":3.14,""width"":3.14,""height"":3.14,""unit"":""INCH""},""weight"":{""unit"":""KILOGRAM"",""value"":3.14159},""items"":[{""quantity"":1,""itemIdentifier"":""V-02"",""description"":""Sundries"",""isHazmat"":false,""weight"":{""unit"":""KILOGRAM"",""value"":1.14159}},{""quantity"":1,""itemIdentifier"":""V-01"",""description"":""Sundries"",""isHazmat"":false,""weight"":{""unit"":""KILOGRAM"",""value"":1.14159}}],""insuredValue"":{""unit"":""GBP"",""value"":29.98},""packageClientReferenceId"":""abcd""}],""channelDetails"":{""channelType"":""AMAZON"",""amazonOrderDetails"":{""orderId"":""113-3080243-4028255""}}}"

        requestHashedPayload = vbSHA256(body)

        request.AddHeader("Content-Type", "application/json")
        request.AddHeader("x-amz-access-token", access_token.access_token)
        'request.AddHeader("X-Amz-Content-Sha256", requestHashedPayload)
        request.AddHeader("X-Amz-Date", Amz_Date)

        'signing_key = getSignatureKey(IAMUserSecretKey, Strings.Left(Amz_Date, 8), "eu-west-1", "execute-api")

        canonicalHeaders = "content-type:application/json" & vbLf
        canonicalHeaders &= "host:sandbox.sellingpartnerapi-eu.amazon.com" & vbLf
        canonicalHeaders &= "x-amz-date:" & Amz_Date

        canonicalRequest = "POST" & vbLf                                  'HTTPRequestMethod 
        canonicalRequest &= "/shipping/v2/shipments/rates" & vbLf         'CanonicalURI 
        canonicalRequest &= "" & vbLf                                     'CanonicalQueryString 
        canonicalRequest &= canonicalHeaders & vbLf                       'CanonicalHeaders 
        canonicalRequest &= "" & vbLf
        canonicalRequest &= "content-type;host;x-amz-date" & vbLf         'SignedHeaders 
        canonicalRequest &= requestHashedPayload

        requestHashedCanonicalRequest = vbSHA256(canonicalRequest)

        credentialScopeStr = Strings.Left(Amz_Date, 8) & "/eu-west-1/execute-api/aws4_request"

        stringToSign = "AWS4-HMAC-SHA256" & vbLf
        stringToSign &= Amz_Date & vbLf
        stringToSign &= credentialScopeStr & vbLf
        stringToSign &= requestHashedCanonicalRequest

        'Signature = BitConverter.ToString(HmacSHA256(stringToSign, Encoding.UTF8.GetBytes(signing_key))).Replace("-", "").ToLower
        Signature = getSignatureKey(stringToSign, IAMUserSecretKey, Strings.Left(Amz_Date, 8), "eu-west-1", "execute-api")

        request.AddHeader("Authorization", "AWS4-HMAC-SHA256 Credential=" & IAMUserAccessKey & "/" & Strings.Left(Amz_Date, 8) & "/eu-west-1/execute-api/aws4_request, SignedHeaders=content-type;host;x-amz-date, Signature=" & Signature)

        request.AddParameter("application/json", body, ParameterType.RequestBody)

        Dim response As IRestResponse = client.Execute(request)

        Console.WriteLine(response.Content)
    End Sub

End Class
