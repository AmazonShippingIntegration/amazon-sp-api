using System;
using System.Text;
using System.Globalization;
using System.Security.Cryptography;
using RestSharp;


public class AmazonShippingAgent
{
    public string IAMUserAccessKey { get; }
    public string IAMUserSecretKey { get; }

    public AmazonShippingAgent(string userAccessKey, string userSecretKey)
    {
        IAMUserAccessKey = userAccessKey;
        IAMUserSecretKey = userSecretKey;
    }

    public IRestResponse POST_Request(string URL, string jsonRequest, string accessToken, string host, string URI)
    {
        try
        {
            RestClient client = new RestClient(URL);
            client.Timeout = -1;

            RestRequest request = new RestRequest(Method.POST);

            string Signature, body, canonicalHeaders, requestHashedPayload, canonicalRequest, requestHashedCanonicalRequest, stringToSign, credentialScopeStr;
            string Amz_Date = Get_Amz_Date();
            string shortDate = Amz_Date.Substring(0, 8);
            string LF = "\n";

            body = jsonRequest;
            
            //Hash of payload
            requestHashedPayload = SHA256(body);

            //Adding Headers
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("x-amz-access-token", accessToken);
            request.AddHeader("X-Amz-Date", Amz_Date);

            //Declaring Canonical Headers
            canonicalHeaders = "content-type:application/json" + LF;
            canonicalHeaders += "host:" + host + LF;
            canonicalHeaders += "x-amz-date:" + Amz_Date;

            //Creating Canonical Request
            canonicalRequest = "POST" + LF;                                  // HTTP RequestMethod 
            canonicalRequest += URI + LF;                                    // CanonicalURI
            canonicalRequest += "" + LF;                                     // CanonicalQueryString 
            canonicalRequest += canonicalHeaders + LF;                       // Adding Canonical Headers 
            canonicalRequest += "" + LF;                                     // 
            canonicalRequest += "content-type;host;x-amz-date" + LF;         // SignedHeaders 
            canonicalRequest += requestHashedPayload;                        // Adding hashed payload

            //Hash of Canonical Request
            requestHashedCanonicalRequest = SHA256(canonicalRequest);
            
            //Scopes
            credentialScopeStr = shortDate + "/eu-west-1/execute-api/aws4_request"; //Strings.Left(Amz_Date, 8)

            //Creating string to sign
            stringToSign = "AWS4-HMAC-SHA256" + LF;
            stringToSign += Amz_Date + LF;
            stringToSign += credentialScopeStr + LF;
            stringToSign += requestHashedCanonicalRequest;

            //Creating actual Signature
            Signature = GetSignatureKey(stringToSign, IAMUserSecretKey, shortDate, "eu-west-1", "execute-api");

            //Adding Authorization Header
            request.AddHeader("Authorization", "AWS4-HMAC-SHA256 Credential=" + IAMUserAccessKey + "/" + shortDate + "/eu-west-1/execute-api/aws4_request, SignedHeaders=content-type;host;x-amz-date, Signature=" + Signature);

            //API POST
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            return response;
        }
        catch (Exception e) 
        {
            throw new Exception("POST_Request:" + e.Message, e);
        }
    }

    private string Get_Amz_Date()
    {
        return DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss").Replace("-", "").Replace(":", "") + "Z";
    }

    private static byte[] HMAC_SHA256(string data, byte[] key)
    {
        try
        {
            KeyedHashAlgorithm kha = new HMACSHA256(key);
            kha.Initialize();

            return kha.ComputeHash(Encoding.UTF8.GetBytes(data));
        }
        catch (Exception e)
        {
            throw new Exception("HMAC_SHA256:" + e.Message, e);
        }
    }

    private static string SHA256(string data)
    {
        try
        {
            byte[] hash = new SHA256Managed().ComputeHash(Encoding.UTF8.GetBytes(data));

            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
        catch (Exception e)
        {
            throw new Exception("SHA256:" + e.Message, e);
        }
    }

    private static string GetSignatureKey(string stringToSign, string key, string dateStamp, string regionName, string serviceName)
    {
        byte[] kSecret = Encoding.UTF8.GetBytes(("AWS4" + key));
        byte[] kDate = HMAC_SHA256(dateStamp, kSecret);
        byte[] kRegion = HMAC_SHA256(regionName, kDate);
        byte[] kService = HMAC_SHA256(serviceName, kRegion);
        byte[] kSigning = HMAC_SHA256("aws4_request", kService);

        return ToHex(HMAC_SHA256(stringToSign, kSigning));
    }

    public static string ToHex(byte[] data)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i <= data.Length - 1; i++)
        {
            sb.Append(data[i].ToString("x2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

}
