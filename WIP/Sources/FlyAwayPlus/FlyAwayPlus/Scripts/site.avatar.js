﻿// ================================================================
//  Description: Avatar Upload supporting script
//  License:     MIT - check License.txt file for details
//  Author:      Codative Corp. (http://www.codative.com/)
// ================================================================
var jcrop_api,
    boundx,
    boundy,
    xsize,
    ysize;

// ToDo - change the size limit of the file. You may need to change web.config if larger files are necessary.
var maxSizeAllowed = 2;     // Upload limit in MB
var maxSizeInBytes = maxSizeAllowed * 1024 * 1024;
var keepUploadBox = true;  // ToDo - Remove if you want to keep the upload box
var keepCropBox = false;    // ToDo - Remove if you want to keep the crop box

$(function () {
    if (typeof $("#avatar-upload-form") !== undefined) {
        initAvatarUpload();
        $("#avatar-max-size").html(maxSizeAllowed);
        $("#avatar-upload-form input:file").on("change", function (e) {
            var files = e.currentTarget.files;
            for (var x in files) {
                if (files[x].name !== "item" && typeof files[x].name != "undefined") {
                    if (files[x].size <= maxSizeInBytes) {
                        // Submit the selected file
                        $("#avatar-upload-form").submit();
                    } else {
                        // File too large
                        alert("Maximum size allowed is " + maxSizeAllowed + "MB");
                    }
                }
            }
        });
    }
});

function initAvatarUpload() {
    $("#avatar-upload-form").ajaxForm({
        beforeSend: function () {
            updateProgress(0);
            $("#avatar-upload-form").addClass("hidden");
        },
        uploadProgress: function (event, position, total, percentComplete) {
            updateProgress(percentComplete);
        },
        success: function (data) {
            updateProgress(100);
            if (data.success === false) {
                $("#status").html(data.errorMessage);
            } else {
                $("#preview-pane .preview-container img").attr("src", data.fileName);
                var img = $("#crop-avatar-target");
                img.attr("src", data.fileName);

                $(".jcrop-holder").find("img").each(function () {
                    $(this).attr("src", data.fileName);
                });

                if (!keepUploadBox) {
                    $("#avatar-upload-box").addClass("hidden");
                }

                $(".upload-avatar-progress").addClass("hidden");
                $("#avatar-upload-form").removeClass("hidden");
                $("#avatar-crop-box").removeClass("hidden");
                initAvatarCrop(img);
            }
        },
        complete: function (xhr) {
        }
    });
}

function updateProgress(percentComplete) {
    $(".upload-avatar-progress .upload-progress").css("width", percentComplete + "%");
    $(".upload-avatar-progress .upload-progress").html(percentComplete + "%");
    if (percentComplete === 0) {
        $(".upload-avatar-progress").removeClass("hidden");
    }
}

function initAvatarCrop(img) {
    img.Jcrop({
        onChange: updatePreviewPane,
        onSelect: updatePreviewPane,
        aspectRatio: xsize / ysize
    }, function () {
        var bounds = this.getBounds();
        boundx = bounds[0];
        boundy = bounds[1];

        jcrop_api = this;
        jcrop_api.setOptions({ allowSelect: true });
        jcrop_api.setOptions({ allowMove: true });
        jcrop_api.setOptions({ allowResize: true });
        jcrop_api.setOptions({ aspectRatio: 1 });

        // Maximise initial selection around the centre of the image,
        // but leave enough space so that the boundaries are easily identified.
        var padding = 10;
        var shortEdge = (boundx < boundy ? boundx : boundy) - padding;
        var longEdge = boundx < boundy ? boundy : boundx;
        var xCoord = longEdge / 2 - shortEdge / 2;
        jcrop_api.animateTo([xCoord, padding, shortEdge, shortEdge]);

        var pcnt = $("#preview-pane .preview-container");
        xsize = pcnt.width();
        ysize = pcnt.height();
        $("#preview-pane").appendTo(jcrop_api.ui.holder);
        jcrop_api.focus();
    });
}

function updatePreviewPane(c) {
    if (parseInt(c.w) > 0) {
        var rx = xsize / c.w;
        var ry = ysize / c.h;

        $("#preview-pane .preview-container img").css({
            width: Math.round(rx * boundx) + "px",
            height: Math.round(ry * boundy) + "px",
            marginLeft: "-" + Math.round(rx * c.x) + "px",
            marginTop: "-" + Math.round(ry * c.y) + "px"
        });
    }
}

function saveAvatar() {
    $(".preview-container").imagesLoaded(function () {
        var img = $("#preview-pane .preview-container img");
        $("#avatar-crop-box button").addClass("disabled");

        $.ajax({
            type: "POST",
            url: "/Login/SaveImage",
            traditional: true,
            data: {
                w: img.css("width"),
                h: img.css("height"),
                l: img.css("marginLeft"),
                t: img.css("marginTop"),
                fileName: img.attr("src")
            }
        }).done(function (data) {
            if (data.success === true) {
                $("#upload-avatar-preview").attr("src", data.avatarFileLocation);

                if (!keepCropBox) {
                    $("#avatar-crop-box").addClass("hidden");
                }
                
                $("#id-register-avatar-path").val(data.avatarFileLocation.replace(/\\/g, "/"));
                $("#upload-avatar-preview").attr("src", data.avatarFileLocation);
                $("#avatar-crop-box button").removeClass("disabled");
            } else {
                alert(data.errorMessage);
            }
        }).fail(function (e) {
            alert("Cannot upload avatar at this time");
        });
    });
}