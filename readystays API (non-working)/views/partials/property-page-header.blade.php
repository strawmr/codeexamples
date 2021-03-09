<script>
    const queryString = window.location.search;
    const urlParams = new URLSearchParams(queryString);
    const propKey = urlParams.get('key');
    console.log(propKey);
    var singleProp = myvr.property(propKey);
    singleProp.then(function(data) {
        console.log(data);
        console.log(data.name);
        document.getElementById('propertyKey').value = propKey;
        console.log(data.photos[0].url);
        console.log('start paul');
        // Amenities
        var amenitiesArray = data.amenities;
        var amenityStr = '<div class="row">';
        amenitiesArray.forEach(function(amenity) {
            amenityStr += '<div class="col-6 col-sm-4 col-md-3 property-item"><div class="flex"><div class="relative text-center pad small">'+ amenity + '</div></div></div>';
        });
        amenityStr += '</div>';
        document.getElementById("propAmenities").innerHTML = amenityStr;
        // Image Slider
        var photoObject = data.photos;
        let photosArray = photoObject.map(({ largeUrl }) => largeUrl);
        photosArray.forEach(function(photo) {
            var photoStr = '<div class="text-center"><img src="'+ photo + '"/></div>';
            jQuery(".gallery-slick").slick("slickAdd", photoStr);
        });
        console.log('end paul');
        console.log(data.rates);
        console.log(data.rates[0].monthlyRate);
        console.log(data.rates[0].weekNightRate);
        console.log(data.rates[0].weeklyRate);
        var propertyTitle = data.name;
        var headerImage = data.photos[0].url;
        var propertyHeadline = data.headline;
        var propertyDescription = data.description;
        var dailyRateAmt = (data.rates[0].weekNightRate/100).toFixed(2);
        var monthyRateAmt= (data.rates[0].monthlyRate/100).toFixed(2);;
        var dailyRate = '<span class="h3"><strong>Daily Rate: </strong></span>' + '<span class="h4">$'+dailyRateAmt+'</span>';
        var monthlyRate = '<span class="h3"><strong>Monthly Rate: </strong></span>' + '<span class="h4">$'+monthyRateAmt+'</span>';
        var propImg =  '<img src="'+data.photos[0].largeUrl+'"/>'
        document.getElementById('propertyTitle').innerHTML = propertyTitle;
        var url = headerImage;
        var div = document.getElementById("propertyPageHeader");
        div.style.background ="linear-gradient(rgba(57, 64, 70, 0.95), rgba(57, 64, 70, 0.45)), url("+url+")";
        document.getElementById('propertyHeadline').innerHTML = propertyHeadline;
        document.getElementById('propertyDescription').innerHTML = propertyDescription;
        document.getElementById('dailyRate').innerHTML = dailyRate;
        document.getElementById('monthlyRate').innerHTML = monthlyRate;
        document.getElementById('propImg').innerHTML = propImg;

    });
</script>
<?php if(!is_front_page()):
$page_id = get_the_ID();

if(is_single()) {
    $font_size = 'h3';
} else { $font_size = 'h2';  }

if(get_field('page_title_alignment') == "Center") {
    $align = 'mx-auto text-center';
} else {
    $align = '';
}
?>

<div id="propertyPageHeader" class="page-header default-banner container-fluid inner-shadow fadeIn wow" style="background-size:cover!important; background-position:center!important;">
    <div class="container">
        <div class="row">
            <div class="col-lg-6 <?php echo $align; ?>">
                <p id="propertyTitle" class="<?php echo $font_size; ?> white no-margin-bottom text-center-xs" data-wow-delay=".5s"></p>
            </div>
        </div>
    </div>
</div>
<?php endif; ?>