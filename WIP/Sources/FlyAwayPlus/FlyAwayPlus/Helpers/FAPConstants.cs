﻿namespace FlyAwayPlus.Helpers
{
    public static class FapConstants
    {
        public static string DatetimeFormat = "yyyy-MM-dd HH:mm:ss";
        public static string DateFormat = "yyyy-MM-dd";
        public static int UploadedImageMaxWidthPixcel = 800;
        public static int UploadedImageMaxHeightPixcel = 800;
        public static string[] ImageFileExtensions = { ".jpg", ".png", ".jpeg" };
        public static int RecordsPerPage = 20;

        public static int JoinAdmin     = 0;
        public static int JoinMember    = 1;
        public static int JoinRequest   = 2;

        public static int PlanGeneral    = 0;
        public static int PlanTimeline   = 1;

        public static string UserActive = "active";
        public static string UserLock = "lock";
        public static string PostLock = "lock";

        public static string Friendship = "friend";
        public static string RequestFriend = "request";
        public static string NotFriend = "none";

        public static int Viewed = 1;
        public static int NotViewed = 0;
    }
}