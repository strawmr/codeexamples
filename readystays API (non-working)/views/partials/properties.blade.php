<div class="container content-padding">
    <div class="row">
        <div class="col-md-12 text-center">
            <p class="h2 secondary">Featured Properties</p>
        </div>
    </div>
<?php
$args = array(
    'post_type'=> 'properties',
    'order'    => 'ASC',
    'posts_per_page' => 6
    );              

$the_query = new WP_Query( $args );
if($the_query->have_posts() ) { ?>
    <div class="row">
    <?php while ( $the_query->have_posts() ) { $the_query->the_post(); ?>
        <div class="col-sm-6 col-lg-4 pad fadeIn wow">
            <div class="product-container full-height">
                <a href="<?php the_permalink(); ?>" class="absolute-link"></a>
                <?php $post_number = get_the_ID(); ?>
                <div class="min-height-smaller" style="background:url(<?php echo get_the_post_thumbnail_url($post_number); ?>); background-size:cover; background-position:center;"></div>
                <p class="sub-heading pad text-center"><?php the_title(); ?></p>
            </div>
        </div>
    <?php } ?>       
    </div>
    <?php } ?>
    <div class="row content-padding no-padding-top">
        <div class="col-md-12 text-center">
            <a href="/properties-available/" class="primary-btn">Search All Properties</a>
        </div>
    </div>
</div>