<?php
  // Get base array of all properties with cURL
  
  $url = "https://api.myvr.com/v1/properties/?limit=100";
  $apiKey = 'INSERT KEY'; // should match with Server key
  $headers = array(
     'Content-Type:application/json',
     'Authorization: Basic' . $apiKey
  );
  // Send request to Server
  $ch = curl_init($url);
  // To save response in a variable from server, set headers;
  curl_setopt($ch, CURLOPT_RETURNTRANSFER, 1);
  curl_setopt($ch, CURLOPT_USERPWD, "$apiKey");
  // Get response
  $response = curl_exec($ch);
  // Decode
  $allProperties = json_decode($response);
  $allProperties = $allProperties->results;
?>

<div class="container content-padding">
  <div class="row">
    <div class="col-md-12 text-center">
      <p class="h2 secondary">All Listings (API)</p>
    </div>
  </div>

  <div class="row">
  <?php foreach($allProperties as $property ): ?>
    <?php
      // Within each property, set a secondary cURL to get the property photos for the background image
      $propertyID = $property->key;
      $url = "https://api.myvr.com/v1/photos/?property=" . $propertyID;
      $apiKey = 'LIVE_2952fefbc7201c637a147ed59f9353a2'; // should match with Server key
      $headers = array(
         'Content-Type:application/json',
         'Authorization: Basic' . $apiKey
      );
      // Send request to Server
      $ch = curl_init($url);
      // To save response in a variable from server, set headers;
      curl_setopt($ch, CURLOPT_RETURNTRANSFER, 1);
      curl_setopt($ch, CURLOPT_USERPWD, "$apiKey");
      // Get response
      $response = curl_exec($ch);
      // Decode
      $propertyPhotos = json_decode($response);
      $propertyPhotos = $propertyPhotos->results;
      if($propertyPhotos[0]->downloadUrl) {
        $backgroundPhoto = $propertyPhotos[0]->downloadUrl;
      }
      $backgroundPhoto = 'https://' . $backgroundPhoto;
    ?>
    <div class="col-sm-6 col-lg-4 pad fadeIn wow">
      <div class="product-container full-height">
        <a href="#" class="absolute-link"></a>
        <div class="min-height-smaller" style="background:url(<?php echo $backgroundPhoto; ?>); background-size:cover; background-position:center;"></div>
        <p class="sub-heading pad text-center"><?php echo $property->baseRate->property->name ?></p>
      </div>
    </div>
  <?php endforeach; ?>       
  </div>
</div>