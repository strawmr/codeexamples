//BACKGROUND


var priceCalc = {
  pricing: [49, 150, 225],
  initWords: 50,
  
  $radioChoices: $('input[name="service"]'),
  $wordsInput: $('#linearfeet'),
  $costSub: $('#costSub'),
  $costTotal: $('#costTotal'),
  
  init: function() {
    this.$wordsInput.val(this.initWords);
    this.calc();
    
    this.$radioChoices.on('change', this.calc.bind(this));
    this.$wordsInput.on('change', this.calc.bind(this));
    this.$wordsInput.on('keyup', this.calc.bind(this));
  },
  getSelectedValue: function() {
    var val = $('input[name="service"]:checked').val();
    return parseInt(val);
  },
  calcRange: function() {
    this.$wordsInput.val(rangeWords);
    this.calc();
  },
  calc: function() {
    var pricing = this.pricing[this.getSelectedValue()];
    var words = parseInt(this.$wordsInput.val());
    var wordsStr = accounting.formatNumber(words);
    var cost = accounting.formatMoney((pricing * words).toFixed(2));
    
    var costSubStr = wordsStr + ' words ' + ' @ $' + (pricing.toFixed(2)) + ' per word:';
    
    this.$costSub.text(costSubStr);
    this.$costTotal.text(cost);
  }
};
priceCalc.init();