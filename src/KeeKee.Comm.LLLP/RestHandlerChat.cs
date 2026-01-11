// Copyright 2025 Robert Adams
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Net;
using System.Text;

using KeeKee.Comm;
using KeeKee.Config;
using KeeKee.Framework;
using KeeKee.Framework.Logging;
using KeeKee.Framework.Utilities;
using Microsoft.Extensions.Options;

using OpenMetaverse;
using OpenMetaverse.StructuredData;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Rest {

    public class RestHandlerChat : IRestHandler {

        private readonly KLogger<RestHandlerChat> m_log;
        private readonly RestManager m_RestManager;
        private readonly ICommProvider m_commProvider;
        private readonly IOptions<CommConfig> m_commConfig;

        public enum ChatEntryType {
            Normal = 0,
            StatusBlue, StatusDarkBlue, LindenChat, ObjectChat,
            StartupTitle, Error, Alert, OwnerSay, Invisible
        };
        string[] ChatEntryTypeString = {
            "ChatTypeNormal",
            "ChatTypeStatusBlue", "ChatTypeStatusDarkBlue", "ChatTypeLindenChat", "ChatTypeObjectChat",
            "ChatTypeStartupTitle", "ChatTypeError", "ChatTypeAlert", "ChatTypeOwnerSay", "ChatTypeInvisible"
        };
        protected class ChatEntry {
            public DateTime time;
            public ChatEntryType chatEntryType;
            public string fromName;
            public string message;
            public OMV.Vector3 position;
            public OMV.ChatSourceType sourceType;
            public OMV.ChatType chatType;
            public string chatTypeString;
            public OMV.UUID ownerID;
            public ChatEntry() {
                time = DateTime.Now;
            }
        }
        protected Queue<ChatEntry> m_chats;

        /// <summary>
        /// </summary>

        // The prefix of the requested URL that is processed by this handler.
        public string Prefix { get; set; }

        public RestHandlerChat(KLogger<RestHandlerChat> pLogger,
                                IOptions<CommConfig> pCommConfig,
                                RestManager pRestManager,
                                ICommProvider pCommProvider
                                ) {
            m_log = pLogger;
            m_RestManager = pRestManager;
            m_commProvider = pCommProvider;
            m_commConfig = pCommConfig;

            Prefix = Utilities.JoinFilePieces(m_RestManager.APIBase, "LLLP/chat");

            m_RestManager.RegisterListener(this);

            m_chats = new Queue<ChatEntry>();

            m_commProvider.GridClient.Self.ChatFromSimulator += new EventHandler<OpenMetaverse.ChatEventArgs>(Self_ChatFromSimulator);
        }

        void Self_ChatFromSimulator(object sender, OpenMetaverse.ChatEventArgs e) {
            m_log.Log(KLogLevel.DCOMMDETAIL, "Self_ChatFromSimulator: {0} says '{1}'", e.FromName, e.Message);
            if (e.Message.Length == 0) {
                // zero length messages are typing start and end
                return;
            }
            ChatEntry ce = new ChatEntry();
            ce.fromName = e.FromName;
            ce.message = e.Message;
            ce.position = e.Position;
            ce.sourceType = e.SourceType;
            ce.chatType = e.Type;
            switch (e.Type) {
                case OMV.ChatType.Normal: ce.chatTypeString = "Normal"; break;
                case OMV.ChatType.Shout: ce.chatTypeString = "Shout"; break;
                case OMV.ChatType.Whisper: ce.chatTypeString = "Whisper"; break;
                case OMV.ChatType.OwnerSay: ce.chatTypeString = "OwnerSay"; break;
                case OMV.ChatType.RegionSay: ce.chatTypeString = "RegionSay"; break;
                case OMV.ChatType.Debug: ce.chatTypeString = "Debug"; break;
                case OMV.ChatType.StartTyping: ce.chatTypeString = "StartTyping"; break;
                case OMV.ChatType.StopTyping: ce.chatTypeString = "StopTyping"; break;
                default: ce.chatTypeString = "Normal"; break;
            }
            ce.ownerID = e.OwnerID;
            ce.chatEntryType = ChatEntryType.Normal;
            if (e.SourceType == OMV.ChatSourceType.Agent && e.FromName.EndsWith("Linden")) {
                ce.chatEntryType = ChatEntryType.LindenChat;
            }
            if (e.SourceType == OMV.ChatSourceType.Object) {
                if (e.Type == OMV.ChatType.OwnerSay) {
                    ce.chatEntryType = ChatEntryType.OwnerSay;
                } else {
                    ce.chatEntryType = ChatEntryType.ObjectChat;
                }
            }
            lock (m_chats) m_chats.Enqueue(ce);
        }

        public async Task ProcessGetOrPostRequest(HttpListenerContext pContext,
                                           HttpListenerRequest pRequest,
                                           HttpListenerResponse pResponse,
                                           CancellationToken pCancelToken) {

            if (pRequest?.HttpMethod.ToUpper().Equals("GET") ?? false) {
                m_log.Log(KLogLevel.RestDetail, "GET: " + (pRequest?.Url?.ToString() ?? "UNKNOWN"));

                OMVSD.OSDMap ret = new OMVSD.OSDMap();
                string lastDate = "xx";
                lock (m_chats) {
                    while (m_chats.Count > 0) {
                        ChatEntry ce = m_chats.Dequeue();
                        string dateString = ce.time.ToString("yyyyMMddhhmmssfff");
                        OMVSD.OSDMap chat = new OMVSD.OSDMap();
                        chat.Add("Time", new OMVSD.OSDString(dateString));
                        chat.Add("From", new OMVSD.OSDString(ce.fromName));
                        chat.Add("Message", new OMVSD.OSDString(ce.message));
                        chat.Add("Type", new OMVSD.OSDString(ce.chatTypeString));
                        chat.Add("EntryType", new OMVSD.OSDString(ChatEntryTypeString[(int)ce.chatEntryType]));
                        chat.Add("Position", new OMVSD.OSDString(ce.position.ToString()));
                        if (ce.ownerID != UUID.Zero) {
                            chat.Add("OwnerID", new OMVSD.OSDString(ce.ownerID.ToString()));
                        }
                        while (ret.ContainsKey(dateString)) {
                            dateString += "1";
                        }
                        ret.Add(dateString, chat);
                        lastDate = dateString;
                    }
                }
                string asJson = OSDParser.SerializeJsonString(ret);
                m_RestManager.DoSimpleResponse(pResponse, "application/json", () => Encoding.UTF8.GetBytes(asJson));

                return;
            }
            if (pRequest?.HttpMethod.ToUpper().Equals("POST") ?? false) {
                m_log.Log(KLogLevel.RestDetail, "POST: " + (pRequest?.Url?.ToString() ?? "UNKNOWN"));

                string strBody = "";
                using (StreamReader rdr = new StreamReader(pRequest.InputStream)) {
                    strBody = rdr.ReadToEnd();
                    // m_log.Log(KLogLevel.RestDetail, "APIPostHandler: Body: '" + strBody + "'");
                }

                try {
                    OMVSD.OSDMap mapBody = m_RestManager.MapizeTheBody(strBody);
                    m_log.Log(KLogLevel.DCOMMDETAIL, "PostHandler: received chat '{0}'", mapBody["Message"]);
                    // collect parameters and send it to the simulator
                    string msg = Uri.UnescapeDataString(mapBody["Message"].AsString().Replace("+", " "));
                    OMVSD.OSD channelString = new OMVSD.OSDString("0");
                    mapBody.TryGetValue("Channel", out channelString);
                    int channel = Int32.Parse(channelString.AsString());
                    OMVSD.OSD typeString = new OMVSD.OSDString("Normal");
                    mapBody.TryGetValue("Type", out typeString);
                    OMV.ChatType chatType = OpenMetaverse.ChatType.Normal;
                    if (typeString.AsString().Equals("Whisper")) chatType = OMV.ChatType.Whisper;
                    if (typeString.AsString().Equals("Shout")) chatType = OMV.ChatType.Shout;
                    m_commProvider.GridClient.Self.Chat(msg, channel, chatType);

                    m_RestManager.DoSimpleResponse(pResponse, "application/json", null);

                    // echo my own message back for the log and chat window
                    /* NOTE: Don't have to do this. The simulator echos it back
                    OMV.ChatEventArgs cea = new OpenMetaverse.ChatEventArgs(m_comm.GridClient.Network.CurrentSim, 
                                    msg, 
                                    OpenMetaverse.ChatAudibleLevel.Fully,
                                    chatType, 
                                    OpenMetaverse.ChatSourceType.Agent, 
                                    m_comm.GridClient.Self.Name, 
                                    OMV.UUID.Zero, 
                                    OMV.UUID.Zero, 
                                    m_comm.GridClient.Self.RelativePosition);
                    this.Self_ChatFromSimulator(this, cea);
                    */
                } catch (Exception e) {
                    m_log.Log(KLogLevel.DCOMM, "ERROR PARSING CHAT MESSAGE: {0}", e);
                }
            }
        }

        public void Dispose() {
            // m_RestManager.UnregisterListener(this);
        }

        // Optional displayable interface to get parameters from. Not used here.
        public OMVSD.OSDMap? GetDisplayable() {
            return null;
        }
    }
}

