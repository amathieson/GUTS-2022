using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpatialCommClient.Models
{
    public class User
    {
        private int userID {get; set;}
        private string username { get; set; }

        public User(int userID, string username)
        {
            this.userID = userID;
            this.username = username;
        }

        public override string? ToString()
        {
            return this.userID + " - " + username;
        }

        public override bool Equals(object? obj)
        {
            return obj is User user &&
                   userID == user.userID;
        }
    }
}
