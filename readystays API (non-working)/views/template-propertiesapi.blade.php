{{--
  Template Name: All Properties API
--}}

@extends('layouts.app')

@section('content')
  @while(have_posts()) @php the_post() @endphp
    @include('partials.page-header')
  @include('partials.search-function')
  @include('partials.results')
  {{-- @include('partials.search-results') --}}

  {{--  @include('partials.properties-api')--}}
  @endwhile
@endsection
