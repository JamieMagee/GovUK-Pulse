var gulp = require('gulp'),
  htmlmin = require('gulp-htmlmin'),
  cdnizer = require("gulp-cdnizer"),
  cleanCSS = require('gulp-clean-css'),
  jshint = require('gulp-jshint'),
  uglify = require('gulp-uglify'),
  rename = require('gulp-rename'),
  concat = require('gulp-concat'),
  notify = require('gulp-notify'),
  cache = require('gulp-cache'),
  livereload = require('gulp-livereload'),
  del = require('del');

var libs = {
  js: [
    'node_modules/jquery/dist/jquery.js',
    'node_modules/bootstrap/dist/js/bootstrap.js',
    'node_modules/d3/build/d3.js',
    'node_modules/datatables.net/js/jquery.dataTables.js',
    'node_modules/datatables.net-bs/js/dataTables.bootstrap.js',
    'node_modules/datatables.net-responsive/js/dataTables.responsive.js',
    'node_modules/datatables.net-responsive-bs/js/responsive.bootstrap.js'
  ],
  css: [
    'node_modules/bootstrap/dist/css/bootstrap.css',
    'node_modules/datatables.net-bs/css/dataTables.bootstrap.css',
    'node_modules/datatables.net-responsive-bs/css/responsive.bootstrap.css'
  ],
  fonts: [
    'node_modules/bootstrap/fonts/*'
  ]
}

// HTML
gulp.task('html', function () {
  return gulp.src('*.html')
    .pipe(cdnizer([
      'cdnjs:jquery@3.1.1',
      {
        cdn: 'cdnjs:twitter-bootstrap@3.3.7',
        package: 'bootstrap',
        test: 'typeof $().emulateTransitionEnd == "function"'
      },
      'cdnjs:d3@4.5.0',
      {
        file: 'js/jquery.dataTables.min.js',
        test: 'typeof $().DataTable() == "object"',
        cdn: '//cdn.datatables.net/1.10.13/js/jquery.dataTables.min.js'
      },
      {
        file: 'js/dataTables.bootstrap.min.js',
        test: 'typeof $().DataTable() == "object"',
        cdn: '//cdn.datatables.net/1.10.13/js/dataTables.bootstrap.min.js'
      },
      {
        file: 'js/dataTables.responsive.min.js',
        test: 'typeof $().DataTable() == "object"',
        cdn: '//cdn.datatables.net/responsive/2.1.1/js/dataTables.responsive.min.js'
      },
      {
        file: 'js/responsive.bootstrap.min.js',
        test: 'typeof $().DataTable() == "object"',
        cdn: '//cdn.datatables.net/responsive/2.1.1/js/responsive.bootstrap.min.js'
      }
    ]))
    .pipe(htmlmin({
      collapseWhitespace: true
    }))
    .pipe(gulp.dest('dist'));
});

// CSS
gulp.task('css', function () {
  return gulp.src('css/*.css')
    .pipe(cleanCSS({
      compatibility: '*'
    }))
    .pipe(rename({
      suffix: '.min'
    }))
    .pipe(gulp.dest('dist/css'));
});

// JS
gulp.task('js', function () {
  return gulp.src('js/*.js')
    .pipe(jshint('.jshintrc'))
    .pipe(jshint.reporter('default'))
    .pipe(rename({
      suffix: '.min'
    }))
    .pipe(uglify())
    .pipe(gulp.dest('dist/js'));
});

// Copy
gulp.task('copy', function () {
  gulp.src(libs.js)
    .pipe(rename({
      suffix: '.min'
    }))
    .pipe(uglify())
    .pipe(gulp.dest('dist/js'));

  gulp.src(libs.css)
    .pipe(rename({
      suffix: '.min'
    }))
    .pipe(cleanCSS({
      compatibility: '*'
    }))
    .pipe(gulp.dest('dist/css'));

  gulp.src(libs.fonts)
    .pipe(gulp.dest('dist/fonts'));

  gulp.src('favicon.ico')
    .pipe(gulp.dest('dist'))

  gulp.src('Web.config')
    .pipe(gulp.dest('dist'))
});

// Clean
gulp.task('clean', function () {
  return del(['dist']);
});

// Default task
gulp.task('default', ['clean'], function () {
  gulp.start('html', 'css', 'js', 'copy');
});

// Watch
gulp.task('watch', function () {

  // Watch .html files
  gulp.watch('*.html', ['html']);

  // Watch .css files
  gulp.watch('css/*.css', ['css']);

  // Watch .js files
  gulp.watch('js/*.js', ['js']);

  // Create LiveReload server
  livereload.listen();

  // Watch any files in dist/, reload on change
  gulp.watch(['dist/**']).on('change', livereload.changed);

});