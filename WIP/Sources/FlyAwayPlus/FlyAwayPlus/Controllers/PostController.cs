﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FlyAwayPlus.Helpers;
using FlyAwayPlus.Helpers.UploadImage;
using FlyAwayPlus.Models;

namespace FlyAwayPlus.Controllers
{
    public class PostController : Controller
    {
        //
        // GET: /PostDetail/
        public ActionResult Index()
        {
            return View();
        }

        public RedirectToRouteResult Add(FormCollection form)
        {
            string message = Request.Form["message"];
            Decimal latitude = Decimal.Parse(Request.Form["lat"]);
            Decimal longitude = Decimal.Parse(Request.Form["lng"]);
            string location = Request.Form["location"];
            string filename = string.Empty;

            filename = UploadImage(Request.Files);

            Post newPost = new Post
            {
                content = message,
                dateCreated = DateTime.Now.ToString(FapConstants.DatetimeFormat)
            };

            Place newPlace = new Place
            {
                name = location,
                latitude = latitude,
                longitude = longitude
            };

            Photo newPhoto = new Photo
            {
                dateCreated = DateTime.Now.ToString(FapConstants.DatetimeFormat),
                url = filename
            };

            GraphDatabaseHelpers.InsertPost(newPost, newPhoto, newPlace);

            return RedirectToAction("Index", "Home");
        }

        private string UploadImage(HttpFileCollectionBase files)
        {
            string filename = string.Empty;
            foreach (string item in files)
            {
                var file = files[item];
                if (file == null || file.ContentLength == 0)
                    continue;

                if (file.ContentLength <= 0) continue;
                ImageUpload imageUpload = new ImageUpload
                {
                    Width = FapConstants.UploadedImageMaxWidthPixcel, 
                    Height = FapConstants.UploadedImageMaxHeightPixcel
                };
                ImageResult imageResult = imageUpload.RenameUploadFile(file);

                if (imageResult.Success)
                {
                    filename = imageResult.ImageName;
                }
                else
                {
                    // TODO: ERROR message.
                    ViewBag.Error = imageResult.ErrorMessage;
                }
            }
            return filename;
        }
    }
}