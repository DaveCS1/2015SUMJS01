﻿using FlyAwayPlus.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FlyAwayPlus.Models;
using Microsoft.Ajax.Utilities;

namespace FlyAwayPlus.Controllers
{
    public class RoomController : Controller
    {
        //
        // GET: /Room/
        public ActionResult Index(string startplace, string targetplace, string startdate, string returndate, int? slotOption, int id = -1)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            if (user == null)
            {
                RedirectToAction("Index", "Home");
            }
            List<Room> listRoom;
            List<User> listAdminRoom = new List<User>();

            if (id == -1)
            {
                // Search all room.
                listRoom = GraphDatabaseHelpers.Instance.SearchRoomByKeyword();

                FilterByStartPlace(ref listRoom, startplace);
                FilterByTargetPlace(ref listRoom, targetplace);
                FilterByStartDate(ref listRoom, startdate);
                FilterByReturnDate(ref listRoom, returndate);
                FilterBySlot(ref listRoom, slotOption);

                listAdminRoom.AddRange(listRoom.Select(room => GraphDatabaseHelpers.Instance.FindAdminInRoom(room.RoomId)));
            }
            else
            {
                listRoom = GraphDatabaseHelpers.Instance.SearchRoomByUserId(user.UserId);   // search room of current User
                for (int i = 0; i < listRoom.Count; i++)
                {
                    listAdminRoom.Add(user);
                }
            }

            ViewData["listRoom"] = listRoom;
            ViewData["listAdminRoom"] = listAdminRoom;
            return View();
        }

        private void FilterBySlot(ref List<Room> listRoom, int? slotOption)
        {
            if (slotOption == null || slotOption.Value == 0) return;

            listRoom = listRoom.Where(r => 5 * (slotOption - 1) <= (r.MaxNoSlots - r.JoinedSlotCount) 
                                            && (r.MaxNoSlots - r.JoinedSlotCount) <= 5 * slotOption).ToList();
        }

        private void FilterByStartDate(ref List<Room> listRoom, string startdate)
        {
            if (string.IsNullOrWhiteSpace(startdate)) return;

            listRoom = listRoom.Where(r => DateTime.ParseExact(r.StartDate, FapConstants.DateFormat, CultureInfo.InvariantCulture) ==
                                           DateTime.ParseExact(startdate, FapConstants.DateFormat, CultureInfo.InvariantCulture)).ToList();
        }

        private void FilterByReturnDate(ref List<Room> listRoom, string returndate)
        {
            if (string.IsNullOrWhiteSpace(returndate)) return;

            listRoom = listRoom.Where(r => DateTime.ParseExact(r.StartDate, FapConstants.DateFormat, CultureInfo.InvariantCulture).AddDays(r.LengthInDays) ==
                                           DateTime.ParseExact(returndate, FapConstants.DateFormat, CultureInfo.InvariantCulture)).ToList();
        }

        private void FilterByTargetPlace(ref List<Room> listRoom, string targetplace)
        {
            if(string.IsNullOrWhiteSpace(targetplace)) return;

            listRoom = listRoom.Where(r => r.DestinationLocation.ToLower().Contains(targetplace.ToLower())).ToList();
        }

        private void FilterByStartPlace(ref List<Room> listRoom, string startplace)
        {
            if (string.IsNullOrWhiteSpace(startplace)) return;
            
            listRoom = listRoom.Where(r => r.StartLocation.ToLower().Contains(startplace.ToLower())).ToList();
        }

        public ActionResult RoomDetail(string id)
        {
            int roomId;
            if (id == null || !int.TryParse(id, out roomId))
            {
                return RedirectToAction("Index", "Home");
            }

            User user = UserHelpers.GetCurrentUser(Session);
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            Room roomInfo = GraphDatabaseHelpers.Instance.GetRoomInformation(roomId);
            List<Post> listPost = GraphDatabaseHelpers.Instance.FindPostInRoom(roomId, 0);
            User admin = GraphDatabaseHelpers.Instance.FindAdminInRoom(roomId);
            List<User> listUserInRoom = GraphDatabaseHelpers.Instance.FindUserInRoom(roomId);
            List<User> listUserRequestJoinRoom = GraphDatabaseHelpers.Instance.FindUserRequestJoinRoom(roomId);
            List<Message> listMessage = GraphDatabaseHelpers.Instance.GetListMessageInRoom(roomId, 0);
            List<User> listUserOwnMessage = listMessage.Select(message => GraphDatabaseHelpers.Instance.FindUser(message)).ToList();
            string relationInRoom = "";
            if (listUserInRoom.Any(u => u.UserId == user.UserId))
            {
                relationInRoom = "Member";
            }
            else if (listUserRequestJoinRoom.Any(u => u.UserId == user.UserId))
            {
                relationInRoom = "Request";
            }


            var roomStartDate = DateTime.ParseExact(roomInfo.StartDate, FapConstants.DateFormat, CultureInfo.InvariantCulture);
            var roomEndDate = roomStartDate.AddDays(roomInfo.LengthInDays);

            List<Plan> listGeneralPlan = GraphDatabaseHelpers.Instance.LoadAllPlansInDateRange(DateTime.MinValue, DateTime.MaxValue, FapConstants.PlanGeneral, roomId);
            ViewData["dictPlanIdPICs"] = listGeneralPlan
                                        .Select(p => p.PlanId)
                                        .ToDictionary(pid => pid, pid => GraphDatabaseHelpers.Instance.GetPersonInCharge(pid));

            ViewData["dictPlanIdCreator"] = listGeneralPlan
                                        .Select(p => p.PlanId)
                                        .ToDictionary(pid => pid, pid => GraphDatabaseHelpers.Instance.GetPersonCreatesPlan(pid));

            FindRelatedInformationPost(listPost);
            ViewData["roomInfo"] = roomInfo;
            ViewData["admin"] = admin;
            ViewData["listUserInRoom"] = listUserInRoom;
            ViewData["listUserRequestJoinRoom"] = listUserRequestJoinRoom;
            ViewData["listMessage"] = listMessage;
            ViewData["listUserOwnMessage"] = listUserOwnMessage;
            ViewData["listGeneralPlan"] = listGeneralPlan;
            ViewData["relationInRoom"] = relationInRoom;
            Session["roomID"] = ViewData["roomID"] = roomId;
            ViewData["placeName"] = roomInfo.DestinationLocation;
            return View();
        }

        private void FindRelatedInformationPost(List<Post> listPost)
        {
            User user = UserHelpers.GetCurrentUser(Session);
            Dictionary<int, List<Photo>> listPhotoDict = new Dictionary<int, List<Photo>>();
            Dictionary<int, Video> listVideoDict = new Dictionary<int, Video>();
            Dictionary<int, User> listUserDict = new Dictionary<int, User>();
            Dictionary<int, int> dictLikeCount = new Dictionary<int, int>();
            Dictionary<int, int> dictDislikeCount = new Dictionary<int, int>();
            Dictionary<int, int> dictCommentCount = new Dictionary<int, int>();
            Dictionary<int, int> dictUserCommentCount = new Dictionary<int, int>();
            Dictionary<int, bool> isLikeDict = new Dictionary<int, bool>();
            Dictionary<int, bool> isDislikeDict = new Dictionary<int, bool>();
            Dictionary<int, List<Comment>> dictListComment = new Dictionary<int, List<Comment>>();
            Dictionary<int, User> dict = new Dictionary<int, User>();
            foreach (int postId in listPost.Select(p => p.PostId))
            {
                listPhotoDict.Add(postId, GraphDatabaseHelpers.Instance.FindPhoto(postId));
                listVideoDict.Add(postId, GraphDatabaseHelpers.Instance.FindVideo(postId));
                listUserDict.Add(postId, GraphDatabaseHelpers.Instance.FindUserByPostInRoom(postId));
                dictLikeCount.Add(postId, GraphDatabaseHelpers.Instance.CountLike(postId));
                dictDislikeCount.Add(postId, GraphDatabaseHelpers.Instance.CountDislike(postId));
                dictCommentCount.Add(postId, GraphDatabaseHelpers.Instance.CountComment(postId));
                dictUserCommentCount.Add(postId, GraphDatabaseHelpers.Instance.CountUserComment(postId));
                var listComment = GraphDatabaseHelpers.Instance.FindComment(postId);
                dictListComment.Add(postId, listComment);

                foreach (var comment in listComment)
                {
                    dict.Add(comment.CommentId, GraphDatabaseHelpers.Instance.FindUser(comment));
                }

                if (user != null)
                {
                    isLikeDict.Add(postId, GraphDatabaseHelpers.Instance.IsLike(postId, user.UserId));
                    isDislikeDict.Add(postId, GraphDatabaseHelpers.Instance.IsDislike(postId, user.UserId));
                }
                else
                {
                    isLikeDict.Add(postId, false);
                    isDislikeDict.Add(postId, false);
                }
            }
            ViewData["userPost"] = user;
            ViewData["listPost"] = listPost;
            ViewData["listPhotoDict"] = listPhotoDict;
            ViewData["listVideoDict"] = listVideoDict;
            ViewData["listUserDict"] = listUserDict;
            ViewData["dislikeCount"] = dictDislikeCount;
            ViewData["likeCount"] = dictLikeCount;
            ViewData["commentCount"] = dictCommentCount;
            ViewData["userCommentCount"] = dictUserCommentCount;
            ViewData["isLikeDict"] = isLikeDict;
            ViewData["isDislikeDict"] = isDislikeDict;
            ViewData["dictListComment"] = dictListComment;
            ViewData["dict"] = dict;

            if (listPost.Count < FapConstants.RecordsPerPage)
            {
                ViewData["isLoadMore"] = "false";
            }
            else
            {
                ViewData["isLoadMore"] = "true";
            }
        }

        ////////////////
        // AJAX CALLS //
        ///////////////

        public void UpdateTimelinePlan(string id, string newEventStart, string newEventEnd)
        {
            GraphDatabaseHelpers.Instance
                .UpdatePlanEvent(id, DateTime.Parse(newEventStart, null, DateTimeStyles.RoundtripKind),
                                     DateTime.Parse(newEventEnd, null, DateTimeStyles.RoundtripKind));
        }

        public bool SaveTimelinePlan(string title, string newEventDate, string newEventTime, string newEventDuration)
        {
            var userId = int.Parse(@Session["UserId"] + string.Empty);

            var newPlan = new Plan
            {
                DatePlanStart = newEventDate + " " + newEventTime,
                LengthInMinute = int.Parse(newEventDuration) * 60,
                WorkItem = title,
                PlanType = FapConstants.PlanTimeline,
                DatePlanCreated = DateTime.Now.ToString(FapConstants.DatetimeFormat, CultureInfo.InvariantCulture)
            };

            return GraphDatabaseHelpers.Instance.InsertPlan(newPlan, (int)Session["roomID"], userId);
        }

        public JsonResult GetPlanEvents(DateTime start, DateTime end)
        {
            int roomid = (int)Session["roomID"];

            var apptListForDate = GraphDatabaseHelpers.Instance.LoadAllPlansInDateRange(start, end, FapConstants.PlanTimeline, roomid);

            if (!apptListForDate.Any())
            {
                return null;
            }

            var eventList = apptListForDate.Select(e => new
            {
                id = e.PlanId,
                title = e.WorkItem,
                start = e.DatePlanStart,
                end = DateTime.ParseExact(e.DatePlanStart, FapConstants.DatetimeFormat, CultureInfo.InvariantCulture).AddMinutes(e.LengthInMinute).ToString(FapConstants.DatetimeFormat, CultureInfo.InvariantCulture),
                allday = false
            });

            var rows = eventList.ToArray();
            return Json(rows, JsonRequestBehavior.AllowGet);
        }

        public RedirectToRouteResult CreateRoom(FormCollection form)
        {
            var roomName = Request.Form["roomname"];
            var roomDesc = Request.Form["roomdesc"];
            var startdate = DateTime.ParseExact(Request.Form["startdate"], FapConstants.DateFormat, CultureInfo.InvariantCulture);
            var enddate = DateTime.ParseExact(Request.Form["enddate"], FapConstants.DateFormat, CultureInfo.InvariantCulture);
            var startAddressName = Request.Form["start_name"];
            var startAdress = Request.Form["start_formatted_address"];
            var startLng = Request.Form["start_lng"];
            var startLat = Request.Form["start_lat"];
            var targetAddressName = Request.Form["end_name"];
            var targetAddress = Request.Form["end_formatted_address"];
            var endLng = Request.Form["end_lng"];
            var endLat = Request.Form["end_lat"];
            int maxNoOfSlots;
            int.TryParse(Request.Form["maxnoslots"], out maxNoOfSlots);

            var currentUserId = int.Parse(@Session["UserId"] + string.Empty);

            var newRoom = new Room
            {
                RoomName = roomName,
                Description = roomDesc,
                JoinedSlotCount = 1,
                MaxNoSlots = maxNoOfSlots,
                StartDate = startdate.ToString(FapConstants.DateFormat, CultureInfo.InvariantCulture),
                LengthInDays = (int)(enddate - startdate).TotalDays,
                StartLocation = startAddressName + " - " + startAdress,
                StartLatitude = startLat,
                StartLongitude = startLng,
                DestinationLocation = targetAddressName + " - " + targetAddress,
                DestinationLatitude = endLat,
                DestinationLongitude = endLng,
                Privacy = "public",
                DateCreated = DateTime.Now.ToString(FapConstants.DatetimeFormat),
                PhotoCoverUrl = string.Empty
            };

            GraphDatabaseHelpers.Instance.InsertRoom(newRoom, currentUserId);
            return RedirectToAction("Index", "Room");
        }

        public JsonResult AddGeneralPlan(string workportion, string note, string assignee, string startdate)
        {
            var assignerId = int.Parse(@Session["UserId"] + string.Empty);
            int roomid = (int)Session["roomID"];
            List<int> assigneesInt = assignee.Split(';').Select(int.Parse).ToList();

            var newPlan = new Plan
            {
                WorkItem = workportion,
                WorkItemDetail = note,
                DatePlanStart = startdate + " 00:00:00",
                PlanType = FapConstants.PlanGeneral,
                DatePlanCreated = DateTime.Now.ToString(FapConstants.DatetimeFormat, CultureInfo.InvariantCulture)
            };

            GraphDatabaseHelpers.Instance.InsertPlan(newPlan, roomid, assignerId, assigneesInt);

            return Json(assigneesInt.Select(a => GraphDatabaseHelpers.Instance.FindUser(a)));
        }

        public JsonResult AddEstimation(string payment, double price, int payerid, string datecreated)
        {
            var creatorId = int.Parse(Session["UserId"] + string.Empty);
            int roomid = int.Parse(Session["roomID"] + string.Empty);

            var newEstimation = new Estimation
            {
                Payment = payment,
                Price = price,
                DateCreated = datecreated
            };

            return Json(GraphDatabaseHelpers.Instance.InsertEstimation(newEstimation, roomid, creatorId, payerid));
        }

        public JsonResult EditEstimation(int id, string payment, double price, int payerid)
        {
            var newEstimation = new Estimation
            {
                EstimationId = id,
                Payment = payment,
                Price = price
            };

            return Json(GraphDatabaseHelpers.Instance.EditEstimation(newEstimation, payerid));
        }

        public JsonResult DeleteEstimation(int id)
        {
            return Json(GraphDatabaseHelpers.Instance.DeleteEstimation(id));
        }

        public JsonResult GetEstimationData(int roomId)
        {
            var estimationsList = GraphDatabaseHelpers.Instance.GetRoomEstimation(roomId);
            var estimationDataInTable = new List<object>();

            foreach (var est in estimationsList)
            {
                var creator = GraphDatabaseHelpers.Instance.GetEstimationCreator(est.EstimationId);
                var payer = GraphDatabaseHelpers.Instance.GetEstimationPayer(est.EstimationId);

                estimationDataInTable.Add(
                    new
                    {
                        id = est.EstimationId,
                        payment = est.Payment,
                        price = est.Price,
                        creator = creator.FirstName + " " + creator.LastName,
                        creatorid = creator.UserId,
                        payer = payer.FirstName + " " + payer.LastName,
                        payerid = payer.UserId,
                        datecreated = est.DateCreated
                    });
            }

            return Json(estimationDataInTable, JsonRequestBehavior.AllowGet);
        }

        public JsonResult RequestJoinRoom(int roomId, int userId)
        {
            bool success = false;
            if (Request.IsAjaxRequest())
            {
                success = GraphDatabaseHelpers.Instance.RequestJoinRoom(roomId, userId);
            }
            return Json(success);
        }

        public JsonResult AcceptRequestJoinRoom(int roomId, int userId)
        {
            bool success = false;
            if (Request.IsAjaxRequest())
            {
                success = GraphDatabaseHelpers.Instance.AcceptRequestJoinRoom(roomId, userId);
            }
            return Json(success);
        }

        public JsonResult DeclineRequestJoinRoom(int roomId, int userId)
        {
            bool success = false;
            if (Request.IsAjaxRequest())
            {
                success = GraphDatabaseHelpers.Instance.DeclineRequestJoinRoom(roomId, userId);
            }
            return Json(success);
        }

        public RedirectToRouteResult AddPostInRoom()
        {
            User user = UserHelpers.GetCurrentUser(Session);
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index", "Room");
        }

        public ActionResult AddPost(string queryString, int roomId)
        {
            var user = UserHelpers.GetCurrentUser(Session);
            var form = HttpUtility.ParseQueryString(queryString);

            string message = form["message"];
            List<string> images = form["uploadedimages"].Split('#').ToList();
            images.RemoveAt(0);
            string privacy = "room";
            string uploadedVideoYoutubeId = form["uploadedvideo"];

            Room room = GraphDatabaseHelpers.Instance.FindRoomById(roomId);

            Post newPost = new Post
            {
                Content = message,
                DateCreated = DateTime.Now.ToString(FapConstants.DatetimeFormat),
                Privacy = privacy
            };

            Video newVideo = null;
            if (!uploadedVideoYoutubeId.IsNullOrWhiteSpace())
            {
                newVideo = new Video
                {
                    Path = uploadedVideoYoutubeId,
                    DateCreated = DateTime.Now.ToString(FapConstants.DatetimeFormat)
                };
            }

            List<Photo> newPhotos = images.Select(img => new Photo { DateCreated = DateTime.Now.ToString(FapConstants.DatetimeFormat), Url = img }).ToList();

            // Insert to Database
            GraphDatabaseHelpers.Instance.InsertPostInRoom(user, roomId, ref newPost, ref newPhotos, ref newVideo);

            ViewData["userPost"] = user;
            ViewData["listPhoto"] = newPhotos;
            ViewData["video"] = newVideo;
            ViewData["placeName"] = room.DestinationLocation;
            ViewData["roomID"] = room.RoomId;

            Dictionary<int, List<Photo>> listPhotoDict = new Dictionary<int, List<Photo>>();
            Dictionary<int, List<Comment>> dictListComment = new Dictionary<int, List<Comment>>();
            Dictionary<int, int> dictLikeCount = new Dictionary<int, int>();
            Dictionary<int, int> dictDislikeCount = new Dictionary<int, int>();
            Dictionary<int, int> dictUserCommentCount = new Dictionary<int, int>();
            Dictionary<int, Video> listVideoDict = new Dictionary<int, Video>();
            Dictionary<int, User> listUserDict = new Dictionary<int, User>();

            dictLikeCount.Add(newPost.PostId, 0);
            dictDislikeCount.Add(newPost.PostId, 0);
            dictUserCommentCount.Add(newPost.PostId, 0);
            dictListComment.Add(newPost.PostId, new List<Comment>());
            listPhotoDict.Add(newPost.PostId, new List<Photo>());
            listVideoDict.Add(newPost.PostId, newVideo);
            listUserDict.Add(newPost.PostId, user);

            ViewData["likeCount"] = dictLikeCount;
            ViewData["dislikeCount"] = dictDislikeCount;
            ViewData["userCommentCount"] = dictUserCommentCount;
            ViewData["dictListComment"] = dictListComment;
            ViewData["listPhotoDict"] = listPhotoDict;
            ViewData["listVideoDict"] = listVideoDict;
            ViewData["listUserDict"] = listUserDict;

            return PartialView("_PostDetailCommonPartial", newPost);
        }
    }
}