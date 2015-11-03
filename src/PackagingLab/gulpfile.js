/// <binding BeforeBuild='nuget-download' />

var gulp = require('gulp'),
    plumber = require('gulp-plumber'),
    request = require('request'),
    fs = require('fs');

gulp.task('default', function () {
    // place code for your default task here
});


gulp.task('nuget-download', function (done) {
    if (fs.existsSync('nuget.exe')) {
        done();
        return;
    }

    request.get('http://nuget.org/nuget.exe')
        .pipe(plumber())
        .pipe(fs.createWriteStream('nuget.exe'))
        .on('close', done);
});

