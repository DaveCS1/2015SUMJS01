﻿using FlyAwayPlus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace FlyAwayPlus.Helpers
{
    public class UserHelpers
    {
        private const string USER = "user";
        public static User GetCurrentUser(HttpSessionStateBase session)
        {

            var userSession = session[USER];

            if (userSession == null || userSession.ToString().Length == 0)
            {
                return null;
            }
            var user = (User)userSession;

            return user;
        }
        public static void SetCurrentUser(HttpSessionStateBase session, User user)
        {
            session[USER] = user;
        }
    }
}