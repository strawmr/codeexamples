@extends('layouts.app')


@section('content')
  @include('partials.blog-page-header')
  <div class="container">
      <div class="row">
          <div class="<?php if ( is_singular( 'batch_numbers' ) ) { echo  'col-md-12 mx-auto'; } else { echo 'col-md-12'; } ?> page-content-padding wow fadeIn" data-wow-duration="1s" data-wow-delay="500ms">
         
              @while (have_posts()) @php the_post() @endphp
                    @include('partials.content-single')
              @endwhile
              
          </div>
      </div>
  </div>
@endsection