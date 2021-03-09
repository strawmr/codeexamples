{{--
  Template Name: Single Property
--}}

@extends('layouts.app')

@section('content')
    @while(have_posts()) @php the_post() @endphp
    @include('partials.property-page-header')
    {{-- @include('partials.search-function') --}}
    @include('partials.single-property')
    @endwhile
@endsection
