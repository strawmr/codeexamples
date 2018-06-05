var currentPrice = $("#roofing_type:checked").data("price");
var currentMeters = 0;

$(".roofing_type").click(function() {
    currentPrice = $(this).data('price');
    $('#estimate').html("$" + (currentMeters * currentPrice).toFixed(2));
});

$(document).ready(function() {
$("#slider").slider({
  animate: true,
  value: 1,
  min: 0,
  max: 100,
  step: 0.1,  
  slide: function(event, d) {
    update(d.value);
  }
});
  
  update();
});

function update(val) {
  
  if(val == null)
    val = 0;
  else
    currentMeters = val;
  
   $('#slider span').html('<label><span class="glyphicon glyphicon-chevron-left"></span> '+ val +' <span class="glyphicon glyphicon-chevron-right"></span></label>');
  $('#estimate').html("$" + (val * currentPrice).toFixed(2));
}


//BACKGROUND
