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
using KeeKee.Framework;

namespace KeeKee.Rest {

    public abstract class RestHandler : IDisposable {

        protected readonly RestManager m_RestManager;

        public RestHandler(RestManager pRestManager, string pPrefix) {
            m_RestManager = pRestManager;
            this.Prefix = pPrefix;

            m_RestManager.RegisterListener(this);
        }

        public const string NOPREFIX = "XX/no-prefix/no-prefix/XX";
        // The prefix string for this handler. The stuff after the 'api/' in the URL
        // Usually constructed with:
        //    Prefix = Utilities.JoinFilePieces(m_RestManager.APIBase, "/subpath/");
        public string Prefix { get; protected set; } = NOPREFIX;

        // Process a GET request. The default is to return an error response.
        public virtual Task ProcessGetRequest(HttpListenerContext pContext,
                                    HttpListenerRequest pRequest,
                                    HttpListenerResponse pResponse,
                                    CancellationToken pCancelToken) {
            m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.BadRequest, null);
            return Task.CompletedTask;
        }

        // Process a POST request. The default is to return an error response.
        public virtual Task ProcessPostRequest(HttpListenerContext pContext,
                                    HttpListenerRequest pRequest,
                                    HttpListenerResponse pResponse,
                                    CancellationToken pCancelToken) {
            m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.BadRequest, null);
            return Task.CompletedTask;
        }

        // Process any other request. The default is to return an error response.
        public virtual Task ProcesstOtherRequest(HttpListenerContext pContext,
                                    HttpListenerRequest pRequest,
                                    HttpListenerResponse pResponse,
                                    CancellationToken pCancelToken) {
            m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.BadRequest, null);
            return Task.CompletedTask;
        }

        public virtual void Dispose() {
        }

    }
}