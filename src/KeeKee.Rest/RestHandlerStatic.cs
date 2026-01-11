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

    public class RestHandlerStatic : IRestHandler {

        private readonly KLogger<RestHandlerStatic> m_log;
        private readonly IOptions<RestManagerConfig> m_restConfig;
        private readonly RestManager _RestManager;

        /// <summary>
        /// API URL to filesystem base directory mapping
        /// "/api/static/"  -->  "/.../bin/KeeKeeUI/Default/"
        /// </summary>
        // Filesystem base directory for UI content. Comes from config "UIContentDir".
        public readonly string BaseUIDir = "KeeKeeUI/";
        // The baseUIDir + skin name + "/"
        // public readonly string staticDir = "KeeKeeUI/Default/";
        public readonly string StaticDir = "";
        // Portion added to API URL to indicate static content
        public const string BaseUrl = "static/";
        // The filesystem directory is added to to "skin" the UI. Comes from config "Skin".
        // If not specified, "Default" is used.
        public const string DefaultSkinName = "Default";

        // The prefix of the requested URL that is processed by this handler.
        public string Prefix { get; set; }

        public RestHandlerStatic(KLogger<RestHandlerStatic> pLogger,
                                IOptions<RestManagerConfig> pOptions,
                                RestManager pRestManager
                                ) {
            m_log = pLogger;
            m_restConfig = pOptions;
            _RestManager = pRestManager;

            // m_baseUIDIR = "/.../bin/KeeKeeUI/"
            BaseUIDir = m_restConfig.Value.UIContentDir;
            if (!BaseUIDir.EndsWith("/")) BaseUIDir += "/";

            // things referenced as static are from the skinning directory below the UI dir
            // m_staticDir = "/.../bin/KeeKeeUI/Default/"
            StaticDir = BaseUIDir;
            if (m_restConfig.Value.Skin != null && m_restConfig.Value.Skin.Length > 0) {
                string skinName = m_restConfig.Value.Skin;
                skinName = skinName.Replace("/", "");  // skin names shouldn't fool with directories
                skinName = skinName.Replace("\\", "");
                skinName = skinName.Replace("..", "");
                StaticDir = StaticDir + skinName;
            }
            if (!StaticDir.EndsWith("/")) StaticDir += "/";

            Prefix = Utilities.JoinFilePieces(m_restConfig.Value.APIBase, "static/");

            m_log.Log(KLogLevel.RestDetail, "RestHandlerStatic: baseUIDir={0}, staticDir={1}, Prefix={2}",
                     BaseUIDir, StaticDir, Prefix);

            _RestManager.RegisterListener(this);
        }

        public async Task ProcessGetOrPostRequest(HttpListenerContext pContext,
                                           HttpListenerRequest pRequest,
                                           HttpListenerResponse pResponse,
                                           CancellationToken pCancelToken) {

            if (pRequest?.HttpMethod.ToUpper().Equals("GET") ?? false) {

                string absURL = pRequest.Url?.AbsolutePath.ToLower() ?? "";
                string afterString = absURL.Substring(Prefix.Length);

                string filePath = Utilities.JoinFilePieces(StaticDir, afterString);

                try {
                    if (File.Exists(filePath)) {
                        m_log.Log(KLogLevel.RestDetail, "RestHandlerStatic: Serving file {0}", filePath);
                        _RestManager.DoSimpleResponse(pResponse, Utilities.GetMimeTypeFromFileName(filePath), () => {
                            return File.ReadAllBytes(filePath);
                        }
                        );
                    } else {
                        m_log.Log(KLogLevel.RestDetail, "RestHandlerStatic: File not found {0}", filePath);
                        _RestManager.DoErrorResponse(pResponse, HttpStatusCode.NotFound, null);
                    }
                } catch (Exception e) {
                    m_log.Log(KLogLevel.Error, "RestHandlerStatic: Exception {0} serving file {1}", e.Message, filePath);
                    _RestManager.DoErrorResponse(pResponse, HttpStatusCode.InternalServerError, null);
                }
            }
        }
        public void Dispose() {
            // _RestManager.UnregisterListener(this);
        }

        // Optional displayable interface to get parameters from. Not used here.
        public OMVSD.OSDMap? GetDisplayable() {
            return null;
        }
    }

}