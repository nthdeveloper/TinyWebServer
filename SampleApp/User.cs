using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
{
    class User
    {
        public string UserName { get; private set; }
        public string Password { get; private set; }

        public User(string userName, string password)
        {
            this.UserName = userName;
            this.Password = password;
        }
    }
}
