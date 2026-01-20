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

using KeeKee.Framework;
using KeeKee.Framework.Logging;
using KeeKee.Framework.Utilities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Rest {

    public class RestHandlerUI : IRestHandler {

        private readonly KLogger<RestHandlerUI> m_log;
        private readonly IOptions<RestManagerConfig> m_restConfig;
        private readonly RestManager m_RestManager;

        /// <summary>
        /// API URL to filesystem base directory mapping
        /// "/api/static/"  -->  "/.../bin/KeeKeeUI/Default/"
        /// </summary>
        // Filesystem base directory for UI content. Comes from config "UIContentDir".
        private readonly string BaseUIDir;
        // The baseUIDir + "UI/" + skin name + "/"
        // public readonly string staticDir = "KeeKeeUI/Default/";
        private readonly string UIDir = "";
        // The filesystem directory is added to to "skin" the UI. Comes from config "Skin".
        // If not specified, "Default" is used.
        private const string DefaultSkinName = "Default";

        // The prefix of the requested URL that is processed by this handler.
        public string Prefix { get; set; } = "/UI/";

        public RestHandlerUI(KLogger<RestHandlerUI> pLogger,
                                IOptions<RestManagerConfig> pRestConfig,
                                RestManager pRestManager
                                ) {
            m_log = pLogger;
            m_restConfig = pRestConfig;
            m_RestManager = pRestManager;

            // m_baseUIDIR = "/.../bin/KeeKeeUI/"
            BaseUIDir = m_restConfig.Value.UIContentDir;

            // things referenced as static are from the skinning directory below the UI dir
            // m_staticDir = "/.../bin/KeeKeeUI/Default/"
            UIDir = Utilities.JoinFilePieces(BaseUIDir, "UI");
            string? skinName = m_restConfig.Value.Skin;
            if (skinName != null && skinName.Length > 0) {
                skinName = skinName.Replace("/", "");  // skin names shouldn't fool with directories
                skinName = skinName.Replace("\\", "");
                skinName = skinName.Replace("..", "");
                UIDir = Utilities.JoinFilePieces(UIDir, skinName);
            }
            if (!UIDir.EndsWith("/")) UIDir += "/";

            m_log.Log(KLogLevel.RestDetail, "baseUIDir={0}, UIDir={1}, Prefix={2}",
                     BaseUIDir, UIDir, Prefix);

            m_RestManager.RegisterListener(this);
        }

        public async Task ProcessGetOrPostRequest(HttpListenerContext pContext,
                                           HttpListenerRequest pRequest,
                                           HttpListenerResponse pResponse,
                                           CancellationToken pCancelToken) {

            if (pRequest?.HttpMethod.ToUpper().Equals("GET") ?? false) {

                string absURL = pRequest.Url?.AbsolutePath ?? "";
                string afterString = absURL.Substring(Prefix.Length);
                // prevent directory traversal attacks
                afterString = afterString.Replace("\\", "");
                afterString = afterString.Replace("..", "");

                string filePath = Utilities.JoinFilePieces(UIDir, afterString);

                try {
                    if (File.Exists(filePath)) {
                        m_log.Log(KLogLevel.RestDetail, "Serving file {0}", filePath);
                        m_RestManager.DoSimpleResponse(pResponse, Utilities.GetMimeTypeFromFileName(filePath), () => {
                            return File.ReadAllBytes(filePath);
                        });
                    } else {
                        m_log.Log(KLogLevel.RestDetail, "File not found {0}", filePath);
                        m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.NotFound, null);
                    }
                } catch (Exception e) {
                    m_log.Log(KLogLevel.Error, "Exception {0} serving file {1}", e.Message, filePath);
                    m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.InternalServerError, null);
                }
            }
            if (pRequest?.HttpMethod.ToUpper().Equals("POST") ?? false) {
                m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.NotImplemented, null);
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