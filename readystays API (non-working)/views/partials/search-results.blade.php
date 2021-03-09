<div class="container content-padding">
<!--    <div class="row">
        <div class="col-md-12 text-center">
            <p class="h2 secondary">Search Availability</p>
        </div>
    </div> -->

    <div class="container">
         <div class="properties row">

         </div>
    </div>


</div>

<script>
    var myvr;
    myvr.init("LIVE_2952fefb91e1df9e93818593d112771a");
    jQuery(document).ready(function ($) {
        // Your code here
        var $properties = $(".properties");
        myvr.properties().then(function (data) {
            console.log(data);
            data.results.forEach(function (property) {
                console.log(property.name);
                $properties.append(
                    '<div class="col-12 col-md-6 col-lg-4">'+
                    '<div class="min-height-smaller" style="background:url(' +
                    property.photo.url +
                    '); background-size:cover; background-position:center;"></div>' +
                    '<p class="sub-heading">' +
                    property.name +
                    "</p>" +
                    property.headline +
                    "<br><br>" +
                    ' <a class="primary-btn booking-link" data-property-id="' +
                    property.key +
                    '" href="/property/?p=' +
                    property.slug+"&key="+property.key +
                    '">Pricing and Details</a> <br><br></div>'
                );
            });
        });

    });
</script>


