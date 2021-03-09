<div class="container">
    <div class="row">
        <div class="col-md-12 text-center">
            <p class="h2 secondary" style="margin-top:3%">Search Availability</p>
        </div>
    </div>
    <div class="row align-items-center justify-content-center text-center">

        <div class="col-12">

            <div class="form-search-wrap p-2">
                <form action="/property-search?places=cities&beds=rooms" id="propertySearch" method="get">
                    <div class="row align-items-center">
                        <div class="col-lg-12 col-xl-2 no-sm-border border-right">
                            <div class="select-wrap myvrhover">
                                <span class="icon"><span class="icon-keyboard_arrow_down"></span></span>
                                <select class="form-control myvrhover" name="cities" id="locationList">
                                    <option hidden value="0">Location</option>
                                </select>
                            </div>
                        </div>

                        <div class="col-lg-12 col-xl-2 no-sm-border border-right">
                            <div class="select-wrap myvrhover">
                                <span class="icon"><span class="icon-keyboard_arrow_down"></span></span>
                                <select class="form-control myvrhover" name="rooms" id="bedroomList">
                                    <option hidden value="0">Bedrooms</option>
                                    <option value="1">1+</option>
                                    <option value="2">2+</option>
                                    <option value="3">3+</option>
                                </select>
                            </div>
                        </div>
                        <div class="col-lg-12 col-xl-3 no-sm-border border-right">
                            <div class="wrap-icon myvrhover">
                                <input name="checkin" type="date" class="form-control myvrhover" value="" id="defaultCheckIn" placeholder="Check-In">
                            </div>
                        </div>
                        <div class="col-lg-12 col-xl-3 no-sm-border border-right">
                            <div class="wrap-icon myvrhover">
                                <input name="checkout" type="date" class="form-control myvrhover" id="defaultCheckOut" placeholder="Check-Out">
                            </div>
                        </div>
                        <div class="col-lg-12 col-xl-2 ml-auto text-right">
                            <input type="submit" id="submitSearch" style="background:#00918e;" class="btn text-white btn-primary" value="Search">
                        </div>

                    </div>
                </form>
            </div>

        </div>
    </div>
</div>

<script>
    jQuery(document).ready(function ($) {
        // Your code here
        myvr.properties().then(function (data) {
            console.log(data);
            var cities = [];
            var bedrooms = [];
            data.results.forEach(function (property) {
             //   console.log(property.name);
                cities.indexOf(property.city)==-1&&cities.push(property.city);
                bedrooms.indexOf(property.bedrooms)==-1&&bedrooms.push(property.bedrooms);
            });
            console.log(cities);
           console.log(bedrooms);
            var $locationList = $("#locationList");
                cities.forEach(function(x){
                return $locationList.append('<option value=' + x + '>'+x+'</option>')
            });
            //Check in date default
            var twoWeeks = new Date(Date.now()+1000*60*60*24*2);
            var $defaultCheckIn = $("#defaultCheckIn");
            $defaultCheckIn.val(twoWeeks.getFullYear()+'-'+ ("0"+(twoWeeks.getMonth()+1)).slice(-2) + '-' + ("0"+(twoWeeks.getDate()+1)).slice(-2));
            //Check out date default
            var sixWeeks = new Date(Date.now()+1000*60*60*24*21);
            var $defaultCheckOut = $("#defaultCheckOut");
            $defaultCheckOut.val(sixWeeks.getFullYear()+'-'+ ("0"+(sixWeeks.getMonth()+1)).slice(-2) + '-' + ("0"+(sixWeeks.getDate()+1)).slice(-2));


        });

    });
</script>