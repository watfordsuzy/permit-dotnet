﻿using System.Collections.Generic;

namespace PermitDotnet.Models
{
    public class User
    {
        public string id = "";
        public string firstName = "";
        public string lastName = "";
        public string email = "";
        public RoleAssignment[] roles = null;
        public Dictionary<string, string> attributes = null;

        public User(
            string email,
            string firstName = "",
            string lastName = "",
            RoleAssignment[] roles = null,
            Dictionary<string, string> attributes = null
        )
        {
            this.email = email;
            this.firstName = firstName;
            this.lastName = lastName;
            this.roles = roles;
            this.attributes = attributes;
        }

        private User() { }
    }


}
