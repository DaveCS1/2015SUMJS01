﻿using FlyAwayPlus.Helpers;

namespace FlyAwayPlus.Models
{
    public class Room
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public string Description { get; set; }
        public string DateCreated { get; set; }
        public string StartDate { get; set; }
        public int LengthInDays { get; set; }
        public int JoinedSlotCount { get; set; }
        public int MaxNoSlots { get; set; }
        public string StartLocation { get; set; }
        public string StartLongitude { get; set; }
        public string StartLatitude { get; set; }
        public string DestinationLocation { get; set; }
        public string DestinationLongitude { get; set; }
        public string DestinationLatitude { get; set; }
        public string Privacy { get; set; }
        public string PhotoCoverUrl { get; set; }
        public string ToRealtime()
        {
            return DateHelpers.Instance.DisplayRealtime(DateCreated);
        }
    }
}