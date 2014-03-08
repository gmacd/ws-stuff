using Alchemy;
using Alchemy.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WsBackend
{
    class User
    {
        public string Name { get; private set; }
        public bool Online { get; set; }

        public User(string name)
        {
            Name = name;
            Online = false;
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
        ulong _nextNewUserId = 0;

        List<Message> _messages = new List<Message>();
        object _messagesLock = new object();


        public Server()
        {
            // Add a couple of test messages to test snapshot
            /*var user1 = new User("dummy1");
            var dummyContext1 = new UserContext(new Context(null, new TcpClient("www.bbc.co.uk", 80)));
            _users[dummyContext1] = user1;
            var user2 = new User("dummy2");
            var dummyContext2 = new UserContext(new Context(null, new TcpClient("www.bbc.co.uk", 443)));
            _users[dummyContext2] = user2;

            AddMessage(user1, "Hello world");
            AddMessage(user2, "foo");
            AddMessage(user1, "Test1");
            AddMessage(user2, "bar");

            // Periodic tick test
            new Timer(_ => {
                var msg = AddMessage(user1, "tick");
                BroadcastMessage(msg);
            }, null, 2000, 2000);*/
        }


        public void OnConnect(UserContext context)
        {
            Console.WriteLine("Connect: " + context.ClientAddress.ToString());
        }

        public void OnConnected(UserContext context)
        {
            Console.WriteLine("Connected: " + context.ClientAddress.ToString());
            var user = RegisterUser(context);
            if (user != null)
                Console.WriteLine("  Registered user: " + user.Name);
            else
                Console.WriteLine("  Failed to register user.");

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
            var msg = (JObject)JsonConvert.DeserializeObject(context.DataFrame.ToString());

            JToken type;
            if (msg.TryGetValue("Type", out type) && type.Value<string>() == "newMsg")
            {
                JToken message;
                if (msg.TryGetValue("Message", out message))
                {
                    var newMessage = AddMessage(_users[context], message.Value<string>());
                    BroadcastMessage(newMessage);
                }
            }
        }

        /// <summary>
        /// If the user has previously registered, change account to online,
        /// if the user is new, create a new unique account
        /// </summary>
        User RegisterUser(UserContext context)
        {
            lock (_usersLock)
            {
                var user = _users.GetOrAdd(
                    context,
                    ctx => {
                        var name = "NewUser" + _nextNewUserId;
                        _nextNewUserId++;
                        return new User(name);
                    });
                user.Online = true;
                return user;
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

        /// <summary>
        /// Broadcast message to all users.
        /// </summary>
        void BroadcastMessage(Message message)
        {
            lock (_usersLock)
            {
                var messageJson = JsonConvert.SerializeObject(
                    new { Type = "msg", Msg = message.Text, User = message.User.Name });
                var onlineUserContexts =
                    _users
                    .Where(kvp => kvp.Value.Online)
                    .Select(kvp => kvp.Key);
                foreach (var userContext in onlineUserContexts)
	            {
		            userContext.Send(messageJson);
	            }
            }
        }
    }
}
