<div class="container content-padding">
    <div class="row">
        <div class="col-md-12 text-center">
            <p class="h2 secondary">Available Properties</p>
        </div>
    </div>

    <div class="container">
        <div id="noResults" class="properties row">

        </div>
    </div>


</div>

<script>
    var searching = function($) {
        return (function(paramsArray) {
            let params = {};

            for (let i = 0; i < paramsArray.length; ++i)
            {
                let param = paramsArray[i]
                    .split('=', 2);

                if (param.length !== 2)
                    continue;

                params[param[0]] = decodeURIComponent(param[1].replace(/\+/g, " "));
            }
            return params;
        })(window.location.search.substr(1).split('&'))
    }()

    var myvr;
    myvr.init("LIVE_2952fefb91e1df9e93818593d112771a");
    jQuery(document).ready(function ($) {
        // Your code here
        var $properties = $(".properties");
        myvr.properties({bedrooms:searching.rooms,cities:searching.cities,startDate:searching.checkin,endDate:searching.checkout}).then(function (data) {
            console.log('searching city is' + searching.cities);
            console.log(data.results);
            if(data.results.length != 0) { // first check for an empty array
              let noResultsCounter = 0;
              // this is hacky, but first loop through and just see if there are any matching city results and if there are, add a variable to our counter
              data.results.forEach(function (property) {
                if(searching.cities == property.city) {
                  console.log('our counter' + property.city);
                  noResultsCounter++;
                  console.log('counter update' + noResultsCounter);
                }
              });
              // end hacky array
              console.log('the no results are' + noResultsCounter);
              if(noResultsCounter > 0) { // only loop through things again if we have at least 1 property to display
                data.results.forEach(function (property) {
                  if(searching.cities == property.city && searching.rooms <= property.bedrooms) { // only show results if the city matches the search
                    console.log(property.city + searching.cities);
                    console.log(property.bedrooms + searching.rooms);
                    // if we have at least one result, create a var to pass to the no results message telling it not to display
                    property.city == searching.cities && $properties.append(
                        '<div class="col-12 col-md-6 col-lg-4">' +
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
                        property.slug + "&key=" + property.key +
                        '">Details & Pricing</a> <br><br></div>'
                    );
                  }
                });
              }
              else {
                document.getElementById('noResults').innerHTML = 'No results found. Please try modifying your search criteria and trying again, or check back later to see if availability has opened up. Thank you.';
              }
            }
            else {
              document.getElementById('noResults').innerHTML = 'No results found. Please try modifying your search criteria and trying again, or check back later to see if availability has opened up. Thank you.';
            }
        });

    });
</script>


