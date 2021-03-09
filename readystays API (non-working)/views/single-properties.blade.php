@extends('layouts.app')


@section('content')
  @include('partials.page-header')
<div class="container">
    <div class="row content-padding">
        <div class="col-lg-6 wow fadeIn" data-wow-duration="1s" data-wow-delay="500ms">
            <?php if(get_field('intro_content')) { ?>
            <?php  the_field('intro_content'); ?>
			<a href="tel:813-551-1500" class="primary-btn">Call Us</a>
			<hr>
            <div class="h3" style="color:#1CB0BC">Inquire about this property</div>
			<?php echo do_shortcode('[gravityform id="4" title="false" description="false" ajax="true"]');?>
			<?php } ?>
        </div>
        <div class="col-lg-6 text-center wow fadeIn" data-wow-duration="1s" data-wow-delay="500ms">            
            <!-- start gallery feed -->
            <?php while(have_rows('gallery')) { the_row(); ?>
            <div class="row fadeIn wow" style="position: sticky; top: 100px;">
                <div class="col-lg-12 mx-auto margin-top-sm text-center">
                    <div class="gallery-slick">
                        <?php $images = get_sub_field('gallery_photos');
                        foreach( $images as $image ) { ?>
                        <div class="text-center">
                            <img src="<?php echo $image[url]; ?>">
                        </div>
                        <?php } ?>
                    </div>
                </div>
            </div>
            <?php } ?>
        </div>
		<div class="col-12">
			
			
		</div>
    </div>
</div>
<div class="container-fluid light-grey-bkg">
    <div class="row">
        <div class="col-md-12 property-item-heading">
            <p class="h2 text-center white" style="margin-bottom:0px; margin-top:0px;">Included in this property</p>
        </div>
        <div class="col-md-12">
            <div class="row">
            <?php while(have_rows('property_includes')) { the_row(); ?>
                <div class="col-6 col-sm-4 col-md-3 property-item">
                    <div class="flex">
                        <div class="relative text-center pad small">
                            <?php the_sub_field('item'); ?>
                        </div>
                    </div>
                </div> 
            <?php } ?>
            </div>
        </div>
    </div>
</div>
<div class="container-fluid">
    <div class="row">
        <div class="col-md-12 no-padding">
            @while (have_posts()) @php the_post() @endphp
            @include('partials.content-single')
            @endwhile
        </div>
    </div>
</div>
@endsection