using Alchemy;
using Alchemy.Classes;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WsBackend
{
    class User
    {
        public string Name { get; private set; }
        public bool Dummy { get; private set; }
        public bool Online { get; set; }

        public User(string name, bool dummy = false)
        {
            Name = name;
            Online = false;
            Dummy = dummy;
            // Photo, details, etc
        }
    }


    class Message
    {
        public string Text { get; private set; }
        public DateTime Timestamp { get; private set; }
        public User User { get; private set; }

        public Message(string text, User user)
        {
            Text = text;
            Timestamp = DateTime.Now;
            User = user;
        }
    }


    class Server
    {
        ConcurrentDictionary<UserContext, User> _users = new ConcurrentDictionary<UserContext, User>();
        object _usersLock = new object();
        Random _userRnd = new Random();

        List<Message> _messages = new List<Message>();
        object _messagesLock = new object();


        public Server()
        {
            // Add a couple of test messages to test snapshot
            var user1 = new User("dummy1", true);
            var dummyContext1 = new UserContext(new Context(null, new System.Net.Sockets.TcpClient("www.bbc.co.uk", 80)));
            _users[dummyContext1] = user1;
            var user2 = new User("dummy2", true);
            var dummyContext2 = new UserContext(new Context(null, new System.Net.Sockets.TcpClient("www.bbc.co.uk", 443)));
            _users[dummyContext2] = user2;

            AddMessage(user1, "Hello world");
            AddMessage(user2, "foo");
            AddMessage(user1, "Test1");
            AddMessage(user2, "bar");

            // Periodic tick test
            new Timer(_ => {
                var msg = AddMessage(user1, "tick");
                BroadcastMessage(msg);
            }, null, 2000, 2000);
        }


        public void OnConnect(UserContext context)
        {
            Console.WriteLine("Connect: " + context.ClientAddress.ToString());
        }

        public void OnConnected(UserContext context)
        {
            Console.WriteLine("Connected: " + context.ClientAddress.ToString());
            var newUser = AddUniqueUser(context);
            if (newUser != null)
                Console.WriteLine("  Added user: " + newUser.Name);
            else
                Console.WriteLine("  Failed to add new user.");

            var messages = _messages.Select(m => new { Msg = m.Text, User = m.User.Name});
            var snapshot = JsonConvert.SerializeObject(new { Type = "snapshot", Msgs = messages });
            context.Send(snapshot);
        }

        public void OnDisconnect(UserContext context)
        {
            Console.WriteLine("Disconnect: " + context.ClientAddress.ToString());
        }

        public void OnSend(UserContext context)
        {
            Console.WriteLine("Send: " + context.ClientAddress.ToString());
        }

        public void OnReceive(UserContext context)
        {
            Console.WriteLine("Receive: " + context.ClientAddress.ToString());
        }

        User AddUniqueUser(UserContext context)
        {
            lock (_usersLock)
            {
                // If we can't add a new user after 1000 attempts (in this case)
                // of generating a random number, we've probably got bigger
                // problems (and would log it).
                var existingNames = new HashSet<string>(_users.Values.Select(u => u.Name));
                for (var i = 0; i < 1000; i++)
                {
                    var name = "User " + _userRnd.Next();
                    if (!existingNames.Contains(name))
                    {
                        return _users.AddOrUpdate(
                            context,
                            new User(name),
                            (addr, oldUser) => new User(name));
                    }
                }
                return null;
            }
        }

        Message AddMessage(User user, string text)
        {
            var message = new Message(text, user);
            lock (_messagesLock)
            {
                _messages.Add(message);
            }
            return message;
        }

        void BroadcastMessage(Message message)
        {
            lock (_usersLock)
            {
                var messageJson = JsonConvert.SerializeObject(
                    new { Type = "msg", Msg = message.Text, User = message.User.Name });
                var realUserContexts =
                    _users
                    .Where(kvp => !kvp.Value.Dummy)
                    .Select(kvp => kvp.Key);
                foreach (var userContext in realUserContexts)
	            {
		            userContext.Send(messageJson);
	            }
            }
        }
    }
}
