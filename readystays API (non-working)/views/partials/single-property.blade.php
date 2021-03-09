<div class="container">
    <div class="row content-padding">
        <div class="col-lg-6 wow fadeIn" data-wow-duration="1s" data-wow-delay="500ms" style="visibility: visible; animation-duration: 1s; animation-delay: 500ms; animation-name: fadeIn;">
            <div class="propertyHeadline h3" style="margin-top:0;" id="propertyHeadline"></div>
            <div class="propertyDescription mb-4" id="propertyDescription"></div>
            <div class="small" style="margin-bottom: 2%;"><em><strong>Pricing includes all furnishings, utilities, cable TV, high speed Wi-Fi, housewares, parking and amenities. Local tax added to agreements less than 6 months. NO security deposits required, NO credit checks. Rental fee paid month to month.</strong></em></div>
            <div>
                <p id="dailyRate"></p>
                <p id="monthlyRate"></p>
            </div>
            <hr>
            <!-- <div class="small"><em>*Available For Your Selected Dates*</em></div>-->
            <div class="h3" style="color:#1CB0BC">Request Booking for This Property</div>
            <div class="col-12">
                <form id="inquireForm" class="form-horizontal">
                    <input id="propertyKey" type="hidden" name="property" value="" />
                    <!--Form Row-->
                    <div class="row">
                        <!--Grid column-->
                        <div class="col-md-6">
                            <div class="md-form">
                                <label for="firstName" class="">First name</label>
                                <input type="text" name="firstName" class="form-control">
                            </div>
                        </div>
                        <!--Grid column-->
                        <!--Grid column-->
                        <div class="col-md-6">
                            <div class="md-form">
                                <label for="lastName" class="">Last name</label>
                                <input type="text" name="lastName" class="form-control">
                            </div>
                        </div>
                        <!--Grid column-->
                    </div>
                    <!--Form Row-->
                    <!--Form Row-->
                    <div class="row">
                        <!--Grid column-->
                        <div class="col-md-6">
                            <div class="md-form">
                                <label for="email" class="">Email</label>
                                <input type="email" name="email" class="form-control">
                            </div>
                        </div>
                        <!--Grid column-->
                        <!--Grid column-->
                        <div class="col-md-6">
                            <div class="md-form">
                                <label for="phone" class="">Phone</label>
                                <input type="tel" name="phone" class="form-control">
                            </div>
                        </div>
                        <!--Grid column-->
                    </div>
                    <!--Form Row-->
                    <!--Form Row-->
                    <div class="row">
                        <!--Grid column-->
                        <div class="col-md-6">
                            <div class="md-form">
                                <label for="firstName" class="">Check In</label>
                                <input type="date" name="checkIn" class="form-control">
                            </div>
                        </div>
                        <!--Grid column-->
                        <!--Grid column-->
                        <div class="col-md-6">
                            <div class="md-form">
                                <label for="lastName" class="">Check Out</label>
                                <input type="date" name="checkOut" class="form-control">
                            </div>
                        </div>
                        <!--Grid column-->
                    </div>
                    <!--Form Row-->
                    <!--Form Row-->
                    <div class="row">
                        <!--Grid column-->
                        <div class="col-md-6">
                            <div class="md-form">
                                <label for="adults" class="">Adults</label>
                                <input type="number" name="adults" class="form-control">
                            </div>
                        </div>
                        <!--Grid column-->
                        <!--Grid column-->
                        <div class="col-md-6">
                            <div class="md-form">
                                <label for="children" class="">Children</label>
                                <input type="number" name="children" class="form-control">
                            </div>
                        </div>
                        <!--Grid column-->
                    </div>
                    <!--Form Row-->
                    <!--Form Row-->
                    <div class="row">
                        <!--Grid column-->
                        <div class="col-12">
                            <div class="md-form">
                                <label for="comments" class="">Comments</label>
                                <textarea class="form-control" name="comments" placeholder="Personal message for owner..."></textarea>
                            </div>
                        </div>
                        <!--Grid column-->
                    </div>
                    <!--Form Row-->
                    <!--Form Row-->
                    <div class="row mt-3">
                        <div class="col-12">
                            <button type="submit" style="border:none;" class="btn btn-primary btn-lg">Submit</button>
                        </div>
                    </div>
                </form>
            </div>


        </div>

        <div class="col-lg-6 text-center wow fadeIn" data-wow-duration="1s" data-wow-delay="500ms" style="visibility: visible; animation-duration: 1s; animation-delay: 500ms; animation-name: fadeIn;">
            <!-- start gallery feed -->
            <div class="row fadeIn wow" style="position: sticky; top: 100px; visibility: visible; animation-name: fadeIn;">
                <div class="col-lg-12 mx-auto margin-top-sm text-center">
                  <div class="gallery-slick" id="propPhotos">
                  </div>
                </div>
            </div>
        </div>
        <div class="col-12">


        </div>
    </div>
</div>
<!-- start paul -->
<div class="container-fluid light-grey-bkg">
    <div class="row">
        <div class="col-md-12 property-item-heading">
            <p class="h2 text-center white" style="margin-bottom:0px; margin-top:0px;">Included in this property</p>
        </div>

        <div class="col-md-12" id="propAmenities">
            <div class="row">
            </div>
        </div>
    </div>
</div>
<!-- end paul -->

<script>
    jQuery(document).ready(function ($) {
        /*FORM SUBMISSION (WORKING)*/
        $("#inquireForm").submit(function(event) {
            event.preventDefault();
            var data = {};
            var fields = $(this).serializeArray();
            $.each(fields, function(i, item) {
                data[item.name] = item.value;
            });
            myvr.inquire(data).then(function() {
                alert("Inquiry Submitted!");
            }, function(response) {
                alert("Error Occurred", response);
            });
        });
    });

</script>