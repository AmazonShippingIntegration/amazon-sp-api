<?php

//https://sellingpartnerapi-eu.amazon.com/shipping/v2/shipments/rates

$response = '{
    "access_token": "YOUR ACCESS TOKEN HERE.",
    "refresh_token": "YOUR REFRESH TOKEN HERE.",
    "token_type": "bearer",
    "expires_in": 3600
}';
    $response = json_decode($response, true);
    $api_token = $response['access_token'];

	/*processing token api ends here*/
	
	/*processing getRates api starts here*/
	
	$api_json_data = '{
      "shipTo": {
        "name": "Ship from Test",
        "addressLine1": "stringShipFrom Line 1",
        "addressLine2": "ShipFrom Line 2",
        "addressLine3": "ShipFrom Line 3",
        "companyName": "YashRetail",
        "stateOrRegion": "Haryana",
        "city": "Rewari",
        "countryCode": "IN",
        "postalCode": "560034",
        "email": "test@gmail.com",
        "phoneNumber": "9729991800"
      },
      "shipFrom": {
        "name": "ShipTo Test",
        "addressLine1": "ShipFrom Line 1",
        "addressLine2": "ShipFrom Line 2",
        "addressLine3": "ShipFrom Line 3",
        "companyName": "YashRetail",
        "stateOrRegion": "Haryana",
        "city": "Rewari",
        "countryCode": "IN",
        "postalCode": "500032",
        "email": "test@gmail.com",
        "phoneNumber": "9729991800"
      },
      "returnTo": {
        "name": "ReturnTo Test",
        "addressLine1": "Returnto Line 1",
        "addressLine2": "Returnto Line 2",
        "addressLine3": "Returnto Line 3",
        "companyName": "YashRetail",
        "stateOrRegion": "Haryana",
        "city": "Rewari",
        "countryCode": "IN",
        "postalCode": "600034",
        "email": "info@yashretail.com",
        "phoneNumber": "9729991800"
      },
      "shipDate": "2022-09-28T09:27:05Z",
      "packages": [
        {
          "dimensions": {
            "length": 1,
            "width": 2,
            "height": 2,
            "unit": "INCH"
          },
          "weight": {
            "unit": "GRAM",
            "value": 250
          },
          "insuredValue": {
            "value": 10,
            "unit": "INR"
          },
          "isHazmat": false,
          "sellerDisplayName": "Dinekaki label display name",
          "charges": [
            {
              "amount": {
                "value": 2,
                "unit": "INR"
              },
              "chargeType": "TAX"
            }
          ],
          "packageClientReferenceId": "fc",
          "items": [
            {
              "itemValue": {
                "value": 10,
                "unit": "INR"
              },
              "description": "Description of the item1.",
              "itemIdentifier": "ITEM-26495734098",
              "quantity": 1,
              "weight": {
                "unit": "GRAM",
                "value": 150
              },
              "isHazmat": false,
              "productType": "Health Care items",
              "invoiceDetails": {
                "invoiceNumber": "0092590411-IV-cancelling2",
                "invoiceDate": "2021-02-11T09:27:05Z"
              }
            }
          ]
        }
      ],
      "valueAddedServices": {
        "collectOnDelivery": {
          "amount": {
            "value": 10,
            "unit": "INR"
          }
        }
      },
      "taxDetails": [
      {
        "taxType": "GST",
        "taxRegistrationNumber": "06AABCY2351G1ZJ"
      }
     ],
      "channelDetails": {
        "channelType": "EXTERNAL"
      }
    }';

    $amz_date_time = getAmazonDateTime();
    $sign = calcualteAwsSignatureAndReturnHeaders($api_token, $api_json_data, $amz_date_time);
   
    $sign = implode(',',$sign);

    $curl = curl_init();
    
    curl_setopt_array($curl, array(
      CURLOPT_URL => 'https://sellingpartnerapi-eu.amazon.com/shipping/v2/shipments/rates',
      CURLOPT_RETURNTRANSFER => true,
      CURLOPT_ENCODING => '',
      CURLOPT_MAXREDIRS => 10,
      CURLOPT_TIMEOUT => 0,
      CURLOPT_FOLLOWLOCATION => true,
      CURLOPT_HTTP_VERSION => CURL_HTTP_VERSION_1_1,
      CURLOPT_CUSTOMREQUEST => 'POST',
      CURLOPT_POSTFIELDS => $api_json_data,
      CURLOPT_HTTPHEADER => array(
        'x-amz-access-token: '.$api_token,
        //'X-Amz-Content-Sha256: beaead3198f7da1e70d03ab969765e0821b24fc913697e929e726aeaebf0eba3',
        'X-Amz-Date: '.$amz_date_time,
        $sign,
        'Content-Type: application/json'
      ),
    ));
    
    
    $response = curl_exec($curl);
    curl_close($curl);
    echo $response;



function calcualteAwsSignatureAndReturnHeaders($reqToken, $data, $amz_date_time){

$host               = "sellingpartnerapi-eu.amazon.com";
$accessKey          = 'PUT YOUR ACCESS KEY';
$secretKey          = 'PUT YOUR SECRET KEY';
$region             = "eu-west-1";
$service            = "execute-api";
$requestUrl         = "https://sellingpartnerapi-eu.amazon.com/shipping/v2/shipments/rates";
$uri                = '/shipping/v2/shipments/rates';
$httpRequestMethod  = 'POST';
$debug              = FALSE;

    $terminationString  = 'aws4_request';
    $algorithm      = 'AWS4-HMAC-SHA256';
    $phpAlgorithm       = 'sha256';
    $canonicalURI       = $uri;
    $canonicalQueryString   = '';
    $signedHeaders = 'content-type;host;x-amz-date';

    $reqDate = getAmazonDate();
    $reqDateTime = $amz_date_time;

    // Create signing key
    $kSecret = $secretKey;
    $kDate = hash_hmac($phpAlgorithm, $reqDate, 'AWS4' . $kSecret, true);
    $kRegion = hash_hmac($phpAlgorithm, $region, $kDate, true);
    $kService = hash_hmac($phpAlgorithm, $service, $kRegion, true);
    $kSigning = hash_hmac($phpAlgorithm, $terminationString, $kService, true);

    // Create canonical headers
    $canonicalHeaders = array();
    $canonicalHeaders[] = 'content-type:application/json';
    $canonicalHeaders[] = 'host:' . $host;
    $canonicalHeaders[] = 'x-amz-date:' . $reqDateTime;
    $canonicalHeadersStr = implode("\n", $canonicalHeaders);

    // Create request payload
    $requestHasedPayload = hash($phpAlgorithm, $data);
    //$requestHasedPayload = Hex(SHA256Hash($data));

    // Create canonical request
    $canonicalRequest = array();
    $canonicalRequest[] = $httpRequestMethod;
    $canonicalRequest[] = $canonicalURI;
    $canonicalRequest[] = $canonicalQueryString;
    $canonicalRequest[] = $canonicalHeadersStr . "\n";
    $canonicalRequest[] = $signedHeaders;
    $canonicalRequest[] = $requestHasedPayload;
    $requestCanonicalRequest = implode("\n", $canonicalRequest);
    $requestHasedCanonicalRequest = hash($phpAlgorithm, utf8_encode($requestCanonicalRequest));
    if($debug){
        /*echo "<h5>Canonical to string</h5>";
        echo "<pre>";
        echo $requestCanonicalRequest;
        echo "</pre>";*/
    }
    
    // Create scope
    $credentialScope = array();
    $credentialScope[] = $reqDate;
    $credentialScope[] = $region;
    $credentialScope[] = $service;
    $credentialScope[] = $terminationString;
    $credentialScopeStr = implode('/', $credentialScope);

    // Create string to signing
    $stringToSign = array();
    $stringToSign[] = $algorithm;
    $stringToSign[] = $reqDateTime;
    $stringToSign[] = $credentialScopeStr;
    $stringToSign[] = $requestHasedCanonicalRequest;
    $stringToSignStr = implode("\n", $stringToSign);
    if($debug){
        /*echo "<h5>String to Sign</h5>";
        echo "<pre>";
        echo $stringToSignStr;
        echo "</pre>";*/
    }

    // Create signature
    $signature = hash_hmac($phpAlgorithm, $stringToSignStr, $kSigning);

 	// Create authorization header
    $authorizationHeader = array();
    $authorizationHeader[] = 'Credential=' . $accessKey . '/' . $credentialScopeStr;
    $authorizationHeader[] = 'SignedHeaders=' . $signedHeaders;
    $authorizationHeader[] = 'Signature=' . ($signature);
    $authorizationHeaderStr = $algorithm . ' ' . implode(', ', $authorizationHeader);


    // Request headers 
    $headers = array(); 
    $headers[] = 'authorization: '.$authorizationHeaderStr; 
    //$headers[] = 'X-Amz-Content-Sha256='.$requestHasedPayload; 
    $headers[] = 'content-length='.strlen($data); 
    $headers[] = 'content-type=application/json'; 
    $headers[] = 'host='. $host; 
    $headers[] = 'x-amz-date='. $reqDateTime; 
    $headers[] = 'x-amz-access-token='. $reqToken;
    $headers[] = 'x-amzn-shipping-business-id=AmazonShipping_IN';


    return $headers;
}
function getAmazonDate(){
    
    $currentDateTime = new DateTime('UTC');
    $reqDate = $currentDateTime->format('Ymd');
    return $reqDate;
}
function getAmazonDateTime(){
    
    $currentDateTime = new DateTime('UTC');
    $reqDate = $currentDateTime->format('Ymd');
    $reqDateTime = $currentDateTime->format('Ymd\THis\Z');
    return $reqDateTime;
}
?>
