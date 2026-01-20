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

using KeeKee.Framework.Logging;
using KeeKee.Framework.Utilities;

using Microsoft.Extensions.Options;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Rest {

    public class RestHandlerStatic : IRestHandler {

        private readonly KLogger<RestHandlerStatic> m_log;
        private readonly IOptions<RestManagerConfig> m_restConfig;
        private readonly RestManager m_RestManager;

        /// <summary>
        /// API URL to filesystem base directory mapping
        /// "/api/std/"  -->  "/.../bin/KeeKeeUI/std/"
        /// </summary>
        // Filesystem base directory for UI content. Comes from config "UIContentDir".
        private readonly string BaseUIDir = "/";
        // Filesystem base directory for standard content
        // The baseUIDir + BaseUrl + "/"
        private string StaticDir = "";

        // The prefix of the requested URL that is processed by this handler.
        public string Prefix { get; set; } = "/static/";

        public RestHandlerStatic(KLogger<RestHandlerStatic> pLogger,
                                IOptions<RestManagerConfig> pRestConfig,
                                RestManager pRestManager
                                ) {
            m_log = pLogger;
            m_restConfig = pRestConfig;
            m_RestManager = pRestManager;

            BaseUIDir = m_restConfig.Value.UIContentDir;
            if (!BaseUIDir.EndsWith("/")) BaseUIDir += "/";

            StaticDir = Utilities.JoinFilePieces(BaseUIDir, Prefix);
            if (!StaticDir.EndsWith("/")) StaticDir += "/";

            m_log.Log(KLogLevel.RestDetail, "baseUIDir={0}, staticDir={1}, Prefix={2}",
                     BaseUIDir, StaticDir, Prefix);

            m_RestManager.RegisterListener(this);
        }

        public async Task ProcessGetOrPostRequest(HttpListenerContext pContext,
                                           HttpListenerRequest pRequest,
                                           HttpListenerResponse pResponse,
                                           CancellationToken pCancelToken) {

            if (pRequest?.HttpMethod.ToUpper().Equals("GET") ?? false) {
                string absURL = pRequest.Url?.AbsolutePath ?? "";
                string afterString = absURL.Substring(Prefix.Length);

                // remove any query string
                int qPos = afterString.IndexOf("?");
                if (qPos >= 0) {
                    afterString = afterString.Substring(0, qPos);
                }

                string filePath = Utilities.JoinFilePieces(StaticDir, afterString);

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
