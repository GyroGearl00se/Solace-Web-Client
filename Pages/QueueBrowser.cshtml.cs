using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Messaging.SDT;

namespace Solace_Web_Client.Pages
{
    public class QueueBrowserModel : PageModel
    {
        private readonly ILogger<QueueBrowserModel> _logger;
        private ContextFactoryProperties _contextFactoryProperties;
        private IContext _context;
        private SolaceSystems.Solclient.Messaging.ISession _session;
        private IQueue _queueEndpoint;
        private SessionProperties _sessionProperties;
        public QueueBrowserModel(ILogger<QueueBrowserModel> logger)
        {
            _logger = logger;
            Output = new OutputModel();
            Output.OutputData = new List<MessageInfo>();
            _contextFactoryProperties = new ContextFactoryProperties();
            _contextFactoryProperties.SolClientLogLevel = SolLogLevel.Warning;
            ContextFactory.Instance.Init(_contextFactoryProperties);

            _sessionProperties = new SessionProperties();
            _sessionProperties.SSLValidateCertificate = true;
            _sessionProperties.SSLExcludedProtocols = "TLSv1,TLSv1.1,SSLv3";
            _sessionProperties.SSLTrustStoreDir = "trustedca";
        }

        public class MessageInfo
        {
            public string? DestinationName { get; set; }
            public string? ApplicationMessageId { get; set; }
            public string? SenderId { get; set; }
            public string? MessageContent { get; set; }
            public string? MessageContentXML { get; set; }
            public string? ApplicationMessageType { get; set; }
            public string? CorrelationId { get; set; }
            public long ADMessageId { get; set; }
            public string? FormattedDateTime { get; set; }
            public Dictionary<string, object>? UserProperties { get; set; }
            public string DeliveryMode { get; set; }
        }

        public class OutputModel
        {
            public List<MessageInfo>? OutputData { get; set; }
        }

        public OutputModel Output { get; set; }

        public ReturnCode returnCode { get; set; }

        public void OnPost()
        {
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                _sessionProperties.Host = Request.Form["host"];
                _sessionProperties.VPNName = Request.Form["vpn"];
                _sessionProperties.UserName = Request.Form["username"];
                _sessionProperties.Password = Request.Form["password"];

                _context = ContextFactory.Instance.CreateContext(new ContextProperties(), null);
                _session = _context.CreateSession(_sessionProperties, null, null);

                returnCode = _session.Connect();
                if (returnCode == ReturnCode.SOLCLIENT_OK)
                {
                    _logger.LogInformation("Session connected");

                    BrowserProperties browserProps = new BrowserProperties();
                    browserProps.TransportWindowSize = 5;

                    _queueEndpoint = ContextFactory.Instance.CreateQueue(Request.Form["queue"]);

                    using (IBrowser browser = _session.CreateBrowser(_queueEndpoint, browserProps))
                    {
                        IMessage message;
                        int messageCount = 0;
                        do
                        {
                            message = browser.GetNext();
                            if (message != null)
                            {
                                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(message.SenderTimestamp);
                                string formattedDateTime = dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss");

                                Dictionary<string, object> keyValuePairs = new Dictionary<string, object>();

                                var userPropertyMap = message.UserPropertyMap;
                                if (userPropertyMap != null)
                                {
                                    while (true)
                                    {
                                        var nextKeyValuePair = userPropertyMap.GetNext();
                                        if (nextKeyValuePair.Key == null)
                                        {
                                            break;
                                        }
                                        var key = nextKeyValuePair.Key;
                                        var valueObject = nextKeyValuePair.Value;
                                        var value = ((ISDTField)valueObject).Value;
                                        keyValuePairs.Add(key, value);
                                    }
                                }
                                Output.OutputData.Add(new MessageInfo
                                {
                                    DestinationName = message.Destination.Name != null ? message.Destination.Name : "N/A",
                                    ApplicationMessageType = message.ApplicationMessageType != null ? message.ApplicationMessageType : "N/A",
                                    ApplicationMessageId = message.ApplicationMessageId != null ? message.ApplicationMessageId : "N/A",
                                    SenderId = message.SenderId != null ? message.SenderId : "N/A",
                                    MessageContent = message.BinaryAttachment != null ? System.Text.Encoding.ASCII.GetString(message.BinaryAttachment) : "N/A",
                                    MessageContentXML = message.XmlContent != null ? System.Text.Encoding.ASCII.GetString(message.XmlContent) : "N/A",
                                    CorrelationId = message.CorrelationId != null ? message.CorrelationId : "N/A",
                                    ADMessageId = message.ADMessageId != 0 ? message.ADMessageId : 0,
                                    FormattedDateTime = formattedDateTime,
                                    UserProperties = keyValuePairs,
                                    DeliveryMode = message.DeliveryMode.ToString()
                                });
                                //_logger.LogInformation("message.Dump: {$1}", message.Dump());
                                messageCount++;
                            }
                        } while (message != null);
                    }
                }
                else
                {
                    _logger.LogError("Failed to connect to session");
                }
                _session.Disconnect();

                TempData["Host"] = Request.Form["host"];
                TempData["VPN"] = Request.Form["vpn"];
                TempData["Queue"] = Request.Form["queue"];
                TempData["Username"] = Request.Form["username"];
                TempData["Password"] = Request.Form["password"];
            }
            catch (OperationErrorException ex)
            {
                _logger.LogError($"Failed to connect to session: {ex.Message}");
                _logger.LogError($"Failed to connect to session: {ex.ErrorInfo}");

                TempData["ErrorMessage"] = "Failed to connect to session. Please check your connection details and try again.";
                TempData["ErrorInfo"] = ex.ErrorInfo;

                TempData["Host"] = Request.Form["host"];
                TempData["VPN"] = Request.Form["vpn"];
                TempData["Queue"] = Request.Form["queue"];
                TempData["Username"] = Request.Form["username"];
                TempData["Password"] = Request.Form["password"];
            }
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public IActionResult OnDeleteRemoveMessage(long messageId, string host, string vpn, string username, string password, string queue)
        {
            try
            {
                ContextProperties contextProperties = new ContextProperties();
                _context = ContextFactory.Instance.CreateContext(contextProperties, null);

                _sessionProperties.Host = host;
                _sessionProperties.VPNName = vpn;
                _sessionProperties.UserName = username;
                _sessionProperties.Password = password;

                _session = _context.CreateSession(_sessionProperties, null, null);
                // _logger.LogInformation("Session properties: Host={tempHost}, VPNName={tempVPN}, UserName={tempUsername}, Password={tempPassword}, Queue={queue}", tempHost, tempVPN, tempUsername, tempPassword, queue);

                returnCode = _session.Connect();
                if (returnCode != ReturnCode.SOLCLIENT_OK)
                {
                    _logger.LogError("Failed to connect to session");
                    return new JsonResult(false);
                }

                _logger.LogInformation("Session connected");

                BrowserProperties browserProps = new BrowserProperties();
                browserProps.TransportWindowSize = 50;
                EndpointProperties endpointProps = new EndpointProperties()
                {
                    Permission = EndpointProperties.EndpointPermission.Consume,
                    AccessType = EndpointProperties.EndpointAccessType.Exclusive
                };

                IQueue queueEndpoint = ContextFactory.Instance.CreateQueue(queue);

                using (IBrowser browser = _session.CreateBrowser(queueEndpoint, browserProps))
                {
                    browser.Remove(messageId);
                }

                TempData["SuccessMessage"] = $"Message with ID {messageId.ToString()} deleted successfully.";
                TempData["DeletedMessageId"] = messageId.ToString();
                _session.Disconnect();
                return new JsonResult("Message deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to delete message: {ex.Message}");
                _logger.LogError($"StackTrace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Failed to delete message. Please try again.";

                return new JsonResult("Failed to delete message");
            }
        }
    }
}
