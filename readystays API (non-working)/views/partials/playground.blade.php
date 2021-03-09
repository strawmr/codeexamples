<?php
/*
  $url = "https://api.myvr.com/v1/properties/?limit=100";
  $apiKey = 'LIVE_2952fefbc7201c637a147ed59f9353a2'; // should match with Server key
  $headers = array(
     'Content-Type:application/json',
     'Authorization: Basic' . $apiKey
  );
  // Send request to Server
  $ch = curl_init($url);
  // To save response in a variable from server, set headers;
  curl_setopt($ch, CURLOPT_RETURNTRANSFER, 1);
  //curl_setopt($ch, CURLOPT_HTTPHEADER, $headers);
  curl_setopt($ch, CURLOPT_USERPWD, "$apiKey");
  // Get response
  $response = curl_exec($ch);
  // Decode
  $result = json_decode($response);
  
  print_r($result);
*/
?>

<script type="text/javascript" src="https://static.myvr.com/public/myvrjs/v1/myvr.js"></script>

<script type="text/javascript">
  myvr.init("LIVE_2952fefb91e1df9e93818593d112771a");
</script>

<script>
  var vrEmptyCall = myvr.properties({
    limit:1000,
  });
  //console.log(vrEmptyCall);
  
for (let [key, value] of Object.entries(vrEmptyCall)) {
  console.log(`${key}: ${value}`);
}
</script>

<div class="container page-container">
  <div class="row">
    <div class="col-12 mx-auto inner-page page-content-padding mx-auto wow fadeIn" data-wow-duration="1s" data-wow-delay="500ms">  
    </div>
  </div>  
</div>