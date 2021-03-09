<!doctype html>
<html @php language_attributes() @endphp>
  @include('partials.head')
  <body @php body_class() @endphp>  
      
    @php do_action('get_header') @endphp
    @include('partials.header')
    <div class="wrap fadeIn wow" role="document">
      <div class="content">
        <main class="main">
          @yield('content')
          <?php echo do_shortcode('[h3]'); ?>
          <div class="hidden">
              <meta itemscope itemprop="mainEntityOfPage" itemType="https://schema.org/WebPage" itemid="<?php echo get_permalink();?>"/>
              <div itemprop="author">
                  <span itemscope itemtype="http://schema.org/Organization">
                      <span itemprop="name"><?php echo do_shortcode('[name]'); ?></span>
                      <link itemprop="url" href="<?php $schema_url = home_url();echo esc_url( $schema_url );?>"/>
                  </span>
              </div>
              <div itemprop="datePublished"><?php the_date(); ?></div>
              <div itemprop="dateModified"><?php echo date("Y/m/d"); ?></div>
              <div itemprop="publisher" itemscope itemtype="https://schema.org/Organization">
                  <meta itemprop="name" content="<?php echo do_shortcode('[name]'); ?>">
              </div>
			  
			  <div class="footer-info" itemscope="" itemtype="http://schema.org/HVACBusiness">
				<div class="h3" itemprop="name"><?php echo do_shortcode('[name]'); ?></div>
				<div itemprop="address" itemscope="" itemtype="http://schema.org/PostalAddress">
				 <div itemprop="streetAddress"><?php echo do_shortcode('[address]'); ?></div>
				 <span itemprop="addressLocality"><?php echo do_shortcode('[city]'); ?></span>, 
				 <span itemprop="addressRegion"><?php echo do_shortcode('[state]'); ?></span> 
				 <span itemprop="postalCode"><?php echo do_shortcode('[zip]'); ?></span>
				</div>
				<div itemprop="telephone"><?php echo do_shortcode('[phone]'); ?></div>
			  </div>
          </div>
        </main>
        @if (App\display_sidebar())
          <aside class="sidebar">
            @include('partials.sidebar')
          </aside>
        @endif
      </div>
    </div>  
    <script type="text/javascript" src="//cdn.jsdelivr.net/npm/slick-carousel@1.8.1/slick/slick.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/wow/1.1.2/wow.min.js"></script>
    <script>
        
      wow = new WOW(
      {
      boxClass:     'wow',      // default
      animateClass: 'animated', // default
      offset:       100,          // defaul = 0
      mobile:       true,      // default
      live:         true,        // default
      })
      wow.init();
    </script>
      
    @php do_action('get_footer') @endphp
    @include('partials.footer')
    @php wp_footer() @endphp
	  
	  <!--linkedin script start-->	  	
<script type="text/javascript"> _linkedin_partner_id = "1929106"; window._linkedin_data_partner_ids = window._linkedin_data_partner_ids || []; window._linkedin_data_partner_ids.push(_linkedin_partner_id); </script><script type="text/javascript"> (function(){var s = document.getElementsByTagName("script")[0]; var b = document.createElement("script"); b.type = "text/javascript";b.async = true; b.src = "https://snap.licdn.com/li.lms-analytics/insight.min.js"; s.parentNode.insertBefore(b, s);})(); </script> <noscript> <img height="1" width="1" style="display:none;" alt="" src="https://px.ads.linkedin.com/collect/?pid=1929106&fmt=gif" /> </noscript>
	  <!--linkedin script start-->

  </body>
</html>