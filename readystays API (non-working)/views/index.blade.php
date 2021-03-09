@extends('layouts.app')


@section('content')
  @include('partials.page-header')
  <div class="container">
      <div class="row">
          <div class="col-md-8 page-content-padding wow fadeIn" data-wow-duration="1s" data-wow-delay="500ms">
              @if (!have_posts())
                <div class="alert alert-warning">
                  {{ __('Sorry, no results were found.', 'sage') }}
                </div>
                {!! get_search_form(false) !!}
              @endif
              <div class="row">
              @while (have_posts()) @php the_post() @endphp
                    <div class="col-md-6">
                    @include('partials.content-'.get_post_type())
                    </div>
              @endwhile
              <div style="clear:both;"></div>
              <hr>
              {!! get_the_posts_navigation() !!}
              </div>
          </div>
          <div class="col-md-4 sidebar wow fadeIn" data-wow-duration="1s" data-wow-delay="500ms">
             <!-- manual sidebar here -->
             <?php dynamic_sidebar('blog'); ?>
          </div>
      </div>
  </div>
@endsection
