using EPiServer.SpecializedProperties;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.Security;
using EPiServer.ServiceApi.Configuration;
using EPiServer.Framework.Blobs;
using System.IO;
using EPiServer.ServiceApi.Models;
using EPiServer.ServiceLocation;
using EPiServer.Shell.Profile;

namespace ServiceAPIExtensions.Controllers
{
    [RequireHttps, RoutePrefix("episerverapi/user")]
    public class UserAPIController : ApiController
    {

        [AuthorizePermission("EPiServerServiceApi", "ReadAccess"), HttpGet, Route("list")]
        public virtual IHttpActionResult ListAllUsers()
        {
            var users = Membership.GetAllUsers().Cast<MembershipUser>().ToArray();
            //TODO: Support pagination
            var lst = new List<ExpandoObject>();
            foreach (var u in users)
            {
                dynamic d = BuildUserObject(u);
                lst.Add(d);
            }

            return Ok(lst.ToArray());
        }

        [HttpGet]
        [Route("roles")]
        [AuthorizePermission("EPiServerServiceApi", "ReadAccess")]
        public virtual IHttpActionResult ListAllRoles()
        {
            return Ok(Roles.GetAllRoles());
        }


        private static void ProfileTest()
        {
            var profilerep = ServiceLocator.Current.GetInstance<IProfileRepository>();
            var p = profilerep.GetProfile("Allan");
            var a = EPiServer.Personalization.EPiServerProfile.Get("Admin");
        }

        //TODO Profile
        private static dynamic BuildUserObject(MembershipUser u)
        {
            dynamic d = new ExpandoObject();
            var dic = d as IDictionary<string, object>;
            //dic.Add("Number", 42);
            d.UserName = u.UserName;
            d.ProviderUserKey = u.ProviderUserKey;
            d.ProviderName = u.ProviderName;
            d.Email = u.Email;
            d.Comment = u.Comment;
            d.CreationDate = u.CreationDate;
            d.IsLockedOut = u.IsLockedOut;
            d.IsOnline = u.IsOnline;
            d.LastActivityDate = u.LastActivityDate;
            return d;
        }

        [AuthorizePermission("EPiServerServiceApi", "WriteAccess"), HttpPut, Route("{UserName}")]
        public virtual IHttpActionResult CreateUpdateUser(string UserName, [FromBody] dynamic Payload)
        {
            var u = FindUser(UserName);
            var generatedPassword = string.Empty;
            if (u == null)
            {
                //User does not exist, create
                generatedPassword = Membership.GeneratePassword(10, 2);
                u = Membership.CreateUser(UserName, generatedPassword);
            }

            if (Payload == null)
            {
                return Ok(new { UserName = u.UserName, Password = generatedPassword });
            }

            var sRoles = (string)Payload.Roles;
            if (!string.IsNullOrWhiteSpace(sRoles))
            {
                AddRoles(u, sRoles);
            }

            var needUpdate = false;
            if (Payload.Email != null)
            {
                u.Email = (string)Payload.Email;
                needUpdate = true;
            }
            if (Payload.LastLoginDate != null)
            {
                u.LastLoginDate = (DateTime)Payload.LastLoginDate;
                needUpdate = true;
            }

            if (needUpdate)
            {
                Membership.UpdateUser(u);
            }

            return Ok(new { UserName = u.UserName, Password = generatedPassword });
        }

        [AuthorizePermission("EPiServerServiceApi", "WriteAccess"), HttpPut, Route("roles/{rolename}")]
        public virtual IHttpActionResult CreateRole(string rolename)
        {
            if (!Roles.RoleExists(rolename))
            {
                Roles.CreateRole(rolename);
            }
            return Ok();
        }

        [AuthorizePermission("EPiServerServiceApi", "WriteAccess"), HttpDelete, Route("roles/{rolename}")]
        public virtual IHttpActionResult DeleteRole(string rolename)
        {
            if (Roles.RoleExists(rolename))
            {
                Roles.DeleteRole(rolename);
            }
            else return NotFound();
            return Ok();
        }

        [AuthorizePermission("EPiServerServiceApi", "WriteAccess"), HttpPost, Route("roles/{rolename}")]
        public virtual IHttpActionResult AddUsersToRole(string rolename, [FromBody] dynamic Payload)
        {
            Roles.AddUsersToRole((string[])Payload.users, rolename);
            return Ok();
        }

        [AuthorizePermission("EPiServerServiceApi", "ReadAccess"), HttpGet, Route("{UserName}")]
        public virtual IHttpActionResult GetUser(string UserName)
        {
            var u = FindUser(UserName);

            return u != null ? (IHttpActionResult)Ok((ExpandoObject)BuildUserObject(u))
                : (IHttpActionResult)NotFound();
        }

        [AuthorizePermission("EPiServerServiceApi", "ReadAccess"), HttpGet, Route("{UserName}/roles")]
        public virtual IHttpActionResult GetRolesForUser(string UserName)
        {
            var u = FindUser(UserName);
            if (u == null)
            {
                return NotFound();
            }

            var lst = Roles.GetRolesForUser(u.UserName);
            return Ok(lst);
        }

        [HttpPut]
        [Route("{UserName}/roles")]
        [AuthorizePermission("EPiServerServiceApi", "WriteAccess")]
        public virtual IHttpActionResult PutUserInRoles(string UserName, [FromBody] dynamic Payload)
        {
            var u = FindUser(UserName);
            if (u == null)
            {
                return NotFound();
            }

            var sRoles = Payload != null ? (string)Payload.Roles : string.Empty;
            if (string.IsNullOrWhiteSpace(sRoles))
            {
                return BadRequest("Roles is required.");
            }
            
            AddRoles(u, sRoles);

            return Ok(Roles.GetRolesForUser(u.UserName));
        }

        [AuthorizePermission("EPiServerServiceApi", "WriteAccess"), HttpDelete, Route("{UserName}/roles")]
        public virtual IHttpActionResult RemoveUserFromRole(string UserName, [FromBody] dynamic Payload)
        {
            var u = FindUser(UserName);
            Roles.RemoveUserFromRole(u.UserName, (string)Payload.Role);
            var lst = Roles.GetRolesForUser(u.UserName);
            return Ok(lst);
        }


        private static MembershipUser FindUser(string UserName)
        {
            if (string.IsNullOrWhiteSpace(UserName))
            {
                return null;
            }

            var col = Membership.FindUsersByName(UserName);
            if ((col.Count == 0) && (UserName.Contains('@'))) col = Membership.FindUsersByEmail(UserName);
            var u = col.Cast<MembershipUser>().FirstOrDefault();
            return u;
        }


        [HttpGet]
        [Route("version")]
        public virtual ApiVersion Version()
        {
            return new ApiVersion();
        }

        private void AddRoles(MembershipUser user, string roles)
        {
            if (user == null && string.IsNullOrWhiteSpace(roles))
            {
                return;
            }

            var userRoles = roles.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var allRoles = Roles.GetAllRoles();
            var validRoles = userRoles.Where(r => !string.IsNullOrWhiteSpace(r) &&
                allRoles.Any(sr => sr.Equals(r, StringComparison.InvariantCultureIgnoreCase))).ToArray();

            Roles.AddUserToRoles(user.UserName, validRoles);
        }
    }
}