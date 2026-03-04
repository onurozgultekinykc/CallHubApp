using CallHubApp.Models;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CallHubApp.Hubs
{
    public class CallHub : Hub
    {
        private static readonly ConcurrentDictionary<string, UserModel> connectedUsers = new();
        private static readonly HttpClient http = new(); // Tek instance, socket leak yok

        // dışarıdan config gelecek
        private readonly List<ServerConfig> _servers;

        public CallHub(List<ServerConfig> servers)
        {
            _servers = servers;
        }




        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var phone = httpContext?.Request.Query["phone"].ToString();
            var userpassword = httpContext?.Request.Query["userpassword"].ToString();

            if (string.IsNullOrWhiteSpace(phone))
                phone = Context.ConnectionId; // fallback

            Console.WriteLine($"[CONNECTED] {Context.ConnectionId} -> {phone}");

            // aynı CallerId varsa önce sil
            foreach (var kvp in connectedUsers.Where(x => x.Value.CallerId == Context.ConnectionId).ToList())
                connectedUsers.TryRemove(kvp.Key, out _);

            // ---- Yönlendirme mantığı ----
            await HandleConnectionLogic(phone, userpassword, Context.ConnectionId);
            // -----------------------------

            await BroadcastUserList();
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // aynı CallerId’ye sahip tüm kayıtları sil
            foreach (var kvp in connectedUsers.Where(x => x.Value.CallerId == Context.ConnectionId).ToList())
                connectedUsers.TryRemove(kvp.Key, out _);

            await BroadcastUserList();
            await base.OnDisconnectedAsync(exception);
        }

        private async Task BroadcastUserList()
        {
            var users = connectedUsers.Values.ToList();
            await Clients.All.SendAsync("UserList", users);
        }

        private async Task HandleConnectionLogic(string phone, string userpassword, string caller)
        {
            if (!string.IsNullOrEmpty(phone) && char.IsLetter(phone[0]))
            {
                await SendEchoIfMatched(phone, caller);
            }
            else
            {
                await PostToServersIfNotMatched(phone, userpassword, caller);
            }
        }

        private async Task SendEchoIfMatched(string phone, string caller)
        {
            // aynı CallerId varsa önce sil
            foreach (var kvp in connectedUsers.Where(x => x.Value.CallerId == caller).ToList())
                connectedUsers.TryRemove(kvp.Key, out _);

            var testEcho = new UserModel
            {
                CallerId = caller,
                FullName = "Arayüz",
                ProductId = Guid.Empty,
                Phone = phone,
                UserId = Guid.Empty,
                Email = "dashboard@localhost",
            };

            connectedUsers[phone] = testEcho;
            await Clients.Client(caller).SendAsync("UserModel", testEcho);
        }

        private async Task PostToServersIfNotMatched(string phone, string userpassword, string caller)
        {
            bool success = false;
            string lastResponse = null;

            foreach (var server in _servers)
            {
                // ✅ URL mantığı düzeltildi
                var url = server.Port != 0
                    ? $"http://{server.Host}:{server.Port}/api/PhoneCallApp/AreYouThere"
                    : $"https://{server.Host}/api/PhoneCallApp/AreYouThere";

                var maskedPhone = $"0({phone[0]}{phone[1]}{phone[2]}) {phone[3]}{phone[4]}{phone[5]}-{phone[6]}{phone[7]}{phone[8]}{phone[9]}";
                var payload = new { phone = maskedPhone, password = userpassword };
                var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");

                try
                {
                    var response = await http.PostAsync(url, content);
                    lastResponse = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            var user = JsonSerializer.Deserialize<UserModel>(lastResponse);

                            if (user != null)
                            {
                                // aynı CallerId varsa önce sil
                                foreach (var kvp in connectedUsers.Where(x => x.Value.CallerId == caller).ToList())
                                    connectedUsers.TryRemove(kvp.Key, out _);

                                user.Host = server.Port != 0
                                    ? $"http://{server.Host}:{server.Port}/api"
                                    : $"https://{server.Host}/api";
                                user.CallerId = caller;

                                await Clients.Client(caller).SendAsync("UserModel", user);
                                connectedUsers[user.Phone] = user;

                                success = true;
                                break; // ✅ İlk başarılı yanıt yeter
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"UserModel parse hatası: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"POST hatası {url}: {ex.Message}");
                }
            }

            if (!success)
                await Clients.Client(caller).SendAsync("RawResponse");
        }

        public async Task HostListener(string candidatename, string from, string to)
        {
            var cekbaba = connectedUsers.Values.FirstOrDefault(f => f.Phone == from);
            if (cekbaba != null)
            {
                var newCallEvent = new CallEventModal
                {
                    From = from,
                    To = to,
                    Fullname = candidatename
                };
                await Clients.Client(cekbaba.CallerId).SendAsync("CallNumber", newCallEvent);
            }
        }
        public async Task CheckCaller(string phoneNumber)
        {
            var cekbaba = connectedUsers.Values.FirstOrDefault(f => f.CallerId == Context.ConnectionId);
            string url = cekbaba.Host + $"/PhoneCallApp/CheckCaller?phone={phoneNumber}";

            var content = new StringContent(JsonSerializer.Serialize(phoneNumber), System.Text.Encoding.UTF8, "application/json");
            try
            {
                var response = await http.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var jsonConverted= JsonSerializer.Deserialize<AnswerEventModel>(json);
                    await Clients.Client(cekbaba.CallerId).SendAsync("WhoIsCalling", jsonConverted);
                }
              
            }
            catch (Exception ex)
            {
                Console.WriteLine($"POST hatası {url}: {ex.Message}");
                await Clients.Client(Context.ConnectionId).SendAsync("WhoIsCalling", "Hata oluştu");
            }
        }

        public async Task ICalled(CallEventModal data)
        {
            var cekbaba = connectedUsers.Values.FirstOrDefault(f => f.Phone == data.From);
            var url = cekbaba.Host+$"/PhoneCallApp/ICalled";

            var content = new StringContent(JsonSerializer.Serialize(data), System.Text.Encoding.UTF8, "application/json");
            try
            {
                await http.PostAsync(url, content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"POST hatası {url}: {ex.Message}");
            }
        }
        public async Task IAnswerd(CallEventModal data)
        {
            var cekbaba = connectedUsers.Values.FirstOrDefault(f => f.CallerId==Context.ConnectionId);
            var url = cekbaba.Host+$"/PhoneCallApp/ICalled";
            if (!data.To.Contains("("))
            {
                var phoner = data.To.Substring(2);
                phoner=$"0({phoner[1]}{phoner[2]}{phoner[3]}) {phoner[4]}{phoner[5]}{phoner[6]}-{phoner[7]}{phoner[8]}{phoner[9]}{phoner[10]}";
                data.To=phoner;
            }
            data.From=cekbaba.Phone;
          

            var content = new StringContent(JsonSerializer.Serialize(data), System.Text.Encoding.UTF8, "application/json");
            try
            {
                await http.PostAsync(url, content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"POST hatası {url}: {ex.Message}");
            }
        }

        

        public async Task ICallEnded(CallEventModal data)
        {
            var cekbaba=new UserModel();
            if (data.From.Contains("00000"))
            {
                 cekbaba = connectedUsers.Values.FirstOrDefault(f => f.CallerId == Context.ConnectionId);
                data.From=cekbaba.Phone;
            }
            else
            {
                cekbaba = connectedUsers.Values.FirstOrDefault(f => f.Phone == data.From);
            }

            var url = cekbaba.Host+$"/PhoneCallApp/ICantCall";
            var content = new StringContent(JsonSerializer.Serialize(data), System.Text.Encoding.UTF8, "application/json");
            try
            {
                await http.PostAsync(url, content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"POST hatası {url}: {ex.Message}");
            }
        }
    }
}
