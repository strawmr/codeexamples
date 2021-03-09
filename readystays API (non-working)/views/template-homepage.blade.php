{{--
  Template Name: Homepage
--}}

@extends('layouts.app')

@section('content')
  @while(have_posts()) @php the_post() @endphp
    @include('partials.flexible-content.flex-builder')
  @endwhile
@endsection