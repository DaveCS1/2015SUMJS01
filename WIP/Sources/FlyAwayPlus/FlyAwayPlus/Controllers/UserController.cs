﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using FlyAwayPlus.Models;
using FlyAwayPlus.Helpers;
using FlyAwayPlus.Helpers.UploadImage;

namespace FlyAwayPlus.Controllers
{
    public class UserController : Controller
    {
        public const int RecordsPerPage = 10;
        //
        // GET: /User/
        public ActionResult Index(int id = 0)
        {
            User userSession = UserHelpers.GetCurrentUser(Session);
            User user;
            List<Post> listPost;
            List<User> friend;

            if (userSession == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (userSession.UserId == id || id == 0)
            {
                user = userSession;
                listPost = GraphDatabaseHelpers.Instance.FindPostOfUser(userSession);
                friend = GraphDatabaseHelpers.Instance.FindFriend(userSession);

                FindRelatedInformationPost(listPost);
            }
            else
            {
                user = GraphDatabaseHelpers.Instance.FindUser(id);
                if (user == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                listPost = GraphDatabaseHelpers.Instance.FindPostOfOtherUser(userSession, user);
                friend = GraphDatabaseHelpers.Instance.FindFriend(user);

                FindRelatedInformationPost(listPost);
            }

            ViewData["userSession"] = userSession;
            ViewData["friend"] = friend;
            ViewData["isFriend"] = GraphDatabaseHelpers.Instance.GetFriendType(userSession.UserId, user.UserId);
            return View(user);
        }

        private void FindRelatedInformationPost(List<Post> listPost)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            Dictionary<int, List<Photo>> listPhotoDict = new Dictionary<int, List<Photo>>();
            Dictionary<int, Video> listVideoDict = new Dictionary<int, Video>();
            Dictionary<int, Place> listPlaceDict = new Dictionary<int, Place>();
            Dictionary<int, User> listUserDict = new Dictionary<int, User>();
            Dictionary<int, int> dictLikeCount = new Dictionary<int, int>();
            Dictionary<int, int> dictDislikeCount = new Dictionary<int, int>();
            Dictionary<int, int> dictCommentCount = new Dictionary<int, int>();
            Dictionary<int, int> dictUserCommentCount = new Dictionary<int, int>();
            Dictionary<int, bool> isLikeDict = new Dictionary<int, bool>();
            Dictionary<int, bool> isDislikeDict = new Dictionary<int, bool>();
            Dictionary<int, bool> isWishDict = new Dictionary<int, bool>();
            List<Photo> listPhoto = new List<Photo>();
            List<Place> listPlace = new List<Place>();
            List<Video> listVideo = new List<Video>();

            foreach (Post po in listPost)
            {
                List<Photo> p = GraphDatabaseHelpers.Instance.FindPhoto(po.PostId);
                listPhotoDict.Add(po.PostId, p);
                listPhoto.AddRange(p);
                listVideoDict.Add(po.PostId, GraphDatabaseHelpers.Instance.FindVideo(po.PostId));
                listVideo.Add(listVideoDict[po.PostId]);
                listPlaceDict.Add(po.PostId, GraphDatabaseHelpers.Instance.FindPlace(po));
                listPlace.Add(listPlaceDict[po.PostId]);
                listUserDict.Add(po.PostId, GraphDatabaseHelpers.Instance.FindUser(po));
                dictLikeCount.Add(po.PostId, GraphDatabaseHelpers.Instance.CountLike(po.PostId));
                dictDislikeCount.Add(po.PostId, GraphDatabaseHelpers.Instance.CountDislike(po.PostId));
                dictCommentCount.Add(po.PostId, GraphDatabaseHelpers.Instance.CountComment(po.PostId));
                dictUserCommentCount.Add(po.PostId, GraphDatabaseHelpers.Instance.CountUserComment(po.PostId));

                if (user != null)
                {
                    isLikeDict.Add(po.PostId, GraphDatabaseHelpers.Instance.IsLike(po.PostId, user.UserId));
                    isDislikeDict.Add(po.PostId, GraphDatabaseHelpers.Instance.IsDislike(po.PostId, user.UserId));
                    isWishDict.Add(po.PostId, GraphDatabaseHelpers.Instance.IsWish(po.PostId, user.UserId));
                }
                else
                {
                    isLikeDict.Add(po.PostId, false);
                    isDislikeDict.Add(po.PostId, false);
                    isWishDict.Add(po.PostId, false);
                }
            }

            listPhoto.RemoveAll(item => item == null);
            listVideo.RemoveAll(item => item == null);
            listPlace.RemoveAll(item => item == null);

            ViewData["listPost"] = listPost;
            ViewData["listPhotoDict"] = listPhotoDict;
            ViewData["listVideoDict"] = listVideoDict;
            ViewData["listPlaceDict"] = listPlaceDict;
            ViewData["listUserDict"] = listUserDict;
            ViewData["dislikeCount"] = dictDislikeCount;
            ViewData["likeCount"] = dictLikeCount;
            ViewData["commentCount"] = dictCommentCount;
            ViewData["userCommentCount"] = dictUserCommentCount;
            ViewData["isLikeDict"] = isLikeDict;
            ViewData["isDislikeDict"] = isDislikeDict;
            ViewData["isWishDict"] = isWishDict;
            ViewData["typePost"] = "index";
            ViewData["listPhoto"] = listPhoto;
            ViewData["listPlace"] = listPlace;
            ViewData["listVideo"] = listVideo;

            if (listPost.Count < RecordsPerPage)
            {
                ViewData["isLoadMore"] = "false";
            }
            else
            {
                ViewData["isLoadMore"] = "true";
            }
        }
        public ActionResult Comment(int postId, string content)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            Dictionary<int, User> dict = new Dictionary<int, Models.User>();
            Comment comment = new Comment();
            comment.Content = content.Replace("\n", "\\n");
            comment.DateCreated = DateTime.Now.ToString(FapConstants.DatetimeFormat);

            bool success = GraphDatabaseHelpers.Instance.InsertComment(postId, comment, user.UserId);
            dict.Add(comment.CommentId, user);

            ViewData["dict"] = dict;
            return PartialView("_PostDetailPartial", comment);
        }

        public JsonResult EditComment(int commentId, string content)
        {
            Comment comment = new Comment();
            comment.CommentId = commentId;
            comment.Content = content;
            comment.Content = comment.Content.Replace("\n", "\\n");
            comment.DateCreated = DateTime.Now.ToString(FapConstants.DatetimeFormat);

            bool success = GraphDatabaseHelpers.Instance.EditComment(comment);
            return Json(success);
        }

        public JsonResult DeleteComment(int commentId)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            bool success = GraphDatabaseHelpers.Instance.DeleteComment(commentId, user.UserId);
            return Json(success);
        }

        public JsonResult Like(int postId)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            Boolean success = false;
            if (user != null)
            {
                int like = GraphDatabaseHelpers.Instance.FindLike(user.UserId, postId);
                if (like == 0)
                {
                    // User like post and delete exist dislike
                    success = GraphDatabaseHelpers.Instance.InsertLike(user.UserId, postId);
                    GraphDatabaseHelpers.Instance.DeleteDislike(user.UserId, postId);
                }
                else
                {
                    // delete exist like post
                    GraphDatabaseHelpers.Instance.DeleteLike(user.UserId, postId);
                }
            }
            return Json(success);
        }

        public JsonResult Dislike(int postId)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            Boolean success = false;
            if (user != null)
            {
                int dislike = GraphDatabaseHelpers.Instance.FindDislike(user.UserId, postId);
                if (dislike == 0)
                {
                    // user dislike post and delete exist like
                    success = GraphDatabaseHelpers.Instance.InsertDislike(user.UserId, postId);
                    GraphDatabaseHelpers.Instance.DeleteLike(user.UserId, postId);
                }
                else
                {
                    // delete exist dislike
                    GraphDatabaseHelpers.Instance.DeleteDislike(user.UserId, postId);
                }
            }
            return Json(success);
        }

        public JsonResult AddToWishlist(int postId)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            bool success = false;
            if (user != null)
            {
                success = GraphDatabaseHelpers.Instance.AddToWishList(postId, user.UserId);
            }
            return Json(success);
        }

        public JsonResult AddWishPlace(int placeId, int userId)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            bool success = false;
            if (user != null)
            {
                success = GraphDatabaseHelpers.Instance.AddToWishlist(placeId, userId);
            }
            return Json(success);
        }

        public JsonResult AddFriend(int userId, int otherUserId)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            bool success = false;
            if (user != null)
            {
                success = GraphDatabaseHelpers.Instance.AddFriend(userId, otherUserId);
            }
            return Json(success);
        }

        public JsonResult DeclineRequestFriend(int userId, int otherUserId)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            bool success = false;
            if (user != null)
            {
                success = GraphDatabaseHelpers.Instance.DeclineRequestFriend(userId, otherUserId);
            }
            return Json(success);
        }

        public string IsFriend(int userId, int otherUserId)
        {
            return GraphDatabaseHelpers.Instance.IsFriend(userId, otherUserId)
                   ? "friend"
                   : GraphDatabaseHelpers.Instance.GetFriendType(userId, otherUserId);
        }

        public JsonResult SendRequestFriend(int userId, int otherUserId)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            bool success = false;
            if (user != null)
            {
                success = GraphDatabaseHelpers.Instance.SendRequestFriend(userId, otherUserId);
            }
            return Json(success);
        }

        public JsonResult CancelRequestFriend(int userId, int otherUserId)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            bool success = false;
            if (user != null)
            {
                success = GraphDatabaseHelpers.Instance.DeclineRequestFriend(userId, otherUserId);
            }
            return Json(success);
        }

        public JsonResult Unfriend(int userId, int otherUserId)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            bool success = false;
            if (user != null)
            {
                success = GraphDatabaseHelpers.Instance.Unfriend(userId, otherUserId);
            }
            return Json(success);
        }

        public JsonResult RemoveFromWishlist(int postId)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            bool success = false;
            if (user != null)
            {
                success = GraphDatabaseHelpers.Instance.RemoveFromWishList(postId, user.UserId);
            }
            return Json(success);
        }

        public JsonResult RemoveWishPlace(int placeId, int userId)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            bool success = false;
            if (user != null)
            {
                success = GraphDatabaseHelpers.Instance.RemoveFromWishlist(placeId, userId);
            }
            return Json(success);
        }

        public JsonResult GetMessage(int friendId)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            List<Message> listMessage = new List<Message>();
            List<User> listUser = new List<User>();
            if (user != null)
            {
                listMessage = GraphDatabaseHelpers.Instance.GetListMessage(user.UserId, friendId, 10);
                for (int i = 0; i < listMessage.Count; i++)
                {
                    listUser.Add(GraphDatabaseHelpers.Instance.FindUser(listMessage[i]));
                }
            }
            KeyValuePair<List<Message>, List<User>> returnObject = new KeyValuePair<List<Message>, List<User>>(listMessage, listUser);
            return Json(returnObject);
        }

        public JsonResult CreateMessage(int friendId, string content)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            Message message = null;
            if (user != null)
            {
                message = GraphDatabaseHelpers.Instance.CreateMessage(content, user.UserId, friendId);

            }
            return Json(message);
        }

        public JsonResult CreateMessageInRoom(int roomId, string content)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            Message message = null;
            if (user != null)
            {
                message = GraphDatabaseHelpers.Instance.CreateMessageInRoom(roomId, user.UserId, content);

            }
            return Json(message);
        }

        public JsonResult EditProfile(string firstName, string lastName, string address, string gender, string phoneNumber,
                            string dateOfBirth, string password)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            bool success = false;
            if (user != null)
            {
                user.FirstName = firstName;
                user.LastName = lastName;
                user.Address = address;
                user.Gender = gender;
                user.PhoneNumber = phoneNumber;
                user.DateOfBirth = dateOfBirth;
                user.Password = password;

                success = GraphDatabaseHelpers.Instance.EditProfile(user);
            }
            return Json(success);
        }
        public JsonResult ReportPost(string url, int postId, int userReportId, int userReportedId, int typeReport)
        {
            var checkReport = GraphDatabaseHelpers.Instance.FindReportPost(postId, userReportId);
            if (checkReport != null)
            {
                return Json(false);
            }
            try
            {
                GraphDatabaseHelpers.Instance.InsertReportPost(postId, userReportId, typeReport);

                //Send warning email to reported user
                var email = GraphDatabaseHelpers.Instance.GetEmailByUserId(userReportedId);
                MailHelpers.Instance.SendMailWarningReportPost(url, email, userReportedId);

                return Json(true);
            }
            catch (Exception e)
            {
                return Json(false);
            }
        }

        public JsonResult ReportUser(int userReportId, int userReportedId, int typeReport)
        {
            var checkUser = GraphDatabaseHelpers.Instance.FindReportUser(userReportId, userReportedId);
            if (checkUser != null)
            {
                return Json(false);
            }
            GraphDatabaseHelpers.Instance.InsertReportUser(userReportId, userReportedId, typeReport);
            //Send warning email to reported user
            var email = GraphDatabaseHelpers.Instance.GetEmailByUserId(userReportedId);
            MailHelpers.Instance.SendMailWarningReportUser(email, userReportedId);

            return Json(true);
        }

        public int CountMutualFriend(string thisUserId, string otherUserId)
        {
            int thisId, oId;

            return !int.TryParse(thisUserId, out thisId) || !int.TryParse(otherUserId, out oId)
                ? 0
                : GraphDatabaseHelpers.Instance.CountMutualFriend(thisId, oId);
        }

        public int CountPlaces(string otherUserId)
        {
            int thisId;

            return !int.TryParse(otherUserId, out thisId)
                ? 0
                : GraphDatabaseHelpers.Instance.CountPlaceOfUser(thisId);
        }

        public JsonResult GetUser(string otherUserId)
        {
            int thisId;

            return !int.TryParse(otherUserId, out thisId)
                ? null
                : Json(GraphDatabaseHelpers.Instance.GetUser(thisId));
        }

        public string SaveImageDataToFile(string imageData, int userId, string type)
        {
            var currentUser = GraphDatabaseHelpers.Instance.GetUser(userId);
            string pathToSaveImage;
            string newAvaName = null;
            if (type.Equals("cover"))
            {
                currentUser.CoverUrl = "/Images/UserUpload/UserCover/" + "cover_uid_" + userId + ".jpg";
                GraphDatabaseHelpers.Instance.EditUserCover(userId, currentUser.CoverUrl);
                var uploadPath = "~/Images/UserUpload/UserCover/";

                ViewData["userSession"] = currentUser;
                UserHelpers.SetCurrentUser(Session, currentUser);
                pathToSaveImage = Path.Combine(System.Web.HttpContext.Current.Request.MapPath(uploadPath), "cover_uid_" + userId + ".jpg");
            }
            else
            {
                var uploadPath = "~/Images/UserUpload/UserAvatar/";

                newAvaName = currentUser.Avatar.StartsWith("http") || currentUser.Avatar.EndsWith("default-avatar.jpg")
                                 ? "ava_uid_" + currentUser.UserId + ".jpg"
                                 : currentUser.Avatar.Split('/').Last();

                currentUser.Avatar = "/Images/UserUpload/UserAvatar/" + newAvaName;
                UserHelpers.SetCurrentUser(Session, currentUser);
                GraphDatabaseHelpers.Instance.EditUserAvatar(userId, currentUser.Avatar);
                pathToSaveImage = Path.Combine(System.Web.HttpContext.Current.Request.MapPath(uploadPath), newAvaName);
            }

            byte[] bytes = Convert.FromBase64String(imageData.Split(',')[1]);

            using (var ms = new MemoryStream(bytes))
            {
                using (var img = Image.FromStream(ms))
                {
                    img.Save(pathToSaveImage);
                }
            }

            return "/Images/UserUpload/UserAvatar/" + newAvaName;
        }

        public JsonResult ViewNotification(int postId, int userId, string activity)
        {
            bool success = GraphDatabaseHelpers.Instance.ViewNotification(postId, userId, activity);
            return Json(success);
        }
    }
}