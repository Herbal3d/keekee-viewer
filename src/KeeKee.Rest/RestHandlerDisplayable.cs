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

using Microsoft.Extensions.Options;

using KeeKee.Framework;
using KeeKee.Framework.Logging;
using KeeKee.Framework.Utilities;

using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace KeeKee.Rest {

    /// <summary>
    /// REST handler that serves displayable data from an IDisplayable source.
    /// The prefix must be set before use so invocation is to create with
    /// the factory and then set the prefix and the IDisplayable source.
    /// </summary>
    public class RestHandlerDisplayable : IRestHandler {

        private readonly KLogger<RestHandlerDisplayable> m_log;
        private readonly IOptions<RestManagerConfig> m_restConfig;
        private readonly RestManager m_RestManager;

        public const string NOPREFIX = "/no-prefix/no-prefix/";
        private string m_prefix = NOPREFIX;
        public string Prefix {
            get { return m_prefix; }
            set { SetPrefix(value); }
        }

        public IDisplayable? DisplayableSource { get; set; } = null;

        public RestHandlerDisplayable(KLogger<RestHandlerDisplayable> pLogger,
                                IOptions<RestManagerConfig> pRestConfig,
                                RestManager pRestManager
                                ) {
            m_log = pLogger;
            m_restConfig = pRestConfig;
            m_RestManager = pRestManager;

            // LIstener is registered when prefix is set
            // m_RestManager.RegisterListener(this);
        }

        /// <summary>
        /// Set the prefix for this handler. Can only be done once.
        /// This allows construction before knowing the prefix.
        /// </summary>
        /// <param name="pPrefix"></param>
        public void SetPrefix(string pPrefix) {
            if (m_prefix == NOPREFIX) {
                m_log.Log(KLogLevel.RestDetail, "Setting Prefix to {0}", pPrefix);
                m_prefix = pPrefix;
                m_RestManager.RegisterListener(this);
            }
            m_prefix = pPrefix;
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

                try {
                    if (DisplayableSource != null) {
                        OMVSD.OSDMap? displayMap = DisplayableSource.GetDisplayable();
                        if (displayMap != null) {
                            m_RestManager.DoSimpleResponse(pResponse, "application/json", () => {
                                return Utilities.StringToBytes(OMVSD.OSDParser.SerializeJsonString(displayMap));
                            });
                        } else {
                            m_log.Log(KLogLevel.RestDetail, "No displayable data from source");
                            m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.NoContent, null);
                        }
                    } else {
                        m_log.Log(KLogLevel.RestDetail, "No displayable source set");
                        m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.NoContent, null);
                    }
                } catch (Exception e) {
                    m_log.Log(KLogLevel.Error, "Exception {0} getting displayable data", e.Message);
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

