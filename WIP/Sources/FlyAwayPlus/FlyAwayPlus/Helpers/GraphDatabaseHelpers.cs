﻿using FlyAwayPlus.Models;
using Neo4jClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using FlyAwayPlus.Models.Relationships;

namespace FlyAwayPlus.Helpers
{
    public class GraphDatabaseHelpers
    {
        private static readonly GraphClient Client = new GraphClient(new Uri(ConfigurationManager.AppSettings["dbGraphUri"]));

        public static void InsertUser(User user)
        {
            // Auto increment Id.
            user.userID = GetGlobalIncrementId();

            Client.Connect();
            var userRef = Client.Cypher.
                        Create("(user:user {newUser})").
                        WithParam("newUser", user)
                        .Return<User>("user")
                        .Results.Single();
        }

        public static void InsertPost(Post post, Photo photo, Place place)
        {
            // Auto increment Id.
            post.postID = GetGlobalIncrementId();
            photo.photoID = GetGlobalIncrementId();
            place.placeID = GetGlobalIncrementId();

            Client.Connect();

            NodeReference<Post> postRef = Client.Create(post);

            NodeReference<Photo> photoRef = Client.Create(photo);

            NodeReference<Place> placeRef = Client.Create(place);

            Client.CreateRelationship(postRef, new HasRelationship(photoRef));
            Client.CreateRelationship(postRef, new AtRelationship(placeRef));
        }

        public static User GetUser(int typeId, string email)
        {
            Client.Connect();
            var listUser = Client.Cypher.Match("(u:user {typeID:" + typeId + ", email: '" + email + "'})")
                .Return<User>("u")
                .Results.ToList();
            return listUser.Count == 0 ? null : listUser.First();
        }

        public static int GetGlobalIncrementId()
        {
            Client.Connect();
            var uniqueId = Client.Cypher.Merge("(id:GlobalUniqueId)")
                            .OnCreate().Set("id.count = 1")
                            .OnMatch().Set("id.count = id.count + 1")
                            .Return<int>("id.count AS uniqueID")
                            .Results.Single();

            return uniqueId;
        }

        public static Post findPost(int id, User user)
        {
            /*
                 * Query:
                 * Find:
                 *     - post of current user
                 *     - post with privacy = 'public'
                 *     - post of friend with privacy = 'friend'
                 * 
                 * match(u1:user {userID:@userID})-[:friend]->(u2:user)
                   with u1, u2
                   match(p:post {postID:@postID}) 
                   where (u1-[:has]->p) or (p.privacy='friend' and u2-[:has]->p) or (p.privacy = 'public')
                   return p
                 */
            Post p = null;
            Client.Connect();
            p = Client.Cypher.Match("(u1:user {userID:" + user.userID + "})-[:friend]->(u2:user)")
                            .With("u1, u2")
                            .Match("(p:post {postID:" + id + "})")
                            .Where("(u1-[:has]->p) or (p.privacy='friend' and u2-[:has]->p) or (p.privacy = 'public')")
                            .ReturnDistinct<Post>("p")
                            .Results.Single();

            return p;
        }

        public static User searchUser(Post post)
        {
            User user = null;
            Client.Connect();
            user = Client.Cypher.Match("(u:user)-[:has]->(p:post)")
                            .Where("p.postID=" + post.postID)
                            .ReturnDistinct<User>("u")
                            .Results.Single();
            return user;
        }

        public static List<Comment> findComment(Post post)
        {
            /*
                 * Query:
                 * Find:
                 *     - find comment of post
                 * 
                 * MATCH(p:post {postID:@postID})-[:has]->(c:comment)
                    return c
                 */
            List<Comment> list = null;
            Client.Connect();
            list = Client.Cypher.Match("(p:post {postID:" + post.postID + "})-[:has]->(c:comment)")
                            .Return<Comment>("c")
                            .OrderBy("c.dateCreated")
                            .Results.ToList();
            return list;
        }

        public static User findUser(Comment comment)
        {
            /*
                 * Query:
                 * Find:
                 *     - find User has comment
                 * 
                 * MATCH(u:user)-[:has]->(c:comment{commentID:@commentID})
                    return u
                 */
            User user = null;
            Client.Connect();
            user = Client.Cypher.Match("(u:user)-[:has]->(c:comment{commentID:" + comment.commentID + "})")
                            .Return<User>("u")
                            .Results.Single();
            return user;
        }

        public static User findUser(int id)
        {
            /*
                 * Query:
                 * Find:
                 *     - find User has id
                 * 
                 * MATCH(u:user {userID:@userID})
                    return u
                 */
            User user = null;
            try
            {
                Client.Connect();
                user = Client.Cypher.Match("(u:user {userID:" + id + "})")
                                .Return<User>("u")
                                .Results.Single();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                user = null;
            }
            return user;
        }

        public static User findUser(Post post)
        {
            /*
                 * Query:
                 * Find:
                 *     - find User has post
                 * 
                 * MATCH(u:user)-[:has]->(c:post{postID:@postID})
                    return u
                 */
            User user = null;
            Client.Connect();
            user = Client.Cypher.Match("(u:user)-[:has]->(p:post{postID:" + post.postID + "})")
                            .Return<User>("u")
                            .Results.Single();
            return user;
        }

        public static List<Post> searchAllPost()
        {

            /*
                 * Query:
                 * Find:
                 *     - Search all post with privacy public
                 * 
                 * match (p:post)
                    where p.privacy='public'
                    return p
                 */

            List<Post> listPost = null;
            Client.Connect();
            listPost = Client.Cypher.Match("(p:post)")
                            .Where("p.privacy='public'")
                            .Return<Post>("p")
                            .OrderByDescending("p.dateCreated")
                            .Results.ToList();
            return listPost;
        }

        public static Photo findPhoto(Post po)
        {

            /*
                 * Query:
                 * Find:
                 *     - Find photo of post
                 * 
                 * match (po:post {postID:@postID})-[:has]->(ph:photo)
                    return ph
                 */

            Photo photo = null;
            Client.Connect();
            photo = Client.Cypher.Match("(po:post {postID:" + po.postID + "})-[:has]->(ph:photo)")
                            .Return<Photo>("ph")
                            .OrderBy("ph.photoID")
                            .Results
                            .ToList()
                            .First();
            return photo;
        }

        public static Place findPlace(Post po)
        {

            /*
                 * Query:
                 * Find:
                 *     - Find Place of post
                 * 
                 * match (po:post {postID:@postID})-[:at]->(pl:place)
                    return pl
                 */

            Place place = null;
            Client.Connect();
            place = Client.Cypher.Match("(po:post {postID:" + po.postID + "})-[:at]->(pl:place)")
                            .Return<Place>("pl")
                            .OrderBy("pl.placeID")
                            .Results
                            .ToList()
                            .First();
            return place;
        }

        public static List<Post> findPostFollowing(User user)
        {
            /*
                 * Query:
                 * Find:
                 *     - post of current user
                 *     - post with privacy = 'public'
                 *     - post of friend with privacy = 'friend'
                 * 
                 * match(u1:user {userID:@userID})-[:friend]->(u2:user)
                   with u1, u2
                   match(p:post) 
                   where (u1-[:has]->p) or (p.privacy='friend' and u2-[:has]->p) or (p.privacy = 'public')
                   return p
                 */
            List<Post> listPost = null;
            Client.Connect();
            listPost = Client.Cypher.Match("(u1:user {userID:" + user.userID + "})-[:friend]->(u2:user)")
                            .With("u1, u2")
                            .Match("(p:post)")
                            .Where("(u1-[:has]->p) or (p.privacy='friend' and u2-[:has]->p) or (p.privacy = 'public')")
                            .ReturnDistinct<Post>("p")
                            .OrderByDescending("p.dateCreated")
                            .Results.ToList();

            return listPost;
        }

        public static List<Post> findPostOfUser(User user)
        {
            /*
                 * Query:
                 * Find:
                 *     - all post of current user
                 * 
                 * match(u1:user {userID:@userID})-[:has]->(p:post)
                   return p
                 */
            List<Post> listPost = null;
            Client.Connect();
            listPost = Client.Cypher.Match("(u1:user {userID:" + user.userID + "})-[:has]->(p:post)")
                            .ReturnDistinct<Post>("p")
                            .OrderByDescending("p.dateCreated")
                            .Results.ToList();

            return listPost;
        }

        public static List<Post> findPostOfOtherUser(User currentUser, User otherUser)
        {
            /*
                 * Query:
                 * Find:
                 *     - all post of otherUser that currentUser can view
                 * 
                 * match(u1:user {userID:@otherUserID}),(u2:user {userID:@currentUserID})
                    with u1,u2
                    match(p:post)<-[:has]-u1
                    where p.privacy='public' or (p.privacy='friend' and u1-[:friend]->u2)
                    return p
                 */
            List<Post> listPost = null;
            Client.Connect();
            listPost = Client.Cypher.Match("(u1:user {userID:" + otherUser.userID + "}), (u2:user {userID:" + currentUser.userID + "})")
                            .With("u1, u2")
                            .Match("(p:post)<-[:has]-u1")
                            .Where("p.privacy='public' or (p.privacy='friend' and u1-[:friend]->u2)")
                            .ReturnDistinct<Post>("p")
                            .OrderByDescending("p.dateCreated")
                            .Results.ToList();

            return listPost;
        }

        public static List<User> findFriend(User user)
        {

            /*
                 * Query:
                 * Find:
                 *     - all friend of current user
                 * 
                 * match(u1:user{userID:@userID})-[m:friend]->(u2:user)
                    return u2;
                 */
            List<User> listUser = null;
            Client.Connect();
            listUser = Client.Cypher.Match("(u1:user {userID:" + user.userID + "})-[:friend]->(u2:user)")
                            .ReturnDistinct<User>("u2")
                            .Results.ToList();

            return listUser;
        }
    }
}